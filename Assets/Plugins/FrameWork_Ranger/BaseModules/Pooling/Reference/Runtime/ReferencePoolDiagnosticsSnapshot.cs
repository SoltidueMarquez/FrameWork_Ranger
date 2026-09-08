using System;

namespace FrameWork_Ranger.Pooling.Reference
{
    /// <summary>
    /// 单个精确类型引用池的只读运行诊断快照。
    /// </summary>
    [FrameworkArchitecture(
        "引用池诊断快照",
        "向 Editor 展示类型、空闲、借出、总量与下一次循环缩容倒计时。",
        FrameworkArchitectureLayer.Contracts,
        122)]
    public readonly struct ReferencePoolDiagnosticsSnapshot
    {
        public Type ItemType { get; }

        public int InactiveCount { get; }

        public int BorrowedCount { get; }

        public int TotalCount { get; }

        public float IdleSecondsRemaining { get; }

        internal ReferencePoolDiagnosticsSnapshot(
            Type itemType,
            int inactiveCount,
            int borrowedCount,
            int totalCount,
            float idleSecondsRemaining)
        {
            ItemType = itemType;
            InactiveCount = inactiveCount;
            BorrowedCount = borrowedCount;
            TotalCount = totalCount;
            IdleSecondsRemaining = idleSecondsRemaining;
        }
    }
}
