using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 一次架构扫描生成的不可变节点、关系和诊断集合。
    /// </summary>
    [FrameworkArchitecture(
        "架构目录",
        "向架构页面提供稳定的类型节点、关系与元数据覆盖诊断。",
        FrameworkArchitectureLayer.EditorIntegration,
        320,
        typeof(FrameworkArchitectureRelation))]
    internal sealed class FrameworkArchitectureCatalog
    {
        internal IReadOnlyList<FrameworkArchitectureTypeDescriptor> Nodes { get; }
        internal IReadOnlyList<FrameworkArchitectureRelation> Relations { get; }
        internal IReadOnlyList<string> Diagnostics { get; }
        internal FrameworkArchitectureGroupDescriptor RootGroup { get; }
        internal IReadOnlyList<FrameworkArchitectureGroupDescriptor> Groups { get; }

        internal FrameworkArchitectureCatalog(
            IReadOnlyList<FrameworkArchitectureTypeDescriptor> nodes,
            IReadOnlyList<FrameworkArchitectureRelation> relations,
            IReadOnlyList<string> diagnostics,
            FrameworkArchitectureGroupDescriptor rootGroup,
            IReadOnlyList<FrameworkArchitectureGroupDescriptor> groups)
        {
            Nodes = nodes;
            Relations = relations;
            Diagnostics = diagnostics;
            RootGroup = rootGroup;
            Groups = groups;
        }

        internal FrameworkArchitectureGroupDescriptor FindGroup(string groupId)
        {
            return Groups.FirstOrDefault(group =>
                string.Equals(group.GroupId, groupId ?? string.Empty, StringComparison.Ordinal));
        }
    }
}
