namespace FrameWork_Ranger
{
    /// <summary>
    /// Framework Center 代码架构图使用的固定逻辑层。
    /// 枚举顺序就是节点图从左到右的显示顺序。
    /// </summary>
    [FrameworkArchitecture(
        "架构逻辑层",
        "定义叶级类型图从契约到编辑器集成的固定横向层次。",
        FrameworkArchitectureLayer.Contracts,
        -110)]
    public enum FrameworkArchitectureLayer
    {
        Contracts,
        Configuration,
        ModuleModel,
        GraphAndScope,
        RuntimeDriving,
        PublicFacade,
        EditorIntegration,
    }
}
