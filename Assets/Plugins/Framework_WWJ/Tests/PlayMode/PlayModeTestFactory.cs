using System.Collections;
using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework_WWJ.Tests
{
    internal sealed class PlayModeTestFactory
    {
        private readonly List<UnityEngine.Object> m_createdObjects = new List<UnityEngine.Object>();
        private FrameworkProjectSettings m_projectSettings;
        private int m_nextSceneHandle = 1000;

        internal T CreateModule<T>() where T : ModuleBase
        {
            var module = ScriptableObject.CreateInstance<T>();
            m_createdObjects.Add(module);
            return module;
        }

        internal PlaySceneModule CreateSceneModule()
        {
            var module = CreateModule<PlaySceneModule>();
            module.SetHandler(new PlaySceneHandler());
            return module;
        }

        internal FrameworkGlobalConfig CreateGlobal(params ModuleBase[] modules)
        {
            var config = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            config.SetModules(CreateEntries(modules));
            m_createdObjects.Add(config);
            return config;
        }

        internal FrameworkSceneConfig CreateScene(params ModuleBase[] modules)
        {
            var config = ScriptableObject.CreateInstance<FrameworkSceneConfig>();
            config.SetModules(CreateEntries(modules));
            m_createdObjects.Add(config);
            return config;
        }

        internal FrameworkProjectSettings CreateProjectSettings(
            FrameworkGlobalConfig globalConfig,
            FrameworkSceneConfig defaultSceneConfig = null)
        {
            var settings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
            settings.SetGlobalConfig(globalConfig);
            settings.SetDefaultSceneConfig(defaultSceneConfig);
            m_createdObjects.Add(settings);
            return settings;
        }

        internal int ActivateScene(
            FrameworkGlobalConfig globalConfig,
            FrameworkSceneConfig sceneConfig)
        {
            if (m_projectSettings == null)
            {
                m_projectSettings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
                m_createdObjects.Add(m_projectSettings);
            }

            m_projectSettings.SetGlobalConfig(globalConfig);
            m_projectSettings.SetDefaultSceneConfig(sceneConfig);
            var handle = m_nextSceneHandle++;
            ObserveOperationAsync(
                Framework.StartProjectSceneAsync(
                    m_projectSettings,
                    new FrameworkSceneDescriptor((ulong)handle, $"Tests/Scene_{handle}.unity", $"Scene_{handle}"))).Forget();
            return handle;
        }

        internal int ActivateScene(FrameworkProjectSettings settings, string scenePath)
        {
            m_projectSettings = settings;
            var handle = m_nextSceneHandle++;
            ObserveOperationAsync(
                Framework.StartProjectSceneAsync(
                    settings,
                    new FrameworkSceneDescriptor((ulong)handle, scenePath, $"Scene_{handle}"))).Forget();
            return handle;
        }

        internal void ActivateWithoutProjectSettings()
        {
            ObserveOperationAsync(
                Framework.StartProjectSceneAsync(
                    null,
                    new FrameworkSceneDescriptor((ulong)m_nextSceneHandle++, "Tests/MissingSettings.unity", "MissingSettings"))).Forget();
        }

        internal void ActivateLoadedScene(FrameworkProjectSettings settings, UnityEngine.SceneManagement.Scene scene)
        {
            ObserveOperationAsync(
                Framework.StartProjectSceneAsync(
                    settings,
                    FrameworkSceneDescriptor.FromScene(scene))).Forget();
        }

        internal void DeactivateScene(int sceneHandle)
        {
            ObserveOperationAsync(Framework.DetachSceneAsync((ulong)sceneHandle)).Forget();
        }

        internal void DestroyAll()
        {
            for (var i = m_createdObjects.Count - 1; i >= 0; i--)
            {
                if (m_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_createdObjects[i]);
                }
            }

            m_createdObjects.Clear();
        }

        private static List<ModuleConfigEntry> CreateEntries(IReadOnlyList<ModuleBase> modules)
        {
            var entries = new List<ModuleConfigEntry>();
            for (var i = 0; i < modules.Count; i++)
            {
                entries.Add(new ModuleConfigEntry(true, modules[i]));
            }

            return entries;
        }

        private static async UniTaskVoid ObserveOperationAsync(UniTask operation)
        {
            try
            {
                await operation;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    internal abstract class FrameworkPlayModeTestBase
    {
        protected PlayModeTestFactory Factory { get; private set; }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            FrameworkBootstrap.ResetForTests();
            PlayLifecycleRecorder.Reset();
            Factory = new PlayModeTestFactory();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            yield return null;
            FrameworkBootstrap.ResetForTests();
            Factory.DestroyAll();
            PlayLifecycleRecorder.Reset();
        }

        protected static IEnumerator WaitForReady(int maxFrames = 180)
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

            Assert.Fail($"等待 Framework Ready 超过 {maxFrames} 帧，当前状态为 {Framework.State}。" );
        }
    }
}
