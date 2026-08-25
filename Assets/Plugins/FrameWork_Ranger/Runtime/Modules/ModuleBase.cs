using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 所有 Framework 模块模板与运行克隆的共同基类。
    /// 模板只保存设计时配置；只有 Runtime 创建的克隆才允许进入生命周期。
    /// </summary>
    [FrameworkArchitecture(
        "模块基类",
        "定义模块模板配置、运行克隆状态、依赖声明和生命周期桥接。",
        FrameworkArchitectureLayer.ModuleModel,
        0,
        typeof(ModuleContext))]
    public abstract class ModuleBase : SerializedScriptableObject
    {
        private static readonly IReadOnlyList<Type> EmptyDependencies = Array.Empty<Type>();

        #region Inspector 配置

        [SerializeField]
        [Tooltip("数值越小，在同一依赖层中越早加载。")]
        private int m_loadPriority;

        #endregion

        #region 运行时状态

        [NonSerialized]
        private ModuleContext m_context;

        [NonSerialized]
        private ModuleBase m_template;

        [NonSerialized]
        private ModuleScopeKind m_scopeKind;

        [NonSerialized]
        private ModuleLifecycleState m_state;

        [NonSerialized]
        private bool m_isRuntimeInstance;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取同一拓扑就绪层中的加载优先级。数值越小越早加载。
        /// </summary>
        public int LoadPriority => m_loadPriority;

        /// <summary>
        /// 获取当前运行克隆的生命周期状态。模板资产始终返回 None。
        /// </summary>
        public ModuleLifecycleState State => m_state;

        /// <summary>
        /// 获取运行克隆所属作用域。读取模板时此值没有运行语义，应先检查 <see cref="IsRuntimeInstance"/>。
        /// </summary>
        public ModuleScopeKind ScopeKind => m_scopeKind;

        /// <summary>
        /// 获取当前对象是否为 Framework 托管的运行克隆。
        /// </summary>
        public bool IsRuntimeInstance => m_isRuntimeInstance;

        /// <summary>
        /// 获取模块运行上下文。只允许派生模块在 Load 成功路径、Tick 与 Unload 中使用。
        /// </summary>
        protected ModuleContext Context => m_context ?? throw new InvalidOperationException(
            $"模块 {GetType().FullName} 尚未绑定运行上下文。");

        /// <summary>
        /// 获取本模块必须先加载的具体 Module 类型。派生类型应返回稳定的只读集合，避免每次查询重新分配。
        /// </summary>
        protected virtual IReadOnlyList<Type> RequiredModuleTypes => EmptyDependencies;

        #endregion

        #region 框架生命周期

        internal void BindRuntime(ModuleBase template, ModuleContext context, ModuleScopeKind scopeKind)
        {
            m_template = template;
            m_context = context;
            m_scopeKind = scopeKind;
            m_state = ModuleLifecycleState.Created;
            m_isRuntimeInstance = true;
        }

        internal async UniTask LoadAsync(System.Threading.CancellationToken cancellationToken)
        {
            if (m_state != ModuleLifecycleState.Created)
            {
                throw new InvalidOperationException(
                    $"模块 {GetType().FullName} 只能从 Created 状态加载，当前状态为 {m_state}。");
            }

            m_state = ModuleLifecycleState.Loading;
            try
            {
                await ExecuteLoadAsync(cancellationToken);
                m_state = ModuleLifecycleState.Loaded;
            }
            catch
            {
                m_state = ModuleLifecycleState.Failed;
                throw;
            }
        }

        internal async UniTask UnloadAsync()
        {
            if (m_state != ModuleLifecycleState.Loaded)
            {
                throw new InvalidOperationException(
                    $"模块 {GetType().FullName} 只能从 Loaded 状态卸载，当前状态为 {m_state}。");
            }

            m_state = ModuleLifecycleState.Unloading;
            try
            {
                await ExecuteUnloadAsync();
                m_state = ModuleLifecycleState.Unloaded;
            }
            catch
            {
                m_state = ModuleLifecycleState.Failed;
                throw;
            }
        }

        internal void ReleaseRuntime()
        {
            OnReleaseRuntime();
            m_context = null;
            m_template = null;
            m_isRuntimeInstance = false;
        }

        protected abstract UniTask ExecuteLoadAsync(System.Threading.CancellationToken cancellationToken);

        protected abstract UniTask ExecuteUnloadAsync();

        /// <summary>
        /// 在 Runtime 即将销毁克隆前释放 Module/Handler 之间的非序列化引用。
        /// </summary>
        protected virtual void OnReleaseRuntime()
        {
        }

        #endregion

        #region 内部实现

        internal IReadOnlyList<Type> GetRequiredModuleTypes()
        {
            return RequiredModuleTypes ?? EmptyDependencies;
        }

        internal virtual object GetAdditionalTickTarget()
        {
            return null;
        }

        internal virtual bool ValidateTemplate(out string error)
        {
            error = null;
            return true;
        }

        internal ModuleBase GetTemplate()
        {
            return m_template;
        }

        internal void SetLoadPriority(int loadPriority)
        {
            m_loadPriority = loadPriority;
        }

        #endregion
    }
}
