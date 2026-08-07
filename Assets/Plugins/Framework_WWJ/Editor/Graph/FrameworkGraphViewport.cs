using System;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 为 Framework_WWJ 节点图提供统一工具栏、裁剪、网格和鼠标导航。
    /// 具体图只负责布局节点与绘制业务语义，不重复实现视口行为。
    /// </summary>
    [FrameworkArchitecture(
        "共享图视口",
        "统一处理节点图适配、100% 重置、鼠标中心缩放和平移输入。",
        FrameworkArchitectureLayer.EditorIntegration,
        365,
        typeof(FrameworkGraphViewportState))]
    internal static class FrameworkGraphViewport
    {
        private const float ToolbarHeight = 24f;
        private const float GridSpacing = 32f;
        private const float ZoomSpeed = 0.08f;

        #region 绘制入口

        internal static void Draw(
            FrameworkGraphViewportState state,
            Rect contentBounds,
            float viewportHeight,
            Action<Rect> drawContent)
        {
            DrawToolbar(state);

            var viewport = GUILayoutUtility.GetRect(
                1f,
                viewportHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(viewportHeight));
            state.ApplyPendingView(new Rect(Vector2.zero, viewport.size), contentBounds);
            HandleInput(viewport, state);

            EditorGUI.DrawRect(viewport, FrameworkCenterStyles.GraphBackgroundColor);
            FrameworkCenterStyles.DrawBorder(viewport, FrameworkCenterStyles.BorderColor);

            GUI.BeginGroup(viewport);
            var localViewport = new Rect(Vector2.zero, viewport.size);
            DrawGrid(localViewport, state);
            drawContent?.Invoke(localViewport);
            GUI.EndGroup();
        }

        #endregion

        #region 工具栏

        private static void DrawToolbar(FrameworkGraphViewportState state)
        {
            EditorGUILayout.BeginHorizontal(FrameworkCenterStyles.GraphToolbar, GUILayout.Height(ToolbarHeight));
            GUILayout.Label("视图", FrameworkCenterStyles.ToolbarLabel, GUILayout.Width(34f));
            if (GUILayout.Button("适配", EditorStyles.toolbarButton, GUILayout.Width(48f)))
            {
                state.RequestFrameAll();
            }

            if (GUILayout.Button("100%", EditorStyles.toolbarButton, GUILayout.Width(48f)))
            {
                state.RequestResetToOne();
            }

            GUILayout.Space(6f);
            GUILayout.Label($"{state.Zoom * 100f:0}%", FrameworkCenterStyles.ToolbarLabel, GUILayout.Width(46f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("滚轮缩放  ·  中键 / Alt+左键平移", FrameworkCenterStyles.ToolbarHint);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 输入

        private static void HandleInput(Rect viewport, FrameworkGraphViewportState state)
        {
            var currentEvent = Event.current;
            var controlId = GUIUtility.GetControlID(FocusType.Passive, viewport);
            var pointerInside = viewport.Contains(currentEvent.mousePosition);

            if (currentEvent.type == EventType.ScrollWheel && pointerInside)
            {
                var localPointer = currentEvent.mousePosition - viewport.position;
                var zoomMultiplier = 1f - currentEvent.delta.y * ZoomSpeed;
                state.SetZoomAround(localPointer, state.Zoom * zoomMultiplier);
                GUI.changed = true;
                currentEvent.Use();
                return;
            }

            var startsPan = currentEvent.type == EventType.MouseDown && pointerInside &&
                            (currentEvent.button == 2 || currentEvent.button == 0 && currentEvent.alt);
            if (startsPan)
            {
                GUIUtility.hotControl = controlId;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
            {
                state.PanBy(currentEvent.delta);
                GUI.changed = true;
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }

        #endregion

        #region 网格

        private static void DrawGrid(Rect viewport, FrameworkGraphViewportState state)
        {
            var spacing = GridSpacing * state.Zoom;
            if (spacing < 12f)
            {
                spacing *= 2f;
            }

            var startX = Repeat(state.Pan.x, spacing);
            var startY = Repeat(state.Pan.y, spacing);
            var color = FrameworkCenterStyles.GraphGridColor;

            for (var x = startX; x < viewport.width; x += spacing)
            {
                EditorGUI.DrawRect(new Rect(x, 0f, 1f, viewport.height), color);
            }

            for (var y = startY; y < viewport.height; y += spacing)
            {
                EditorGUI.DrawRect(new Rect(0f, y, viewport.width, 1f), color);
            }
        }

        private static float Repeat(float value, float length)
        {
            return value - Mathf.Floor(value / length) * length;
        }

        #endregion
    }
}
