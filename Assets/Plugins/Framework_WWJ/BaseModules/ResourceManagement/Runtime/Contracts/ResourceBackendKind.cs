namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 资源地址明确指向的加载后端。枚举不表达优先级，也不会在后端之间自动回退。
    /// </summary>
    [FrameworkArchitecture(
        "资源后端种类",
        "标识资源键应路由到 Unity Resources 或 Addressables，不提供隐式回退。",
        FrameworkArchitectureLayer.Contracts,
        110)]
    public enum ResourceBackendKind
    {
        UnityResources = 0,
        Addressables = 1,
    }
}
