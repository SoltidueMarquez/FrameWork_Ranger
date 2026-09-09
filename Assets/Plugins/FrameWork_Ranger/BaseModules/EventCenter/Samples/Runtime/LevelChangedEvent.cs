using System;

namespace FrameWork_Ranger.Events.Samples
{
    /// <summary>等级变化示例：一个事件一个文件，静态入口隐藏泛型发送和借还细节。</summary>
    public sealed class LevelChangedEvent : EventBase
    {
        /// <summary>发生变化的业务实体标识。</summary>
        public string EntityId { get; private set; }
        /// <summary>变化前等级。</summary>
        public int PreviousLevel { get; private set; }
        /// <summary>变化后等级。</summary>
        public int CurrentLevel { get; private set; }

        /// <summary>主线程显式订阅；中心未加载时明确报错。</summary>
        public static void Subscribe(Action<LevelChangedEvent> callback)
        {
            Framework.GetModule<EventModule>().Subscribe(callback);
        }

        /// <summary>成对注销；中心已经卸载时安全结束，不创建中心。</summary>
        public static void Unsubscribe(Action<LevelChangedEvent> callback)
        {
            if (Framework.TryGetModule<EventModule>(out var module)) module.Unsubscribe(callback);
        }

        /// <summary>同步发送等级变化，返回前载荷已归还；监听者只能在回调期间借读。</summary>
        public static void Throw(string entityId, int previousLevel, int currentLevel)
        {
            Framework.GetModule<EventModule>().Publish<LevelChangedEvent>(payload =>
            {
                payload.EntityId = entityId;
                payload.PreviousLevel = previousLevel;
                payload.CurrentLevel = currentLevel;
            });
        }

        /// <summary>由引用池归还时清除数据，避免跨发送保留业务引用。</summary>
        public override void OnReturn()
        {
            EntityId = null;
            PreviousLevel = 0;
            CurrentLevel = 0;
        }
    }
}
