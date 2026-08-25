using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FrameWork_Ranger
{
    /// <summary>
    /// Framework 装配流程的多态扩展点。派生类可以在固定步骤前后执行附加逻辑，
    /// 但排序、状态迁移、失败回滚和所有权仍由 Runtime 控制。
    /// </summary>
    [FrameworkArchitecture(
        "框架驱动扩展基类",
        "提供 Scope 与 Module 加载/卸载前后的受限异步钩子。",
        FrameworkArchitectureLayer.RuntimeDriving,
        10,
        typeof(FrameworkDriverContext))]
    [Serializable]
    public abstract class FrameworkDriverHandlerBase
    {
        [NonSerialized]
        private FrameworkDriverContext m_context;

        /// <summary>
        /// 获取 Framework 只读驱动上下文。
        /// </summary>
        protected FrameworkDriverContext Context => m_context ?? throw new InvalidOperationException(
            "FrameworkDriverHandler 尚未绑定运行上下文。");

        #region 可重写钩子

        /// <summary>
        /// 在一个 Scope 开始加载、任何模块尚未加载时调用。
        /// </summary>
        protected virtual UniTask OnBeforeScopeLoadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在一个 Scope 的全部模块加载成功后调用。
        /// </summary>
        protected virtual UniTask OnAfterScopeLoadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在单个 Module 加载前调用。
        /// </summary>
        protected virtual UniTask OnBeforeModuleLoadAsync(
            ModuleBase module,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在单个 Module 成功加载后调用。
        /// </summary>
        protected virtual UniTask OnAfterModuleLoadAsync(
            ModuleBase module,
            CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在一个 Scope 停止 Tick、开始卸载时调用。卸载流程不可取消。
        /// </summary>
        protected virtual UniTask OnBeforeScopeUnloadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在一个 Scope 的模块完成卸载后调用。
        /// </summary>
        protected virtual UniTask OnAfterScopeUnloadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在单个 Module 卸载前调用。
        /// </summary>
        protected virtual UniTask OnBeforeModuleUnloadAsync(ModuleBase module)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 在单个 Module 完成卸载尝试后调用，即使 Module 卸载失败也会执行。
        /// </summary>
        protected virtual UniTask OnAfterModuleUnloadAsync(ModuleBase module)
        {
            return UniTask.CompletedTask;
        }

        #endregion

        #region Runtime 桥接

        internal void BindRuntime(FrameworkDriverContext context)
        {
            m_context = context;
        }

        internal void ReleaseRuntime()
        {
            m_context = null;
        }

        internal UniTask BeforeScopeLoadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules,
            CancellationToken cancellationToken)
        {
            return OnBeforeScopeLoadAsync(scopeKind, modules, cancellationToken);
        }

        internal UniTask AfterScopeLoadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules,
            CancellationToken cancellationToken)
        {
            return OnAfterScopeLoadAsync(scopeKind, modules, cancellationToken);
        }

        internal UniTask BeforeModuleLoadAsync(ModuleBase module, CancellationToken cancellationToken)
        {
            return OnBeforeModuleLoadAsync(module, cancellationToken);
        }

        internal UniTask AfterModuleLoadAsync(ModuleBase module, CancellationToken cancellationToken)
        {
            return OnAfterModuleLoadAsync(module, cancellationToken);
        }

        internal UniTask BeforeScopeUnloadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules)
        {
            return OnBeforeScopeUnloadAsync(scopeKind, modules);
        }

        internal UniTask AfterScopeUnloadAsync(
            ModuleScopeKind scopeKind,
            IReadOnlyList<ModuleBase> modules)
        {
            return OnAfterScopeUnloadAsync(scopeKind, modules);
        }

        internal UniTask BeforeModuleUnloadAsync(ModuleBase module)
        {
            return OnBeforeModuleUnloadAsync(module);
        }

        internal UniTask AfterModuleUnloadAsync(ModuleBase module)
        {
            return OnAfterModuleUnloadAsync(module);
        }

        #endregion
    }
}
