using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 为单画布代码架构图计算确定性的分组泳道、类型节点和折叠关系。
    /// 绘制器只消费布局结果，不在 IMGUI 事件中重复决定可见性与关系聚合。
    /// </summary>
    [FrameworkArchitecture(
        "代码架构复合布局",
        "递归计算可折叠分组泳道、全局逻辑层列、类型节点与折叠关系代理。",
        FrameworkArchitectureLayer.EditorIntegration,
        345,
        typeof(FrameworkArchitectureCatalog),
        typeof(FrameworkArchitectureGroupDescriptor),
        typeof(FrameworkArchitectureRelation))]
    internal sealed class FrameworkArchitectureGraphLayout
    {
        #region 布局常量

        internal const float NodeWidth = 214f;
        internal const float NodeHeight = 72f;
        internal const float LayerGap = 58f;
        internal const float NodeRowGap = 16f;
        internal const float CanvasPadding = 26f;
        internal const float LayerHeaderHeight = 30f;
        internal const float GroupDepthIndent = 20f;
        internal const float GroupHeaderHeight = 36f;
        internal const float GroupPadding = 12f;
        internal const float GroupGap = 14f;
        internal const float SectionGap = 14f;
        internal const float CollapsedGroupHeight = 54f;
        internal const float EmptyContentHeight = 20f;

        #endregion

        #region 布局结果

        internal IReadOnlyDictionary<FrameworkArchitectureGroupDescriptor, GroupEntry> Groups { get; }

        internal IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> TypeRects { get; }

        internal IReadOnlyDictionary<FrameworkArchitectureLayer, float> LayerXs { get; }

        internal IReadOnlyList<DisplayRelation> Relations { get; }

        internal SearchResult Search { get; }

        internal Rect ContentBounds { get; }

        #endregion

        private FrameworkArchitectureGraphLayout(
            IReadOnlyDictionary<FrameworkArchitectureGroupDescriptor, GroupEntry> groups,
            IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> typeRects,
            IReadOnlyDictionary<FrameworkArchitectureLayer, float> layerXs,
            IReadOnlyList<DisplayRelation> relations,
            SearchResult search,
            Rect contentBounds)
        {
            Groups = groups;
            TypeRects = typeRects;
            LayerXs = layerXs;
            Relations = relations;
            Search = search;
            ContentBounds = contentBounds;
        }

        #region 构建入口

        internal static FrameworkArchitectureGraphLayout Build(
            FrameworkArchitectureCatalog catalog,
            ExpansionState expansionState,
            string searchText,
            RelationVisibility relationVisibility)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (expansionState == null)
            {
                throw new ArgumentNullException(nameof(expansionState));
            }

            var search = SearchResult.Build(catalog, expansionState, searchText);
            var builder = new LayoutBuilder(catalog, search, relationVisibility);
            return builder.Build();
        }

        #endregion

        #region 公开查询

        internal bool TryGetVisibleEndpoint(
            FrameworkArchitectureTypeDescriptor type,
            out Endpoint endpoint)
        {
            if (type != null && TypeRects.TryGetValue(type, out var typeRect))
            {
                endpoint = Endpoint.ForType(type, typeRect);
                return true;
            }

            var path = GetGroupPathFromRoot(type?.Group);
            for (var i = 0; i < path.Count; i++)
            {
                var group = path[i];
                if (Groups.TryGetValue(group, out var entry) && !entry.IsExpanded)
                {
                    endpoint = Endpoint.ForGroup(group, entry.HeaderRect);
                    return true;
                }
            }

            endpoint = null;
            return false;
        }

        internal Rect GetSearchMatchBounds()
        {
            var hasBounds = false;
            var bounds = default(Rect);
            foreach (var type in Search.MatchedTypes)
            {
                if (TypeRects.TryGetValue(type, out var typeRect))
                {
                    Encapsulate(ref bounds, ref hasBounds, typeRect);
                }
            }

            foreach (var group in Search.MatchedGroups)
            {
                if (Groups.TryGetValue(group, out var groupEntry))
                {
                    Encapsulate(ref bounds, ref hasBounds, groupEntry.HeaderRect);
                }
            }

            return hasBounds ? bounds : default;
        }

        private static void Encapsulate(ref Rect bounds, ref bool hasBounds, Rect value)
        {
            if (!hasBounds)
            {
                bounds = value;
                hasBounds = true;
                return;
            }

            var xMin = Mathf.Min(bounds.xMin, value.xMin);
            var yMin = Mathf.Min(bounds.yMin, value.yMin);
            var xMax = Mathf.Max(bounds.xMax, value.xMax);
            var yMax = Mathf.Max(bounds.yMax, value.yMax);
            bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        #endregion

        #region 布局算法

        private sealed class LayoutBuilder
        {
            private readonly FrameworkArchitectureCatalog m_catalog;
            private readonly SearchResult m_search;
            private readonly RelationVisibility m_relationVisibility;
            private readonly Dictionary<FrameworkArchitectureGroupDescriptor, GroupEntry> m_groups =
                new Dictionary<FrameworkArchitectureGroupDescriptor, GroupEntry>();
            private readonly Dictionary<FrameworkArchitectureTypeDescriptor, Rect> m_typeRects =
                new Dictionary<FrameworkArchitectureTypeDescriptor, Rect>();
            private readonly Dictionary<FrameworkArchitectureLayer, float> m_layerXs =
                new Dictionary<FrameworkArchitectureLayer, float>();
            private float m_groupRight;

            internal LayoutBuilder(
                FrameworkArchitectureCatalog catalog,
                SearchResult search,
                RelationVisibility relationVisibility)
            {
                m_catalog = catalog;
                m_search = search;
                m_relationVisibility = relationVisibility;
            }

            internal FrameworkArchitectureGraphLayout Build()
            {
                BuildLayerColumns();
                var y = CanvasPadding + LayerHeaderHeight;
                for (var i = 0; i < m_catalog.RootGroup.Children.Count; i++)
                {
                    y += LayoutGroup(m_catalog.RootGroup.Children[i], 0, y);
                    if (i < m_catalog.RootGroup.Children.Count - 1)
                    {
                        y += GroupGap;
                    }
                }

                var contentHeight = Mathf.Max(
                    CanvasPadding * 2f + LayerHeaderHeight + CollapsedGroupHeight,
                    y + CanvasPadding);
                var contentBounds = new Rect(
                    0f,
                    0f,
                    m_groupRight + CanvasPadding,
                    contentHeight);
                var relations = BuildDisplayRelations();
                return new FrameworkArchitectureGraphLayout(
                    m_groups,
                    m_typeRects,
                    m_layerXs,
                    relations,
                    m_search,
                    contentBounds);
            }

            private void BuildLayerColumns()
            {
                var layers = Enum.GetValues(typeof(FrameworkArchitectureLayer))
                    .Cast<FrameworkArchitectureLayer>()
                    .OrderBy(layer => layer)
                    .ToArray();
                var maximumDepth = m_catalog.Groups.Count == 0
                    ? 0
                    : m_catalog.Groups.Max(GetDepth);
                var layerStartX = CanvasPadding +
                                  (maximumDepth + 1) * GroupDepthIndent +
                                  GroupPadding;
                for (var i = 0; i < layers.Length; i++)
                {
                    m_layerXs.Add(layers[i], layerStartX + i * (NodeWidth + LayerGap));
                }

                m_groupRight = m_layerXs[layers[layers.Length - 1]] + NodeWidth + GroupPadding;
            }

            private float LayoutGroup(
                FrameworkArchitectureGroupDescriptor group,
                int depth,
                float y)
            {
                var x = CanvasPadding + depth * GroupDepthIndent;
                var width = m_groupRight - x;
                var isExpanded = m_search.EffectiveExpandedGroupIds.Contains(group.GroupId);
                if (!isExpanded)
                {
                    var collapsedRect = new Rect(x, y, width, CollapsedGroupHeight);
                    m_groups.Add(
                        group,
                        new GroupEntry(group, collapsedRect, collapsedRect, depth, false));
                    return CollapsedGroupHeight;
                }

                var contentY = y + GroupHeaderHeight + GroupPadding;
                var hadContent = false;
                if (group.Nodes.Count > 0)
                {
                    contentY += LayoutTypes(group.Nodes, contentY);
                    hadContent = true;
                }

                if (group.Children.Count > 0)
                {
                    if (hadContent)
                    {
                        contentY += SectionGap;
                    }

                    for (var i = 0; i < group.Children.Count; i++)
                    {
                        contentY += LayoutGroup(group.Children[i], depth + 1, contentY);
                        if (i < group.Children.Count - 1)
                        {
                            contentY += GroupGap;
                        }
                    }

                    hadContent = true;
                }

                if (!hadContent)
                {
                    contentY += EmptyContentHeight;
                }

                contentY += GroupPadding;
                var groupRect = new Rect(x, y, width, contentY - y);
                var headerRect = new Rect(x, y, width, GroupHeaderHeight);
                m_groups.Add(group, new GroupEntry(group, groupRect, headerRect, depth, true));
                return groupRect.height;
            }

            private float LayoutTypes(
                IReadOnlyList<FrameworkArchitectureTypeDescriptor> nodes,
                float y)
            {
                var maxRows = 0;
                foreach (var pair in m_layerXs)
                {
                    var layerNodes = nodes
                        .Where(node => node.Metadata.Layer == pair.Key)
                        .ToArray();
                    maxRows = Mathf.Max(maxRows, layerNodes.Length);
                    for (var row = 0; row < layerNodes.Length; row++)
                    {
                        m_typeRects.Add(
                            layerNodes[row],
                            new Rect(
                                pair.Value,
                                y + row * (NodeHeight + NodeRowGap),
                                NodeWidth,
                                NodeHeight));
                    }
                }

                return maxRows == 0
                    ? EmptyContentHeight
                    : maxRows * NodeHeight + Mathf.Max(0, maxRows - 1) * NodeRowGap;
            }

            private List<DisplayRelation> BuildDisplayRelations()
            {
                var relationsByKey = new Dictionary<string, DisplayRelation>(StringComparer.Ordinal);
                for (var i = 0; i < m_catalog.Relations.Count; i++)
                {
                    var relation = m_catalog.Relations[i];
                    if (!IncludesRelation(relation.Kind) ||
                        !TryResolveEndpoint(relation.Source, out var source) ||
                        !TryResolveEndpoint(relation.Target, out var target) ||
                        string.Equals(source.Key, target.Key, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var key = $"{source.Key}|{target.Key}|{relation.Kind}";
                    if (!relationsByKey.TryGetValue(key, out var displayRelation))
                    {
                        displayRelation = new DisplayRelation(source, target, relation.Kind);
                        relationsByKey.Add(key, displayRelation);
                    }

                    displayRelation.Count++;
                    if (m_search.MatchedTypes.Contains(relation.Source) ||
                        m_search.MatchedTypes.Contains(relation.Target))
                    {
                        displayRelation.HasSearchMatch = true;
                    }
                }

                return relationsByKey.Values
                    .OrderBy(relation => relation.Source.Key, StringComparer.Ordinal)
                    .ThenBy(relation => relation.Target.Key, StringComparer.Ordinal)
                    .ThenBy(relation => relation.Kind)
                    .ToList();
            }

            private bool IncludesRelation(FrameworkArchitectureRelationKind kind)
            {
                switch (kind)
                {
                    case FrameworkArchitectureRelationKind.Inheritance:
                        return (m_relationVisibility & RelationVisibility.Inheritance) != 0;
                    case FrameworkArchitectureRelationKind.InterfaceImplementation:
                        return (m_relationVisibility & RelationVisibility.InterfaceImplementation) != 0;
                    default:
                        return (m_relationVisibility & RelationVisibility.Collaboration) != 0;
                }
            }

            private bool TryResolveEndpoint(
                FrameworkArchitectureTypeDescriptor type,
                out Endpoint endpoint)
            {
                if (m_typeRects.TryGetValue(type, out var typeRect))
                {
                    endpoint = Endpoint.ForType(type, typeRect);
                    return true;
                }

                var path = GetGroupPathFromRoot(type.Group);
                for (var i = 0; i < path.Count; i++)
                {
                    var group = path[i];
                    if (m_groups.TryGetValue(group, out var entry) && !entry.IsExpanded)
                    {
                        endpoint = Endpoint.ForGroup(group, entry.HeaderRect);
                        return true;
                    }
                }

                endpoint = null;
                return false;
            }

            private static int GetDepth(FrameworkArchitectureGroupDescriptor group)
            {
                var depth = -1;
                var current = group;
                while (current != null)
                {
                    depth++;
                    current = current.Parent;
                }

                return Mathf.Max(0, depth - 1);
            }
        }

        #endregion

        #region 展开与搜索状态

        /// <summary>
        /// 保存用户主动展开的 GroupId，并把它们限定在当前 Unity 编辑器会话。
        /// 搜索产生的临时展开集合不会写回这里。
        /// </summary>
        internal sealed class ExpansionState
        {
            internal const string DefaultSessionKey =
                "Framework_WWJ.FrameworkCenter.Architecture.ExpandedGroups.v1";

            private readonly string m_sessionKey;
            private readonly HashSet<string> m_userExpandedGroupIds =
                new HashSet<string>(StringComparer.Ordinal);
            private string m_pendingAnchorGroupId;
            private Vector2 m_pendingAnchorCanvasPoint;

            internal IReadOnlyCollection<string> UserExpandedGroupIds => m_userExpandedGroupIds;

            internal ExpansionState(string sessionKey = DefaultSessionKey)
            {
                m_sessionKey = string.IsNullOrWhiteSpace(sessionKey)
                    ? DefaultSessionKey
                    : sessionKey;
            }

            internal void Restore(FrameworkArchitectureCatalog catalog)
            {
                m_userExpandedGroupIds.Clear();
                var serialized = SessionState.GetString(m_sessionKey, string.Empty);
                var values = serialized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < values.Length; i++)
                {
                    m_userExpandedGroupIds.Add(values[i]);
                }

                Sanitize(catalog);
            }

            internal void Sanitize(FrameworkArchitectureCatalog catalog)
            {
                var validIds = new HashSet<string>(
                    catalog.Groups
                        .Where(group => !group.IsRoot)
                        .Select(group => group.GroupId),
                    StringComparer.Ordinal);
                m_userExpandedGroupIds.RemoveWhere(groupId => !validIds.Contains(groupId));
                Save();
            }

            internal bool IsUserExpanded(FrameworkArchitectureGroupDescriptor group)
            {
                return group != null && m_userExpandedGroupIds.Contains(group.GroupId);
            }

            internal void Toggle(
                FrameworkArchitectureGroupDescriptor group,
                Vector2 oldHeaderCanvasPoint)
            {
                if (group == null || group.IsRoot)
                {
                    return;
                }

                if (!m_userExpandedGroupIds.Add(group.GroupId))
                {
                    m_userExpandedGroupIds.Remove(group.GroupId);
                }

                m_pendingAnchorGroupId = group.GroupId;
                m_pendingAnchorCanvasPoint = oldHeaderCanvasPoint;
                Save();
            }

            internal void ExpandAll(FrameworkArchitectureCatalog catalog)
            {
                m_userExpandedGroupIds.Clear();
                foreach (var group in catalog.Groups)
                {
                    if (!group.IsRoot)
                    {
                        m_userExpandedGroupIds.Add(group.GroupId);
                    }
                }

                ClearPendingAnchor();
                Save();
            }

            internal void CollapseAll()
            {
                m_userExpandedGroupIds.Clear();
                ClearPendingAnchor();
                Save();
            }

            internal bool TryConsumeAnchor(
                FrameworkArchitectureGraphLayout layout,
                out Vector2 oldCanvasPoint,
                out Vector2 newCanvasPoint)
            {
                oldCanvasPoint = default;
                newCanvasPoint = default;
                if (string.IsNullOrEmpty(m_pendingAnchorGroupId))
                {
                    return false;
                }

                var group = layout.Groups.Keys.FirstOrDefault(candidate =>
                    string.Equals(candidate.GroupId, m_pendingAnchorGroupId, StringComparison.Ordinal));
                if (group == null || !layout.Groups.TryGetValue(group, out var entry))
                {
                    ClearPendingAnchor();
                    return false;
                }

                oldCanvasPoint = m_pendingAnchorCanvasPoint;
                newCanvasPoint = entry.HeaderRect.center;
                ClearPendingAnchor();
                return true;
            }

            private void Save()
            {
                var serialized = string.Join(
                    "\n",
                    m_userExpandedGroupIds.OrderBy(groupId => groupId, StringComparer.Ordinal));
                SessionState.SetString(m_sessionKey, serialized);
            }

            private void ClearPendingAnchor()
            {
                m_pendingAnchorGroupId = string.Empty;
                m_pendingAnchorCanvasPoint = default;
            }
        }

        internal sealed class SearchResult
        {
            internal string SearchText { get; }
            internal IReadOnlyCollection<string> EffectiveExpandedGroupIds { get; }
            internal ISet<FrameworkArchitectureTypeDescriptor> MatchedTypes { get; }
            internal ISet<FrameworkArchitectureGroupDescriptor> MatchedGroups { get; }
            internal bool IsActive => !string.IsNullOrWhiteSpace(SearchText);
            internal int MatchCount => MatchedTypes.Count + MatchedGroups.Count;

            private SearchResult(
                string searchText,
                IReadOnlyCollection<string> effectiveExpandedGroupIds,
                ISet<FrameworkArchitectureTypeDescriptor> matchedTypes,
                ISet<FrameworkArchitectureGroupDescriptor> matchedGroups)
            {
                SearchText = searchText;
                EffectiveExpandedGroupIds = effectiveExpandedGroupIds;
                MatchedTypes = matchedTypes;
                MatchedGroups = matchedGroups;
            }

            internal static SearchResult Build(
                FrameworkArchitectureCatalog catalog,
                ExpansionState expansionState,
                string searchText)
            {
                var effective = new HashSet<string>(
                    expansionState.UserExpandedGroupIds,
                    StringComparer.Ordinal);
                var matchedTypes = new HashSet<FrameworkArchitectureTypeDescriptor>();
                var matchedGroups = new HashSet<FrameworkArchitectureGroupDescriptor>();
                var normalizedSearch = searchText?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(normalizedSearch))
                {
                    return new SearchResult(normalizedSearch, effective, matchedTypes, matchedGroups);
                }

                foreach (var group in catalog.Groups)
                {
                    if (group.IsRoot || !MatchesGroup(group, normalizedSearch))
                    {
                        continue;
                    }

                    matchedGroups.Add(group);
                    AddGroupAndAncestors(group, effective);
                }

                foreach (var node in catalog.Nodes)
                {
                    if (!MatchesType(node, normalizedSearch))
                    {
                        continue;
                    }

                    matchedTypes.Add(node);
                    AddGroupAndAncestors(node.Group, effective);
                }

                return new SearchResult(normalizedSearch, effective, matchedTypes, matchedGroups);
            }

            private static void AddGroupAndAncestors(
                FrameworkArchitectureGroupDescriptor group,
                ISet<string> target)
            {
                var current = group;
                while (current != null && !current.IsRoot)
                {
                    target.Add(current.GroupId);
                    current = current.Parent;
                }
            }

            private static bool MatchesGroup(
                FrameworkArchitectureGroupDescriptor group,
                string searchText)
            {
                return Contains(group.DisplayName, searchText) ||
                       Contains(group.Responsibility, searchText) ||
                       Contains(group.GroupId, searchText);
            }

            private static bool MatchesType(
                FrameworkArchitectureTypeDescriptor node,
                string searchText)
            {
                return Contains(node.Metadata.DisplayName, searchText) ||
                       Contains(node.Metadata.Responsibility, searchText) ||
                       Contains(node.Type.FullName, searchText);
            }

            private static bool Contains(string source, string searchText)
            {
                return !string.IsNullOrEmpty(source) &&
                       source.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        #endregion

        #region 结果类型

        [Flags]
        internal enum RelationVisibility
        {
            None = 0,
            Inheritance = 1 << 0,
            InterfaceImplementation = 1 << 1,
            Collaboration = 1 << 2,
            All = Inheritance | InterfaceImplementation | Collaboration,
        }

        internal sealed class GroupEntry
        {
            internal FrameworkArchitectureGroupDescriptor Group { get; }
            internal Rect Bounds { get; }
            internal Rect HeaderRect { get; }
            internal int Depth { get; }
            internal bool IsExpanded { get; }

            internal GroupEntry(
                FrameworkArchitectureGroupDescriptor group,
                Rect bounds,
                Rect headerRect,
                int depth,
                bool isExpanded)
            {
                Group = group;
                Bounds = bounds;
                HeaderRect = headerRect;
                Depth = depth;
                IsExpanded = isExpanded;
            }
        }

        internal sealed class Endpoint
        {
            internal string Key { get; }
            internal FrameworkArchitectureTypeDescriptor Type { get; }
            internal FrameworkArchitectureGroupDescriptor Group { get; }
            internal Rect Rect { get; }
            internal bool IsGroup => Group != null;

            private Endpoint(
                string key,
                FrameworkArchitectureTypeDescriptor type,
                FrameworkArchitectureGroupDescriptor group,
                Rect rect)
            {
                Key = key;
                Type = type;
                Group = group;
                Rect = rect;
            }

            internal static Endpoint ForType(
                FrameworkArchitectureTypeDescriptor type,
                Rect rect)
            {
                return new Endpoint($"T:{type.Type.AssemblyQualifiedName}", type, null, rect);
            }

            internal static Endpoint ForGroup(
                FrameworkArchitectureGroupDescriptor group,
                Rect rect)
            {
                return new Endpoint($"G:{group.GroupId}", null, group, rect);
            }
        }

        internal sealed class DisplayRelation
        {
            internal Endpoint Source { get; }
            internal Endpoint Target { get; }
            internal FrameworkArchitectureRelationKind Kind { get; }
            internal int Count { get; set; }
            internal bool HasSearchMatch { get; set; }
            internal bool IsAggregated => Source.IsGroup || Target.IsGroup;

            internal DisplayRelation(
                Endpoint source,
                Endpoint target,
                FrameworkArchitectureRelationKind kind)
            {
                Source = source;
                Target = target;
                Kind = kind;
            }
        }

        #endregion

        #region 路径辅助

        private static List<FrameworkArchitectureGroupDescriptor> GetGroupPathFromRoot(
            FrameworkArchitectureGroupDescriptor leaf)
        {
            var path = new List<FrameworkArchitectureGroupDescriptor>();
            var current = leaf;
            while (current != null && !current.IsRoot)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        #endregion
    }
}
