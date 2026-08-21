using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework_WWJ.Tests
{
    internal sealed class ModuleLifecycleStateTests
    {
        [UnityTest]
        public IEnumerator Scope_LoadsDirectAndHandler_ThenUnloadsInReverseOrder()
        {
            return UniTask.ToCoroutine(async () =>
            {
                TestLifecycleRecorder.Reset();
                var directTemplate = ScriptableObject.CreateInstance<TestModuleA>();
                var handlerTemplate = ScriptableObject.CreateInstance<TestHandlerModule>();
                handlerTemplate.SetHandler(new TestModuleHandler());
                var config = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
                config.SetModules(new List<ModuleConfigEntry>
                {
                    new ModuleConfigEntry(true, directTemplate),
                    new ModuleConfigEntry(true, handlerTemplate),
                });

                var graph = ModuleGraphResolver.Inspect(config);
                var runtime = new FrameworkRuntime();
                var driver = new DefaultFrameworkDriverHandler();
                driver.BindRuntime(new FrameworkDriverContext(runtime));
                var scope = new ModuleScopeRuntime(runtime, ModuleScopeKind.Global, graph.OrderedGlobalNodes);

                await scope.LoadAsync(driver, default);
                Assert.That(scope.Modules[0].State, Is.EqualTo(ModuleLifecycleState.Loaded));
                Assert.That(scope.Modules[1].State, Is.EqualTo(ModuleLifecycleState.Loaded));
                var directRuntime = (TestModuleA)scope.Modules[0];
                var handlerRuntime = (TestModuleHandler)scope.Modules[1].GetAdditionalTickTarget();
                Assert.That(handlerRuntime.WasLoaded, Is.True);
                Assert.That(handlerRuntime.HasRuntimeBinding, Is.True);

                var errors = await scope.UnloadAndDestroyAsync(driver);
                Assert.That(errors, Is.Empty);
                CollectionAssert.AreEqual(
                    new[] { "A.Load", "Handler.Load", "Handler.Unload", "A.Unload" },
                    TestLifecycleRecorder.Events);
                Assert.That(directTemplate.State, Is.EqualTo(ModuleLifecycleState.None));
                Assert.That(handlerTemplate.State, Is.EqualTo(ModuleLifecycleState.None));
                Assert.That(directRuntime.WasVisibleDuringUnload, Is.False);
                Assert.That(handlerRuntime.HasRuntimeBinding, Is.False);

                driver.ReleaseRuntime();
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(directTemplate);
                Object.DestroyImmediate(handlerTemplate);
            });
        }

        [UnityTest]
        public IEnumerator Scope_UnloadFailure_IsCollected_AndRemainingModulesStillUnload()
        {
            return UniTask.ToCoroutine(async () =>
            {
                TestLifecycleRecorder.Reset();
                var firstTemplate = ScriptableObject.CreateInstance<TestModuleA>();
                var failingTemplate = ScriptableObject.CreateInstance<TestUnloadFailModule>();
                var config = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
                config.SetModules(new List<ModuleConfigEntry>
                {
                    new ModuleConfigEntry(true, firstTemplate),
                    new ModuleConfigEntry(true, failingTemplate),
                });

                var graph = ModuleGraphResolver.Inspect(config);
                var runtime = new FrameworkRuntime();
                var driver = new DefaultFrameworkDriverHandler();
                driver.BindRuntime(new FrameworkDriverContext(runtime));
                var scope = new ModuleScopeRuntime(runtime, ModuleScopeKind.Global, graph.OrderedGlobalNodes);

                await scope.LoadAsync(driver, default);
                var errors = await scope.UnloadAndDestroyAsync(driver);

                Assert.That(errors.Count, Is.EqualTo(1));
                Assert.That(errors[0].Message, Is.EqualTo("EditMode unload failure"));
                CollectionAssert.AreEqual(
                    new[] { "A.Load", "Fail.Load", "Fail.Unload", "A.Unload" },
                    TestLifecycleRecorder.Events);

                driver.ReleaseRuntime();
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(firstTemplate);
                Object.DestroyImmediate(failingTemplate);
            });
        }
    }
}
