using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.ResourceManagement.Tests
{
    internal sealed class ResourceModuleLifecycleTests
    {
        private readonly List<UnityEngine.Object> m_created = new List<UnityEngine.Object>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            FrameworkBootstrap.ResetForTests();
            ResourcePlayModeProvider.ResetLifecycleCounts();
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
        }

        [UnityTest]
        public IEnumerator GlobalResourceModule_LoadsProviders_AndShutdownInvalidatesLease()
        {
            var asset = Track(new TextAsset("resource lifecycle"));
            var template = CreateTemplate(asset, false);
            StartFramework(template);
            yield return WaitForState(FrameworkState.Ready);

            var runtime = Framework.GetModule<ResourceModule>();
            Assert.That(runtime, Is.Not.SameAs(template));
            ResourceLease<TextAsset> lease = null;
            yield return UniTask.ToCoroutine(async () =>
            {
                lease = await runtime.AcquireAsync<TextAsset>(
                    ResourceKey.FromResources("lifecycle"));
            });
            Assert.That(lease.Value, Is.SameAs(asset));

            yield return Framework.ShutdownAsync().ToCoroutine();
            Assert.That(lease.IsValid, Is.False);
            Assert.Throws<ObjectDisposedException>(() => _ = lease.Value);
            Assert.That(template.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.That(ResourcePlayModeProvider.ResourcesInitializeCount, Is.EqualTo(1));
            Assert.That(ResourcePlayModeProvider.ResourcesShutdownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AddressablesProviderInitializationFailure_RollsBackGlobalScope()
        {
            LogAssert.Expect(LogType.Error, new Regex("GlobalScope 初始化失败"));
            LogAssert.Expect(LogType.Exception, new Regex("PlayMode provider initialization failed"));
            var template = CreateTemplate(Track(new TextAsset("failure")), true);

            StartFramework(template);
            yield return WaitForState(FrameworkState.Failed);

            Assert.That(Framework.IsReady, Is.False);
            Assert.That(Framework.TryGetModule<ResourceModule>(out _), Is.False);
            Assert.That(Framework.LastException?.Message, Does.Contain("initialization failed"));
            Assert.That(template.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.That(ResourcePlayModeProvider.ResourcesInitializeCount, Is.EqualTo(1));
            Assert.That(ResourcePlayModeProvider.ResourcesShutdownCount, Is.EqualTo(1));
            Assert.That(ResourcePlayModeProvider.AddressablesShutdownCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DuplicateProvider_FailsBeforeAnyProviderInitialization()
        {
            LogAssert.Expect(LogType.Error, new Regex("GlobalScope 初始化失败"));
            LogAssert.Expect(LogType.Exception, new Regex("重复配置"));
            var template = Track(ScriptableObject.CreateInstance<ResourceModule>());
            var asset = Track(new TextAsset("duplicate"));
            var handler = new ResourceHandler();
            handler.SetProviders(new ResourceProviderBase[]
            {
                new ResourcePlayModeProvider(ResourceBackendKind.UnityResources, asset),
                new ResourcePlayModeProvider(ResourceBackendKind.UnityResources, asset),
                new ResourcePlayModeProvider(ResourceBackendKind.Addressables, asset),
            });
            template.SetHandler(handler);

            StartFramework(template);
            yield return WaitForState(FrameworkState.Failed);

            Assert.That(ResourcePlayModeProvider.ResourcesInitializeCount, Is.Zero);
            Assert.That(ResourcePlayModeProvider.AddressablesInitializeCount, Is.Zero);
        }

        private ResourceModule CreateTemplate(TextAsset asset, bool failAddressables)
        {
            var template = Track(ScriptableObject.CreateInstance<ResourceModule>());
            var resources = new ResourcePlayModeProvider(ResourceBackendKind.UnityResources, asset);
            var addressables = new ResourcePlayModeProvider(ResourceBackendKind.Addressables, asset)
            {
                FailInitialization = failAddressables,
            };
            var handler = new ResourceHandler();
            handler.SetProviders(new ResourceProviderBase[] { resources, addressables });
            template.SetHandler(handler);
            return template;
        }

        private void StartFramework(ResourceModule template)
        {
            var global = Track(ScriptableObject.CreateInstance<FrameworkGlobalConfig>());
            global.SetModules(new[] { new ModuleConfigEntry(true, template) });
            var scene = Track(ScriptableObject.CreateInstance<FrameworkSceneConfig>());
            scene.SetModules(Array.Empty<ModuleConfigEntry>());
            var settings = Track(ScriptableObject.CreateInstance<FrameworkProjectSettings>());
            settings.SetGlobalConfig(global);
            settings.SetDefaultSceneConfig(scene);
            Framework.StartProjectSceneAsync(
                    settings,
                    new FrameworkSceneDescriptor(9876, "Tests/Resource.unity", "Resource"))
                .Forget();
        }

        private static IEnumerator WaitForState(FrameworkState expected, int maxFrames = 180)
        {
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (Framework.State == expected)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"等待 Framework 状态 {expected} 超时，当前为 {Framework.State}。" );
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            m_created.Add(value);
            return value;
        }
    }
}
