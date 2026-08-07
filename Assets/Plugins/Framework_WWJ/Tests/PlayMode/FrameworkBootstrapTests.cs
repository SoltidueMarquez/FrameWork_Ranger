using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Framework_WWJ.Tests
{
    internal sealed class FrameworkBootstrapTests : FrameworkPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator CentralBootstrap_LoadsGlobalBeforeScene_AndKeepsTemplatesClean()
        {
            var globalTemplate = Factory.CreateModule<PlayGlobalModule>();
            var sceneTemplate = Factory.CreateSceneModule();
            Factory.ActivateScene(
                Factory.CreateGlobal(globalTemplate),
                Factory.CreateScene(sceneTemplate));

            yield return WaitForReady();

            CollectionAssert.AreEqual(
                new[] { "Global.Load", "SceneHandler.Load" },
                PlayLifecycleRecorder.Events);
            Assert.That(Framework.GetModule<PlayGlobalModule>(), Is.Not.SameAs(globalTemplate));
            Assert.That(Framework.GetModule<PlaySceneModule>(), Is.Not.SameAs(sceneTemplate));
            Assert.That(globalTemplate.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.That(sceneTemplate.State, Is.EqualTo(ModuleLifecycleState.None));
        }

        [UnityTest]
        public IEnumerator UnmappedSceneWithoutDefault_LoadsLegalEmptySceneScope()
        {
            var globalTemplate = Factory.CreateModule<PlayGlobalModule>();
            var settings = Factory.CreateProjectSettings(Factory.CreateGlobal(globalTemplate));

            Factory.ActivateScene(settings, "Assets/Scenes/Unmapped.unity");
            yield return WaitForReady();

            Assert.That(Framework.GetModule<PlayGlobalModule>(), Is.Not.Null);
            Assert.That(Framework.TryGetModule<PlaySceneModule>(out _), Is.False);
            CollectionAssert.AreEqual(new[] { "Global.Load" }, PlayLifecycleRecorder.Events);
        }
    }
}
