namespace Framework_WWJ
{
    /// <summary>
    /// 声明对象需要接收 Unity FixedUpdate 时序。
    /// </summary>
    [FrameworkArchitecture(
        "FixedUpdate 能力",
        "声明模块或 Handler 需要接收固定帧更新。",
        FrameworkArchitectureLayer.Contracts,
        20)]
    public interface IModuleFixedUpdate
    {
        /// <summary>
        /// 在 Framework Host 的 FixedUpdate 中调用。
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间步长。</param>
        void OnModuleFixedUpdate(float fixedDeltaTime);
    }
}
