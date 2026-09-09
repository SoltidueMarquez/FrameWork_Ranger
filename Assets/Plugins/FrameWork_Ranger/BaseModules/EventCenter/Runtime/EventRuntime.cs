using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Pooling.Reference;

namespace FrameWork_Ranger.Events
{
    /// <summary>一个模块克隆拥有的发送容器；负责载荷借还和卸载期间的发送收尾。</summary>
    [FrameworkArchitecture("事件运行容器", "连接类型通道与引用池，等待活动发送归还后结束关闭。",
        FrameworkArchitectureLayer.RuntimeDriving, 30, typeof(EventChannel<>), typeof(ReferencePoolModule))]
    internal sealed class EventRuntime
    {
        private readonly ReferencePoolModule m_pool;
        private readonly int m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
        private readonly Dictionary<Type, IEventChannel> m_channels = new Dictionary<Type, IEventChannel>();
        private bool m_acceptingRequests = true;
        private int m_activePublishes;
        private UniTaskCompletionSource m_drained;

        internal EventRuntime(ReferencePoolModule pool) { m_pool = pool; }

        internal void Subscribe<T>(Action<T> callback) where T : EventBase
        {
            AssertReady();
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (!m_channels.TryGetValue(typeof(T), out var channel))
            {
                channel = new EventChannel<T>();
                m_channels.Add(typeof(T), channel);
            }
            ((EventChannel<T>)channel).Subscribe(callback);
        }

        internal void Unsubscribe<T>(Action<T> callback) where T : EventBase
        {
            AssertMainThread();
            if (callback == null || !m_channels.TryGetValue(typeof(T), out var channel)) return;
            var typed = (EventChannel<T>)channel;
            typed.Unsubscribe(callback);
            if (typed.IsEmpty) m_channels.Remove(typeof(T));
        }

        internal void Publish<T>(Action<T> initialize) where T : EventBase, new()
        {
            AssertReady();
            // 必须覆盖 Rent/OnRent 和 Return/OnReturn：这些业务钩子也可能发起关闭。
            m_activePublishes++;
            try
            {
                var payload = m_pool.Rent<T>();
                Exception initializationError = null;
                try
                {
                    initialize?.Invoke(payload);
                    if (m_acceptingRequests && m_channels.TryGetValue(typeof(T), out var channel))
                    {
                        var typed = (EventChannel<T>)channel;
                        typed.Dispatch(payload);
                        if (typed.IsEmpty) m_channels.Remove(typeof(T));
                    }
                }
                catch (Exception exception)
                {
                    initializationError = exception;
                    throw;
                }
                finally
                {
                    try { m_pool.Return(payload); }
                    catch (Exception cleanupError)
                    {
                        if (initializationError != null)
                            throw new AggregateException("事件初始化与归还均失败。", initializationError, cleanupError);
                        throw;
                    }
                }
            }
            finally
            {
                m_activePublishes--;
                if (m_activePublishes == 0) m_drained?.TrySetResult();
            }
        }

        internal UniTask ShutdownAsync()
        {
            AssertMainThread();
            m_acceptingRequests = false;
            foreach (var channel in m_channels.Values) channel.Clear();
            m_channels.Clear();
            if (m_activePublishes == 0) return UniTask.CompletedTask;
            m_drained ??= new UniTaskCompletionSource();
            return m_drained.Task;
        }

        private void AssertReady()
        {
            AssertMainThread();
            if (!m_acceptingRequests) throw new InvalidOperationException("EventModule 正在卸载或已卸载。");
        }

        private void AssertMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != m_mainThreadId)
                throw new InvalidOperationException("EventModule 只能从 Unity 主线程调用。");
        }
    }
}
