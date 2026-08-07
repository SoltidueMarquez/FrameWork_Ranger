using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework_WWJ
{
    /// <summary>
    /// Framework_WWJ 面向业务代码的静态只读门面。它提供状态、就绪等待和按具体类型查询，
    /// 但不允许业务代码修改模块注册表或直接驱动生命周期。
    /// </summary>
    [FrameworkArchitecture(
        "Framework 静态门面",
        "向业务暴露只读状态、就绪等待、模块查询与显式关停。",
        FrameworkArchitectureLayer.PublicFacade,
        0,
        typeof(FrameworkRuntime),
        typeof(FrameworkRuntimeHost))]
    public static class Framework
    {
        #region 运行时状态

        private static FrameworkRuntime s_runtime;
        private static FrameworkRuntimeHost s_host;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取 Framework 当前状态。尚未创建 Runtime 时返回 Uninitialized。
        /// </summary>
        public static FrameworkState State => s_runtime?.State ?? FrameworkState.Uninitialized;

        /// <summary>
        /// 获取 Global 与当前 SceneScope 是否都已完整加载。
        /// </summary>
        public static bool IsReady => s_runtime?.IsReady ?? false;

        /// <summary>
        /// 获取最近一次配置或生命周期操作的原始异常。Tick 隔离异常不会写入此属性。
        /// </summary>
        public static Exception LastException => s_runtime?.LastException;

        #endregion

        #region 公开 API

        /// <summary>
        /// 等待当前装配批次进入 Ready。调用方取消只结束自己的等待，不取消 Framework 加载。
        /// </summary>
        public static UniTask WhenReadyAsync(CancellationToken cancellationToken = default)
        {
            // Shutdown 后仍保留已完成 Runtime 供重复 Shutdown 与状态查询使用。
            // WhenReady 不能因此通过 EnsureRuntime 偷偷创建一个新空 Runtime；只有新的 Entry 才代表明确重启。
            // 这里直接转交旧 Runtime，让调用方收到“框架已关停”的确定性错误。
            if (s_runtime != null &&
                (s_runtime.State == FrameworkState.ShuttingDown || s_runtime.State == FrameworkState.Shutdown))
            {
                return s_runtime.WhenReadyAsync(cancellationToken);
            }

            return EnsureRuntime().WhenReadyAsync(cancellationToken);
        }

        /// <summary>
        /// 获取一个已经加载的具体 Module 类型；不存在时抛出明确错误。
        /// </summary>
        public static T GetModule<T>() where T : ModuleBase
        {
            if (TryGetModule<T>(out var module))
            {
                return module;
            }

            throw new InvalidOperationException($"Framework 中不存在已加载的 {typeof(T).FullName}。" );
        }

        /// <summary>
        /// 尝试获取一个已经加载的具体 Module 类型。
        /// </summary>
        public static bool TryGetModule<T>(out T module) where T : ModuleBase
        {
            if (s_runtime != null && s_runtime.TryGetModule(typeof(T), out var result))
            {
                module = (T)result;
                return true;
            }

            module = null;
            return false;
        }

        /// <summary>
        /// 确定性地卸载 Scene、Global 并销毁 Runtime Host。卸载步骤本身不接受取消。
        /// </summary>
        public static UniTask ShutdownAsync()
        {
            return s_runtime == null ? UniTask.CompletedTask : ShutdownAndReleaseHostAsync(s_runtime);
        }

        #endregion

        #region 场景桥接

        internal static UniTask StartProjectSceneAsync(
            FrameworkProjectSettings settings,
            FrameworkSceneDescriptor scene)
        {
            var runtime = EnsureRuntime();
            if (runtime.State == FrameworkState.ShuttingDown || runtime.State == FrameworkState.Shutdown)
            {
                return UniTask.FromException(new InvalidOperationException(
                    "Framework 已经关停，本次 Play Session 不会自动重新启动。"));
            }

            s_host.Configure(settings);
            return s_host.ActivateSceneAsync(scene);
        }

        internal static UniTask DetachSceneAsync(int sceneHandle)
        {
            return s_runtime == null
                ? UniTask.CompletedTask
                : s_runtime.DetachSceneAsync(sceneHandle);
        }

        #endregion

        #region 内部实现

        internal static void ResetForSubsystemRegistration()
        {
            // 关闭 Domain Reload 时静态字段不会自动重置，此入口确保每次 Play 都从干净门面开始。
            if (s_host != null)
            {
                RuntimeObjectUtility.Destroy(s_host.gameObject);
            }

            s_runtime = null;
            s_host = null;
        }

        private static FrameworkRuntime EnsureRuntime()
        {
            if (s_runtime != null)
            {
                return s_runtime;
            }

            var hostObject = new GameObject("[Framework_WWJ]");
            UnityEngine.Object.DontDestroyOnLoad(hostObject);
            s_host = hostObject.AddComponent<FrameworkRuntimeHost>();
            s_runtime = new FrameworkRuntime();
            s_host.Initialize(s_runtime);
            return s_runtime;
        }

        private static async UniTask ShutdownAndReleaseHostAsync(FrameworkRuntime runtime)
        {
            try
            {
                s_host?.PrepareForShutdown();
                await runtime.ShutdownAsync();
            }
            finally
            {
                if (s_runtime == runtime)
                {
                    RuntimeObjectUtility.Destroy(s_host == null ? null : s_host.gameObject);
                    s_host = null;
                }
            }
        }

        #endregion
    }
}
