using System;

namespace Framework_WWJ.Samples
{
    /// <summary>
    /// 示例场景 B 使用的快速脉冲 Handler。
    /// </summary>
    [Serializable]
    public sealed class SamplePulseHandler : SampleSceneHandlerBase
    {
        /// <inheritdoc />
        public override string DisplayName => "Pulse Handler（场景 B）";

        protected override float TickAmount => 3f;
    }
}
