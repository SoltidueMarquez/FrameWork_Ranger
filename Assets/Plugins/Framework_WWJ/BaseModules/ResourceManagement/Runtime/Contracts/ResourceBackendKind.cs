namespace Framework_WWJ.ResourceManagement
{
    /// <summary>
    /// 资源地址明确指向的加载后端。枚举不表达优先级，也不会在后端之间自动回退。
    /// </summary>
    public enum ResourceBackendKind
    {
        UnityResources = 0,
        Addressables = 1,
    }
}
