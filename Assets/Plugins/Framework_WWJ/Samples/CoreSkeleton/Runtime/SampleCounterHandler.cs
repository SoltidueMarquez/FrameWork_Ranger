using System;

namespace Framework_WWJ.Samples
{
    /// <summary>
    /// 示例场景 A 使用的平稳计数 Handler。
    /// </summary>
    [Serializable]
    public sealed class SampleCounterHandler : SampleSceneHandlerBase
    {
        /// <inheritdoc />
        public override string DisplayName => "Counter Handler（场景 A）";

        protected override float TickAmount => 1f;
    }
}
