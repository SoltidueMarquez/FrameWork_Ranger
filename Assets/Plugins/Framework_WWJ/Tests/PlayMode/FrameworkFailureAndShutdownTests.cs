using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Framework_WWJ.Tests
{
    internal sealed class FrameworkFailureAndShutdownTests : FrameworkPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SceneFailure_RollsBackScene_ButAllowsRetryWithSameGlobal()
        {
            LogAssert.Expect(LogType.Error, new Regex("SceneScope 加载失败"));
            LogAssert.Expect(LogType.Exception, new Regex("Scene load failed"));

            var globalConfig = Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>());
            Factory.ActivateScene(
                globalConfig,
                Factory.CreateScene(
                    Factory.CreateSceneModule(),
                    Factory.CreateModule<PlayFailSceneModule>()));
            var readinessTask = Framework.WhenReadyAsync();

            yield return new WaitUntil(() => Framework.State == FrameworkState.GlobalReady && Framework.LastException != null);
            var globalInstance = Framework.GetModule<PlayGlobalModule>();
            Assert.That(Framework.TryGetModule<PlayFailSceneModule>(out _), Is.False);
            CollectionAssert.Contains(PlayLifecycleRecorder.Events, "SceneHandler.Unload");

            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    await readinessTask;
                    Assert.Fail("失败的 SceneScope 就绪批次不应正常完成。");
                }
                catch (InvalidOperationException exception)
                {
                    Assert.That(exception.Message, Is.EqualTo("Scene load failed"));
                    Assert.That(Framework.LastException, Is.SameAs(exception));
                }
            });

            Factory.ActivateScene(globalConfig, Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();
            Assert.That(Framework.GetModule<PlayGlobalModule>(), Is.SameAs(globalInstance));
        }

        [UnityTest]
        public IEnumerator GlobalFailure_EntersFailedUntilShutdown()
        {
            LogAssert.Expect(LogType.Error, new Regex("GlobalScope 初始化失败"));
            LogAssert.Expect(LogType.Exception, new Regex("Global load failed"));

            Factory.ActivateScene(
                Factory.CreateGlobal(
                    Factory.CreateModule<PlayGlobalModule>(),
                    Factory.CreateModule<PlayFailGlobalModule>()),
                Factory.CreateScene());

            yield return new WaitUntil(() => Framework.State == FrameworkState.Failed);
            Assert.That(Framework.LastException?.Message, Is.EqualTo("Global load failed"));
            Assert.That(Framework.IsReady, Is.False);
            Assert.That(Framework.TryGetModule<PlayGlobalModule>(out _), Is.False);
            CollectionAssert.AreEqual(
                new[] { "Global.Load", "Global.Unload" },
                PlayLifecycleRecorder.Events);
        }

        [UnityTest]
        public IEnumerator InvalidConfiguration_FailsBeforeCreatingRuntimeClones()
        {
            LogAssert.Expect(LogType.Error, new Regex("Framework 模块配置无效"));
            LogAssert.Expect(LogType.Exception, new Regex("Framework 模块配置无效"));

            var firstTemplate = Factory.CreateModule<PlayGlobalModule>();
            var duplicateTemplate = Factory.CreateModule<PlayGlobalModule>();
            Factory.ActivateScene(
                Factory.CreateGlobal(firstTemplate, duplicateTemplate),
                Factory.CreateScene());

            yield return new WaitUntil(() => Framework.State == FrameworkState.Failed);

            Assert.That(Framework.LastException, Is.TypeOf<FrameworkConfigurationException>());
            Assert.That(firstTemplate.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.That(duplicateTemplate.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.That(PlayLifecycleRecorder.Events, Is.Empty);
            Assert.That(Resources.FindObjectsOfTypeAll<PlayGlobalModule>().Length, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator MissingProjectSettings_FailsBeforeCreatingRuntimeClones()
        {
            LogAssert.Expect(LogType.Error, new Regex("找不到固定的 FrameworkProjectSettings"));
            LogAssert.Expect(LogType.Exception, new Regex("找不到固定的 FrameworkProjectSettings"));

            Factory.ActivateWithoutProjectSettings();
            yield return new WaitUntil(() => Framework.State == FrameworkState.Failed);

            Assert.That(Framework.LastException, Is.TypeOf<FrameworkConfigurationException>());
            Assert.That(Resources.FindObjectsOfTypeAll<PlayGlobalModule>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator Shutdown_IsIdempotent_DestroysClones_AndRejectsReadyWait()
        {
            Factory.ActivateScene(
                Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>()),
                Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();

            var globalClone = Framework.GetModule<PlayGlobalModule>();
            var sceneClone = Framework.GetModule<PlaySceneModule>();

            yield return Framework.ShutdownAsync().ToCoroutine();
            yield return Framework.ShutdownAsync().ToCoroutine();
            yield return null;

            Assert.That(Framework.State, Is.EqualTo(FrameworkState.Shutdown));
            Assert.That(globalClone == null, Is.True);
            Assert.That(sceneClone == null, Is.True);
            Assert.That(GameObject.Find("[Framework_WWJ]"), Is.Null);

            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    await Framework.WhenReadyAsync();
                    Assert.Fail("Shutdown 状态下的 WhenReadyAsync 不应创建新的空 Runtime。");
                }
                catch (InvalidOperationException exception)
                {
                    StringAssert.Contains("已经关停", exception.Message);
                }
            });
        }
    }
}
