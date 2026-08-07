using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 保存节点图视口的缩放、平移和待执行视图命令。
    /// 坐标换算保持为纯数学逻辑，便于两类节点图共享并由 EditMode 测试验证。
    /// </summary>
    [FrameworkArchitecture(
        "图视口状态",
        "保存节点图 Zoom、Pan、适配请求并提供双向坐标换算。",
        FrameworkArchitectureLayer.EditorIntegration,
        360)]
    internal sealed class FrameworkGraphViewportState
    {
        internal const float MinZoom = 0.35f;
        internal const float MaxZoom = 2f;
        internal const float FramePadding = 32f;

        #region 运行时状态

        private float m_zoom = 1f;
        private Vector2 m_pan;
        private bool m_frameAllRequested = true;
        private bool m_resetToOneRequested;

        #endregion

        #region 公开属性

        internal float Zoom => m_zoom;

        internal Vector2 Pan => m_pan;

        #endregion

        #region 视图命令

        internal void RequestFrameAll()
        {
            m_frameAllRequested = true;
            m_resetToOneRequested = false;
        }

        internal void RequestResetToOne()
        {
            m_resetToOneRequested = true;
            m_frameAllRequested = false;
        }

        internal void ApplyPendingView(Rect viewport, Rect contentBounds)
        {
            if (viewport.width <= 0f || viewport.height <= 0f ||
                contentBounds.width <= 0f || contentBounds.height <= 0f)
            {
                return;
            }

            if (m_frameAllRequested)
            {
                FrameAll(viewport.size, contentBounds);
                m_frameAllRequested = false;
            }
            else if (m_resetToOneRequested)
            {
                ResetToOne(viewport.size, contentBounds);
                m_resetToOneRequested = false;
            }
        }

        internal void SetZoomAround(Vector2 viewportPoint, float requestedZoom)
        {
            var canvasPoint = ViewportToCanvas(viewportPoint);
            m_zoom = Mathf.Clamp(requestedZoom, MinZoom, MaxZoom);
            m_pan = viewportPoint - canvasPoint * m_zoom;
        }

        internal void PanBy(Vector2 delta)
        {
            m_pan += delta;
        }

        internal void FrameAll(Vector2 viewportSize, Rect contentBounds)
        {
            var availableWidth = Mathf.Max(1f, viewportSize.x - FramePadding * 2f);
            var availableHeight = Mathf.Max(1f, viewportSize.y - FramePadding * 2f);
            var widthZoom = availableWidth / Mathf.Max(1f, contentBounds.width);
            var heightZoom = availableHeight / Mathf.Max(1f, contentBounds.height);
            m_zoom = Mathf.Clamp(Mathf.Min(widthZoom, heightZoom), MinZoom, MaxZoom);
            CenterContent(viewportSize, contentBounds);
        }

        internal void ResetToOne(Vector2 viewportSize, Rect contentBounds)
        {
            m_zoom = 1f;
            CenterContent(viewportSize, contentBounds);
        }

        #endregion

        #region 坐标换算

        internal Vector2 CanvasToViewport(Vector2 canvasPoint)
        {
            return canvasPoint * m_zoom + m_pan;
        }

        internal Rect CanvasToViewport(Rect canvasRect)
        {
            return new Rect(
                CanvasToViewport(canvasRect.position),
                canvasRect.size * m_zoom);
        }

        internal Vector2 ViewportToCanvas(Vector2 viewportPoint)
        {
            return (viewportPoint - m_pan) / m_zoom;
        }

        #endregion

        #region 内部实现

        private void CenterContent(Vector2 viewportSize, Rect contentBounds)
        {
            m_pan = viewportSize * 0.5f - contentBounds.center * m_zoom;
        }

        #endregion
    }
}
