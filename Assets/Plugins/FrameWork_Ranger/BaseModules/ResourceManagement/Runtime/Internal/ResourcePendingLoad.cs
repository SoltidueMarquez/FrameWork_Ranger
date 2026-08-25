using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FrameWork_Ranger.ResourceManagement
{
    /// <summary>
    /// 协调一个缓存键的共享底层加载与多个独立等待者。
    /// </summary>
    [FrameworkArchitecture(
        "资源合并加载状态",
        "让同一缓存键共享一次 Provider 加载，同时维护独立等待者、取消和完成所有权。",
        FrameworkArchitectureLayer.GraphAndScope,
        150,
        typeof(ResourceCacheKey),
        typeof(ResourceProviderBase),
        typeof(ResourceCacheEntry))]
    internal sealed class ResourcePendingLoad : IDisposable
    {
        private readonly CancellationTokenSource m_providerCancellation = new CancellationTokenSource();

        internal ResourceCacheKey CacheKey { get; }

        internal ResourceProviderBase Provider { get; }

        internal UniTaskCompletionSource<ResourceCacheEntry> Completion { get; } =
            new UniTaskCompletionSource<ResourceCacheEntry>();

        internal CancellationToken ProviderToken => m_providerCancellation.Token;

        internal int WaiterCount { get; set; }

        internal bool IsAbandoned { get; private set; }

        internal bool ProviderFinished { get; set; }

        internal UniTask ProviderTask { get; set; }

        internal ResourceCacheEntry CompletedEntry { get; set; }

        internal bool IsDisposed { get; private set; }

        internal ResourcePendingLoad(ResourceCacheKey cacheKey, ResourceProviderBase provider)
        {
            CacheKey = cacheKey;
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        internal void Abandon()
        {
            if (IsAbandoned)
            {
                return;
            }

            IsAbandoned = true;
            Completion.TrySetCanceled(m_providerCancellation.Token);
            m_providerCancellation.Cancel();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            m_providerCancellation.Dispose();
        }
    }
}
