using System;
namespace FrameWork_Ranger.UI
{
    /// <summary>每个面板实例克隆一份逻辑，模板不保存 View/上下文。</summary>
    [Serializable]
    [FrameworkArchitecture("面板逻辑", "定义独立于组件的面板生命周期。", FrameworkArchitectureLayer.Contracts, 130)]
    public class UILogic
    {
        [NonSerialized] private UIView m_view;
        [NonSerialized] private UIContext m_context;
        protected UIView View => m_view;
        protected UIContext UI => m_context;
        public virtual Type ViewType => typeof(UIView);
        internal void Bind(UIView view, UIContext context) { m_view = view; m_context = context; }
        internal void Unbind() { m_view = null; m_context = null; }
        public virtual void OnCreate() { }
        public virtual void OnOpen(object data) { }
        public virtual void OnRefresh(object data) { }
        public virtual void OnFocus() { }
        public virtual void OnBlur() { }
        public virtual void OnClose() { }
        public virtual void OnDispose() { }
    }
    [Serializable]
    [FrameworkArchitecture("强类型面板逻辑", "约束逻辑所使用的 View 类型。", FrameworkArchitectureLayer.Contracts, 135)]
    public class UILogic<TView> : UILogic where TView : UIView
    {
        protected new TView View => (TView)base.View;
        public override Type ViewType => typeof(TView);
    }
}
