using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework_WWJ.Samples
{
    /// <summary>
    /// 示例全局时钟模块，用最少状态证明 Direct Module、跨场景常驻和 Update 驱动。
    /// </summary>
    [CreateAssetMenu(fileName = "SampleGlobalClockModule", menuName = "Framework WWJ/Samples/Global Clock Module")]
    public sealed class SampleGlobalClockModule : DirectModuleBase, IModuleUpdate
    {
        #region 运行时状态

        [NonSerialized]
        private float m_elapsedSeconds;

        [NonSerialized]
        private int m_tickCount;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取当前运行克隆累计的时间。
        /// </summary>
        public float ElapsedSeconds => m_elapsedSeconds;

        /// <summary>
        /// 获取当前运行克隆接收 Update 的次数。
        /// </summary>
        public int TickCount => m_tickCount;

        /// <summary>
        /// 获取运行克隆的 Unity EntityId 哈希，用于直观看到跨场景切换时全局实例没有重建。
        /// </summary>
        public int RuntimeInstanceId => GetEntityId().GetHashCode();

        #endregion

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            m_elapsedSeconds = 0f;
            m_tickCount = 0;
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void OnModuleUpdate(float deltaTime)
        {
            m_elapsedSeconds += deltaTime;
            m_tickCount++;
        }
    }
}
