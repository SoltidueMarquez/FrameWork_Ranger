using System;
using System.Collections.Generic;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// Framework Center 与测试按需读取的资源运行状态，不向模板回写任何数据。
    /// </summary>
    internal sealed class ResourceDiagnosticsSnapshot
    {
        internal bool IsAcceptingRequests { get; }

        internal int CacheCount { get; }

        internal int PendingCount { get; }

        internal int LeaseCount { get; }

        internal IReadOnlyList<ResourceBackendDiagnostic> Backends { get; }

        internal IReadOnlyList<ResourceEntryDiagnostic> Entries { get; }

        internal ResourceDiagnosticsSnapshot(
            bool isAcceptingRequests,
            int cacheCount,
            int pendingCount,
            int leaseCount,
            IReadOnlyList<ResourceBackendDiagnostic> backends,
            IReadOnlyList<ResourceEntryDiagnostic> entries)
        {
            IsAcceptingRequests = isAcceptingRequests;
            CacheCount = cacheCount;
            PendingCount = pendingCount;
            LeaseCount = leaseCount;
            Backends = backends;
            Entries = entries;
        }
    }

    internal readonly struct ResourceBackendDiagnostic
    {
        internal ResourceBackendKind Backend { get; }

        internal string ProviderName { get; }

        internal ResourceBackendDiagnostic(ResourceBackendKind backend, string providerName)
        {
            Backend = backend;
            ProviderName = providerName;
        }
    }

    internal readonly struct ResourceEntryDiagnostic
    {
        internal ResourceKey Key { get; }

        internal Type AssetType { get; }

        internal string ProviderName { get; }

        internal int RefCount { get; }

        internal int WaiterCount { get; }

        internal bool IsPending { get; }

        internal ResourceEntryDiagnostic(
            ResourceKey key,
            Type assetType,
            string providerName,
            int refCount,
            int waiterCount,
            bool isPending)
        {
            Key = key;
            AssetType = assetType;
            ProviderName = providerName;
            RefCount = refCount;
            WaiterCount = waiterCount;
            IsPending = isPending;
        }
    }
}
