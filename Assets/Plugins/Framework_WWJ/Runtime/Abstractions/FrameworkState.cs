namespace Framework_WWJ
{
    /// <summary>
    /// Framework 根运行时的可观察状态。业务代码只能读取状态，状态迁移统一由
    /// <see cref="FrameworkRuntime"/> 完成。
    /// </summary>
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
