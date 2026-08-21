using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 表示代码架构目录中的一个可进入分组。分组由生产程序集的稳定路径逐级合并而成，
    /// 自身不对应某个具体脚本。
    /// </summary>
    [FrameworkArchitecture(
        "架构分组描述",
        "保存分层架构目录中的父子分组、直属程序集、内部类型与职责摘要。",
        FrameworkArchitectureLayer.EditorIntegration,
        292,
        typeof(FrameworkArchitectureTypeDescriptor))]
    internal sealed class FrameworkArchitectureGroupDescriptor
    {
        #region 构建状态

        private readonly List<FrameworkArchitectureGroupDescriptor> m_children =
            new List<FrameworkArchitectureGroupDescriptor>();
        private readonly List<FrameworkArchitectureTypeDescriptor> m_nodes =
            new List<FrameworkArchitectureTypeDescriptor>();
        private readonly List<string> m_assemblyNames = new List<string>();
        private readonly List<string> m_responsibilities = new List<string>();

        #endregion

        #region 公开属性

        internal string GroupId { get; }

        internal string DisplayName { get; }

        internal int Order { get; }

        internal FrameworkArchitectureGroupDescriptor Parent { get; }

        internal IReadOnlyList<FrameworkArchitectureGroupDescriptor> Children => m_children;

        internal IReadOnlyList<FrameworkArchitectureTypeDescriptor> Nodes => m_nodes;

        internal IReadOnlyList<string> AssemblyNames => m_assemblyNames;

        internal bool IsRoot => Parent == null;

        internal bool IsLeaf => m_children.Count == 0;

        internal int DescendantTypeCount => m_nodes.Count + m_children.Sum(child => child.DescendantTypeCount);

        internal int DescendantAssemblyCount =>
            m_assemblyNames.Count + m_children.Sum(child => child.DescendantAssemblyCount);

        internal string Responsibility
        {
            get
            {
                if (m_responsibilities.Count == 1)
                {
                    return m_responsibilities[0];
                }

                if (m_responsibilities.Count > 1)
                {
                    return string.Join("\n", m_responsibilities);
                }

                return IsRoot
                    ? "Framework_WWJ 生产代码架构目录。"
                    : $"组织 {DisplayName} 下的生产程序集与核心类型。";
            }
        }

        #endregion

        internal FrameworkArchitectureGroupDescriptor(
            string groupId,
            string displayName,
            int order,
            FrameworkArchitectureGroupDescriptor parent)
        {
            GroupId = groupId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Order = order;
            Parent = parent;
        }

        #region 目录构建

        internal void AddChild(FrameworkArchitectureGroupDescriptor child)
        {
            if (child != null && !m_children.Contains(child))
            {
                m_children.Add(child);
            }
        }

        internal void AddAssembly(string assemblyName, string responsibility)
        {
            if (!string.IsNullOrWhiteSpace(assemblyName) && !m_assemblyNames.Contains(assemblyName))
            {
                m_assemblyNames.Add(assemblyName);
            }

            if (!string.IsNullOrWhiteSpace(responsibility) && !m_responsibilities.Contains(responsibility))
            {
                m_responsibilities.Add(responsibility);
            }
        }

        internal void AddNode(FrameworkArchitectureTypeDescriptor node)
        {
            if (node != null && !m_nodes.Contains(node))
            {
                m_nodes.Add(node);
            }
        }

        internal void Seal()
        {
            m_children.Sort(CompareGroups);
            m_nodes.Sort(FrameworkArchitectureCatalogBuilder.CompareDescriptors);
            m_assemblyNames.Sort(StringComparer.Ordinal);
            for (var i = 0; i < m_children.Count; i++)
            {
                m_children[i].Seal();
            }
        }

        #endregion

        #region 查询

        internal IEnumerable<FrameworkArchitectureTypeDescriptor> EnumerateDescendantNodes()
        {
            for (var i = 0; i < m_nodes.Count; i++)
            {
                yield return m_nodes[i];
            }

            for (var childIndex = 0; childIndex < m_children.Count; childIndex++)
            {
                foreach (var node in m_children[childIndex].EnumerateDescendantNodes())
                {
                    yield return node;
                }
            }
        }

        internal FrameworkArchitectureGroupDescriptor GetDirectChildContaining(
            FrameworkArchitectureGroupDescriptor descendant)
        {
            var current = descendant;
            while (current != null && current.Parent != this)
            {
                current = current.Parent;
            }

            return current != null && current.Parent == this ? current : null;
        }

        private static int CompareGroups(
            FrameworkArchitectureGroupDescriptor left,
            FrameworkArchitectureGroupDescriptor right)
        {
            var order = left.Order.CompareTo(right.Order);
            if (order != 0)
            {
                return order;
            }

            var displayName = string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
            return displayName != 0
                ? displayName
                : string.Compare(left.GroupId, right.GroupId, StringComparison.Ordinal);
        }

        #endregion
    }
}
