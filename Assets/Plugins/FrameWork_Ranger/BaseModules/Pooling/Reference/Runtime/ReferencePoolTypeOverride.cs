using System;
using FrameWork_Ranger.Pooling;
using Sirenix.Serialization;

namespace FrameWork_Ranger.Pooling.Reference
{
    /// <summary>
    /// 为一个精确引用类型覆盖默认池容量设置。
    /// </summary>
    [Serializable]
    [FrameworkArchitecture(
        "引用池类型覆盖",
        "按 System.Type 为预配置引用池声明独立容量并触发 GlobalScope 预热。",
        FrameworkArchitectureLayer.Configuration,
        121,
        typeof(PoolCapacitySettings))]
    public sealed class ReferencePoolTypeOverride
    {
        [OdinSerialize]
        private Type m_itemType;

        [OdinSerialize]
        private PoolCapacitySettings m_capacity = PoolCapacitySettings.CreateReferenceDefaults();

        public Type ItemType => m_itemType;

        public PoolCapacitySettings Capacity => m_capacity;

        public ReferencePoolTypeOverride()
        {
        }

        public ReferencePoolTypeOverride(Type itemType, PoolCapacitySettings capacity)
        {
            m_itemType = itemType;
            m_capacity = capacity;
        }
    }
}
