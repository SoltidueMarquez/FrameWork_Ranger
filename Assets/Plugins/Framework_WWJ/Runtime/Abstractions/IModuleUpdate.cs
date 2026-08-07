namespace Framework_WWJ
{
    /// <summary>
    /// 声明对象需要接收 Unity Update 时序。只有完整加载成功的 Scope 才会驱动此接口。
    /// </summary>
    [FrameworkArchitecture(
        "Update 能力",
        "声明模块或 Handler 需要接收逐帧 Update。",
        FrameworkArchitectureLayer.Contracts,
        10)]
    public interface IModuleUpdate
    {
        /// <summary>
        /// 在 Framework Host 的 Update 中调用。
        /// </summary>
        /// <param name="deltaTime">当前帧的缩放时间间隔。</param>
        void OnModuleUpdate(float deltaTime);
    }
}
