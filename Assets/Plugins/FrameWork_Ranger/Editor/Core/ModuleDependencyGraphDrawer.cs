using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 使用轻量 IMGUI 绘制 Resolver 结果。图完全只读，不提供拖动、修复或隐式资产写入。
    /// </summary>
    [FrameworkArchitecture(
        "模块依赖图绘制器",
        "按拓扑层级绘制只读 Global/Scene 模块配置图。",
        FrameworkArchitectureLayer.EditorIntegration,
        5,
        typeof(ModuleGraphResult))]
    internal static class ModuleDependencyGraphDrawer
    {
        private const float NodeWidth = 230f;
        private const float NodeHeight = 76f;
        private const float HorizontalGap = 54f;
        private const float VerticalGap = 18f;
        private const float ScopeGap = 42f;
        private const float CanvasPadding = 16f;

        private static readonly Color GlobalColor = new Color(0.28f, 0.55f, 0.82f, 0.9f);
        private static readonly Color SceneColor = new Color(0.90f, 0.56f, 0.22f, 0.9f);
        private static readonly Color DisabledColor = new Color(0.42f, 0.42f, 0.42f, 0.75f);
        private static readonly Color ErrorColor = new Color(0.78f, 0.25f, 0.25f, 0.92f);

        internal static void DrawDiagnostics(ModuleGraphResult result)
        {
            if (result.Diagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox("配置图校验通过。", MessageType.Info);
                return;
            }

            for (var i = 0; i < result.Diagnostics.Count; i++)
            {
                var diagnostic = result.Diagnostics[i];
                EditorGUILayout.HelpBox(
                    diagnostic.ToString(),
                    ToMessageType(diagnostic.Severity));
            }
        }

        internal static void DrawGraph(ModuleGraphResult result, FrameworkGraphViewportState viewportState)
        {
            if (result.Nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("当前配置中没有模块条目。", MessageType.Info);
                return;
            }

            var layout = BuildLayout(result);
            FrameworkGraphViewport.Draw(
                viewportState,
                new Rect(0f, 0f, layout.CanvasWidth, layout.CanvasHeight),
                Mathf.Min(480f, Mathf.Max(260f, layout.CanvasHeight)),
                _ =>
                {
                    DrawScopeLabels(viewportState, layout);
                    DrawDependencyLines(viewportState, result, layout.Rectangles);
                    DrawNodes(viewportState, result, layout.Rectangles);
                });
        }

        #region 布局

        private static GraphLayout BuildLayout(ModuleGraphResult result)
        {
            var maxLevel = 0;
            var globalRows = new Dictionary<int, int>();
            var sceneRows = new Dictionary<int, int>();
            for (var i = 0; i < result.Nodes.Count; i++)
            {
                var node = result.Nodes[i];
                maxLevel = Mathf.Max(maxLevel, node.TopologicalLevel);
                var rows = node.ScopeKind == ModuleScopeKind.Global ? globalRows : sceneRows;
                rows[node.TopologicalLevel] = rows.TryGetValue(node.TopologicalLevel, out var count)
                    ? count + 1
                    : 1;
            }

            var maxGlobalRows = MaxRows(globalRows);
            var maxSceneRows = MaxRows(sceneRows);
            var globalHeight = maxGlobalRows * (NodeHeight + VerticalGap);
            var sceneStartY = CanvasPadding + 24f + globalHeight + ScopeGap;
            var rectangles = new Dictionary<ModuleGraphNode, Rect>();
            var globalIndices = new Dictionary<int, int>();
            var sceneIndices = new Dictionary<int, int>();

            for (var i = 0; i < result.Nodes.Count; i++)
            {
                var node = result.Nodes[i];
                var indices = node.ScopeKind == ModuleScopeKind.Global ? globalIndices : sceneIndices;
                var row = indices.TryGetValue(node.TopologicalLevel, out var current) ? current : 0;
                indices[node.TopologicalLevel] = row + 1;

                var x = CanvasPadding + node.TopologicalLevel * (NodeWidth + HorizontalGap);
                var yBase = node.ScopeKind == ModuleScopeKind.Global
                    ? CanvasPadding + 24f
                    : sceneStartY + 24f;
                var y = yBase + row * (NodeHeight + VerticalGap);
                rectangles.Add(node, new Rect(x, y, NodeWidth, NodeHeight));
            }

            return new GraphLayout(
                rectangles,
                sceneStartY,
                CanvasPadding * 2f + (maxLevel + 1) * NodeWidth + maxLevel * HorizontalGap,
                sceneStartY + 24f + maxSceneRows * (NodeHeight + VerticalGap) + CanvasPadding);
        }

        private static int MaxRows(IReadOnlyDictionary<int, int> rows)
        {
            var max = 0;
            foreach (var pair in rows)
            {
                max = Mathf.Max(max, pair.Value);
            }

            return max;
        }

        #endregion

        #region 绘制

        private static void DrawScopeLabels(FrameworkGraphViewportState viewportState, GraphLayout layout)
        {
            var globalRect = viewportState.CanvasToViewport(
                new Rect(CanvasPadding, CanvasPadding, 200f, 20f));
            var sceneRect = viewportState.CanvasToViewport(
                new Rect(CanvasPadding, layout.SceneStartY, 200f, 20f));
            GUI.Label(
                globalRect,
                "Global Scope",
                FrameworkCenterStyles.GraphLayerLabel);
            GUI.Label(
                sceneRect,
                "Scene Scope",
                FrameworkCenterStyles.GraphLayerLabel);
        }

        private static void DrawDependencyLines(
            FrameworkGraphViewportState viewportState,
            ModuleGraphResult result,
            IReadOnlyDictionary<ModuleGraphNode, Rect> rectangles)
        {
            var firstNodeByType = new Dictionary<Type, ModuleGraphNode>();
            for (var i = 0; i < result.Nodes.Count; i++)
            {
                var node = result.Nodes[i];
                if (node.Enabled && node.ModuleType != null && !firstNodeByType.ContainsKey(node.ModuleType))
                {
                    firstNodeByType.Add(node.ModuleType, node);
                }
            }

            Handles.BeginGUI();
            var previousColor = Handles.color;
            Handles.color = new Color(0.75f, 0.75f, 0.75f, 0.8f);
            for (var i = 0; i < result.Nodes.Count; i++)
            {
                var dependent = result.Nodes[i];
                if (!dependent.Enabled)
                {
                    continue;
                }

                for (var dependencyIndex = 0; dependencyIndex < dependent.Dependencies.Count; dependencyIndex++)
                {
                    if (!firstNodeByType.TryGetValue(dependent.Dependencies[dependencyIndex], out var dependency))
                    {
                        continue;
                    }

                    var dependencyRect = viewportState.CanvasToViewport(rectangles[dependency]);
                    var dependentRect = viewportState.CanvasToViewport(rectangles[dependent]);
                    var from = dependencyRect.center;
                    var to = dependentRect.center;
                    from.x += dependencyRect.width * 0.5f;
                    to.x -= dependentRect.width * 0.5f;
                    var lineWidth = Mathf.Clamp(2f * viewportState.Zoom, 1f, 3f);
                    Handles.DrawAAPolyLine(lineWidth, from, to);
                    DrawArrowHead(from, to, viewportState.Zoom);
                }
            }

            Handles.color = previousColor;
            Handles.EndGUI();
        }

        private static void DrawNodes(
            FrameworkGraphViewportState viewportState,
            ModuleGraphResult result,
            IReadOnlyDictionary<ModuleGraphNode, Rect> rectangles)
        {
            var nodeStyle = FrameworkCenterStyles.CreateGraphNodeStyle(viewportState.Zoom);
            for (var i = 0; i < result.Nodes.Count; i++)
            {
                var node = result.Nodes[i];
                var rect = viewportState.CanvasToViewport(rectangles[node]);
                EditorGUI.DrawRect(rect, GetNodeColor(node, result));

                var typeName = node.ModuleType == null ? "<空模板>" : node.ModuleType.Name;
                var assetName = node.Template == null ? "无资产" : node.Template.name;
                var text = viewportState.Zoom < 0.58f
                    ? typeName
                    : $"{typeName}\n{assetName}\n优先级 {node.LoadPriority}  |  配置 #{node.ConfigIndex}\n{(node.Enabled ? "Enabled" : "Disabled")}";
                GUI.Box(rect, text, nodeStyle);
            }
        }

        private static Color GetNodeColor(ModuleGraphNode node, ModuleGraphResult result)
        {
            if (!node.Enabled)
            {
                return DisabledColor;
            }

            for (var i = 0; i < result.Diagnostics.Count; i++)
            {
                var diagnostic = result.Diagnostics[i];
                if (diagnostic.Severity == ModuleGraphDiagnosticSeverity.Error &&
                    diagnostic.ScopeKind == node.ScopeKind &&
                    diagnostic.ConfigIndex == node.ConfigIndex)
                {
                    return ErrorColor;
                }
            }

            return node.ScopeKind == ModuleScopeKind.Global ? GlobalColor : SceneColor;
        }

        private static void DrawArrowHead(Vector3 from, Vector3 to, float zoom)
        {
            var direction = (to - from).normalized;
            var perpendicular = new Vector3(-direction.y, direction.x, 0f);
            var tip = to;
            var back = to - direction * Mathf.Max(4f, 9f * zoom);
            var wing = Mathf.Max(2f, 4f * zoom);
            var lineWidth = Mathf.Clamp(2f * zoom, 1f, 3f);
            Handles.DrawAAPolyLine(lineWidth, tip, back + perpendicular * wing);
            Handles.DrawAAPolyLine(lineWidth, tip, back - perpendicular * wing);
        }

        #endregion

        private static MessageType ToMessageType(ModuleGraphDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case ModuleGraphDiagnosticSeverity.Warning:
                    return MessageType.Warning;
                case ModuleGraphDiagnosticSeverity.Error:
                    return MessageType.Error;
                default:
                    return MessageType.Info;
            }
        }

        private sealed class GraphLayout
        {
            internal IReadOnlyDictionary<ModuleGraphNode, Rect> Rectangles { get; }
            internal float SceneStartY { get; }
            internal float CanvasWidth { get; }
            internal float CanvasHeight { get; }

            internal GraphLayout(
                IReadOnlyDictionary<ModuleGraphNode, Rect> rectangles,
                float sceneStartY,
                float canvasWidth,
                float canvasHeight)
            {
                Rectangles = rectangles;
                SceneStartY = sceneStartY;
                CanvasWidth = canvasWidth;
                CanvasHeight = canvasHeight;
            }
        }
    }
}
