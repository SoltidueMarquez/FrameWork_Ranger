using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ
{
    /// <summary>
    /// 将 Unity 帧消息转交给纯 C# Runtime 的常驻宿主。
    /// </summary>
    [FrameworkArchitecture(
        "Runtime Unity 宿主",
        "承载常驻 GameObject、Unity Tick 与 SceneManager 事件。",
        FrameworkArchitectureLayer.RuntimeDriving,
        60,
        typeof(FrameworkSceneCoordinator),
        typeof(FrameworkRuntime))]
    [DefaultExecutionOrder(-32000)]
    internal sealed class FrameworkRuntimeHost : MonoBehaviour
    {
        #region 运行时状态

        private FrameworkRuntime m_runtime;
        private FrameworkProjectSettings m_settings;
        private FrameworkSceneCoordinator m_sceneCoordinator;
        private bool m_configured;
        private bool m_sceneEventsSubscribed;

        #endregion

        #region 初始化与场景驱动

        internal void Initialize(FrameworkRuntime runtime)
        {
            m_runtime = runtime;
        }

        internal void Configure(FrameworkProjectSettings settings)
        {
            if (m_configured)
            {
                if (m_settings != settings)
                {
                    throw new InvalidOperationException("FrameworkRuntimeHost 不能在同一次 Play Session 中切换 ProjectSettings。" );
                }

                return;
            }

            m_configured = true;
            m_settings = settings;
            m_sceneCoordinator = new FrameworkSceneCoordinator(m_runtime, settings);
            SubscribeSceneEvents();
        }

        internal UniTask ActivateSceneAsync(FrameworkSceneDescriptor scene)
        {
            return m_sceneCoordinator == null
                ? UniTask.FromException(new InvalidOperationException("FrameworkRuntimeHost 尚未配置中央项目设置。"))
                : m_sceneCoordinator.ActivateSceneAsync(scene);
        }

        internal void PrepareForShutdown()
        {
            UnsubscribeSceneEvents();
            m_sceneCoordinator?.PrepareForShutdown();
        }

        #endregion

        #region Unity 生命周期

        private void Update()
        {
            m_runtime?.TickUpdate(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            m_runtime?.TickFixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            m_runtime?.TickLateUpdate(Time.deltaTime);
        }

        private void OnApplicationQuit()
        {
            ObserveApplicationQuitAsync().Forget();
        }

        private void OnDestroy()
        {
            UnsubscribeSceneEvents();
            m_sceneCoordinator?.Dispose();
            m_sceneCoordinator = null;
            m_runtime = null;
        }

        #endregion

        #region 场景事件

        private void SubscribeSceneEvents()
        {
            if (m_sceneEventsSubscribed)
            {
                return;
            }

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            m_sceneEventsSubscribed = true;
        }

        private void UnsubscribeSceneEvents()
        {
            if (!m_sceneEventsSubscribed)
            {
                return;
            }

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            m_sceneEventsSubscribed = false;
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (!nextScene.IsValid() || m_sceneCoordinator == null)
            {
                return;
            }

            ObserveSceneOperationAsync(
                m_sceneCoordinator.ActivateSceneAsync(
                    FrameworkSceneDescriptor.FromScene(nextScene))).Forget();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (m_sceneCoordinator == null)
            {
                return;
            }

            ObserveSceneOperationAsync(m_sceneCoordinator.DeactivateSceneAsync(scene.handle)).Forget();
        }

        private static async UniTaskVoid ObserveSceneOperationAsync(UniTask operation)
        {
            try
            {
                await operation;
            }
            catch (OperationCanceledException)
            {
                // 场景被更快的新活动场景取代时，取消是正常的所有权转移信号。
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        #endregion

        #region 应用退出

        private static async UniTaskVoid ObserveApplicationQuitAsync()
        {
            try
            {
                await Framework.ShutdownAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        #endregion
    }
}
