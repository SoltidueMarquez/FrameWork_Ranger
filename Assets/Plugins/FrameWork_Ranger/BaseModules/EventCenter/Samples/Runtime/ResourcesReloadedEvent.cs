using System;

namespace FrameWork_Ranger.Events.Samples
{
    /// <summary>无载荷的重载完成通知示例；不依赖或操作资源模块。</summary>
    public sealed class ResourcesReloadedEvent : EventBase
    {
        /// <summary>中心加载后在主线程订阅通知。</summary>
        public static void Subscribe(Action<ResourcesReloadedEvent> callback)
        {
            Framework.GetModule<EventModule>().Subscribe(callback);
        }

        /// <summary>显式结束监听，卸载后允许幂等清理。</summary>
        public static void Unsubscribe(Action<ResourcesReloadedEvent> callback)
        {
            if (Framework.TryGetModule<EventModule>(out var module)) module.Unsubscribe(callback);
        }

        /// <summary>同步发送无载荷通知，同样完成引用池借还。</summary>
        public static void Throw() { Framework.GetModule<EventModule>().Publish<ResourcesReloadedEvent>(); }

        /// <summary>此通知不保存业务数据，无需清理。</summary>
        public override void OnReturn() { }
    }
}
