using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 架构图程序集筛选范围。
    /// </summary>
    internal enum FrameworkArchitectureAssemblyFilter
    {
        All,
        Runtime,
        Editor,
    }

    /// <summary>
    /// 使用 IMGUI 与 Handles 绘制可点击的分层代码架构图。
    /// </summary>
    [FrameworkArchitecture(
        "代码架构图绘制器",
        "按逻辑层绘制类与接口节点、关系边，并处理选择和源码双击。",
        FrameworkArchitectureLayer.EditorIntegration,
        350,
        typeof(FrameworkArchitectureCatalog))]
    internal static class FrameworkArchitectureGraphDrawer
    {
        private const float NodeWidth = 214f;
        private const float NodeHeight = 72f;
        private const float LayerGap = 58f;
        private const float RowGap = 16f;
        private const float HeaderHeight = 30f;
        private const float Padding = 14f;

        private static readonly Color ClassColor = new Color(0.20f, 0.48f, 0.72f, 0.92f);
        private static readonly Color InterfaceColor = new Color(0.35f, 0.67f, 0.48f, 0.92f);
        private static readonly Color SelectedColor = new Color(0.95f, 0.62f, 0.18f, 0.98f);
        private static readonly Color DimColor = new Color(0.30f, 0.30f, 0.30f, 0.62f);

        internal static FrameworkArchitectureTypeDescriptor Draw(
            FrameworkArchitectureCatalog catalog,
            FrameworkGraphViewportState viewportState,
            FrameworkArchitectureTypeDescriptor selected,
            string searchText,
            FrameworkArchitectureAssemblyFilter assemblyFilter)
        {
            var visibleNodes = catalog.Nodes.Where(node => MatchesAssembly(node, assemblyFilter)).ToArray();
            if (visibleNodes.Length == 0)
            {
                EditorGUILayout.HelpBox("当前筛选下没有架构节点。", MessageType.Info);
                return selected;
            }

            var layout = BuildLayout(visibleNodes);
            FrameworkGraphViewport.Draw(
                viewportState,
                new Rect(0f, 0f, layout.CanvasWidth, layout.CanvasHeight),
                Mathf.Min(610f, Mathf.Max(360f, layout.CanvasHeight)),
                _ =>
                {
                    DrawLayerHeaders(viewportState, layout.LayerXs);
                    DrawRelations(viewportState, catalog.Relations, layout.NodeRects);
                    selected = DrawNodes(
                        viewportState,
                        visibleNodes,
                        layout.NodeRects,
                        selected,
                        searchText);
                });
            return selected;
        }

        #region 布局

        private static GraphLayout BuildLayout(IReadOnlyList<FrameworkArchitectureTypeDescriptor> nodes)
        {
            var nodeRects = new Dictionary<FrameworkArchitectureTypeDescriptor, Rect>();
            var layerXs = new Dictionary<FrameworkArchitectureLayer, float>();
            var layers = nodes.Select(node => node.Metadata.Layer).Distinct().OrderBy(layer => layer).ToArray();
            var maxRows = 0;

            for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                var layer = layers[layerIndex];
                var x = Padding + layerIndex * (NodeWidth + LayerGap);
                layerXs.Add(layer, x);
                var layerNodes = nodes.Where(node => node.Metadata.Layer == layer).ToArray();
                maxRows = Mathf.Max(maxRows, layerNodes.Length);
                for (var row = 0; row < layerNodes.Length; row++)
                {
                    nodeRects.Add(
                        layerNodes[row],
                        new Rect(x, Padding + HeaderHeight + row * (NodeHeight + RowGap), NodeWidth, NodeHeight));
                }
            }

            var width = Padding * 2f + layers.Length * NodeWidth + Mathf.Max(0, layers.Length - 1) * LayerGap;
            var height = Padding * 2f + HeaderHeight + maxRows * (NodeHeight + RowGap);
            return new GraphLayout(nodeRects, layerXs, width, height);
        }

        #endregion

        #region 绘制

        private static void DrawLayerHeaders(
            FrameworkGraphViewportState viewportState,
            IReadOnlyDictionary<FrameworkArchitectureLayer, float> layerXs)
        {
            foreach (var pair in layerXs)
            {
                var rect = viewportState.CanvasToViewport(
                    new Rect(pair.Value, Padding, NodeWidth, 22f));
                GUI.Label(
                    rect,
                    GetLayerName(pair.Key),
                    FrameworkCenterStyles.GraphLayerLabel);
            }
        }

        private static void DrawRelations(
            FrameworkGraphViewportState viewportState,
            IReadOnlyList<FrameworkArchitectureRelation> relations,
            IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> nodeRects)
        {
            Handles.BeginGUI();
            var oldColor = Handles.color;
            for (var i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                if (!nodeRects.TryGetValue(relation.Source, out var sourceRect) ||
                    !nodeRects.TryGetValue(relation.Target, out var targetRect))
                {
                    continue;
                }

                sourceRect = viewportState.CanvasToViewport(sourceRect);
                targetRect = viewportState.CanvasToViewport(targetRect);
                var from = sourceRect.center;
                var to = targetRect.center;
                from.x += Mathf.Sign(to.x - from.x) * sourceRect.width * 0.5f;
                to.x -= Mathf.Sign(to.x - from.x) * targetRect.width * 0.5f;
                var lineWidth = Mathf.Clamp(2f * viewportState.Zoom, 1f, 3f);

                switch (relation.Kind)
                {
                    case FrameworkArchitectureRelationKind.Inheritance:
                        Handles.color = new Color(0.80f, 0.80f, 0.95f, 0.75f);
                        Handles.DrawAAPolyLine(lineWidth, from, to);
                        break;
                    case FrameworkArchitectureRelationKind.InterfaceImplementation:
                        Handles.color = new Color(0.55f, 0.92f, 0.65f, 0.72f);
                        Handles.DrawDottedLine(from, to, Mathf.Max(2f, 5f * viewportState.Zoom));
                        break;
                    default:
                        Handles.color = new Color(0.95f, 0.72f, 0.38f, 0.65f);
                        Handles.DrawDottedLine(from, to, Mathf.Max(1.5f, 2.5f * viewportState.Zoom));
                        break;
                }

                DrawArrowHead(from, to, viewportState.Zoom);
            }

            Handles.color = oldColor;
            Handles.EndGUI();
        }

        private static FrameworkArchitectureTypeDescriptor DrawNodes(
            FrameworkGraphViewportState viewportState,
            IReadOnlyList<FrameworkArchitectureTypeDescriptor> nodes,
            IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> nodeRects,
            FrameworkArchitectureTypeDescriptor selected,
            string searchText)
        {
            var nodeStyle = FrameworkCenterStyles.CreateGraphNodeStyle(viewportState.Zoom);
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var rect = viewportState.CanvasToViewport(nodeRects[node]);
                var matchesSearch = string.IsNullOrWhiteSpace(searchText) || MatchesSearch(node, searchText);
                var color = ReferenceEquals(node, selected)
                    ? SelectedColor
                    : !matchesSearch ? DimColor : node.IsInterface ? InterfaceColor : ClassColor;

                EditorGUI.DrawRect(rect, color);
                if (node.IsInterface)
                {
                    DrawBorder(rect, new Color(0.72f, 1f, 0.80f, 1f), 2f);
                }

                var label = viewportState.Zoom < 0.58f
                    ? node.Metadata.DisplayName
                    : $"{node.Metadata.DisplayName}\n{node.Type.Name}\n{(node.IsInterface ? "Interface" : "Class")}";
                if (GUI.Button(rect, label, nodeStyle))
                {
                    selected = node;
                    if (Event.current.clickCount >= 2 && node.Script != null)
                    {
                        AssetDatabase.OpenAsset(node.Script);
                    }
                }
            }

            return selected;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawArrowHead(Vector3 from, Vector3 to, float zoom)
        {
            var direction = (to - from).normalized;
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            var perpendicular = new Vector3(-direction.y, direction.x, 0f);
            var back = to - direction * Mathf.Max(4f, 8f * zoom);
            var wing = Mathf.Max(2f, 3.5f * zoom);
            var lineWidth = Mathf.Clamp(2f * zoom, 1f, 3f);
            Handles.DrawAAPolyLine(lineWidth, to, back + perpendicular * wing);
            Handles.DrawAAPolyLine(lineWidth, to, back - perpendicular * wing);
        }

        #endregion

        #region 筛选辅助

        private static bool MatchesAssembly(
            FrameworkArchitectureTypeDescriptor node,
            FrameworkArchitectureAssemblyFilter filter)
        {
            switch (filter)
            {
                case FrameworkArchitectureAssemblyFilter.Runtime:
                    return node.AssemblyName == "Framework_WWJ.Runtime";
                case FrameworkArchitectureAssemblyFilter.Editor:
                    return node.AssemblyName == "Framework_WWJ.Editor";
                default:
                    return true;
            }
        }

        private static bool MatchesSearch(FrameworkArchitectureTypeDescriptor node, string searchText)
        {
            return Contains(node.Metadata.DisplayName, searchText) ||
                   Contains(node.Metadata.Responsibility, searchText) ||
                   Contains(node.Type.FullName, searchText);
        }

        private static bool Contains(string source, string search)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
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

        private sealed class GraphLayout
        {
            internal IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> NodeRects { get; }
            internal IReadOnlyDictionary<FrameworkArchitectureLayer, float> LayerXs { get; }
            internal float CanvasWidth { get; }
            internal float CanvasHeight { get; }

            internal GraphLayout(
                IReadOnlyDictionary<FrameworkArchitectureTypeDescriptor, Rect> nodeRects,
                IReadOnlyDictionary<FrameworkArchitectureLayer, float> layerXs,
                float canvasWidth,
                float canvasHeight)
            {
                NodeRects = nodeRects;
                LayerXs = layerXs;
                CanvasWidth = canvasWidth;
                CanvasHeight = canvasHeight;
            }
        }
    }
}
