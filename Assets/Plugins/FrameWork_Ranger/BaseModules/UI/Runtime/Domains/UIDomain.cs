using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [FrameworkArchitecture("UI 呈现域", "拥有 Canvas 并解析借用绑定。", FrameworkArchitectureLayer.RuntimeDriving, 130)]
    internal sealed class UIDomain
    {
        internal readonly UIDomainDefinition Definition;
        private readonly UIContext m_context;
        private readonly SortedDictionary<int, RectTransform> m_layers = new SortedDictionary<int, RectTransform>();
        private Canvas m_canvas;
        private GraphicRaycaster m_raycaster;
        private UIDomainBinding m_binding;
        internal GameObject Root { get; private set; }
        internal bool IsPresented { get; private set; }
        internal string BindingError { get; private set; }
        internal UIDomain(UIContext context, UIDomainDefinition definition)
        { m_context = context; Definition = definition; }

        internal void UpdateBinding()
        {
            m_binding = null;
            BindingError = null;
            if (Definition.RenderMode != RenderMode.ScreenSpaceOverlay)
            {
                foreach (var candidate in UIDomainBinding.Candidates)
                {
                    if (!candidate || !candidate.Matches(m_context, Definition.Key)) continue;
                    if (m_binding != null) { BindingError = "存在多个有效绑定"; break; }
                    m_binding = candidate;
                }
                if (BindingError == null && (!m_binding || !m_binding.Camera || !m_binding.Camera.isActiveAndEnabled))
                    BindingError = "缺少有效相机";
                if (BindingError == null && Definition.RenderMode == RenderMode.WorldSpace && !m_binding.WorldAnchor)
                    BindingError = "缺少世界挂载点";
            }
            IsPresented = m_context.IsAlive && BindingError == null;
            if (!Root) return;
            Root.SetActive(IsPresented);
            if (m_canvas) m_canvas.worldCamera = IsPresented && m_binding ? m_binding.Camera : null;
            if (IsPresented && Definition.RenderMode == RenderMode.WorldSpace)
                Root.transform.SetPositionAndRotation(m_binding.WorldAnchor.position, m_binding.WorldAnchor.rotation);
        }
        internal void RequireBinding()
        {
            UpdateBinding();
            if (!IsPresented) throw new InvalidOperationException(
                $"UI 域 {m_context.Owner}/{Definition.Key} 无法打开：{BindingError ?? "所有者已结束"}。");
        }
        internal RectTransform GetLayer(int layer)
        {
            if (!Root)
            {
                Root = new GameObject("UI Domain " + Definition.Key, typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                Root.transform.SetParent(m_context.Root.transform, false);
                m_canvas = Root.GetComponent<Canvas>();
                m_canvas.renderMode = Definition.RenderMode;
                m_canvas.sortingOrder = Definition.SortingOrder;
                m_canvas.planeDistance = Definition.PlaneDistance;
                m_raycaster = Root.GetComponent<GraphicRaycaster>();
                var scaler = Root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = Definition.ReferenceResolution;
                scaler.matchWidthOrHeight = Definition.MatchWidthOrHeight;
                if (Definition.RenderMode == RenderMode.WorldSpace)
                {
                    ((RectTransform)Root.transform).sizeDelta = Definition.WorldSize;
                    Root.transform.localScale = Vector3.one * Definition.WorldScale;
                }
                UpdateBinding();
            }
            if (!m_layers.TryGetValue(layer, out var result))
            {
                result = new GameObject("Layer " + layer, typeof(RectTransform)).GetComponent<RectTransform>();
                result.SetParent(Root.transform, false);
                result.anchorMin = Vector2.zero; result.anchorMax = Vector2.one;
                result.offsetMin = result.offsetMax = Vector2.zero;
                m_layers.Add(layer, result);
                foreach (var item in m_layers) item.Value.SetAsLastSibling();
            }
            return result;
        }
    }
}
