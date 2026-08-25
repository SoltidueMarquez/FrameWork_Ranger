using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.ResourceManagement.Tests
{
    internal sealed class ResourceStoreTests
    {
        private readonly List<Object> m_assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < m_assets.Count; i++)
            {
                Object.DestroyImmediate(m_assets[i]);
            }

            m_assets.Clear();
        }

        [UnityTest]
        public IEnumerator ConcurrentAcquire_UsesOneLoadAndIndependentLeases()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var asset = CreateAsset("shared");
                var provider = new ResourceFakeProvider(ResourceBackendKind.Addressables)
                {
                    LoadGate = new UniTaskCompletionSource<Object>(),
                };
                var store = CreateStore(provider);
                var key = ResourceKey.FromAddressables("shared");

                var firstTask = store.AcquireAsync<TextAsset>(key, default);
                var secondTask = store.AcquireAsync<TextAsset>(key, default);
                Assert.That(provider.LoadCount, Is.EqualTo(1));

                provider.LoadGate.TrySetResult(asset);
                var first = await firstTask;
                var second = await secondTask;
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.Value, Is.SameAs(asset));
                Assert.That(store.CreateDiagnosticsSnapshot().LeaseCount, Is.EqualTo(2));

                first.Dispose();
                Assert.That(provider.ReleaseCount, Is.Zero);
                second.Dispose();
                Assert.That(provider.ReleaseCount, Is.EqualTo(1));
                Assert.That(store.CreateDiagnosticsSnapshot().CacheCount, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator SameLocationWithDifferentBackends_NeverFallsBackOrSharesCache()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var resourcesAsset = CreateAsset("resources");
                var addressablesAsset = CreateAsset("addressables");
                var resources = new ResourceFakeProvider(ResourceBackendKind.UnityResources)
                {
                    Asset = resourcesAsset,
                };
                var addressables = new ResourceFakeProvider(ResourceBackendKind.Addressables)
                {
                    Asset = addressablesAsset,
                };
                var store = CreateStore(resources, addressables);

                var resourcesLease = await store.AcquireAsync<TextAsset>(
                    ResourceKey.FromResources("same"),
                    default);
                var addressablesLease = await store.AcquireAsync<TextAsset>(
                    ResourceKey.FromAddressables("same"),
                    default);

                Assert.That(resourcesLease.Value, Is.SameAs(resourcesAsset));
                Assert.That(addressablesLease.Value, Is.SameAs(addressablesAsset));
                Assert.That(resources.LoadCount, Is.EqualTo(1));
                Assert.That(addressables.LoadCount, Is.EqualTo(1));
                resourcesLease.Dispose();
                addressablesLease.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator FailureIsNotCached_AndNextAcquireRetries()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var provider = new ResourceFakeProvider(ResourceBackendKind.Addressables)
                {
                    Asset = CreateAsset("retry"),
                    FailLoads = true,
                };
                var store = CreateStore(provider);
                var key = ResourceKey.FromAddressables("retry");

                await ExpectExceptionAsync<ResourceLoadException>(
                    () => store.AcquireAsync<TextAsset>(key, default).AsTask());
                provider.FailLoads = false;
                var lease = await store.AcquireAsync<TextAsset>(key, default);

                Assert.That(provider.LoadCount, Is.EqualTo(2));
                lease.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator CancellingOneWaiter_DoesNotCancelOtherWaiter()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var provider = new ResourceFakeProvider(ResourceBackendKind.Addressables)
                {
                    LoadGate = new UniTaskCompletionSource<Object>(),
                };
                var store = CreateStore(provider);
                var cancellation = new CancellationTokenSource();
                var key = ResourceKey.FromAddressables("cancel-one");
                var cancelledTask = store.AcquireAsync<TextAsset>(key, cancellation.Token);
                var survivingTask = store.AcquireAsync<TextAsset>(key, default);

                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(() => cancelledTask.AsTask());
                provider.LoadGate.TrySetResult(CreateAsset("survivor"));
                var lease = await survivingTask;

                Assert.That(provider.LoadCount, Is.EqualTo(1));
                Assert.That(lease.IsValid, Is.True);
                lease.Dispose();
                cancellation.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator CancellingAllWaiters_RejectsLateResultAndReleasesHandle()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var provider = new ResourceFakeProvider(ResourceBackendKind.Addressables)
                {
                    LoadGate = new UniTaskCompletionSource<Object>(),
                    IgnoreCancellation = true,
                };
                var store = CreateStore(provider);
                var cancellation = new CancellationTokenSource();
                var task = store.AcquireAsync<TextAsset>(
                    ResourceKey.FromAddressables("cancel-all"),
                    cancellation.Token);

                cancellation.Cancel();
                await ExpectExceptionAsync<OperationCanceledException>(() => task.AsTask());
                provider.LoadGate.TrySetResult(CreateAsset("late"));
                await UniTask.Yield();

                Assert.That(store.CreateDiagnosticsSnapshot().CacheCount, Is.Zero);
                Assert.That(provider.ReleaseCount, Is.EqualTo(1));
                cancellation.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator Shutdown_InvalidatesLeasesAndDisposesEveryBackendHandle()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var provider = new ResourceFakeProvider(ResourceBackendKind.UnityResources)
                {
                    Asset = CreateAsset("shutdown"),
                };
                var store = CreateStore(provider);
                var lease = await store.AcquireAsync<TextAsset>(
                    ResourceKey.FromResources("shutdown"),
                    default);

                await store.ShutdownAsync();

                Assert.That(lease.IsValid, Is.False);
                Assert.Throws<ObjectDisposedException>(() => _ = lease.Value);
                Assert.That(provider.ReleaseCount, Is.EqualTo(1));
                lease.Dispose();
            });
        }

        [UnityTest]
        public IEnumerator ExactRequestedType_UsesIndependentCacheEntries()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var provider = new ResourceFakeProvider(ResourceBackendKind.Addressables)
                {
                    Asset = CreateAsset("exact-type"),
                };
                var store = CreateStore(provider);
                var key = ResourceKey.FromAddressables("exact-type");

                var textLease = await store.AcquireAsync<TextAsset>(key, default);
                var objectLease = await store.AcquireAsync<Object>(key, default);

                Assert.That(provider.LoadCount, Is.EqualTo(2));
                Assert.That(store.CreateDiagnosticsSnapshot().CacheCount, Is.EqualTo(2));
                textLease.Dispose();
                objectLease.Dispose();
                Assert.That(provider.ReleaseCount, Is.EqualTo(2));
            });
        }

        [UnityTest]
        public IEnumerator Dispose_IsIdempotent_AndMainThreadIsEnforced()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var provider = new ResourceFakeProvider(ResourceBackendKind.UnityResources)
                {
                    Asset = CreateAsset("thread"),
                };
                var store = CreateStore(provider);
                var lease = await store.AcquireAsync<TextAsset>(
                    ResourceKey.FromResources("thread"),
                    default);

                var backgroundError = await Task.Run(() =>
                {
                    try
                    {
                        lease.Dispose();
                        return null;
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                });

                Assert.That(backgroundError, Is.TypeOf<InvalidOperationException>());
                Assert.That(lease.IsValid, Is.True);
                lease.Dispose();
                lease.Dispose();
                Assert.That(provider.ReleaseCount, Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator InvalidKeyAndMissingBackend_FailBeforeProviderSideEffects()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                var resources = new ResourceFakeProvider(ResourceBackendKind.UnityResources)
                {
                    Asset = CreateAsset("unused"),
                };
                var store = CreateStore(resources);

                await ExpectExceptionAsync<ArgumentException>(
                    () => store.AcquireAsync<TextAsset>(default, default).AsTask());
                await ExpectExceptionAsync<InvalidOperationException>(
                    () => store.AcquireAsync<TextAsset>(
                        ResourceKey.FromAddressables("missing"),
                        default).AsTask());
                Assert.That(resources.LoadCount, Is.Zero);
            });
        }

        private TextAsset CreateAsset(string name)
        {
            var asset = new TextAsset(name);
            m_assets.Add(asset);
            return asset;
        }

        private static ResourceStore CreateStore(params ResourceFakeProvider[] providers)
        {
            var map = new Dictionary<ResourceBackendKind, ResourceProviderBase>();
            for (var i = 0; i < providers.Length; i++)
            {
                map.Add(providers[i].Backend, providers[i]);
            }

            return new ResourceStore(map);
        }

        private static async Task<TException> ExpectExceptionAsync<TException>(Func<Task> operation)
            where TException : Exception
        {
            try
            {
                await operation();
            }
            catch (TException exception)
            {
                return exception;
            }

            Assert.Fail($"预期抛出 {typeof(TException).FullName}。" );
            return null;
        }
    }
}
