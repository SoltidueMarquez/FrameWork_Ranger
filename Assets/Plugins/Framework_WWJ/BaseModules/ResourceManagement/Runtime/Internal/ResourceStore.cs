using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// ResourceModule 的运行容器，集中管理后端路由、缓存、共享加载和 Lease 所有权。
    /// </summary>
    internal sealed class ResourceStore
    {
        private readonly Dictionary<ResourceBackendKind, ResourceProviderBase> m_providers;
        private readonly Dictionary<ResourceCacheKey, ResourceCacheEntry> m_cache =
            new Dictionary<ResourceCacheKey, ResourceCacheEntry>();
        private readonly Dictionary<ResourceCacheKey, ResourcePendingLoad> m_pending =
            new Dictionary<ResourceCacheKey, ResourcePendingLoad>();
        private readonly int m_mainThreadId;
        private bool m_acceptingRequests = true;

        internal ResourceStore(Dictionary<ResourceBackendKind, ResourceProviderBase> providers)
        {
            m_providers = providers ?? throw new ArgumentNullException(nameof(providers));
            m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        internal int MainThreadId => m_mainThreadId;

        #region 获取与共享加载

        internal async UniTask<ResourceLease<T>> AcquireAsync<T>(
            ResourceKey key,
            CancellationToken cancellationToken)
            where T : Object
        {
            EnsureMainThread();
            if (!m_acceptingRequests)
            {
                throw new InvalidOperationException("ResourceModule 已开始关闭，不再接受新的资源请求。" );
            }

            if (!key.TryValidate(out var keyError))
            {
                throw new ArgumentException(keyError, nameof(key));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var cacheKey = new ResourceCacheKey(key, typeof(T));
            if (m_cache.TryGetValue(cacheKey, out var cachedEntry))
            {
                return cachedEntry.CreateLease<T>(this);
            }

            if (!m_providers.TryGetValue(key.Backend, out var provider))
            {
                throw new InvalidOperationException(
                    $"ResourceModule 没有配置 {key.Backend} Provider，不能加载 {key.Location}。" );
            }

            if (!m_pending.TryGetValue(cacheKey, out var pending))
            {
                pending = new ResourcePendingLoad(cacheKey, provider);
                pending.WaiterCount = 1;
                m_pending.Add(cacheKey, pending);
                pending.ProviderTask = RunProviderLoadAsync<T>(pending);
            }
            else
            {
                pending.WaiterCount++;
            }

            return await AwaitPendingAsync<T>(pending, cancellationToken);
        }

        private async UniTask<ResourceLease<T>> AwaitPendingAsync<T>(
            ResourcePendingLoad pending,
            CancellationToken cancellationToken)
            where T : Object
        {
            var cancelledByCaller = false;
            try
            {
                var completionTask = pending.Completion.Task;
                var entry = cancellationToken.CanBeCanceled
                    ? await completionTask.AttachExternalCancellation(cancellationToken)
                    : await completionTask;
                EnsureMainThread();
                return entry.CreateLease<T>(this);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancelledByCaller = true;
                throw;
            }
            finally
            {
                EnsureMainThread();
                FinishWaiter(pending, cancelledByCaller);
            }
        }

        private async UniTask RunProviderLoadAsync<T>(ResourcePendingLoad pending) where T : Object
        {
            IResourceBackendHandle<T> handle = null;
            try
            {
                handle = await pending.Provider.LoadAsync<T>(
                    pending.CacheKey.ResourceKey.Location,
                    pending.ProviderToken);
                EnsureMainThread();

                if (handle == null || !handle.IsValid || handle.Value == null)
                {
                    throw new ResourceLoadException(
                        pending.CacheKey.ResourceKey,
                        typeof(T),
                        pending.Provider.ProviderName,
                        $"Provider {pending.Provider.ProviderName} 未返回有效资源：{pending.CacheKey}." );
                }

                if (pending.IsAbandoned || !IsCurrentPending(pending))
                {
                    return;
                }

                var entry = new ResourceCacheEntry(
                    pending.CacheKey,
                    pending.Provider.ProviderName,
                    handle.Value,
                    handle);
                handle = null;
                pending.CompletedEntry = entry;
                m_cache.Add(pending.CacheKey, entry);
                m_pending.Remove(pending.CacheKey);
                pending.Completion.TrySetResult(entry);
            }
            catch (OperationCanceledException) when (pending.ProviderToken.IsCancellationRequested)
            {
                pending.Completion.TrySetCanceled(pending.ProviderToken);
            }
            catch (Exception exception)
            {
                var loadException = exception as ResourceLoadException ?? new ResourceLoadException(
                    pending.CacheKey.ResourceKey,
                    typeof(T),
                    pending.Provider.ProviderName,
                    $"资源加载失败：{pending.CacheKey}，Provider={pending.Provider.ProviderName}。",
                    exception);
                pending.Completion.TrySetException(loadException);
            }
            finally
            {
                EnsureMainThread();
                handle?.Dispose();
                pending.ProviderFinished = true;
                if (IsCurrentPending(pending))
                {
                    m_pending.Remove(pending.CacheKey);
                }

                TryDisposePending(pending);
            }
        }

        private void FinishWaiter(ResourcePendingLoad pending, bool cancelledByCaller)
        {
            if (pending.WaiterCount > 0)
            {
                pending.WaiterCount--;
            }

            if (cancelledByCaller && pending.WaiterCount == 0 && !pending.ProviderFinished)
            {
                if (IsCurrentPending(pending))
                {
                    m_pending.Remove(pending.CacheKey);
                }

                pending.Abandon();
            }

            if (pending.WaiterCount == 0 &&
                pending.CompletedEntry != null &&
                pending.CompletedEntry.RefCount == 0)
            {
                ReleaseEntry(pending.CompletedEntry);
            }

            TryDisposePending(pending);
        }

        private bool IsCurrentPending(ResourcePendingLoad pending)
        {
            return m_pending.TryGetValue(pending.CacheKey, out var current) &&
                   ReferenceEquals(current, pending);
        }

        private static void TryDisposePending(ResourcePendingLoad pending)
        {
            if (pending.ProviderFinished && pending.WaiterCount == 0)
            {
                pending.Dispose();
            }
        }

        #endregion

        #region Lease 与关闭

        internal void ReleaseLease(ResourceLeaseState state, ResourceCacheEntry entry)
        {
            EnsureMainThread();
            if (entry.RemoveLease(state))
            {
                ReleaseEntry(entry);
            }
        }

        private void ReleaseEntry(ResourceCacheEntry entry)
        {
            if (m_cache.TryGetValue(entry.CacheKey, out var current) && ReferenceEquals(current, entry))
            {
                m_cache.Remove(entry.CacheKey);
            }

            var exception = entry.InvalidateAndDispose();
            if (exception != null)
            {
                throw new InvalidOperationException($"释放资源失败：{entry.CacheKey}。", exception);
            }
        }

        internal async UniTask ShutdownAsync()
        {
            EnsureMainThread();
            if (!m_acceptingRequests && m_pending.Count == 0 && m_cache.Count == 0)
            {
                return;
            }

            m_acceptingRequests = false;
            var pendingLoads = new List<ResourcePendingLoad>(m_pending.Values);
            m_pending.Clear();
            for (var i = 0; i < pendingLoads.Count; i++)
            {
                pendingLoads[i].Abandon();
            }

            for (var i = 0; i < pendingLoads.Count; i++)
            {
                try
                {
                    await pendingLoads[i].ProviderTask;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            EnsureMainThread();
            var disposeErrors = new List<Exception>();
            var entries = new List<ResourceCacheEntry>(m_cache.Values);
            m_cache.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.RefCount > 0)
                {
                    Debug.LogWarning(
                        $"[Framework_WWJ] ResourceModule 关闭时仍有 {entry.RefCount} 份 Lease 未归还：" +
                        $"{entry.CacheKey}，Provider={entry.ProviderName}。" );
                }

                var disposeError = entry.InvalidateAndDispose();
                if (disposeError != null)
                {
                    disposeErrors.Add(disposeError);
                }
            }

            if (disposeErrors.Count > 0)
            {
                throw new AggregateException("ResourceModule 关闭时有后端句柄释放失败。", disposeErrors);
            }
        }

        #endregion

        #region 诊断与线程约束

        internal ResourceDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            EnsureMainThread();
            var backends = new List<ResourceBackendDiagnostic>(m_providers.Count);
            foreach (var pair in m_providers)
            {
                backends.Add(new ResourceBackendDiagnostic(pair.Key, pair.Value.ProviderName));
            }

            backends.Sort((left, right) => left.Backend.CompareTo(right.Backend));
            var entries = new List<ResourceEntryDiagnostic>(m_cache.Count + m_pending.Count);
            var leaseCount = 0;
            foreach (var pair in m_cache)
            {
                leaseCount += pair.Value.RefCount;
                entries.Add(new ResourceEntryDiagnostic(
                    pair.Key.ResourceKey,
                    pair.Key.AssetType,
                    pair.Value.ProviderName,
                    pair.Value.RefCount,
                    0,
                    false));
            }

            foreach (var pair in m_pending)
            {
                entries.Add(new ResourceEntryDiagnostic(
                    pair.Key.ResourceKey,
                    pair.Key.AssetType,
                    pair.Value.Provider.ProviderName,
                    0,
                    pair.Value.WaiterCount,
                    true));
            }

            entries.Sort((left, right) => string.Compare(
                left.Key.ToString(),
                right.Key.ToString(),
                StringComparison.Ordinal));
            return new ResourceDiagnosticsSnapshot(
                m_acceptingRequests,
                m_cache.Count,
                m_pending.Count,
                leaseCount,
                backends,
                entries);
        }

        internal void EnsureMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != m_mainThreadId)
            {
                throw new InvalidOperationException("ResourceModule 只能在创建它的 Unity 主线程上访问。" );
            }
        }

        #endregion
    }
}
