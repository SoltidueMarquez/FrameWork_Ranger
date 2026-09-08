using FrameWork_Ranger.ResourceManagement;

namespace FrameWork_Ranger.Pooling.GameObjects
{
    /// <summary>
    /// 单个 GameObject 池的只读运行诊断快照。
    /// </summary>
    [FrameworkArchitecture(
        "GameObject 池诊断快照",
        "向 Editor 展示池名、Prefab ResourceKey、数量与下一次循环缩容倒计时。",
        FrameworkArchitectureLayer.Contracts,
        132,
        typeof(ResourceKey))]
    public readonly struct GameObjectPoolDiagnosticsSnapshot
    {
        public string PoolName { get; }

        public ResourceKey ResourceKey { get; }

        public int InactiveCount { get; }

        public int BorrowedCount { get; }

        public int TotalCount { get; }

        public float IdleSecondsRemaining { get; }

        internal GameObjectPoolDiagnosticsSnapshot(
            string poolName,
            ResourceKey resourceKey,
            int inactiveCount,
            int borrowedCount,
            int totalCount,
            float idleSecondsRemaining)
        {
            PoolName = poolName;
            ResourceKey = resourceKey;
            InactiveCount = inactiveCount;
            BorrowedCount = borrowedCount;
            TotalCount = totalCount;
            IdleSecondsRemaining = idleSecondsRemaining;
        }
    }
}
