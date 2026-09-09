using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/Layout/内容尺寸适配")]
    [FrameworkArchitecture("UI 内容适配", "测量内容并按宽高上下限调整容器，保持 uGUI 布局顺序。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIContentFitter : ContentSizeFitter
    {
        [LabelText("限制宽度")] public bool LimitWidth;
        [LabelText("宽度范围")] public Vector2 WidthRange = new Vector2(0, 1000);
        [LabelText("限制高度")] public bool LimitHeight;
        [LabelText("高度范围")] public Vector2 HeightRange = new Vector2(0, 1000);
        public override void SetLayoutHorizontal() { base.SetLayoutHorizontal(); Clamp(0, LimitWidth, WidthRange, horizontalFit); }
        public override void SetLayoutVertical() { base.SetLayoutVertical(); Clamp(1, LimitHeight, HeightRange, verticalFit); }
        private void Clamp(int axis, bool limit, Vector2 range, FitMode mode)
        {
            if (!limit || mode == FitMode.Unconstrained) return;
            var rect = (RectTransform)transform;
            rect.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, Mathf.Clamp(rect.rect.size[axis], Mathf.Max(0, range.x), Mathf.Max(range.x, range.y)));
        }
        public void RefreshLayout() { UILayoutScheduler.MarkDirty((RectTransform)transform); }
    }
}
