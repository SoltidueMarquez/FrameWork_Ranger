namespace FrameWork_Ranger
{
    /// <summary>
    /// Framework 根运行时的可观察状态。业务代码只能读取状态，状态迁移统一由
    /// <see cref="FrameworkRuntime"/> 完成。
    /// </summary>
    [FrameworkArchitecture(
        "框架状态",
        "定义 FrameworkRuntime 从未初始化、加载、就绪、失败到关停的可观察状态。",
        FrameworkArchitectureLayer.Contracts,
        0,
        typeof(FrameworkRuntime))]
    public enum FrameworkState
    {
        Uninitialized,
        InitializingGlobal,
        GlobalReady,
        LoadingScene,
        Ready,
        UnloadingScene,
        Failed,
        ShuttingDown,
        Shutdown,
    }
}
