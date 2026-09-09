using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FrameWork_Ranger.Events.Samples
{
    /// <summary>挂到已接入框架启动的场景对象上，启用后监听，停用时取消等待并注销。</summary>
    public sealed class EventCenterSampleView : MonoBehaviour
    {
        private CancellationTokenSource m_enableCancellation;
        private int m_level;

        /// <summary>本组件累计收到的等级通知数量，停用后应保持不变。</summary>
        public int ReceivedLevelChanges { get; private set; }
        /// <summary>本组件累计收到的重载通知数量。</summary>
        public int ReceivedReloads { get; private set; }
        /// <summary>是否已完成本次启用周期的订阅。</summary>
        public bool IsListening { get; private set; }

        private void OnEnable()
        {
            m_enableCancellation = new CancellationTokenSource();
            StartListeningAsync(m_enableCancellation.Token).Forget();
        }

        private void OnDisable()
        {
            m_enableCancellation?.Cancel();
            m_enableCancellation?.Dispose();
            m_enableCancellation = null;
            LevelChangedEvent.Unsubscribe(OnLevelChanged);
            ResourcesReloadedEvent.Unsubscribe(OnResourcesReloaded);
            IsListening = false;
        }

        private async UniTask StartListeningAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Framework.WhenReadyAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                LevelChangedEvent.Subscribe(OnLevelChanged);
                ResourcesReloadedEvent.Subscribe(OnResourcesReloaded);
                IsListening = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        /// <summary>在 Play Mode 就绪后通过组件菜单触发一次等级变化。</summary>
        [ContextMenu("发送等级变化")]
        public void SendLevelChange()
        {
            var previous = m_level++;
            LevelChangedEvent.Throw(gameObject.name, previous, m_level);
        }

        /// <summary>在 Play Mode 就绪后通过组件菜单触发一次重载完成通知。</summary>
        [ContextMenu("发送重载完成")]
        public void SendReload() { ResourcesReloadedEvent.Throw(); }

        private void OnLevelChanged(LevelChangedEvent payload)
        {
            ReceivedLevelChanges++;
            // 只读取业务数据，不保存载荷引用用于下一帧。
            Debug.Log($"{payload.EntityId} 等级 {payload.PreviousLevel} → {payload.CurrentLevel}", this);
        }

        private void OnResourcesReloaded(ResourcesReloadedEvent payload)
        {
            ReceivedReloads++;
            Debug.Log("收到重载完成通知", this);
        }
    }
}
