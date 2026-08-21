namespace Framework_WWJ
{
    /// <summary>
    /// 单个模块运行克隆的生命周期状态。原始模板资产始终保持 <see cref="None"/>。
    /// </summary>
    [FrameworkArchitecture(
        "模块生命周期状态",
        "描述模块运行克隆从创建、加载、已加载到卸载或失败的状态。",
        FrameworkArchitectureLayer.Contracts,
        10,
        typeof(ModuleBase))]
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
