using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using FrameWork_Ranger.ResourceManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace FrameWork_Ranger.Pooling.Tests
{
    internal sealed class PoolingModuleLifecycleTests
    {
        private readonly List<UnityEngine.Object> m_created = new List<UnityEngine.Object>();
        private FrameworkProjectSettings m_settings;
        private GameObject m_prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            FrameworkBootstrap.ResetForTests();
            PoolingCallbackProbe.ResetEvents();
            PoolingTestResourceProvider.Reset();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            yield return null;
            FrameworkBootstrap.ResetForTests();
            for (var i = m_created.Count - 1; i >= 0; i--)
            {
                if (m_created[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_created[i]);
                }
            }

            m_created.Clear();
            PoolingCallbackProbe.ResetEvents();
        }

        [UnityTest]
        public IEnumerator SpawnDespawn_AppliesPoseActivationCallbacksAndStrictOwnership()
        {
            CreateSettings(initialCount: 1);
            yield return ActivateScene(1001, "Pooling_A");

            var module = Framework.GetModule<GameObjectPoolModule>();
            var parent = Track(new GameObject("Spawn Parent"));
            var position = new Vector3(2f, 3f, 4f);
            var rotation = Quaternion.Euler(10f, 20f, 30f);
            var instance = module.Spawn("Primary", position, rotation, parent.transform);

            Assert.That(instance.activeSelf, Is.True);
            Assert.That(instance.transform.parent, Is.SameAs(parent.transform));
            Assert.That(instance.transform.position, Is.EqualTo(position).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(instance.transform.rotation.eulerAngles, Is.EqualTo(rotation.eulerAngles).Using(Vector3ComparerWithEqualsOperator.Instance));
            CollectionAssert.AreEqual(
                new[] { "root.spawn", "child.spawn" },
                PoolingCallbackProbe.RecordedEvents);

            Assert.That(module.TryDespawn("Secondary", instance), Is.False, "错池归还必须失败");
            module.Despawn("Primary", instance);
            Assert.That(instance.activeSelf, Is.False);
            CollectionAssert.AreEqual(
                new[] { "root.spawn", "child.spawn", "child.despawn", "root.despawn" },
                PoolingCallbackProbe.RecordedEvents);
            Assert.That(module.TryDespawn("Primary", instance), Is.False, "重复归还必须失败");

            var reused = module.Spawn("Primary");
            Assert.That(reused, Is.SameAs(instance));
            module.Despawn("Primary", reused);
        }

        [UnityTest]
        public IEnumerator SceneReplacement_RecreatesGameObjectPoolsButKeepsGlobalReferencePool()
        {
            CreateSettings(initialCount: 1);
            yield return ActivateScene(2001, "Pooling_First");
            var referenceBefore = Framework.GetModule<ReferencePoolModule>();
            var gameObjectBefore = Framework.GetModule<GameObjectPoolModule>();
            var payload = referenceBefore.Rent<PoolingReferencePayload>();
            var leaked = gameObjectBefore.Spawn("Primary");
            LogAssert.Expect(LogType.Warning, new Regex("池 Primary 仍有 1 个实例未归还"));

            yield return ActivateScene(2002, "Pooling_Second");
            yield return null;

            Assert.That(Framework.GetModule<ReferencePoolModule>(), Is.SameAs(referenceBefore));
            Assert.That(Framework.GetModule<GameObjectPoolModule>(), Is.Not.SameAs(gameObjectBefore));
            Assert.That(leaked == null, Is.True, "旧 SceneScope 借出对象应被强制销毁");
            referenceBefore.Return(payload);
            Assert.That(payload.ReturnCount, Is.EqualTo(1));
            Assert.That(PoolingTestResourceProvider.ReleaseCount, Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator ExternallyDestroyedInstance_IsDiagnosedAndNeverRecached()
        {
            CreateSettings(initialCount: 0);
            yield return ActivateScene(3001, "Pooling_ExternalDestroy");
            var module = Framework.GetModule<GameObjectPoolModule>();
            var instance = module.Spawn("Primary");
            UnityEngine.Object.Destroy(instance);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex("被外部提前销毁"));
            Assert.That(module.TryDespawn("Primary", instance), Is.False);
            var replacement = module.Spawn("Primary");
            Assert.That(replacement, Is.Not.Null);
            module.Despawn("Primary", replacement);
        }

        [UnityTest]
        public IEnumerator BothPoolModules_RejectBackgroundThreadAccess()
        {
            CreateSettings(initialCount: 0);
            yield return ActivateScene(4001, "Pooling_MainThread");
            var reference = Framework.GetModule<ReferencePoolModule>();
            var gameObjects = Framework.GetModule<GameObjectPoolModule>();
            Exception referenceException = null;
            Exception gameObjectException = null;
            var task = Task.Run(() =>
            {
                try
                {
                    reference.Rent<PoolingReferencePayload>();
                }
                catch (Exception exception)
                {
                    referenceException = exception;
                }

                try
                {
                    gameObjects.TrySpawn("Primary", out _);
                }
                catch (Exception exception)
                {
                    gameObjectException = exception;
                }
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.That(referenceException, Is.TypeOf<InvalidOperationException>());
            Assert.That(referenceException.Message, Does.Contain("Unity 主线程"));
            Assert.That(gameObjectException, Is.TypeOf<InvalidOperationException>());
            Assert.That(gameObjectException.Message, Does.Contain("Unity 主线程"));
        }

        [UnityTest]
        public IEnumerator PrewarmBudget_DelaysReadyAcrossFrames()
        {
            CreateSettings(initialCount: 21);
            var startFrame = Time.frameCount;

            yield return ActivateScene(5001, "Pooling_Budget");

            Assert.That(Time.frameCount - startFrame, Is.GreaterThanOrEqualTo(2));
            var snapshots = Framework.GetModule<GameObjectPoolModule>().CreateDiagnosticsSnapshots();
            Assert.That(snapshots[0].InactiveCount, Is.EqualTo(21));
        }

        [UnityTest]
        public IEnumerator LaterPoolLoadFailure_RollsBackEarlierPoolAndReleasesItsLease()
        {
            LogAssert.Expect(LogType.Error, new Regex("SceneScope 加载失败"));
            CreateSettings(initialCount: 1, failAddressablesLoad: true);
            Exception observed = null;

            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    await Framework.StartProjectSceneAsync(
                        m_settings,
                        new FrameworkSceneDescriptor(
                            6001,
                            "Tests/Pooling_Rollback.unity",
                            "Pooling_Rollback"));
                }
                catch (Exception exception)
                {
                    observed = exception;
                }
            });
            yield return null;

            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.Message, Does.Contain("Pooling test resource load failed"));
            Assert.That(Framework.State, Is.EqualTo(FrameworkState.GlobalReady));
            Assert.That(Framework.TryGetModule<GameObjectPoolModule>(out _), Is.False);
            Assert.That(PoolingTestResourceProvider.ReleaseCount, Is.EqualTo(1));
        }

        private IEnumerator ActivateScene(ulong handle, string name)
        {
            yield return Framework.StartProjectSceneAsync(
                    m_settings,
                    new FrameworkSceneDescriptor(handle, $"Tests/{name}.unity", name))
                .ToCoroutine();
            Assert.That(Framework.IsReady, Is.True, Framework.LastException?.ToString());
        }

        private void CreateSettings(int initialCount, bool failAddressablesLoad = false)
        {
            m_prefab = Track(new GameObject("Pooling Test Prefab"));
            m_prefab.SetActive(false);
            m_prefab.AddComponent<PoolingCallbackProbe>().Label = "root";
            var child = new GameObject("Child");
            child.transform.SetParent(m_prefab.transform, false);
            child.AddComponent<PoolingCallbackProbe>().Label = "child";

            var resource = Track(ScriptableObject.CreateInstance<ResourceModule>());
            var resourceHandler = new ResourceHandler();
            resourceHandler.SetProviders(new ResourceProviderBase[]
            {
                new PoolingTestResourceProvider(ResourceBackendKind.UnityResources, m_prefab),
                new PoolingTestResourceProvider(
                    ResourceBackendKind.Addressables,
                    m_prefab,
                    failAddressablesLoad),
            });
            resource.SetHandler(resourceHandler);

            var reference = Track(ScriptableObject.CreateInstance<ReferencePoolModule>());
            var referenceHandler = new ReferencePoolHandler();
            referenceHandler.SetConfiguration(
                new PoolCapacitySettings(0, 2, -1, 0f),
                Array.Empty<ReferencePoolTypeOverride>());
            reference.SetHandler(referenceHandler);

            var gameObject = Track(ScriptableObject.CreateInstance<GameObjectPoolModule>());
            var gameObjectHandler = new GameObjectPoolHandler();
            gameObjectHandler.SetConfiguration(new[]
            {
                new GameObjectPoolDefinition(
                    "Primary",
                    ResourceBackendKind.UnityResources,
                    "Pooling/TestPrefab",
                    new PoolCapacitySettings(initialCount, 2, -1, 0f)),
                new GameObjectPoolDefinition(
                    "Secondary",
                    ResourceBackendKind.Addressables,
                    "Pooling/TestPrefabAddressable",
                    new PoolCapacitySettings(0, 1, -1, 0f)),
            });
            gameObject.SetHandler(gameObjectHandler);

            var global = Track(ScriptableObject.CreateInstance<FrameworkGlobalConfig>());
            global.SetModules(new ModuleConfigEntry[]
            {
                new ModuleConfigEntry(true, resource),
                new ModuleConfigEntry(true, reference),
            });
            var scene = Track(ScriptableObject.CreateInstance<FrameworkSceneConfig>());
            scene.SetModules(new[] { new ModuleConfigEntry(true, gameObject) });
            m_settings = Track(ScriptableObject.CreateInstance<FrameworkProjectSettings>());
            m_settings.SetGlobalConfig(global);
            m_settings.SetDefaultSceneConfig(scene);
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            m_created.Add(value);
            return value;
        }
    }
}
