using System;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 在共享 IMGUI 视口中绘制紧凑分组卡片、可展开容器、类型节点与关系曲线。
    /// </summary>
    [FrameworkArchitecture(
        "代码架构复合图绘制器",
        "绘制紧凑复合架构图，并处理选择、展开、会话拖动、聚焦和源码菜单。",
        FrameworkArchitectureLayer.EditorIntegration,
        350,
        typeof(FrameworkArchitectureGraphLayout),
        typeof(FrameworkArchitectureGraphPositionState),
        typeof(FrameworkGraphViewport))]
    internal static class FrameworkArchitectureGraphDrawer
    {
        private const float DragThreshold = 4f;
        private const int InteractionControlHint = 0x46574731;

        private static readonly Vector3[] s_linePoints = new Vector3[2];
        private static readonly Vector3[] s_arrowPoints = new Vector3[3];

        #region 绘制入口

        internal static DrawResult Draw(
            FrameworkArchitectureGraphLayout layout,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            FrameworkArchitectureGraphPositionState positionState,
            InteractionState interactionState,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType,
            bool frameSearchMatches)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

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
                viewport =>
                {
                    var hover = HitTest(layout, viewportState, Event.current.mousePosition);
                    DrawGroupContainers(viewport, viewportState, layout, selectedGroup);
                    DrawRelations(viewport, viewportState, layout, selectedGroup, selectedType);
                    DrawLayerHeaders(viewport, viewportState, layout);
                    DrawGroupCards(
                        viewport,
                        viewportState,
                        layout,
                        hover,
                        selectedGroup,
                        selectedType);
                    DrawTypeNodes(
                        viewport,
                        viewportState,
                        layout,
                        hover,
                        selectedGroup,
                        selectedType);
                    HandleKeyboard(
                        layout,
                        viewportState,
                        selectedGroup,
                        selectedType);
                    HandlePointer(
                        layout,
                        viewportState,
                        expansionState,
                        positionState,
                        interactionState,
                        hover,
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

        private static void DrawGroupContainers(
            Rect viewport,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureGroupDescriptor selectedGroup)
        {
            Handles.BeginGUI();
            for (var entryIndex = 0; entryIndex < layout.GroupEntries.Count; entryIndex++)
            {
                var entry = layout.GroupEntries[entryIndex];
                if (!entry.IsExpanded)
                {
                    continue;
                }

                var rect = viewportState.CanvasToViewport(entry.Bounds);
                if (!IsVisible(rect, viewport))
                {
                    continue;
                }

                var depthFactor = Mathf.Clamp01(entry.Depth / 6f);
                var fill = Color.Lerp(GetContainerColor(), GetNestedContainerColor(), depthFactor);
                if (layout.Search.IsActive && !ContainsSearchMatch(entry.Group, layout.Search))
                {
                    fill.a *= 0.42f;
                }

                FrameworkCenterStyles.DrawRoundedRect(
                    rect,
                    fill,
                    ReferenceEquals(entry.Group, selectedGroup)
                        ? GetSelectedColor()
                        : GetContainerBorderColor(),
                    Mathf.Clamp(9f * viewportState.Zoom, 3f, 9f),
                    ReferenceEquals(entry.Group, selectedGroup) ? 2f : 1f);
            }

            Handles.EndGUI();
        }

        private static void DrawGroupCards(
            Rect viewport,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            HitTarget hover,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            Handles.BeginGUI();
            for (var entryIndex = 0; entryIndex < layout.GroupEntries.Count; entryIndex++)
            {
                var entry = layout.GroupEntries[entryIndex];
                var rect = viewportState.CanvasToViewport(entry.NodeRect);
                if (!IsVisible(rect, viewport))
                {
                    continue;
                }

                var isSelected = ReferenceEquals(entry.Group, selectedGroup);
                var isHovered = hover != null && ReferenceEquals(hover.Group, entry.Group);
                var isDimmed = ShouldDimGroup(layout, entry.Group, selectedGroup, selectedType);
                var fill = entry.IsExpanded ? GetExpandedHeaderColor() : GetGroupCardColor();
                if (isHovered)
                {
                    fill = Color.Lerp(fill, GetHoverColor(), 0.42f);
                }

                if (isDimmed)
                {
                    fill.a *= 0.38f;
                }

                FrameworkCenterStyles.DrawRoundedRect(
                    rect,
                    fill,
                    isSelected ? GetSelectedColor() : GetGroupBorderColor(),
                    Mathf.Clamp((entry.IsExpanded ? 8f : 10f) * viewportState.Zoom, 3f, 10f),
                    isSelected ? 2.4f : isHovered ? 1.8f : 1f);
            }

            Handles.EndGUI();

            for (var entryIndex = 0; entryIndex < layout.GroupEntries.Count; entryIndex++)
            {
                var entry = layout.GroupEntries[entryIndex];
                var rect = viewportState.CanvasToViewport(entry.NodeRect);
                if (!IsVisible(rect, viewport))
                {
                    continue;
                }

                var previousGuiColor = GUI.color;
                if (ShouldDimGroup(layout, entry.Group, selectedGroup, selectedType))
                {
                    GUI.color = new Color(
                        previousGuiColor.r,
                        previousGuiColor.g,
                        previousGuiColor.b,
                        previousGuiColor.a * 0.34f);
                }

                DrawGroupCardText(entry, rect, viewportState.Zoom, layout.Search.IsActive);
                GUI.color = previousGuiColor;
                EditorGUIUtility.AddCursorRect(
                    rect,
                    layout.Search.IsActive ? MouseCursor.Arrow : MouseCursor.MoveArrow);
            }
        }

        private static void DrawGroupCardText(
            FrameworkArchitectureGraphLayout.GroupEntry entry,
            Rect rect,
            float zoom,
            bool searchActive)
        {
            var foldoutRect = GetFoldoutRect(rect, zoom);
            var foldout = zoom < 0.20f ? string.Empty : entry.IsExpanded ? "−" : "+";
            GUI.Label(
                foldoutRect,
                new GUIContent(foldout, searchActive
                    ? "清空搜索后调整展开状态"
                    : entry.IsExpanded ? "收起分组" : "展开分组"),
                FrameworkCenterStyles.GetArchitectureBadge(zoom));

            if (zoom < 0.20f)
            {
                return;
            }

            var left = rect.x + Mathf.Max(7f, 13f * zoom);
            var right = foldoutRect.x - Mathf.Max(4f, 8f * zoom);
            var titleRight = entry.IsExpanded && zoom >= 0.58f
                ? Mathf.Max(left, right - 152f * zoom)
                : right;
            var titleRect = new Rect(
                left,
                rect.y + Mathf.Max(4f, 8f * zoom),
                Mathf.Max(1f, titleRight - left),
                Mathf.Max(12f, 22f * zoom));
            GUI.Label(
                titleRect,
                new GUIContent(entry.Group.DisplayName, entry.Group.Responsibility),
                FrameworkCenterStyles.GetArchitectureTitle(zoom));

            if (entry.IsExpanded)
            {
                if (zoom >= 0.58f)
                {
                    var countRect = new Rect(
                        Mathf.Max(left, right - 148f * zoom),
                        titleRect.y,
                        Mathf.Min(148f * zoom, right - left),
                        titleRect.height);
                    GUI.Label(
                        countRect,
                        $"{entry.Group.DescendantTypeCount} 类型 · {entry.Group.DescendantAssemblyCount} 程序集",
                        FrameworkCenterStyles.GetArchitectureBody(zoom));
                }

                return;
            }

            if (zoom >= 0.52f)
            {
                var responsibilityRect = new Rect(
                    left,
                    titleRect.yMax + 2f,
                    Mathf.Max(1f, rect.xMax - left - 12f * zoom),
                    Mathf.Max(10f, 19f * zoom));
                GUI.Label(
                    responsibilityRect,
                    entry.Group.Responsibility.Replace('\n', ' '),
                    FrameworkCenterStyles.GetArchitectureBody(zoom));

                var badgesRect = new Rect(
                    left,
                    rect.yMax - Mathf.Max(20f, 26f * zoom),
                    Mathf.Max(1f, rect.width - 26f * zoom),
                    Mathf.Max(12f, 18f * zoom));
                GUI.Label(
                    badgesRect,
                    $"TYPE {entry.Group.DescendantTypeCount}     ASM {entry.Group.DescendantAssemblyCount}",
                    FrameworkCenterStyles.GetArchitectureBadge(zoom));
            }
        }

        #endregion

        #region 类型与局部分层

        private static void DrawLayerHeaders(
            Rect viewport,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout)
        {
            if (viewportState.Zoom < 0.32f)
            {
                return;
            }

            for (var i = 0; i < layout.LayerHeaders.Count; i++)
            {
                var entry = layout.LayerHeaders[i];
                var rect = viewportState.CanvasToViewport(entry.Rect);
                if (!IsVisible(rect, viewport))
                {
                    continue;
                }

                var colorRect = new Rect(rect.x, rect.center.y - 2f, Mathf.Max(4f, 18f * viewportState.Zoom), 4f);
                EditorGUI.DrawRect(colorRect, GetLayerColor(entry.Layer));
                var labelRect = new Rect(
                    colorRect.xMax + 4f,
                    rect.y,
                    Mathf.Max(1f, rect.xMax - colorRect.xMax - 4f),
                    rect.height);
                GUI.Label(
                    labelRect,
                    GetLayerName(entry.Layer),
                    FrameworkCenterStyles.GetArchitectureBadge(viewportState.Zoom));
            }
        }

        private static void DrawTypeNodes(
            Rect viewport,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            HitTarget hover,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            Handles.BeginGUI();
            foreach (var pair in layout.TypeRects)
            {
                var node = pair.Key;
                var rect = viewportState.CanvasToViewport(pair.Value);
                if (!IsVisible(rect, viewport))
                {
                    continue;
                }

                var isSelected = ReferenceEquals(node, selectedType);
                var isHovered = hover != null && ReferenceEquals(hover.Type, node);
                var fill = GetTypeCardColor();
                if (isHovered)
                {
                    fill = Color.Lerp(fill, GetHoverColor(), 0.36f);
                }

                if (ShouldDimType(layout, node, selectedGroup, selectedType))
                {
                    fill.a *= 0.34f;
                }

                FrameworkCenterStyles.DrawRoundedRect(
                    rect,
                    fill,
                    isSelected ? GetSelectedColor() : GetKindColor(node.Kind),
                    Mathf.Clamp(8f * viewportState.Zoom, 3f, 8f),
                    isSelected ? 2.4f : isHovered ? 1.8f : 1f);
            }

            Handles.EndGUI();

            foreach (var pair in layout.TypeRects)
            {
                var node = pair.Key;
                var rect = viewportState.CanvasToViewport(pair.Value);
                if (!IsVisible(rect, viewport))
                {
                    continue;
                }

                var previousGuiColor = GUI.color;
                if (ShouldDimType(layout, node, selectedGroup, selectedType))
                {
                    GUI.color = new Color(
                        previousGuiColor.r,
                        previousGuiColor.g,
                        previousGuiColor.b,
                        previousGuiColor.a * 0.32f);
                }

                DrawTypeNodeText(node, rect, viewportState.Zoom);
                GUI.color = previousGuiColor;
                EditorGUIUtility.AddCursorRect(
                    rect,
                    layout.Search.IsActive ? MouseCursor.Arrow : MouseCursor.MoveArrow);
            }
        }

        private static void DrawTypeNodeText(
            FrameworkArchitectureTypeDescriptor node,
            Rect rect,
            float zoom)
        {
            var stripWidth = Mathf.Clamp(5f * zoom, 2f, 5f);
            EditorGUI.DrawRect(
                new Rect(rect.x + 1f, rect.y + 5f, stripWidth, Mathf.Max(1f, rect.height - 10f)),
                GetLayerColor(node.Metadata.Layer));
            if (zoom < 0.22f)
            {
                return;
            }

            var left = rect.x + Mathf.Max(8f, 13f * zoom);
            var titleRect = new Rect(
                left,
                rect.y + Mathf.Max(4f, 7f * zoom),
                Mathf.Max(1f, rect.width - (left - rect.x) - 8f * zoom),
                Mathf.Max(12f, 20f * zoom));
            GUI.Label(
                titleRect,
                new GUIContent(node.Metadata.DisplayName, node.Metadata.Responsibility),
                FrameworkCenterStyles.GetArchitectureTitle(zoom));

            if (zoom >= 0.50f)
            {
                var typeNameRect = new Rect(
                    left,
                    titleRect.yMax,
                    titleRect.width,
                    Mathf.Max(10f, 18f * zoom));
                GUI.Label(
                    typeNameRect,
                    node.Type.Name,
                    FrameworkCenterStyles.GetArchitectureBody(zoom));

                var metadataRect = new Rect(
                    left,
                    rect.yMax - Mathf.Max(18f, 22f * zoom),
                    titleRect.width,
                    Mathf.Max(10f, 16f * zoom));
                GUI.Label(
                    metadataRect,
                    $"{GetKindName(node.Kind)}  ·  {GetLayerName(node.Metadata.Layer)}",
                    FrameworkCenterStyles.GetArchitectureBadge(zoom));
            }
        }

        #endregion

        #region 关系绘制

        private static void DrawRelations(
            Rect viewport,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            Handles.BeginGUI();
            for (var i = 0; i < layout.Relations.Count; i++)
            {
                var relation = layout.Relations[i];
                var geometry = relation.Geometry;
                var curveBounds = viewportState.CanvasToViewport(geometry.Bounds);
                if (!IsVisible(ExpandRect(curveBounds, 20f), viewport))
                {
                    continue;
                }

                var start = viewportState.CanvasToViewport(geometry.Start);
                var control1 = viewportState.CanvasToViewport(geometry.Control1);
                var control2 = viewportState.CanvasToViewport(geometry.Control2);
                var end = viewportState.CanvasToViewport(geometry.End);
                var hasSelection = selectedGroup != null || selectedType != null;
                var related = !hasSelection || layout.IsRelatedToSelection(relation, selectedGroup, selectedType);
                var alpha = related ? 0.86f : 0.12f;
                if (layout.Search.IsActive && !relation.HasSearchMatch)
                {
                    alpha *= 0.26f;
                }

                var color = GetRelationColor(relation.Kind, alpha);
                var width = Mathf.Clamp((related ? 2.2f : 1.2f) * viewportState.Zoom, 0.7f, 3.2f);
                if (relation.Kind == FrameworkArchitectureRelationKind.Inheritance)
                {
                    Handles.DrawBezier(start, end, control1, control2, color, null, width);
                }
                else
                {
                    DrawDottedBezier(start, control1, control2, end, color, width);
                }

                DrawPort(start, color, viewportState.Zoom);
                DrawPort(end, color, viewportState.Zoom);
                DrawArrowHead(control2, end, color, viewportState.Zoom);
                if (relation.IsAggregated && relation.Count > 1 && viewportState.Zoom >= 0.28f)
                {
                    DrawRelationCount(
                        EvaluateBezier(start, control1, control2, end, 0.5f),
                        relation.Count,
                        color,
                        viewportState.Zoom);
                }
            }

            Handles.EndGUI();
        }

        private static void DrawDottedBezier(
            Vector2 start,
            Vector2 control1,
            Vector2 control2,
            Vector2 end,
            Color color,
            float width)
        {
            Handles.color = color;
            const int SegmentCount = 24;
            for (var i = 0; i < SegmentCount; i += 2)
            {
                s_linePoints[0] = EvaluateBezier(start, control1, control2, end, i / (float)SegmentCount);
                s_linePoints[1] = EvaluateBezier(start, control1, control2, end, (i + 1f) / SegmentCount);
                Handles.DrawAAPolyLine(width, s_linePoints);
            }
        }

        private static Vector2 EvaluateBezier(
            Vector2 start,
            Vector2 control1,
            Vector2 control2,
            Vector2 end,
            float time)
        {
            var inverse = 1f - time;
            return inverse * inverse * inverse * start +
                   3f * inverse * inverse * time * control1 +
                   3f * inverse * time * time * control2 +
                   time * time * time * end;
        }

        private static void DrawPort(Vector2 center, Color color, float zoom)
        {
            Handles.color = color;
            Handles.DrawSolidDisc(center, Vector3.forward, Mathf.Clamp(3.5f * zoom, 1.5f, 4f));
        }

        private static void DrawArrowHead(Vector2 control, Vector2 end, Color color, float zoom)
        {
            var direction = (end - control).normalized;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            var perpendicular = new Vector2(-direction.y, direction.x);
            var length = Mathf.Clamp(9f * zoom, 4f, 10f);
            var width = Mathf.Clamp(4.5f * zoom, 2f, 5f);
            s_arrowPoints[0] = end;
            s_arrowPoints[1] = end - direction * length + perpendicular * width;
            s_arrowPoints[2] = end - direction * length - perpendicular * width;
            Handles.color = color;
            Handles.DrawAAConvexPolygon(s_arrowPoints);
        }

        private static void DrawRelationCount(
            Vector2 center,
            int count,
            Color color,
            float zoom)
        {
            var rect = new Rect(
                center.x - 20f,
                center.y - 9f,
                40f,
                18f);
            FrameworkCenterStyles.DrawRoundedRect(
                rect,
                GetTypeCardColor(),
                color,
                9f,
                1f);
            GUI.Label(rect, $"×{count}", FrameworkCenterStyles.GetArchitectureBadge(zoom));
        }

        #endregion

        #region 输入与菜单

        private static void HandleKeyboard(
            FrameworkArchitectureGraphLayout layout,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown || currentEvent.keyCode != KeyCode.F)
            {
                return;
            }

            if (TryGetSelectionBounds(layout, selectedGroup, selectedType, out var bounds))
            {
                viewportState.RequestFrame(ExpandRect(bounds, 36f));
                currentEvent.Use();
            }
        }

        private static void HandlePointer(
            FrameworkArchitectureGraphLayout layout,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            FrameworkArchitectureGraphPositionState positionState,
            InteractionState interactionState,
            HitTarget hover,
            ref FrameworkArchitectureGroupDescriptor selectedGroup,
            ref FrameworkArchitectureTypeDescriptor selectedType)
        {
            var currentEvent = Event.current;
            var controlId = GUIUtility.GetControlID(InteractionControlHint, FocusType.Passive);
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && hover != null)
            {
                Select(hover, ref selectedGroup, ref selectedType);
                if (hover.Group != null &&
                    !layout.Search.IsActive &&
                    layout.Groups.TryGetValue(hover.Group, out var foldoutEntry) &&
                    GetFoldoutCanvasRect(foldoutEntry).Contains(
                        viewportState.ViewportToCanvas(currentEvent.mousePosition)))
                {
                    ToggleGroup(foldoutEntry, expansionState, ref selectedGroup, ref selectedType);
                    currentEvent.Use();
                    return;
                }

                if (!layout.Search.IsActive)
                {
                    interactionState.Begin(controlId, hover, currentEvent.mousePosition);
                    GUIUtility.hotControl = controlId;
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDrag &&
                GUIUtility.hotControl == interactionState.ControlId &&
                interactionState.Target != null)
            {
                interactionState.UpdateDrag(currentEvent.mousePosition);
                if (interactionState.IsDragging)
                {
                    var delta = ToCanvasDragDelta(currentEvent.delta, viewportState.Zoom);
                    if (interactionState.Target.Group != null)
                    {
                        var movingGroup = interactionState.Target.Group;
                        if (movingGroup.Parent != null &&
                            !movingGroup.Parent.IsRoot &&
                            layout.Groups.TryGetValue(movingGroup, out var movingEntry) &&
                            layout.Groups.TryGetValue(movingGroup.Parent, out var parentEntry))
                        {
                            delta = ClampDragDeltaToUpperLeftBoundary(
                                movingEntry.Bounds,
                                parentEntry.ContentRect.position,
                                delta);
                        }

                        positionState.MoveGroup(interactionState.Target.Group, delta);
                    }
                    else
                    {
                        var movingType = interactionState.Target.Type;
                        if (layout.TypeRects.TryGetValue(movingType, out var movingRect) &&
                            layout.Groups.TryGetValue(movingType.Group, out var ownerEntry))
                        {
                            var boundaryMinimum = ownerEntry.ContentRect.position;
                            for (var headerIndex = 0;
                                 headerIndex < layout.LayerHeaders.Count;
                                 headerIndex++)
                            {
                                var header = layout.LayerHeaders[headerIndex];
                                if (ReferenceEquals(header.Group, movingType.Group) &&
                                    header.Layer == movingType.Metadata.Layer)
                                {
                                    boundaryMinimum.y = header.Rect.yMax;
                                    break;
                                }
                            }

                            delta = ClampDragDeltaToUpperLeftBoundary(
                                movingRect,
                                boundaryMinimum,
                                delta);
                        }

                        positionState.MoveType(interactionState.Target.Type, delta);
                    }

                    GUI.changed = true;
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 0 &&
                GUIUtility.hotControl == interactionState.ControlId)
            {
                var target = interactionState.Target;
                var wasDragging = interactionState.IsDragging;
                interactionState.End(positionState);
                GUIUtility.hotControl = 0;
                if (!wasDragging && currentEvent.clickCount >= 2 && target != null)
                {
                    if (target.Group != null &&
                        layout.Groups.TryGetValue(target.Group, out var entry) &&
                        !layout.Search.IsActive)
                    {
                        ToggleGroup(entry, expansionState, ref selectedGroup, ref selectedType);
                    }
                    else if (target.Type?.Script != null)
                    {
                        AssetDatabase.OpenAsset(target.Type.Script);
                    }
                }

                currentEvent.Use();
                return;
            }

            if ((currentEvent.type == EventType.ContextClick ||
                 currentEvent.type == EventType.MouseUp && currentEvent.button == 1) &&
                hover != null)
            {
                Select(hover, ref selectedGroup, ref selectedType);
                ShowContextMenu(
                    layout,
                    viewportState,
                    expansionState,
                    positionState,
                    hover);
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseLeaveWindow && interactionState.Target != null)
            {
                interactionState.End(positionState);
            }
        }

        private static void ShowContextMenu(
            FrameworkArchitectureGraphLayout layout,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            FrameworkArchitectureGraphPositionState positionState,
            HitTarget target)
        {
            var menu = new GenericMenu();
            if (target.Group != null && layout.Groups.TryGetValue(target.Group, out var entry))
            {
                if (layout.Search.IsActive)
                {
                    menu.AddDisabledItem(new GUIContent("清空搜索后调整展开状态"));
                }
                else
                {
                    menu.AddItem(
                        new GUIContent(entry.IsExpanded ? "收起分组" : "展开分组"),
                        false,
                        () => expansionState.Toggle(
                            entry.Group,
                            FrameworkArchitectureGraphLayout.ExpansionState.GetHeaderAnchor(entry.HeaderRect)));
                }

                menu.AddItem(
                    new GUIContent("聚焦"),
                    false,
                    () => viewportState.RequestFrame(ExpandRect(entry.Bounds, 36f)));
                if (positionState.HasOffset(entry.Group))
                {
                    menu.AddItem(
                        new GUIContent("重置当前位置"),
                        false,
                        () =>
                        {
                            positionState.Reset(entry.Group);
                            positionState.Save();
                        });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("重置当前位置"));
                }
            }
            else if (target.Type != null && layout.TypeRects.TryGetValue(target.Type, out var rect))
            {
                menu.AddItem(
                    new GUIContent("聚焦"),
                    false,
                    () => viewportState.RequestFrame(ExpandRect(rect, 36f)));
                if (target.Type.Script != null)
                {
                    menu.AddItem(
                        new GUIContent("Ping 脚本"),
                        false,
                        () =>
                        {
                            Selection.activeObject = target.Type.Script;
                            EditorGUIUtility.PingObject(target.Type.Script);
                        });
                    menu.AddItem(
                        new GUIContent("打开脚本"),
                        false,
                        () => AssetDatabase.OpenAsset(target.Type.Script));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Ping 脚本"));
                    menu.AddDisabledItem(new GUIContent("打开脚本"));
                }

                if (positionState.HasOffset(target.Type))
                {
                    menu.AddItem(
                        new GUIContent("重置当前位置"),
                        false,
                        () =>
                        {
                            positionState.Reset(target.Type);
                            positionState.Save();
                        });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("重置当前位置"));
                }
            }

            menu.ShowAsContext();
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

            expansionState.Toggle(
                entry.Group,
                FrameworkArchitectureGraphLayout.ExpansionState.GetHeaderAnchor(entry.HeaderRect));
            GUI.changed = true;
        }

        #endregion

        #region 命中与选择

        private static HitTarget HitTest(
            FrameworkArchitectureGraphLayout layout,
            FrameworkGraphViewportState viewportState,
            Vector2 viewportPoint)
        {
            var canvasPoint = viewportState.ViewportToCanvas(viewportPoint);
            foreach (var pair in layout.TypeRects)
            {
                if (pair.Value.Contains(canvasPoint))
                {
                    return HitTarget.ForType(pair.Key);
                }
            }

            FrameworkArchitectureGraphLayout.GroupEntry best = null;
            foreach (var entry in layout.Groups.Values)
            {
                if (entry.NodeRect.Contains(canvasPoint) &&
                    (best == null || entry.Depth >= best.Depth))
                {
                    best = entry;
                }
            }

            return best == null ? null : HitTarget.ForGroup(best.Group);
        }

        private static void Select(
            HitTarget target,
            ref FrameworkArchitectureGroupDescriptor selectedGroup,
            ref FrameworkArchitectureTypeDescriptor selectedType)
        {
            selectedGroup = target.Group;
            selectedType = target.Type;
        }

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

        private static bool TryGetSelectionBounds(
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType,
            out Rect bounds)
        {
            if (selectedType != null && layout.TypeRects.TryGetValue(selectedType, out bounds))
            {
                return true;
            }

            if (selectedGroup != null && layout.Groups.TryGetValue(selectedGroup, out var entry))
            {
                bounds = entry.Bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private static bool ShouldDimGroup(
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureGroupDescriptor group,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            if (layout.Search.IsActive && !ContainsSearchMatch(group, layout.Search))
            {
                return true;
            }

            if (selectedGroup == null && selectedType == null ||
                selectedGroup != null &&
                (IsSameOrDescendant(group, selectedGroup) ||
                 IsSameOrDescendant(selectedGroup, group)) ||
                selectedType != null && IsSameOrDescendant(selectedType.Group, group))
            {
                return false;
            }

            for (var i = 0; i < layout.Relations.Count; i++)
            {
                var relation = layout.Relations[i];
                if (layout.IsRelatedToSelection(relation, selectedGroup, selectedType) &&
                    (FrameworkArchitectureGraphLayout.EndpointBelongsToGroup(relation.Source, group) ||
                     FrameworkArchitectureGraphLayout.EndpointBelongsToGroup(relation.Target, group)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ShouldDimType(
            FrameworkArchitectureGraphLayout layout,
            FrameworkArchitectureTypeDescriptor type,
            FrameworkArchitectureGroupDescriptor selectedGroup,
            FrameworkArchitectureTypeDescriptor selectedType)
        {
            if (layout.Search.IsActive && !layout.Search.MatchedTypes.Contains(type))
            {
                return true;
            }

            if (selectedGroup == null && selectedType == null ||
                ReferenceEquals(type, selectedType) ||
                selectedGroup != null && IsSameOrDescendant(type.Group, selectedGroup))
            {
                return false;
            }

            for (var i = 0; i < layout.Relations.Count; i++)
            {
                var relation = layout.Relations[i];
                if (layout.IsRelatedToSelection(relation, selectedGroup, selectedType) &&
                    (ReferenceEquals(relation.Source.Type, type) ||
                     ReferenceEquals(relation.Target.Type, type)))
                {
                    return false;
                }
            }

            return true;
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

        #region 几何与显示

        private static Rect GetFoldoutCanvasRect(FrameworkArchitectureGraphLayout.GroupEntry entry)
        {
            return new Rect(entry.NodeRect.xMax - 28f, entry.NodeRect.y + 6f, 22f, 22f);
        }

        internal static Vector2 ToCanvasDragDelta(Vector2 viewportDelta, float zoom)
        {
            return viewportDelta / Mathf.Max(0.0001f, zoom);
        }

        internal static Vector2 ClampDragDeltaToUpperLeftBoundary(
            Rect movingRect,
            Vector2 boundaryMinimum,
            Vector2 desiredDelta)
        {
            desiredDelta.x = Mathf.Max(desiredDelta.x, boundaryMinimum.x - movingRect.xMin);
            desiredDelta.y = Mathf.Max(desiredDelta.y, boundaryMinimum.y - movingRect.yMin);
            return desiredDelta;
        }

        private static Rect GetFoldoutRect(Rect viewportRect, float zoom)
        {
            var size = Mathf.Clamp(22f * zoom, 10f, 22f);
            return new Rect(
                viewportRect.xMax - size - Mathf.Max(3f, 6f * zoom),
                viewportRect.y + Mathf.Max(3f, 6f * zoom),
                size,
                size);
        }

        private static Rect ExpandRect(Rect rect, float padding)
        {
            return new Rect(
                rect.x - padding,
                rect.y - padding,
                rect.width + padding * 2f,
                rect.height + padding * 2f);
        }

        private static bool IsVisible(Rect rect, Rect viewport)
        {
            return rect.Overlaps(viewport, true);
        }

        private static Color GetContainerColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.105f, 0.12f, 0.145f, 0.94f)
                : new Color(0.82f, 0.85f, 0.89f, 0.96f);
        }

        private static Color GetNestedContainerColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.16f, 0.19f, 0.94f)
                : new Color(0.88f, 0.90f, 0.93f, 0.96f);
        }

        private static Color GetContainerBorderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.29f, 0.42f, 0.57f, 0.84f)
                : new Color(0.37f, 0.50f, 0.68f, 0.86f);
        }

        private static Color GetGroupCardColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.245f, 0.34f, 0.98f)
                : new Color(0.73f, 0.82f, 0.94f, 0.99f);
        }

        private static Color GetExpandedHeaderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.13f, 0.31f, 0.48f, 0.99f)
                : new Color(0.55f, 0.73f, 0.94f, 0.99f);
        }

        private static Color GetTypeCardColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.19f, 0.205f, 0.235f, 0.99f)
                : new Color(0.93f, 0.94f, 0.96f, 0.99f);
        }

        private static Color GetHoverColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.34f, 0.43f, 0.55f, 1f)
                : new Color(0.78f, 0.88f, 1f, 1f);
        }

        private static Color GetGroupBorderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.36f, 0.66f, 0.92f, 0.88f)
                : new Color(0.18f, 0.43f, 0.75f, 0.90f);
        }

        private static Color GetSelectedColor()
        {
            return new Color(0.98f, 0.64f, 0.20f, 1f);
        }

        private static Color GetKindColor(FrameworkArchitectureTypeKind kind)
        {
            switch (kind)
            {
                case FrameworkArchitectureTypeKind.Interface:
                    return new Color(0.35f, 0.78f, 0.52f, 1f);
                case FrameworkArchitectureTypeKind.Struct:
                    return new Color(0.61f, 0.51f, 0.88f, 1f);
                case FrameworkArchitectureTypeKind.Enum:
                    return new Color(0.24f, 0.72f, 0.78f, 1f);
                default:
                    return new Color(0.28f, 0.59f, 0.90f, 1f);
            }
        }

        private static Color GetLayerColor(FrameworkArchitectureLayer layer)
        {
            switch (layer)
            {
                case FrameworkArchitectureLayer.Contracts:
                    return new Color(0.32f, 0.72f, 0.91f, 1f);
                case FrameworkArchitectureLayer.Configuration:
                    return new Color(0.62f, 0.55f, 0.91f, 1f);
                case FrameworkArchitectureLayer.ModuleModel:
                    return new Color(0.34f, 0.77f, 0.54f, 1f);
                case FrameworkArchitectureLayer.GraphAndScope:
                    return new Color(0.93f, 0.67f, 0.28f, 1f);
                case FrameworkArchitectureLayer.RuntimeDriving:
                    return new Color(0.91f, 0.43f, 0.37f, 1f);
                case FrameworkArchitectureLayer.PublicFacade:
                    return new Color(0.32f, 0.78f, 0.78f, 1f);
                default:
                    return new Color(0.66f, 0.68f, 0.72f, 1f);
            }
        }

        private static Color GetRelationColor(
            FrameworkArchitectureRelationKind kind,
            float alpha)
        {
            switch (kind)
            {
                case FrameworkArchitectureRelationKind.Inheritance:
                    return new Color(0.70f, 0.76f, 1f, alpha);
                case FrameworkArchitectureRelationKind.InterfaceImplementation:
                    return new Color(0.48f, 0.90f, 0.61f, alpha);
                default:
                    return new Color(0.98f, 0.69f, 0.31f, alpha);
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

        #region 结果与交互状态

        internal sealed class InteractionState
        {
            internal int ControlId { get; private set; }
            internal HitTarget Target { get; private set; }
            internal bool IsDragging { get; private set; }

            private Vector2 m_pressPosition;

            internal void Begin(int controlId, HitTarget target, Vector2 pressPosition)
            {
                ControlId = controlId;
                Target = target;
                m_pressPosition = pressPosition;
                IsDragging = false;
            }

            internal void UpdateDrag(Vector2 pointerPosition)
            {
                if (!IsDragging &&
                    (pointerPosition - m_pressPosition).sqrMagnitude >= DragThreshold * DragThreshold)
                {
                    IsDragging = true;
                }
            }

            internal void End(FrameworkArchitectureGraphPositionState positionState)
            {
                positionState?.Save();
                if (ControlId != 0 && GUIUtility.hotControl == ControlId)
                {
                    GUIUtility.hotControl = 0;
                }

                ControlId = 0;
                Target = null;
                IsDragging = false;
            }
        }

        internal sealed class HitTarget
        {
            internal FrameworkArchitectureGroupDescriptor Group { get; }
            internal FrameworkArchitectureTypeDescriptor Type { get; }

            private HitTarget(
                FrameworkArchitectureGroupDescriptor group,
                FrameworkArchitectureTypeDescriptor type)
            {
                Group = group;
                Type = type;
            }

            internal static HitTarget ForGroup(FrameworkArchitectureGroupDescriptor group)
            {
                return new HitTarget(group, null);
            }

            internal static HitTarget ForType(FrameworkArchitectureTypeDescriptor type)
            {
                return new HitTarget(null, type);
            }
        }

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

        #endregion
    }
}
