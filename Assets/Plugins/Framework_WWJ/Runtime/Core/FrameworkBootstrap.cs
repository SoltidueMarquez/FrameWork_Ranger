using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ
{
    /// <summary>
    /// Framework_WWJ 的自动启动入口。每次 Play Session 只读取一次中央项目设置并提交首个活动场景。
    /// </summary>
    [FrameworkArchitecture(
        "框架自动启动",
        "重置静态状态、读取固定设置并提交首个活动场景。",
        FrameworkArchitectureLayer.RuntimeDriving,
        0,
        typeof(Framework),
        typeof(FrameworkProjectSettings))]
    internal static class FrameworkBootstrap
    {
        private static bool s_started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            s_started = false;
            Framework.ResetForSubsystemRegistration();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartAfterFirstSceneLoad()
        {
            if (s_started)
            {
                return;
            }

            s_started = true;
            var settings = Resources.Load<FrameworkProjectSettings>(
                FrameworkProjectSettings.ResourcesLoadPath);
            var activeScene = SceneManager.GetActiveScene();
            ObserveStartAsync(
                Framework.StartProjectSceneAsync(
                    settings,
                    FrameworkSceneDescriptor.FromScene(activeScene))).Forget();
        }

        internal static void ResetForTests()
        {
            s_started = false;
            Framework.ResetForSubsystemRegistration();
        }

        private static async UniTaskVoid ObserveStartAsync(UniTask startTask)
        {
            try
            {
                await startTask;
            }
            catch (OperationCanceledException)
            {
                // 快速场景切换或退出 Play Mode 会取消尚未完成的首场景加载，Runtime 已负责回滚。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
