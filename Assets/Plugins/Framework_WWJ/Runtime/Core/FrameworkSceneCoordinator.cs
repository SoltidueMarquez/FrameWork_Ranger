using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework_WWJ
{
    /// <summary>
    /// 将活动场景事件转换为串行的 SceneScope 挂载与卸载请求，并拥有场景加载取消源。
    /// </summary>
    [FrameworkArchitecture(
        "活动场景协调器",
        "把活动场景和卸载事件转换为带所有者令牌的 Scope 操作。",
        FrameworkArchitectureLayer.RuntimeDriving,
        40,
        typeof(FrameworkRuntime),
        typeof(FrameworkProjectSettingsResolver))]
    internal sealed class FrameworkSceneCoordinator : IDisposable
    {
        #region 运行时状态

        private readonly FrameworkRuntime m_runtime;
        private readonly FrameworkProjectSettings m_settings;
        private readonly Dictionary<ulong, CancellationTokenSource> m_sceneCancellations =
            new Dictionary<ulong, CancellationTokenSource>();

        private ulong m_activeSceneHandle;
        private bool m_disposed;

        #endregion

        internal FrameworkSceneCoordinator(
            FrameworkRuntime runtime,
            FrameworkProjectSettings settings)
        {
            m_runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            m_settings = settings;
        }

        #region 场景驱动

        internal UniTask ActivateSceneAsync(FrameworkSceneDescriptor scene)
        {
            if (m_disposed)
            {
                return UniTask.FromException(new ObjectDisposedException(nameof(FrameworkSceneCoordinator)));
            }

            if (m_activeSceneHandle == scene.Handle && m_sceneCancellations.ContainsKey(scene.Handle))
            {
                return UniTask.CompletedTask;
            }

            // 活动场景已经改变，旧场景即使仍以 Additive 方式存在，也不再拥有唯一 SceneScope。
            CancelAllSceneLoads();
            var cancellation = new CancellationTokenSource();
            m_sceneCancellations.Add(scene.Handle, cancellation);
            m_activeSceneHandle = scene.Handle;
            return m_runtime.AttachSceneAsync(scene, m_settings, cancellation.Token);
        }

        internal UniTask DeactivateSceneAsync(ulong sceneHandle)
        {
            if (m_sceneCancellations.TryGetValue(sceneHandle, out var cancellation))
            {
                cancellation.Cancel();
                cancellation.Dispose();
                m_sceneCancellations.Remove(sceneHandle);
            }

            if (m_activeSceneHandle == sceneHandle)
            {
                m_activeSceneHandle = 0;
            }

            // Detach 即使迟到也必须提交；Runtime 会用 Scene Handle 判断它是否仍拥有当前 Scope。
            return m_runtime.DetachSceneAsync(sceneHandle);
        }

        #endregion

        #region 清理

        internal void PrepareForShutdown()
        {
            CancelAllSceneLoads();
            m_activeSceneHandle = 0;
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            PrepareForShutdown();
        }

        private void CancelAllSceneLoads()
        {
            foreach (var pair in m_sceneCancellations)
            {
                pair.Value.Cancel();
                pair.Value.Dispose();
            }

            m_sceneCancellations.Clear();
        }

        #endregion
    }
}
