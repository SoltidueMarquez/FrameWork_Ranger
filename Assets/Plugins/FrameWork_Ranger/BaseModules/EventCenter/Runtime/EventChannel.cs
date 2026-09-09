using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrameWork_Ranger.Events
{
    /// <summary>允许中心在卸载时清空不同事件类型的监听引用。</summary>
    [FrameworkArchitecture("事件通道清理契约", "为中心统一释放强类型监听提供入口。",
        FrameworkArchitectureLayer.Contracts, 10)]
    internal interface IEventChannel
    {
        void Clear();
    }

    /// <summary>管理一个精确类型的有序监听；嵌套派发分别保留自己的进入边界。</summary>
    [FrameworkArchitecture("事件监听通道", "处理回调去重、即时注销、延后新增和逐监听异常隔离。",
        FrameworkArchitectureLayer.RuntimeDriving, 20, typeof(EventBase))]
    internal sealed class EventChannel<T> : IEventChannel where T : EventBase
    {
        private readonly List<Action<T>> m_listeners = new List<Action<T>>();
        private readonly Dictionary<Action<T>, int> m_indices = new Dictionary<Action<T>, int>();
        private int m_dispatchDepth;

        internal bool IsEmpty => m_indices.Count == 0 && m_dispatchDepth == 0;

        internal void Subscribe(Action<T> callback)
        {
            if (m_indices.ContainsKey(callback)) return;
            m_indices.Add(callback, m_listeners.Count);
            m_listeners.Add(callback);
        }

        internal void Unsubscribe(Action<T> callback)
        {
            if (!m_indices.TryGetValue(callback, out var index)) return;
            m_indices.Remove(callback);
            m_listeners[index] = null;
            if (m_dispatchDepth == 0) Compact();
        }

        internal void Dispatch(T payload)
        {
            var boundary = m_listeners.Count;
            m_dispatchDepth++;
            try
            {
                for (var i = 0; i < boundary; i++)
                {
                    var callback = m_listeners[i];
                    if (callback == null) continue;
                    try
                    {
                        callback(payload);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(new InvalidOperationException(
                            $"EventModule Global 派发 {typeof(T).FullName}，监听 " +
                            $"{callback.Method.DeclaringType?.FullName}.{callback.Method.Name} 失败。", exception));
                    }
                }
            }
            finally
            {
                m_dispatchDepth--;
                if (m_dispatchDepth == 0) Compact();
            }
        }

        public void Clear()
        {
            m_indices.Clear();
            // 活动遍历仍需访问旧下标，清除委托但保留槽位至最外层返回。
            if (m_dispatchDepth == 0) m_listeners.Clear();
            else for (var i = 0; i < m_listeners.Count; i++) m_listeners[i] = null;
        }

        private void Compact()
        {
            if (m_indices.Count == m_listeners.Count) return;
            var write = 0;
            for (var read = 0; read < m_listeners.Count; read++)
            {
                var callback = m_listeners[read];
                if (callback == null) continue;
                m_listeners[write] = callback;
                m_indices[callback] = write++;
            }
            m_listeners.RemoveRange(write, m_listeners.Count - write);
        }
    }
}
