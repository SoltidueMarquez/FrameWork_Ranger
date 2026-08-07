using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework_WWJ
{
    /// <summary>
    /// Handler 多态实现的共同基类。Handler 是 Module 运行克隆内部的托管对象，
    /// 不能脱离所属 Module 单独注册或通过 Framework 全局查询。
    /// </summary>
    [FrameworkArchitecture(
        "模块 Handler 基类",
        "承载可替换逻辑并在运行时绑定 Owner Module 与 ModuleContext。",
        FrameworkArchitectureLayer.ModuleModel,
        10,
        typeof(ModuleBase),
        typeof(ModuleContext))]
    [Serializable]
    public abstract class ModuleHandlerBase
    {
        #region 运行时状态

        [NonSerialized]
        private ModuleBase m_owner;

        [NonSerialized]
        private ModuleContext m_context;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取拥有当前 Handler 的 Module 运行克隆。
        /// </summary>
        protected ModuleBase Owner => m_owner ?? throw new InvalidOperationException("Handler 尚未绑定 Module。");

        /// <summary>
        /// 获取与所属 Module 相同的运行上下文。
        /// </summary>
        protected ModuleContext Context => m_context ?? throw new InvalidOperationException("Handler 尚未绑定 ModuleContext。");

        #endregion

        #region 框架生命周期

        /// <summary>
        /// 加载 Handler 的运行逻辑。取消与失败语义和所属 Module 一致。
        /// </summary>
        protected virtual UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 卸载 Handler 已经成功接管的运行资源。
        /// </summary>
        protected virtual UniTask OnUnloadAsync()
        {
            return UniTask.CompletedTask;
        }

        internal void BindRuntime(ModuleBase owner, ModuleContext context)
        {
            m_owner = owner;
            m_context = context;
        }

        internal UniTask LoadAsync(CancellationToken cancellationToken)
        {
            return OnLoadAsync(cancellationToken);
        }

        internal UniTask UnloadAsync()
        {
            return OnUnloadAsync();
        }

        internal void ReleaseRuntime()
        {
            m_owner = null;
            m_context = null;
        }

        #endregion

        /// <summary>
        /// 以明确的 Module 类型取得所属门面；类型不匹配时立即暴露配置或编程错误。
        /// </summary>
        protected TModule GetOwner<TModule>() where TModule : ModuleBase
        {
            return Owner as TModule ?? throw new InvalidCastException(
                $"Handler {GetType().FullName} 的 Owner 不是 {typeof(TModule).FullName}。");
        }
    }
}
