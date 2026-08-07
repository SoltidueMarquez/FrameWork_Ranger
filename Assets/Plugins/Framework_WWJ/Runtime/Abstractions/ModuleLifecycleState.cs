namespace Framework_WWJ
{
    /// <summary>
    /// 单个模块运行克隆的生命周期状态。原始模板资产始终保持 <see cref="None"/>。
    /// </summary>
    public enum ModuleLifecycleState
    {
        None,
        Created,
        Loading,
        Loaded,
        Unloading,
        Unloaded,
        Failed,
    }
}
