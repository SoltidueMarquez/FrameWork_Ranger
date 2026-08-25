using System;

namespace FrameWork_Ranger
{
    /// <summary>
    /// Module/Handler 在运行期访问已加载依赖的只读上下文。
    /// 它显式保留调用来源，使 Global 模块无法越过作用域边界访问 Scene 模块。
    /// </summary>
    [FrameworkArchitecture(
        "模块上下文",
        "向运行模块提供遵守 Global/Scene 可见性规则的依赖查询。",
        FrameworkArchitectureLayer.ModuleModel,
        40,
        typeof(ModuleBase))]
    public sealed class ModuleContext
    {
        private readonly FrameworkRuntime m_runtime;
        private readonly ModuleScopeRuntime m_ownerScope;

        /// <summary>
        /// 获取当前 Module 所属的作用域。
        /// </summary>
        public ModuleScopeKind ScopeKind => m_ownerScope.ScopeKind;

        internal ModuleContext(FrameworkRuntime runtime, ModuleScopeRuntime ownerScope)
        {
            m_runtime = runtime;
            m_ownerScope = ownerScope;
        }

        /// <summary>
        /// 取得一个已加载的具体 Module 类型；未找到时抛出包含调用作用域的错误。
        /// </summary>
        public T GetModule<T>() where T : ModuleBase
        {
            if (TryGetModule<T>(out var module))
            {
                return module;
            }

            throw new InvalidOperationException(
                $"{ScopeKind} 模块上下文中不存在已加载的 {typeof(T).FullName}。请确认已经声明依赖并满足作用域方向。" );
        }

        /// <summary>
        /// 尝试取得一个已加载的具体 Module 类型。
        /// </summary>
        public bool TryGetModule<T>(out T module) where T : ModuleBase
        {
            if (m_runtime.TryGetModuleForContext(typeof(T), m_ownerScope, out var result))
            {
                module = (T)result;
                return true;
            }

            module = null;
            return false;
        }
    }
}
