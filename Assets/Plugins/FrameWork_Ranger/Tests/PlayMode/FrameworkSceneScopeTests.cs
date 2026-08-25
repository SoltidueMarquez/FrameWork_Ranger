using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.Tests
{
    internal sealed class FrameworkSceneScopeTests : FrameworkPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator NewActiveScene_ReplacesSceneScope_ButKeepsGlobalClone()
        {
            var globalConfig = Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>());
            var firstSceneHandle = Factory.ActivateScene(
                globalConfig,
                Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();

            var globalInstance = Framework.GetModule<PlayGlobalModule>();
            var firstSceneInstance = Framework.GetModule<PlaySceneModule>();

            Factory.ActivateScene(
                globalConfig,
                Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();

            Assert.That(Framework.GetModule<PlayGlobalModule>(), Is.SameAs(globalInstance));
            Assert.That(Framework.GetModule<PlaySceneModule>(), Is.Not.SameAs(firstSceneInstance));
            CollectionAssert.Contains(PlayLifecycleRecorder.Events, "SceneHandler.Unload");

            Factory.DeactivateScene(firstSceneHandle);
            yield return null;
            Assert.That(Framework.IsReady, Is.True, "迟到的旧场景卸载不得卸载新 SceneScope。" );
        }

        [UnityTest]
        public IEnumerator DifferentGlobalConfig_IsRejectedWithoutReplacingReadyScope()
        {
            var originalGlobalConfig = Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>());
            Factory.ActivateScene(originalGlobalConfig, Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();

            var globalClone = Framework.GetModule<PlayGlobalModule>();
            LogAssert.Expect(LogType.Exception, new Regex("GlobalConfig 已发生变化"));
            Factory.ActivateScene(
                Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>()),
                Factory.CreateScene(Factory.CreateSceneModule()));
            yield return null;

            Assert.That(Framework.State, Is.EqualTo(FrameworkState.Ready));
            Assert.That(Framework.GetModule<PlayGlobalModule>(), Is.SameAs(globalClone));
            StringAssert.Contains("GlobalConfig 已发生变化", Framework.LastException?.Message);
        }
    }
}
