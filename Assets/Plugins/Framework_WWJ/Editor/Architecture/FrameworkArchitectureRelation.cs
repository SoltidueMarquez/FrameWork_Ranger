namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 架构图关系类型。
    /// </summary>
    [FrameworkArchitecture(
        "架构关系种类",
        "区分继承、接口实现与显式协作三种架构关系。",
        FrameworkArchitectureLayer.EditorIntegration,
        305)]
    internal enum FrameworkArchitectureRelationKind
    {
        Inheritance,
        InterfaceImplementation,
        Collaboration,
    }

    /// <summary>
    /// 描述两个架构节点之间的一条有向关系。
    /// </summary>
    [FrameworkArchitecture(
        "架构关系",
        "表达继承、接口实现或显式协作三种节点连线。",
        FrameworkArchitectureLayer.EditorIntegration,
        310,
        typeof(FrameworkArchitectureTypeDescriptor))]
    internal sealed class FrameworkArchitectureRelation
    {
        internal FrameworkArchitectureTypeDescriptor Source { get; }
        internal FrameworkArchitectureTypeDescriptor Target { get; }
        internal FrameworkArchitectureRelationKind Kind { get; }

        internal FrameworkArchitectureRelation(
            FrameworkArchitectureTypeDescriptor source,
            FrameworkArchitectureTypeDescriptor target,
            FrameworkArchitectureRelationKind kind)
        {
            Source = source;
            Target = target;
            Kind = kind;
        }
    }
}
