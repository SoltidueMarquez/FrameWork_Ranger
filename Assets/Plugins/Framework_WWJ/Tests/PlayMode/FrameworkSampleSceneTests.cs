using System.Collections;
using Framework_WWJ.Samples;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Framework_WWJ.Tests
{
    /// <summary>
    /// 使用真正加入 Build Settings 的 A/B 示例场景验证跨场景所有权，而非只依赖测试工厂对象。
    /// </summary>
    internal sealed class FrameworkSampleSceneTests : FrameworkPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SampleScenes_SwitchHandler_AndKeepGlobalClone()
        {
            yield return SceneManager.LoadSceneAsync("CoreSkeleton_A", LoadSceneMode.Single);
            Factory.ActivateLoadedScene(
                Resources.Load<FrameworkProjectSettings>(FrameworkProjectSettings.ResourcesLoadPath),
                SceneManager.GetActiveScene());
            yield return WaitForReady();

            var globalA = Framework.GetModule<SampleGlobalClockModule>();
            var sceneA = Framework.GetModule<SampleSceneModule>();
            var globalInstanceId = globalA.RuntimeInstanceId;
            Assert.That(sceneA.HandlerTypeName, Is.EqualTo(nameof(SampleCounterHandler)));

            yield return SceneManager.LoadSceneAsync("CoreSkeleton_B", LoadSceneMode.Single);
            yield return WaitForReady();

            var globalB = Framework.GetModule<SampleGlobalClockModule>();
            var sceneB = Framework.GetModule<SampleSceneModule>();
            Assert.That(globalB.RuntimeInstanceId, Is.EqualTo(globalInstanceId));
            Assert.That(sceneB.HandlerTypeName, Is.EqualTo(nameof(SamplePulseHandler)));
        }
    }
}
