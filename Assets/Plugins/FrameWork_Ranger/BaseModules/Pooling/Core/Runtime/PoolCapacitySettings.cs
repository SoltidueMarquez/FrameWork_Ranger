using System;
using UnityEngine;

namespace FrameWork_Ranger.Pooling
{
    /// <summary>
    /// 描述池的初始空闲量、批量扩容量、空闲保留上限与循环缩容间隔。
    /// </summary>
    [Serializable]
    [FrameworkArchitecture(
        "池容量设置",
        "统一定义引用池与 GameObject 池的预热、扩容、保留和循环缩容语义。",
        FrameworkArchitectureLayer.Configuration,
        110)]
    public sealed class PoolCapacitySettings
    {
        public const int UnlimitedRetained = -1;

        [SerializeField]
        [Min(0)]
        private int m_initialCount;

        [SerializeField]
        [Min(1)]
        private int m_expansionBatchSize = 1;

        [SerializeField]
        private int m_maxRetained = UnlimitedRetained;

        [SerializeField]
        private float m_idleShrinkIntervalSeconds;

        public int InitialCount => m_initialCount;

        public int ExpansionBatchSize => m_expansionBatchSize;

        /// <summary>
        /// 只约束空闲缓存；-1 表示不限制。借出数量没有硬上限。
        /// </summary>
        public int MaxRetained => m_maxRetained;

        /// <summary>
        /// 小于或等于零时关闭循环缩容。
        /// </summary>
        public float IdleShrinkIntervalSeconds => m_idleShrinkIntervalSeconds;

        public bool HasRetainedLimit => m_maxRetained >= 0;

        public static PoolCapacitySettings CreateReferenceDefaults()
        {
            return new PoolCapacitySettings(30, 20, UnlimitedRetained, 15f);
        }

        public static PoolCapacitySettings CreateGameObjectDefaults()
        {
            return new PoolCapacitySettings(0, 5, UnlimitedRetained, 120f);
        }

        public PoolCapacitySettings()
            : this(0, 1, UnlimitedRetained, 0f)
        {
        }

        public PoolCapacitySettings(
            int initialCount,
            int expansionBatchSize,
            int maxRetained,
            float idleShrinkIntervalSeconds)
        {
            m_initialCount = initialCount;
            m_expansionBatchSize = expansionBatchSize;
            m_maxRetained = maxRetained;
            m_idleShrinkIntervalSeconds = idleShrinkIntervalSeconds;
        }

        public PoolCapacitySettings Copy()
        {
            return new PoolCapacitySettings(
                m_initialCount,
                m_expansionBatchSize,
                m_maxRetained,
                m_idleShrinkIntervalSeconds);
        }

        public bool TryValidate(out string error)
        {
            if (m_initialCount < 0)
            {
                error = "InitialCount 不能小于 0。";
                return false;
            }

            if (m_expansionBatchSize <= 0)
            {
                error = "ExpansionBatchSize 必须大于 0。";
                return false;
            }

            if (m_maxRetained < UnlimitedRetained)
            {
                error = "MaxRetained 只能是 -1 或非负数。";
                return false;
            }

            if (m_maxRetained >= 0 && m_initialCount > m_maxRetained)
            {
                error = "有限 MaxRetained 不能小于 InitialCount。";
                return false;
            }

            if (float.IsNaN(m_idleShrinkIntervalSeconds) ||
                float.IsInfinity(m_idleShrinkIntervalSeconds))
            {
                error = "IdleShrinkIntervalSeconds 必须是有限数值。";
                return false;
            }

            error = null;
            return true;
        }
    }
}
