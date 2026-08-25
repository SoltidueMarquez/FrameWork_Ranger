using System;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.ResourceManagement
{
    /// <summary>
    /// 由引用类型保存单份 Lease 的状态，避免值类型复制导致引用计数重复归还。
    /// </summary>
    [FrameworkArchitecture(
        "资源租约状态",
        "保存单份 Lease 的活动状态与主线程约束，并把释放请求归还给 ResourceStore。",
        FrameworkArchitectureLayer.GraphAndScope,
        140,
        typeof(ResourceStore),
        typeof(ResourceCacheEntry))]
    internal sealed class ResourceLeaseState
    {
        private ResourceStore m_store;
        private ResourceCacheEntry m_entry;
        private readonly int m_mainThreadId;
        private bool m_active;

        internal ResourceKey Key { get; }

        internal bool IsValid
        {
            get
            {
                EnsureMainThread();
                return m_active && m_entry != null && m_entry.IsValid;
            }
        }

        internal ResourceLeaseState(
            ResourceStore store,
            ResourceCacheEntry entry,
            ResourceKey key)
        {
            m_store = store ?? throw new ArgumentNullException(nameof(store));
            m_entry = entry ?? throw new ArgumentNullException(nameof(entry));
            m_mainThreadId = store.MainThreadId;
            Key = key;
            m_active = true;
        }

        internal Object GetValue()
        {
            EnsureMainThread();
            if (!IsValid)
            {
                throw new ObjectDisposedException(
                    nameof(ResourceLeaseState),
                    $"资源 Lease 已释放或所属 ResourceModule 已关闭：{Key}");
            }

            return m_entry.Asset;
        }

        internal void Release()
        {
            EnsureMainThread();
            if (!m_active)
            {
                return;
            }

            m_store.ReleaseLease(this, m_entry);
        }

        internal void Invalidate()
        {
            m_active = false;
            m_store = null;
            m_entry = null;
        }

        private void EnsureMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != m_mainThreadId)
            {
                throw new InvalidOperationException("ResourceLease 只能在创建它的 Unity 主线程上访问。");
            }
        }
    }
}
