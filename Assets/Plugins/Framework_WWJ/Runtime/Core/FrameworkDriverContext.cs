using System;

namespace Framework_WWJ
{
    /// <summary>
    /// Framework DriverHandler 使用的只读运行上下文。它允许观察状态和读取模块，
    /// 但不提供修改排序、Scope 或注册表的入口。
    /// </summary>
    [FrameworkArchitecture(
        "框架驱动上下文",
        "向 DriverHandler 提供受控状态与已加载模块查询。",
        FrameworkArchitectureLayer.RuntimeDriving,
        20,
        typeof(FrameworkRuntime))]
    public sealed class FrameworkDriverContext
    {
        private readonly FrameworkRuntime m_runtime;

        /// <summary>
        /// 获取 Framework 当前状态。
        /// </summary>
        public FrameworkState State => m_runtime.State;

        internal FrameworkDriverContext(FrameworkRuntime runtime)
        {
            m_runtime = runtime;
        }

        /// <summary>
        /// 获取一个已经加载的具体 Module 类型。
        /// </summary>
        public T GetModule<T>() where T : ModuleBase
        {
            if (TryGetModule<T>(out var module))
            {
                return module;
            }

            throw new InvalidOperationException($"Framework 中不存在已加载的 {typeof(T).FullName}。");
        }

        /// <summary>
        /// 尝试获取一个已经加载的具体 Module 类型。
        /// </summary>
        public bool TryGetModule<T>(out T module) where T : ModuleBase
        {
            if (m_runtime.TryGetModule(typeof(T), out var result))
            {
                module = (T)result;
                return true;
            }

            module = null;
            return false;
        }
    }
}
