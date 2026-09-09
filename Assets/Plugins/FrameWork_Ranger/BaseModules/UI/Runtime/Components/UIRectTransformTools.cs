using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [FrameworkArchitecture("UI 矩形工具", "锚点、轴心、边距、尺寸与坐标转换。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public static class UIRectTransformTools
    {
        public static void StretchToParent(this RectTransform rect, Vector2 minOffset = default, Vector2 maxOffset = default)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = minOffset; rect.offsetMax = maxOffset; UILayoutScheduler.MarkDirty(rect); }
        public static void SetSize(this RectTransform rect, Vector2 size)
        { rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x); rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y); UILayoutScheduler.MarkDirty(rect); }
        public static void SetWidth(this RectTransform rect, float width) => rect.SetSize(new Vector2(width, rect.rect.height));
        public static void SetHeight(this RectTransform rect, float height) => rect.SetSize(new Vector2(rect.rect.width, height));
        public static void CenterInParent(this RectTransform rect)
        { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; UILayoutScheduler.MarkDirty(rect); }
        public static void SetAnchors(this RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; UILayoutScheduler.MarkDirty(rect); }
        public static void SetMargins(this RectTransform rect, float left, float top, float right, float bottom)
        { rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top); UILayoutScheduler.MarkDirty(rect); }
        public static void SetPivotKeepingPosition(this RectTransform rect, Vector2 pivot)
        {
            Vector3 before = rect.TransformPoint(rect.rect.min);
            rect.pivot = pivot;
            rect.position += before - rect.TransformPoint(rect.rect.min);
            UILayoutScheduler.MarkDirty(rect);
        }
        public static bool ScreenToLocal(this RectTransform rect, Vector2 screen, out Vector2 local, Camera camera = null)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            if (!camera && canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) camera = canvas.worldCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, camera, out local);
        }
    }
}
