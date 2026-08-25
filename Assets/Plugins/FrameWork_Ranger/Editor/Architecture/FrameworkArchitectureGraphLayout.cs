using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 为代码架构图计算紧凑分组卡片、可展开容器、局部逻辑层和关系代理。
    /// 绘制器只消费不可变布局结果，不在 IMGUI Repaint 中重新决定架构语义。
    /// </summary>
    [FrameworkArchitecture(
        "代码架构复合布局",
        "递归计算紧凑分组卡片、局部分层类型节点、会话偏移与折叠关系代理。",
        FrameworkArchitectureLayer.EditorIntegration,
        345,
        typeof(FrameworkArchitectureCatalog),
        typeof(FrameworkArchitectureGroupDescriptor),
        typeof(FrameworkArchitectureRelation),
        typeof(FrameworkArchitectureGraphPositionState))]
    internal sealed class FrameworkArchitectureGraphLayout
    {
        #region 布局常量

        internal const float TypeNodeWidth = 190f;
        internal const float TypeNodeHeight = 72f;
        internal const float TypeColumnGap = 42f;
        internal const float TypeRowGap = 16f;
        internal const float CanvasPadding = 36f;
        internal const float LayerHeaderHeight = 24f;
        internal const float GroupHeaderHeight = 44f;
        internal const float GroupPadding = 18f;
        internal const float GroupGap = 34f;
        internal const float GroupRowGap = 28f;
        internal const float RootGap = 56f;
        internal const float SectionGap = 26f;
        internal const float CollapsedGroupWidth = 258f;
        internal const float CollapsedGroupHeight = 96f;
        internal const float EmptyContentHeight = 24f;

        private const int MaxCollapsedChildrenPerRow = 3;
        private const int MaxRootGroupsPerRow = 3;

        #endregion

        #region 布局结果

        internal IReadOnlyDictionary<FrameworkArchitectureGroupDescriptor, GroupEntry> Groups { get; }

        internal IReadOnlyList<GroupEntry> GroupEntries { get; }

        internal IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> TypeRects { get; }

        internal IReadOnlyList<LayerHeaderEntry> LayerHeaders { get; }

        internal IReadOnlyList<DisplayRelation> Relations { get; }

        internal SearchResult Search { get; }

        internal Rect ContentBounds { get; }

        #endregion

        private FrameworkArchitectureGraphLayout(
            IReadOnlyDictionary<FrameworkArchitectureGroupDescriptor, GroupEntry> groups,
            IReadOnlyList<GroupEntry> groupEntries,
            IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> typeRects,
            IReadOnlyList<LayerHeaderEntry> layerHeaders,
            IReadOnlyList<DisplayRelation> relations,
            SearchResult search,
            Rect contentBounds)
        {
            Groups = groups;
            GroupEntries = groupEntries;
            TypeRects = typeRects;
            LayerHeaders = layerHeaders;
            Relations = relations;
            Search = search;
            ContentBounds = contentBounds;
        }

        #region 构建入口

        internal static FrameworkArchitectureGraphLayout Build(
            FrameworkArchitectureCatalog catalog,
            ExpansionState expansionState,
            FrameworkArchitectureGraphPositionState positionState,
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

            if (positionState == null)
            {
                throw new ArgumentNullException(nameof(positionState));
            }

            var search = SearchResult.Build(catalog, expansionState, searchText);
            return new LayoutBuilder(catalog, search, positionState, relationVisibility).Build();
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
                    endpoint = Endpoint.ForGroup(group, entry.NodeRect);
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
                    Encapsulate(ref bounds, ref hasBounds, groupEntry.NodeRect);
                }
            }

            return hasBounds ? bounds : default;
        }

        internal bool IsRelatedToSelection(
            DisplayRelation relation,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            if (relation == null)
            {
                return false;
            }

            return selectedType != null &&
                   (ReferenceEquals(relation.Source.Type, selectedType) ||
                    ReferenceEquals(relation.Target.Type, selectedType)) ||
                   selectedGroup != null &&
                   (EndpointBelongsToGroup(relation.Source, selectedGroup) ||
                    EndpointBelongsToGroup(relation.Target, selectedGroup));
        }

        internal static bool EndpointBelongsToGroup(
            Endpoint endpoint,
            FrameworkArchitectureGroupDescriptor group)
        {
            if (endpoint == null || group == null)
            {
                return false;
            }

            var current = endpoint.Type != null ? endpoint.Type.Group : endpoint.Group;
            while (current != null)
            {
                if (ReferenceEquals(current, group))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        private static void Encapsulate(ref Rect bounds, ref bool hasBounds, Rect value)
        {
            if (!hasBounds)
            {
                bounds = value;
                hasBounds = true;
                return;
            }

            bounds = Rect.MinMaxRect(
                Mathf.Min(bounds.xMin, value.xMin),
                Mathf.Min(bounds.yMin, value.yMin),
                Mathf.Max(bounds.xMax, value.xMax),
                Mathf.Max(bounds.yMax, value.yMax));
        }

        #endregion

        #region 布局算法

        private sealed class LayoutBuilder
        {
            private readonly FrameworkArchitectureCatalog m_catalog;
            private readonly SearchResult m_search;
            private readonly FrameworkArchitectureGraphPositionState m_positionState;
            private readonly RelationVisibility m_relationVisibility;
            private readonly Dictionary<FrameworkArchitectureGroupDescriptor, GroupEntry> m_groups =
                new Dictionary<FrameworkArchitectureGroupDescriptor, GroupEntry>();
            private readonly Dictionary<FrameworkArchitectureTypeDescriptor, Rect> m_typeRects =
                new Dictionary<FrameworkArchitectureTypeDescriptor, Rect>();
            private readonly List<LayerHeaderEntry> m_layerHeaders = new List<LayerHeaderEntry>();
            private Rect m_visibleBounds;
            private bool m_hasVisibleBounds;

            internal LayoutBuilder(
                FrameworkArchitectureCatalog catalog,
                SearchResult search,
                FrameworkArchitectureGraphPositionState positionState,
                RelationVisibility relationVisibility)
            {
                m_catalog = catalog;
                m_search = search;
                m_positionState = positionState;
                m_relationVisibility = relationVisibility;
            }

            internal FrameworkArchitectureGraphLayout Build()
            {
                LayoutRootGroups();
                var relations = BuildDisplayRelations();
                for (var i = 0; i < relations.Count; i++)
                {
                    Encapsulate(ref m_visibleBounds, ref m_hasVisibleBounds, relations[i].Geometry.Bounds);
                }

                var contentBounds = m_hasVisibleBounds
                    ? ExpandRect(m_visibleBounds, CanvasPadding)
                    : new Rect(0f, 0f, CollapsedGroupWidth + CanvasPadding * 2f,
                        CollapsedGroupHeight + CanvasPadding * 2f);
                return new FrameworkArchitectureGraphLayout(
                    m_groups,
                    m_groups.Values.OrderBy(entry => entry.Depth).ToArray(),
                    m_typeRects,
                    m_layerHeaders,
                    relations,
                    m_search,
                    contentBounds);
            }

            private void LayoutRootGroups()
            {
                var x = CanvasPadding;
                var y = CanvasPadding;
                var rowHeight = 0f;
                var rowCount = 0;
                for (var i = 0; i < m_catalog.RootGroup.Children.Count; i++)
                {
                    var group = m_catalog.RootGroup.Children[i];
                    var isExpanded = m_search.EffectiveExpandedGroupIds.Contains(group.GroupId);
                    if (isExpanded && rowCount > 0)
                    {
                        x = CanvasPadding;
                        y += rowHeight + RootGap;
                        rowHeight = 0f;
                        rowCount = 0;
                    }

                    var result = LayoutGroup(group, 0, new Vector2(x, y), new Vector2(x, y));
                    x += result.AutoSize.x + RootGap;
                    rowHeight = Mathf.Max(rowHeight, result.AutoSize.y);
                    rowCount++;

                    if (isExpanded || rowCount >= MaxRootGroupsPerRow)
                    {
                        x = CanvasPadding;
                        y += rowHeight + RootGap;
                        rowHeight = 0f;
                        rowCount = 0;
                    }
                }
            }

            private LayoutResult LayoutGroup(
                FrameworkArchitectureGroupDescriptor group,
                int depth,
                Vector2 automaticPosition,
                Vector2 minimumPosition)
            {
                var origin = automaticPosition + m_positionState.GetOffset(group);
                if (group.Parent != null && !group.Parent.IsRoot)
                {
                    origin.x = Mathf.Max(minimumPosition.x, origin.x);
                    origin.y = Mathf.Max(minimumPosition.y, origin.y);
                }

                var isExpanded = m_search.EffectiveExpandedGroupIds.Contains(group.GroupId);
                if (!isExpanded)
                {
                    var cardRect = new Rect(
                        origin.x,
                        origin.y,
                        CollapsedGroupWidth,
                        CollapsedGroupHeight);
                    AddGroup(group, new GroupEntry(
                        group,
                        cardRect,
                        cardRect,
                        Rect.zero,
                        depth,
                        false));
                    return new LayoutResult(cardRect.size);
                }

                var contentStart = new Vector2(
                    origin.x + GroupPadding,
                    origin.y + GroupHeaderHeight + GroupPadding);
                var cursorY = contentStart.y;
                var contentRight = contentStart.x;
                var hadContent = false;

                if (group.Children.Count > 0)
                {
                    var childResult = LayoutChildGroups(group.Children, depth + 1, contentStart);
                    cursorY += childResult.Height;
                    contentRight = Mathf.Max(contentRight, childResult.Right);
                    hadContent = true;
                }

                if (group.Nodes.Count > 0)
                {
                    if (hadContent)
                    {
                        cursorY += SectionGap;
                    }

                    var typeResult = LayoutTypes(group, group.Nodes, new Vector2(contentStart.x, cursorY));
                    cursorY += typeResult.Height;
                    contentRight = Mathf.Max(contentRight, typeResult.Right);
                    hadContent = true;
                }

                if (!hadContent)
                {
                    cursorY += EmptyContentHeight;
                }

                var width = Mathf.Max(
                    CollapsedGroupWidth,
                    contentRight - origin.x + GroupPadding);
                var height = Mathf.Max(
                    CollapsedGroupHeight,
                    cursorY - origin.y + GroupPadding);
                var bounds = new Rect(origin.x, origin.y, width, height);
                var headerRect = new Rect(origin.x, origin.y, width, GroupHeaderHeight);
                var contentRect = new Rect(
                    contentStart.x,
                    contentStart.y,
                    Mathf.Max(1f, width - GroupPadding * 2f),
                    Mathf.Max(1f, height - GroupHeaderHeight - GroupPadding * 2f));
                AddGroup(group, new GroupEntry(
                    group,
                    bounds,
                    headerRect,
                    contentRect,
                    depth,
                    true));
                return new LayoutResult(bounds.size);
            }

            private SectionResult LayoutChildGroups(
                IReadOnlyList<FrameworkArchitectureGroupDescriptor> children,
                int depth,
                Vector2 start)
            {
                var x = start.x;
                var y = start.y;
                var rowHeight = 0f;
                var right = start.x;
                var rowCount = 0;
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    var isExpanded = m_search.EffectiveExpandedGroupIds.Contains(child.GroupId);
                    if (isExpanded && rowCount > 0)
                    {
                        x = start.x;
                        y += rowHeight + GroupRowGap;
                        rowHeight = 0f;
                        rowCount = 0;
                    }

                    var result = LayoutGroup(child, depth, new Vector2(x, y), start);
                    var entry = m_groups[child];
                    right = Mathf.Max(right, entry.Bounds.xMax);
                    rowHeight = Mathf.Max(rowHeight, entry.Bounds.yMax - y);
                    rowCount++;

                    if (isExpanded || rowCount >= MaxCollapsedChildrenPerRow)
                    {
                        x = start.x;
                        y += rowHeight + GroupRowGap;
                        rowHeight = 0f;
                        rowCount = 0;
                    }
                    else
                    {
                        x = Mathf.Max(
                            x + result.AutoSize.x,
                            entry.Bounds.xMax) + GroupGap;
                    }
                }

                var bottom = rowCount > 0 ? y + rowHeight : y - GroupRowGap;
                return new SectionResult(
                    Mathf.Max(0f, bottom - start.y),
                    right);
            }

            private SectionResult LayoutTypes(
                FrameworkArchitectureGroupDescriptor group,
                IReadOnlyList<FrameworkArchitectureTypeDescriptor> nodes,
                Vector2 start)
            {
                var layers = nodes
                    .Select(node => node.Metadata.Layer)
                    .Distinct()
                    .OrderBy(layer => layer)
                    .ToArray();
                var right = start.x;
                var bottom = start.y + LayerHeaderHeight;
                for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                {
                    var layer = layers[layerIndex];
                    var x = start.x + layerIndex * (TypeNodeWidth + TypeColumnGap);
                    m_layerHeaders.Add(new LayerHeaderEntry(
                        group,
                        layer,
                        new Rect(x, start.y, TypeNodeWidth, LayerHeaderHeight)));

                    var layerNodes = nodes.Where(node => node.Metadata.Layer == layer).ToArray();
                    for (var row = 0; row < layerNodes.Length; row++)
                    {
                        var node = layerNodes[row];
                        var automaticPosition = new Vector2(
                            x,
                            start.y + LayerHeaderHeight + row * (TypeNodeHeight + TypeRowGap));
                        var nodePosition = automaticPosition + m_positionState.GetOffset(node);
                        nodePosition.x = Mathf.Max(start.x, nodePosition.x);
                        nodePosition.y = Mathf.Max(start.y + LayerHeaderHeight, nodePosition.y);
                        var rect = new Rect(
                            nodePosition,
                            new Vector2(TypeNodeWidth, TypeNodeHeight));
                        m_typeRects.Add(node, rect);
                        Encapsulate(ref m_visibleBounds, ref m_hasVisibleBounds, rect);
                        right = Mathf.Max(right, rect.xMax);
                        bottom = Mathf.Max(bottom, rect.yMax);
                    }
                }

                return new SectionResult(
                    Mathf.Max(EmptyContentHeight, bottom - start.y),
                    right);
            }

            private void AddGroup(
                FrameworkArchitectureGroupDescriptor group,
                GroupEntry entry)
            {
                m_groups.Add(group, entry);
                Encapsulate(ref m_visibleBounds, ref m_hasVisibleBounds, entry.Bounds);
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
                        endpoint = Endpoint.ForGroup(group, entry.NodeRect);
                        return true;
                    }
                }

                endpoint = null;
                return false;
            }

            private static Rect ExpandRect(Rect rect, float padding)
            {
                return new Rect(
                    rect.x - padding,
                    rect.y - padding,
                    rect.width + padding * 2f,
                    rect.height + padding * 2f);
            }
        }

        private readonly struct LayoutResult
        {
            internal Vector2 AutoSize { get; }

            internal LayoutResult(Vector2 autoSize)
            {
                AutoSize = autoSize;
            }
        }

        private readonly struct SectionResult
        {
            internal float Height { get; }
            internal float Right { get; }

            internal SectionResult(float height, float right)
            {
                Height = height;
                Right = right;
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
                "FrameWork_Ranger.FrameworkCenter.Architecture.ExpandedGroups.v1";

            private readonly string m_sessionKey;
            private readonly HashSet<string> m_userExpandedGroupIds =
                new HashSet<string>(StringComparer.Ordinal);
            private string m_pendingAnchorGroupId;
            private Vector2 m_pendingAnchorCanvasPoint;

            internal IReadOnlyCollection<string> UserExpandedGroupIds => m_userExpandedGroupIds;

            internal int Revision { get; private set; }

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

                Revision++;
                Sanitize(catalog);
            }

            internal void Sanitize(FrameworkArchitectureCatalog catalog)
            {
                var validIds = new HashSet<string>(
                    catalog.Groups
                        .Where(group => !group.IsRoot)
                        .Select(group => group.GroupId),
                    StringComparer.Ordinal);
                var previousCount = m_userExpandedGroupIds.Count;
                m_userExpandedGroupIds.RemoveWhere(groupId => !validIds.Contains(groupId));
                if (previousCount != m_userExpandedGroupIds.Count)
                {
                    Revision++;
                }

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
                Revision++;
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
                Revision++;
                Save();
            }

            internal void CollapseAll()
            {
                if (m_userExpandedGroupIds.Count == 0)
                {
                    return;
                }

                m_userExpandedGroupIds.Clear();
                ClearPendingAnchor();
                Revision++;
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
                newCanvasPoint = GetHeaderAnchor(entry.HeaderRect);
                ClearPendingAnchor();
                return true;
            }

            internal static Vector2 GetHeaderAnchor(Rect headerRect)
            {
                return new Vector2(headerRect.x + 24f, headerRect.center.y);
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
            internal Rect ContentRect { get; }
            internal int Depth { get; }
            internal bool IsExpanded { get; }
            internal Rect NodeRect => IsExpanded ? HeaderRect : Bounds;

            internal GroupEntry(
                FrameworkArchitectureGroupDescriptor group,
                Rect bounds,
                Rect headerRect,
                Rect contentRect,
                int depth,
                bool isExpanded)
            {
                Group = group;
                Bounds = bounds;
                HeaderRect = headerRect;
                ContentRect = contentRect;
                Depth = depth;
                IsExpanded = isExpanded;
            }
        }

        internal sealed class LayerHeaderEntry
        {
            internal FrameworkArchitectureGroupDescriptor Group { get; }
            internal FrameworkArchitectureLayer Layer { get; }
            internal Rect Rect { get; }

            internal LayerHeaderEntry(
                FrameworkArchitectureGroupDescriptor group,
                FrameworkArchitectureLayer layer,
                Rect rect)
            {
                Group = group;
                Layer = layer;
                Rect = rect;
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
            internal RelationGeometry Geometry { get; }
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
                Geometry = RelationGeometry.Create(source.Rect, target.Rect, source.Key, target.Key, kind);
            }
        }

        internal sealed class RelationGeometry
        {
            internal Vector2 Start { get; }
            internal Vector2 Control1 { get; }
            internal Vector2 Control2 { get; }
            internal Vector2 End { get; }
            internal Rect Bounds { get; }

            private RelationGeometry(
                Vector2 start,
                Vector2 control1,
                Vector2 control2,
                Vector2 end)
            {
                Start = start;
                Control1 = control1;
                Control2 = control2;
                End = end;
                Bounds = Rect.MinMaxRect(
                    Mathf.Min(start.x, control1.x, control2.x, end.x),
                    Mathf.Min(start.y, control1.y, control2.y, end.y),
                    Mathf.Max(start.x, control1.x, control2.x, end.x),
                    Mathf.Max(start.y, control1.y, control2.y, end.y));
            }

            internal static RelationGeometry Create(
                Rect source,
                Rect target,
                string sourceKey,
                string targetKey,
                FrameworkArchitectureRelationKind kind)
            {
                var direction = target.center - source.center;
                var horizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
                Vector2 start;
                Vector2 end;
                Vector2 control1;
                Vector2 control2;
                if (horizontal)
                {
                    var sign = direction.x >= 0f ? 1f : -1f;
                    start = new Vector2(sign > 0f ? source.xMax : source.xMin, source.center.y);
                    end = new Vector2(sign > 0f ? target.xMin : target.xMax, target.center.y);
                    var distance = Mathf.Max(46f, Mathf.Abs(end.x - start.x) * 0.42f);
                    control1 = start + Vector2.right * sign * distance;
                    control2 = end - Vector2.right * sign * distance;
                }
                else
                {
                    var sign = direction.y >= 0f ? 1f : -1f;
                    start = new Vector2(source.center.x, sign > 0f ? source.yMax : source.yMin);
                    end = new Vector2(target.center.x, sign > 0f ? target.yMin : target.yMax);
                    var distance = Mathf.Max(46f, Mathf.Abs(end.y - start.y) * 0.42f);
                    control1 = start + Vector2.up * sign * distance;
                    control2 = end - Vector2.up * sign * distance;
                }

                // 多种关系共享端点时使用稳定微偏移，避免三条曲线完全重叠。
                var relationOffset = ((int)kind - 1) * 6f;
                var stableSign = string.Compare(sourceKey, targetKey, StringComparison.Ordinal) <= 0 ? 1f : -1f;
                var perpendicular = horizontal ? Vector2.up : Vector2.right;
                control1 += perpendicular * relationOffset * stableSign;
                control2 += perpendicular * relationOffset * stableSign;
                return new RelationGeometry(start, control1, control2, end);
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
