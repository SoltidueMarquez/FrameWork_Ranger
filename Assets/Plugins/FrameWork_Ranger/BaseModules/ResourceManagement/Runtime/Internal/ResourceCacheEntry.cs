using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.ResourceManagement
{
    /// <summary>
    /// 保存一份底层加载结果、后端所有权句柄和全部活动 Lease。
    /// </summary>
    [FrameworkArchitecture(
        "资源缓存条目",
        "独占一份后端加载结果，创建并跟踪 Lease，在最后引用归还后释放后端句柄。",
        FrameworkArchitectureLayer.GraphAndScope,
        130,
        typeof(ResourceCacheKey),
        typeof(ResourceLeaseState))]
    internal sealed class ResourceCacheEntry
    {
        private readonly HashSet<ResourceLeaseState> m_leases = new HashSet<ResourceLeaseState>();
        private Object m_asset;
        private IDisposable m_backendHandle;

        internal ResourceCacheKey CacheKey { get; }

        internal string ProviderName { get; }

        internal bool IsValid => m_asset != null && m_backendHandle != null;

        internal int RefCount => m_leases.Count;

        internal Object Asset => IsValid
            ? m_asset
            : throw new ObjectDisposedException(nameof(ResourceCacheEntry));

        internal ResourceCacheEntry(
            ResourceCacheKey cacheKey,
            string providerName,
            Object asset,
            IDisposable backendHandle)
        {
            CacheKey = cacheKey;
            ProviderName = providerName;
            m_asset = asset != null ? asset : throw new ArgumentNullException(nameof(asset));
            m_backendHandle = backendHandle ?? throw new ArgumentNullException(nameof(backendHandle));
        }

        internal ResourceLease<T> CreateLease<T>(ResourceStore store) where T : Object
        {
            if (!(Asset is T))
            {
                throw new InvalidCastException(
                    $"缓存资源 {CacheKey} 的实际类型 {Asset.GetType().FullName} 不能转换为 {typeof(T).FullName}。" );
            }

            var state = new ResourceLeaseState(store, this, CacheKey.ResourceKey);
            m_leases.Add(state);
            return new ResourceLease<T>(state);
        }

        internal bool RemoveLease(ResourceLeaseState state)
        {
            if (!m_leases.Remove(state))
            {
                return m_leases.Count == 0;
            }

            state.Invalidate();
            return m_leases.Count == 0;
        }

        internal IReadOnlyList<ResourceLeaseState> GetLeaseSnapshot()
        {
            return new List<ResourceLeaseState>(m_leases);
        }

        internal Exception InvalidateAndDispose()
        {
            var leases = GetLeaseSnapshot();
            for (var i = 0; i < leases.Count; i++)
            {
                leases[i].Invalidate();
            }

            m_leases.Clear();
            m_asset = null;
            var handle = m_backendHandle;
            m_backendHandle = null;
            if (handle == null)
            {
                return null;
            }

            try
            {
                handle.Dispose();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
