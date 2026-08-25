using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 将稳定 Module 门面与可替换 Handler 实现组合起来的模块基类。
    /// </summary>
    /// <typeparam name="THandler">Odin 在模板资产中保存的 Handler 抽象基类或具体类型。</typeparam>
    [FrameworkArchitecture(
        "Handler 模块基类",
        "让 Module 作为门面并把生命周期固定转发给 Odin 多态 Handler。",
        FrameworkArchitectureLayer.ModuleModel,
        30,
        typeof(ModuleHandlerBase))]
    public abstract class HandlerModuleBase<THandler> : ModuleBase where THandler : ModuleHandlerBase
    {
        #region Inspector 配置

        [OdinSerialize]
        [HideLabel]
        [InlineProperty]
        private THandler m_handler;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取当前 Module 内嵌的 Handler。该属性只对派生 Module 开放，业务调用者只能使用 Module 门面。
        /// </summary>
        protected THandler Handler => m_handler;

        #endregion

        #region 框架生命周期

        protected sealed override UniTask ExecuteLoadAsync(CancellationToken cancellationToken)
        {
            m_handler.BindRuntime(this, Context);
            return m_handler.LoadAsync(cancellationToken);
        }

        protected sealed override UniTask ExecuteUnloadAsync()
        {
            return m_handler.UnloadAsync();
        }

        protected override void OnReleaseRuntime()
        {
            m_handler?.ReleaseRuntime();
        }

        #endregion

        #region 内部实现

        internal override object GetAdditionalTickTarget()
        {
            return m_handler;
        }

        internal override bool ValidateTemplate(out string error)
        {
            if (m_handler != null)
            {
                error = null;
                return true;
            }

            error = $"Handler 模块 {GetType().FullName} 没有配置 Handler。";
            return false;
        }

        internal void SetHandler(THandler handler)
        {
            m_handler = handler;
        }

        #endregion
    }
}
