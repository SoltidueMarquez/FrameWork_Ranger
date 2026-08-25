using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.Tests
{
    internal sealed class FrameworkTickTests : FrameworkPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TickException_Isolated_AndOtherPhasesContinue()
        {
            Factory.ActivateScene(
                Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>()),
                Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();

            var global = Framework.GetModule<PlayGlobalModule>();
            var scene = Framework.GetModule<PlaySceneModule>();
            var sceneTicksBeforeFailure = scene.TickCount;

            LogAssert.Expect(LogType.Error, new Regex("在 Update 中抛出异常"));
            LogAssert.Expect(LogType.Exception, new Regex("PlayMode tick failure"));
            global.ThrowOnNextUpdate = true;
            yield return null;

            Assert.That(scene.TickCount, Is.GreaterThan(sceneTicksBeforeFailure));
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(global.FixedUpdateCount, Is.GreaterThan(0));
            Assert.That(global.LateUpdateCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator TickOrder_IsGlobalThenModuleThenHandler()
        {
            var sceneModule = Factory.CreateModule<PlayTickOrderSceneModule>();
            sceneModule.SetHandler(new PlayTickOrderHandler());
            Factory.ActivateScene(
                Factory.CreateGlobal(Factory.CreateModule<PlayTickOrderGlobalModule>()),
                Factory.CreateScene(sceneModule));
            yield return WaitForReady();

            PlayLifecycleRecorder.Reset();
            yield return new WaitUntil(() => PlayLifecycleRecorder.Events.Count >= 3);

            CollectionAssert.AreEqual(
                new[]
                {
                    "Order.Global.Update",
                    "Order.Module.Update",
                    "Order.Handler.Update",
                },
                PlayLifecycleRecorder.Events.GetRange(0, 3));
        }

        [UnityTest]
        public IEnumerator SceneTransition_StopsOldSceneTick_ButKeepsGlobalTick()
        {
            var globalConfig = Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>());
            Factory.ActivateScene(globalConfig, Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();
            yield return null;

            var global = Framework.GetModule<PlayGlobalModule>();
            var globalTicksBeforeTransition = global.UpdateCount;
            PlayLifecycleRecorder.SceneUpdateCount = 0;

            Factory.ActivateScene(
                globalConfig,
                Factory.CreateScene(Factory.CreateModule<PlaySlowSceneModule>()));
            yield return new WaitUntil(() => Framework.State == FrameworkState.LoadingScene);
            yield return null;

            Assert.That(global.UpdateCount, Is.GreaterThan(globalTicksBeforeTransition));
            Assert.That(PlayLifecycleRecorder.SceneUpdateCount, Is.Zero);
            yield return WaitForReady();
        }
    }
}
