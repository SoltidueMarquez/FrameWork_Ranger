using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 在同一可导航画布中绘制可折叠分组泳道、类型节点和折叠关系代理。
    /// </summary>
    [FrameworkArchitecture(
        "代码架构复合图绘制器",
        "在单一画布中组合分组容器、全局逻辑层、类型节点、关系筛选和展开交互。",
        FrameworkArchitectureLayer.EditorIntegration,
        350,
        typeof(FrameworkArchitectureCatalog),
        typeof(FrameworkArchitectureGraphLayout),
        typeof(FrameworkGraphViewport))]
    internal static class FrameworkArchitectureGraphDrawer
    {
        private static readonly Color ClassColor = new Color(0.20f, 0.48f, 0.72f, 0.92f);
        private static readonly Color InterfaceColor = new Color(0.35f, 0.67f, 0.48f, 0.92f);
        private static readonly Color StructColor = new Color(0.48f, 0.42f, 0.72f, 0.92f);
        private static readonly Color EnumColor = new Color(0.22f, 0.60f, 0.66f, 0.92f);
        private static readonly Color SelectedColor = new Color(0.95f, 0.62f, 0.18f, 0.98f);
        private static readonly Color DimColor = new Color(0.30f, 0.30f, 0.30f, 0.48f);
        private static readonly Color GroupHeaderColor = new Color(0.16f, 0.38f, 0.60f, 0.98f);
        private static readonly Color GroupBorderColor = new Color(0.35f, 0.64f, 0.88f, 0.78f);

        #region 绘制入口

        internal static DrawResult Draw(
            FrameworkArchitectureCatalog catalog,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType,
            string searchText,
            FrameworkArchitectureGraphLayout.RelationVisibility relationVisibility,
            bool frameSearchMatches)
        {
            var layout = FrameworkArchitectureGraphLayout.Build(
                catalog,
                expansionState,
                searchText,
                relationVisibility);

            if (expansionState.TryConsumeAnchor(
                    layout,
                    out var oldAnchorCanvasPoint,
                    out var newAnchorCanvasPoint))
            {
                viewportState.KeepCanvasAnchor(oldAnchorCanvasPoint, newAnchorCanvasPoint);
            }

            NormalizeSelection(layout, ref selectedGroup, ref selectedType);
            if (frameSearchMatches && layout.Search.MatchCount > 0)
            {
                var matchBounds = layout.GetSearchMatchBounds();
                if (matchBounds.width > 0f && matchBounds.height > 0f)
                {
                    viewportState.RequestFrame(ExpandRect(matchBounds, 28f));
                }
            }

            FrameworkGraphViewport.Draw(
                viewportState,
                layout.ContentBounds,
                610f,
                _ =>
                {
                    DrawLayerHeaders(viewportState, layout.LayerXs);
                    DrawGroupBackgrounds(viewportState, layout, selectedGroup);
                    DrawRelations(viewportState, layout);
                    DrawGroupHeaders(
                        viewportState,
                        layout,
                        expansionState,
                        ref selectedGroup,
                        ref selectedType);
                    DrawTypeNodes(
                        viewportState,
                        layout,
                        ref selectedGroup,
                        ref selectedType);
                });

            return new DrawResult(
                layout,
                selectedGroup,
                selectedType,
                layout.Search.MatchCount);
        }

        #endregion

        #region 分组绘制

        private static void DrawGroupBackgrounds(
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureGroupDescriptor selectedGroup)
        {
            var entries = layout.Groups.Values
                .OrderBy(entry => entry.Depth)
                .ThenBy(entry => entry.Bounds.y)
                .ToArray();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var rect = viewportState.CanvasToViewport(entry.Bounds);
                var depthFactor = Mathf.Clamp01(entry.Depth / 6f);
                var background = Color.Lerp(
                    new Color(0.12f, 0.16f, 0.21f, 0.88f),
                    new Color(0.18f, 0.19f, 0.22f, 0.88f),
                    depthFactor);
                if (layout.Search.IsActive && !ContainsSearchMatch(entry.Group, layout.Search))
                {
                    background.a *= 0.48f;
                }

                EditorGUI.DrawRect(rect, background);
                FrameworkCenterStyles.DrawBorder(
                    rect,
                    ReferenceEquals(entry.Group, selectedGroup)
                        ? SelectedColor
                        : GroupBorderColor);
            }
        }

        private static void DrawGroupHeaders(
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            ref FrameworkArchitectureGroupDescriptor selectedGroup,
            ref FrameworkArchitectureTypeDescriptor selectedType)
        {
            var entries = layout.Groups.Values
                .OrderBy(entry => entry.Bounds.y)
                .ThenBy(entry => entry.Depth)
                .ToArray();
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var headerRect = viewportState.CanvasToViewport(entry.HeaderRect);
                var matchesSearch = !layout.Search.IsActive || ContainsSearchMatch(entry.Group, layout.Search);
                var color = ReferenceEquals(entry.Group, selectedGroup)
                    ? SelectedColor
                    : matchesSearch ? GroupHeaderColor : DimColor;
                EditorGUI.DrawRect(headerRect, color);

                var foldoutWidth = Mathf.Clamp(24f * viewportState.Zoom, 8f, 24f);
                var foldoutRect = new Rect(
                    headerRect.x + 2f,
                    headerRect.y + 1f,
                    foldoutWidth,
                    Mathf.Max(4f, headerRect.height - 2f));
                var titleRect = new Rect(
                    foldoutRect.xMax,
                    headerRect.y,
                    Mathf.Max(1f, headerRect.xMax - foldoutRect.xMax),
                    headerRect.height);

                var foldoutLabel = viewportState.Zoom < 0.20f
                    ? string.Empty
                    : entry.IsExpanded ? "▼" : "▶";
                var canToggle = !layout.Search.IsActive;
                using (new EditorGUI.DisabledScope(!canToggle))
                {
                    if (GUI.Button(
                            foldoutRect,
                            new GUIContent(
                                foldoutLabel,
                                canToggle
                                    ? entry.IsExpanded ? "收起分组" : "展开分组"
                                    : "清空搜索后调整用户展开状态"),
                            EditorStyles.miniButton))
                    {
                        ToggleGroup(
                            entry,
                            expansionState,
                            ref selectedGroup,
                            ref selectedType);
                    }
                }

                var label = GetGroupLabel(entry, viewportState.Zoom);
                if (GUI.Button(
                        titleRect,
                        new GUIContent(label, entry.Group.Responsibility),
                        CreateGroupHeaderStyle(viewportState.Zoom)))
                {
                    selectedGroup = entry.Group;
                    selectedType = null;
                    if (Event.current.clickCount >= 2 && canToggle)
                    {
                        ToggleGroup(
                            entry,
                            expansionState,
                            ref selectedGroup,
                            ref selectedType);
                    }
                }
            }
        }

        private static void ToggleGroup(
            FrameworkArchitectureGraphLayout.GroupEntry entry,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            ref FrameworkArchitectureGroupDescriptor selectedGroup,
            ref FrameworkArchitectureTypeDescriptor selectedType)
        {
            if (entry.IsExpanded &&
                selectedType != null &&
                IsSameOrDescendant(selectedType.Group, entry.Group))
            {
                selectedType = null;
                selectedGroup = entry.Group;
            }
            else if (entry.IsExpanded &&
                     selectedGroup != null &&
                     !ReferenceEquals(selectedGroup, entry.Group) &&
                     IsSameOrDescendant(selectedGroup, entry.Group))
            {
                selectedGroup = entry.Group;
            }

            expansionState.Toggle(entry.Group, entry.HeaderRect.center);
            GUI.changed = true;
        }

        #endregion

        #region 类型与关系绘制

        private static void DrawTypeNodes(
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            ref FrameworkArchitectureGroupDescriptor selectedGroup,
            ref FrameworkArchitectureTypeDescriptor selectedType)
        {
            var nodeStyle = FrameworkCenterStyles.CreateGraphNodeStyle(viewportState.Zoom);
            foreach (var pair in layout.TypeRects.OrderBy(pair => pair.Value.y).ThenBy(pair => pair.Value.x))
            {
                var node = pair.Key;
                var rect = viewportState.CanvasToViewport(pair.Value);
                var matchesSearch = !layout.Search.IsActive || layout.Search.MatchedTypes.Contains(node);
                var color = ReferenceEquals(node, selectedType)
                    ? SelectedColor
                    : matchesSearch ? GetKindColor(node.Kind) : DimColor;

                EditorGUI.DrawRect(rect, color);
                if (node.Kind == FrameworkArchitectureTypeKind.Interface)
                {
                    FrameworkCenterStyles.DrawBorder(rect, new Color(0.72f, 1f, 0.80f, 1f));
                }

                var label = GetTypeLabel(node, viewportState.Zoom);
                if (GUI.Button(rect, new GUIContent(label, node.Metadata.Responsibility), nodeStyle))
                {
                    selectedType = node;
                    selectedGroup = null;
                    if (Event.current.clickCount >= 2 && node.Script != null)
                    {
                        AssetDatabase.OpenAsset(node.Script);
                    }
                }
            }
        }

        private static void DrawRelations(
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout)
        {
            Handles.BeginGUI();
            var oldColor = Handles.color;
            for (var i = 0; i < layout.Relations.Count; i++)
            {
                var relation = layout.Relations[i];
                var sourceRect = viewportState.CanvasToViewport(relation.Source.Rect);
                var targetRect = viewportState.CanvasToViewport(relation.Target.Rect);
                var from = GetEdgePoint(sourceRect, targetRect.center);
                var to = GetEdgePoint(targetRect, sourceRect.center);
                var alpha = layout.Search.IsActive && !relation.HasSearchMatch ? 0.18f : 0.72f;
                var lineWidth = Mathf.Clamp(2f * viewportState.Zoom, 0.7f, 3f);

                switch (relation.Kind)
                {
                    case FrameworkArchitectureRelationKind.Inheritance:
                        Handles.color = new Color(0.80f, 0.80f, 0.95f, alpha);
                        Handles.DrawAAPolyLine(lineWidth, from, to);
                        break;
                    case FrameworkArchitectureRelationKind.InterfaceImplementation:
                        Handles.color = new Color(0.55f, 0.92f, 0.65f, alpha);
                        Handles.DrawDottedLine(from, to, Mathf.Max(1.5f, 5f * viewportState.Zoom));
                        break;
                    default:
                        Handles.color = new Color(0.95f, 0.72f, 0.38f, alpha);
                        Handles.DrawDottedLine(from, to, Mathf.Max(1f, 2.5f * viewportState.Zoom));
                        break;
                }

                DrawArrowHead(from, to, viewportState.Zoom);
                if (relation.IsAggregated && relation.Count > 1 && viewportState.Zoom >= 0.22f)
                {
                    var countRect = new Rect(
                        (from.x + to.x) * 0.5f - 20f,
                        (from.y + to.y) * 0.5f - 10f,
                        40f,
                        20f);
                    GUI.Label(countRect, $"×{relation.Count}", EditorStyles.miniBoldLabel);
                }
            }

            Handles.color = oldColor;
            Handles.EndGUI();
        }

        #endregion

        #region 层标题与样式

        private static void DrawLayerHeaders(
            FrameworkGraphViewportState viewportState,
            IReadOnlyDictionary<FrameworkArchitectureLayer, float> layerXs)
        {
            if (viewportState.Zoom < 0.20f)
            {
                return;
            }

            foreach (var pair in layerXs.OrderBy(pair => pair.Key))
            {
                var rect = viewportState.CanvasToViewport(new Rect(
                    pair.Value,
                    FrameworkArchitectureGraphLayout.CanvasPadding,
                    FrameworkArchitectureGraphLayout.NodeWidth,
                    22f));
                GUI.Label(rect, GetLayerName(pair.Key), FrameworkCenterStyles.GraphLayerLabel);
            }
        }

        private static GUIStyle CreateGroupHeaderStyle(float zoom)
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(11f * zoom), 6, 13),
                padding = new RectOffset(5, 5, 0, 0),
            };
        }

        private static string GetGroupLabel(
            FrameworkArchitectureGraphLayout.GroupEntry entry,
            float zoom)
        {
            if (zoom < 0.20f)
            {
                return string.Empty;
            }

            if (zoom < 0.48f)
            {
                return entry.Group.DisplayName;
            }

            return $"{entry.Group.DisplayName}    " +
                   $"{entry.Group.DescendantTypeCount} 类型 · " +
                   $"{entry.Group.DescendantAssemblyCount} 程序集";
        }

        private static string GetTypeLabel(
            FrameworkArchitectureTypeDescriptor node,
            float zoom)
        {
            if (zoom < 0.22f)
            {
                return string.Empty;
            }

            if (zoom < 0.58f)
            {
                return node.Metadata.DisplayName;
            }

            return $"{node.Metadata.DisplayName}\n{node.Type.Name}\n{GetKindName(node.Kind)}";
        }

        #endregion

        #region 选择与搜索辅助

        private static void NormalizeSelection(
            FrameworkArchitectureGraphLayout layout,
            ref FrameworkArchitectureGroupDescriptor selectedGroup,
            ref FrameworkArchitectureTypeDescriptor selectedType)
        {
            if (selectedType != null && !layout.TypeRects.ContainsKey(selectedType))
            {
                if (layout.TryGetVisibleEndpoint(selectedType, out var endpoint) && endpoint.IsGroup)
                {
                    selectedGroup = endpoint.Group;
                }

                selectedType = null;
            }

            if (selectedGroup == null || layout.Groups.ContainsKey(selectedGroup))
            {
                return;
            }

            var current = selectedGroup.Parent;
            while (current != null && !layout.Groups.ContainsKey(current))
            {
                current = current.Parent;
            }

            selectedGroup = current;
        }

        private static bool ContainsSearchMatch(
            FrameworkArchitectureGroupDescriptor group,
            FrameworkArchitectureGraphLayout.SearchResult search)
        {
            if (search.MatchedGroups.Contains(group))
            {
                return true;
            }

            foreach (var type in search.MatchedTypes)
            {
                if (IsSameOrDescendant(type.Group, group))
                {
                    return true;
                }
            }

            foreach (var matchedGroup in search.MatchedGroups)
            {
                if (IsSameOrDescendant(matchedGroup, group))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameOrDescendant(
            FrameworkArchitectureGroupDescriptor candidate,
            FrameworkArchitectureGroupDescriptor ancestor)
        {
            var current = candidate;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        #endregion

        #region 几何与显示名称

        private static Rect ExpandRect(Rect rect, float padding)
        {
            return new Rect(
                rect.x - padding,
                rect.y - padding,
                rect.width + padding * 2f,
                rect.height + padding * 2f);
        }

        private static Vector3 GetEdgePoint(Rect rect, Vector2 toward)
        {
            var direction = toward - rect.center;
            if (Mathf.Abs(direction.x) * rect.height > Mathf.Abs(direction.y) * rect.width)
            {
                return new Vector3(
                    direction.x >= 0f ? rect.xMax : rect.xMin,
                    rect.center.y +
                    direction.y / Mathf.Max(1f, Mathf.Abs(direction.x)) * rect.width * 0.5f,
                    0f);
            }

            return new Vector3(
                rect.center.x +
                direction.x / Mathf.Max(1f, Mathf.Abs(direction.y)) * rect.height * 0.5f,
                direction.y >= 0f ? rect.yMax : rect.yMin,
                0f);
        }

        private static void DrawArrowHead(Vector3 from, Vector3 to, float zoom)
        {
            var direction = (to - from).normalized;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            var perpendicular = new Vector3(-direction.y, direction.x, 0f);
            var back = to - direction * Mathf.Max(3f, 8f * zoom);
            var wing = Mathf.Max(1.5f, 3.5f * zoom);
            var lineWidth = Mathf.Clamp(2f * zoom, 0.7f, 3f);
            Handles.DrawAAPolyLine(lineWidth, to, back + perpendicular * wing);
            Handles.DrawAAPolyLine(lineWidth, to, back - perpendicular * wing);
        }

        private static Color GetKindColor(FrameworkArchitectureTypeKind kind)
        {
            switch (kind)
            {
                case FrameworkArchitectureTypeKind.Interface:
                    return InterfaceColor;
                case FrameworkArchitectureTypeKind.Struct:
                    return StructColor;
                case FrameworkArchitectureTypeKind.Enum:
                    return EnumColor;
                default:
                    return ClassColor;
            }
        }

        private static string GetKindName(FrameworkArchitectureTypeKind kind)
        {
            switch (kind)
            {
                case FrameworkArchitectureTypeKind.Interface:
                    return "Interface";
                case FrameworkArchitectureTypeKind.Struct:
                    return "Struct";
                case FrameworkArchitectureTypeKind.Enum:
                    return "Enum";
                default:
                    return "Class";
            }
        }

        private static string GetLayerName(FrameworkArchitectureLayer layer)
        {
            switch (layer)
            {
                case FrameworkArchitectureLayer.Contracts:
                    return "契约";
                case FrameworkArchitectureLayer.Configuration:
                    return "配置";
                case FrameworkArchitectureLayer.ModuleModel:
                    return "模块模型";
                case FrameworkArchitectureLayer.GraphAndScope:
                    return "图与作用域";
                case FrameworkArchitectureLayer.RuntimeDriving:
                    return "运行驱动";
                case FrameworkArchitectureLayer.PublicFacade:
                    return "公开门面";
                default:
                    return "编辑器集成";
            }
        }

        #endregion

        internal sealed class DrawResult
        {
            internal FrameworkArchitectureGraphLayout Layout { get; }
            internal FrameworkArchitectureGroupDescriptor SelectedGroup { get; }
            internal FrameworkArchitectureTypeDescriptor SelectedType { get; }
            internal int SearchMatchCount { get; }

            internal DrawResult(
                FrameworkArchitectureGraphLayout layout,
                FrameworkArchitectureGroupDescriptor selectedGroup,
                FrameworkArchitectureTypeDescriptor selectedType,
                int searchMatchCount)
            {
                Layout = layout;
                SelectedGroup = selectedGroup;
                SelectedType = selectedType;
                SearchMatchCount = searchMatchCount;
            }
        }
    }
}
