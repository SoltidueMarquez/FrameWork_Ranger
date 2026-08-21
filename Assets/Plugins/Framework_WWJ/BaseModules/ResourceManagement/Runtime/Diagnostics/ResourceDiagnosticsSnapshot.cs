using System;
using System.Collections.Generic;

namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// Framework Center 与测试按需读取的资源运行状态，不向模板回写任何数据。
    /// </summary>
    [FrameworkArchitecture(
        "资源诊断快照",
        "汇总当前 Provider、缓存、Pending 与 Lease 数量，供编辑器和测试只读观察。",
        FrameworkArchitectureLayer.GraphAndScope,
        190,
        typeof(ResourceStore),
        typeof(ResourceBackendDiagnostic),
        typeof(ResourceEntryDiagnostic))]
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

    [FrameworkArchitecture(
        "资源后端诊断项",
        "记录一个已初始化资源后端的种类与 Provider 名称。",
        FrameworkArchitectureLayer.GraphAndScope,
        191,
        typeof(ResourceBackendKind))]
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

    [FrameworkArchitecture(
        "资源条目诊断项",
        "记录一个缓存或 Pending 条目的键、类型、Provider 与引用/等待数量。",
        FrameworkArchitectureLayer.GraphAndScope,
        192,
        typeof(ResourceKey))]
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
