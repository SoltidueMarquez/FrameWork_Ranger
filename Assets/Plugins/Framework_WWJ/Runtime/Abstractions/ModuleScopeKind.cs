namespace Framework_WWJ
{
    /// <summary>
    /// 模块运行实例的所有权范围。
    /// </summary>
    [FrameworkArchitecture(
        "模块作用域种类",
        "区分常驻 GlobalScope 与随活动场景替换的 SceneScope。",
        FrameworkArchitectureLayer.Contracts,
        20)]
    public enum ModuleScopeKind
    {
        Global,
        Scene,
    }
}
