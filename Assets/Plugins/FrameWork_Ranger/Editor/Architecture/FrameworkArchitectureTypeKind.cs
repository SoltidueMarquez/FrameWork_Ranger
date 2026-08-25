namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 架构节点支持的顶层代码类型种类。
    /// </summary>
    [FrameworkArchitecture(
        "架构类型种类",
        "区分类、接口、结构体和枚举节点，以便架构图采用稳定的文字与颜色语义。",
        FrameworkArchitectureLayer.EditorIntegration,
        295)]
    internal enum FrameworkArchitectureTypeKind
    {
        Class,
        Interface,
        Struct,
        Enum,
    }
}
