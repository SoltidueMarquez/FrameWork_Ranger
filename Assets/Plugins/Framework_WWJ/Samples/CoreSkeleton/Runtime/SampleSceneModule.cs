using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework_WWJ.Samples
{
    /// <summary>
    /// 示例场景模块。Module 类型在两个场景中保持一致，具体逻辑由不同 Odin 多态 Handler 实现。
    /// </summary>
    [CreateAssetMenu(fileName = "SampleSceneModule", menuName = "Framework WWJ/Samples/Scene Module")]
    public sealed class SampleSceneModule : HandlerModuleBase<SampleSceneHandlerBase>
    {
        private static readonly Type[] Dependencies =
        {
            typeof(SampleGlobalClockModule),
        };

        #region 运行时状态

        [NonSerialized]
        private string m_handlerLabel;

        [NonSerialized]
        private int m_tickCount;

        [NonSerialized]
        private float m_accumulatedValue;

        [NonSerialized]
        private float m_observedGlobalTime;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取当前运行克隆所使用的 Handler 标签。
        /// </summary>
        public string HandlerLabel => m_handlerLabel;

        /// <summary>
        /// 获取 Handler 的具体类型名，但不暴露 Handler 对象本身。
        /// </summary>
        public string HandlerTypeName => Handler == null ? "<未配置>" : Handler.GetType().Name;

        /// <summary>
        /// 获取当前 SceneScope 运行克隆的 Unity EntityId 哈希，用于验证场景切换会替换场景模块。
        /// </summary>
        public int RuntimeInstanceId => GetEntityId().GetHashCode();

        /// <summary>
        /// 获取 Handler Tick 次数。
        /// </summary>
        public int TickCount => m_tickCount;

        /// <summary>
        /// 获取由 Handler 按不同策略累计的示例数值。
        /// </summary>
        public float AccumulatedValue => m_accumulatedValue;

        /// <summary>
        /// 获取本模块最近观察到的全局时钟时间。
        /// </summary>
        public float ObservedGlobalTime => m_observedGlobalTime;

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        #endregion

        internal void ResetRuntime(string handlerLabel)
        {
            m_handlerLabel = handlerLabel;
            m_tickCount = 0;
            m_accumulatedValue = 0f;
            m_observedGlobalTime = 0f;
        }

        internal void RecordTick(float amount, float globalTime)
        {
            m_tickCount++;
            m_accumulatedValue += amount;
            m_observedGlobalTime = globalTime;
        }
    }
}
