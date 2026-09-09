namespace FrameWork_Ranger.UI
{
    /// <summary>可复用列表项。由控制器绑定数据，移除/结束时解除自身订阅。</summary>
    [FrameworkArchitecture("UI 列表条目", "提供数据绑定与解绑生命周期。", FrameworkArchitectureLayer.Contracts, 135)]
    public class UIItemView : UIView
    {
        public virtual void Bind(object data) { }
        public virtual void Unbind() { }
    }
}
