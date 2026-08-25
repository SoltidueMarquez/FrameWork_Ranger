using System;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 不添加附加行为的默认 Framework DriverHandler。
    /// </summary>
    [FrameworkArchitecture(
        "默认框架驱动扩展",
        "提供不附加行为的 FrameworkDriverHandler 默认实现。",
        FrameworkArchitectureLayer.RuntimeDriving,
        30)]
    [Serializable]
    public sealed class DefaultFrameworkDriverHandler : FrameworkDriverHandlerBase
    {
    }
}
