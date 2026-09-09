using FrameWork_Ranger.Pooling.Reference;

namespace FrameWork_Ranger.Events
{
    /// <summary>同步事件的池化载荷；监听方只能在回调期间借读，延后处理须复制数据。</summary>
    [FrameworkArchitecture("事件载荷", "定义事件引用池借还契约，由具体事件清空业务数据。",
        FrameworkArchitectureLayer.Contracts, 0, typeof(IReferencePoolItem))]
    public abstract class EventBase : IReferencePoolItem
    {
        /// <summary>引用池借出时调用；默认无需准备，异常由发送方接收。</summary>
        public virtual void OnRent() { }

        /// <summary>发送结束后清空业务数据和引用；由中心归还时调用，监听方不得自行调用。</summary>
        public abstract void OnReturn();
    }
}
