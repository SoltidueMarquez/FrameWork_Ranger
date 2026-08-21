using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Framework_WWJ.ResourceManagement.Tests
{
    internal sealed class ResourceBackendsIntegrationTests
    {
        private const string SampleSceneName = "SampleScene";
        private const string AddressablesLocation =
            "framework-wwj/samples/resource-management/addressables-prefab";
        private const string ResourcesLocation =
            "Framework_WWJ/ResourceManagement/ResourcesSamplePrefab";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            FrameworkBootstrap.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            yield return null;
            FrameworkBootstrap.ResetForTests();
        }

        [UnityTest]
        public IEnumerator RealBackends_AcquireReleaseSwitchScenesAndShutdown()
        {
            yield return SceneManager.LoadSceneAsync(SampleSceneName, LoadSceneMode.Single);
            var settings = Resources.Load<FrameworkProjectSettings>(
                FrameworkProjectSettings.ResourcesLoadPath);
            Assert.That(settings, Is.Not.Null);
            yield return Framework.StartProjectSceneAsync(
                    settings,
                    FrameworkSceneDescriptor.FromScene(SceneManager.GetActiveScene()))
                .ToCoroutine();
            yield return WaitForReady();

            var module = Framework.GetModule<ResourceModule>();
            ResourceLease<GameObject> resourcesLease = null;
            ResourceLease<GameObject> addressablesLeaseA = null;
            ResourceLease<GameObject> addressablesLeaseB = null;
            GameObject resourcesInstance = null;
            GameObject addressablesInstance = null;
            yield return UniTask.ToCoroutine(async () =>
            {
                resourcesLease = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromResources(ResourcesLocation));
                addressablesLeaseA = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromAddressables(AddressablesLocation));
                addressablesLeaseB = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromAddressables(AddressablesLocation));
                resourcesInstance = UnityEngine.Object.Instantiate(resourcesLease.Value);
                addressablesInstance = UnityEngine.Object.Instantiate(addressablesLeaseA.Value);
            });

            Assert.That(resourcesLease.Value.name, Is.EqualTo("ResourcesSamplePrefab"));
            Assert.That(addressablesLeaseA.Value.name, Is.EqualTo("AddressablesSamplePrefab"));
            Assert.That(module.CreateDiagnosticsSnapshot().LeaseCount, Is.EqualTo(3));
            UnityEngine.Object.Destroy(resourcesInstance);
            resourcesLease.Dispose();
            Assert.That(module.CreateDiagnosticsSnapshot().CacheCount, Is.EqualTo(1));
            addressablesLeaseA.Dispose();
            Assert.That(module.CreateDiagnosticsSnapshot().LeaseCount, Is.EqualTo(1));

            yield return SceneManager.LoadSceneAsync(SampleSceneName, LoadSceneMode.Single);
            yield return WaitForReady();
            Assert.That(Framework.GetModule<ResourceModule>(), Is.SameAs(module));
            yield return SceneManager.LoadSceneAsync(SampleSceneName, LoadSceneMode.Single);
            yield return WaitForReady();
            Assert.That(Framework.GetModule<ResourceModule>(), Is.SameAs(module));

            UnityEngine.Object.Destroy(addressablesInstance);
            yield return Framework.ShutdownAsync().ToCoroutine();
            Assert.That(addressablesLeaseB.IsValid, Is.False);
            Assert.Throws<ObjectDisposedException>(() => _ = addressablesLeaseB.Value);
            addressablesLeaseB.Dispose();
        }

        [UnityTest]
        public IEnumerator InvalidLocations_ThrowResourceLoadExceptionWithoutFallback()
        {
            yield return SceneManager.LoadSceneAsync(SampleSceneName, LoadSceneMode.Single);
            var settings = Resources.Load<FrameworkProjectSettings>(
                FrameworkProjectSettings.ResourcesLoadPath);
            yield return Framework.StartProjectSceneAsync(
                    settings,
                    FrameworkSceneDescriptor.FromScene(SceneManager.GetActiveScene()))
                .ToCoroutine();
            yield return WaitForReady();

            var module = Framework.GetModule<ResourceModule>();
            ResourceLoadException resourcesError = null;
            ResourceLoadException addressablesError = null;
            LogAssert.Expect(
                LogType.Error,
                new Regex("InvalidKeyException.*missing/addressables-prefab", RegexOptions.Singleline));
            yield return UniTask.ToCoroutine(async () =>
            {
                resourcesError = await CaptureLoadErrorAsync(module, ResourceKey.FromResources("Missing/Prefab"));
                addressablesError = await CaptureLoadErrorAsync(
                    module,
                    ResourceKey.FromAddressables("missing/addressables-prefab"));
            });

            Assert.That(resourcesError, Is.Not.Null);
            Assert.That(resourcesError.Key.Backend, Is.EqualTo(ResourceBackendKind.UnityResources));
            Assert.That(addressablesError, Is.Not.Null);
            Assert.That(addressablesError.Key.Backend, Is.EqualTo(ResourceBackendKind.Addressables));
            Assert.That(module.CreateDiagnosticsSnapshot().CacheCount, Is.Zero);
        }

        private static async UniTask<ResourceLoadException> CaptureLoadErrorAsync(
            ResourceModule module,
            ResourceKey key)
        {
            try
            {
                using (await module.AcquireAsync<GameObject>(key))
                {
                    Assert.Fail($"无效位置不应加载成功：{key}");
                }
            }
            catch (ResourceLoadException exception)
            {
                return exception;
            }

            return null;
        }

        private static IEnumerator WaitForReady(int maxFrames = 300)
        {
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (Framework.IsReady)
                {
                    yield break;
                }

                if (Framework.LastException != null)
                {
                    Assert.Fail($"Framework 初始化失败：{Framework.LastException}");
                }

                yield return null;
            }

            Assert.Fail($"等待 Framework Ready 超时，当前状态为 {Framework.State}。");
        }
    }
}
