using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>横纵布局共用测量/分配：最小尺寸、偏好尺寸、可伸展空间与交叉轴对齐。</summary>
    [ExecuteAlways]
    [FrameworkArchitecture("UI 横纵布局", "按轴测量和分配子项几何。", FrameworkArchitectureLayer.RuntimeDriving, 130)]
    public abstract class UIStackLayout : LayoutGroup
    {
        [SerializeField, Min(0)] private float m_spacing;
        [SerializeField] private bool m_expandWidth;
        [SerializeField] private bool m_expandHeight;
        protected abstract int MainAxis { get; }
        public float Spacing { get => m_spacing; set { m_spacing = Mathf.Max(0, value); SetDirty(); } }
        public bool ExpandWidth { get => m_expandWidth; set { m_expandWidth = value; SetDirty(); } }
        public bool ExpandHeight { get => m_expandHeight; set { m_expandHeight = value; SetDirty(); } }
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            // uGUI 接受任一 ignoreLayout=false 的组件；Ranger 忽略标记显式优先。
            rectChildren.RemoveAll(child => child.GetComponent<UIIgnoreLayout>() is UIIgnoreLayout ignored && ignored.isActiveAndEnabled);
            Measure(0);
        }
        public override void CalculateLayoutInputVertical() { Measure(1); }
        private void Measure(int axis)
        {
            float pad = axis == 0 ? padding.horizontal : padding.vertical;
            float minimum = 0, preferred = 0, flexible = 0;
            bool main = axis == MainAxis;
            foreach (var child in rectChildren)
            {
                Read(child, axis, out float min, out float pref, out float flex);
                if (main) { minimum += min; preferred += pref; flexible += flex; }
                else { minimum = Mathf.Max(minimum, min); preferred = Mathf.Max(preferred, pref); flexible = Mathf.Max(flexible, flex); }
            }
            float spacing = main ? Mathf.Max(0, rectChildren.Count - 1) * m_spacing : 0;
            SetLayoutInputForAxis(minimum + pad + spacing, preferred + pad + spacing, flexible, axis);
        }
        private bool Expand(int axis) => axis == 0 ? m_expandWidth : m_expandHeight;
        private static UILayoutItem Item(RectTransform child)
        { var item = child.GetComponent<UILayoutItem>(); return item && item.isActiveAndEnabled ? item : null; }
        private void Read(RectTransform child, int axis, out float min, out float pref, out float flex)
        {
            var item = Item(child);
            if (item && !item.Control(axis)) { min = pref = child.rect.size[axis]; flex = 0; return; }
            min = LayoutUtility.GetMinSize(child, axis);
            pref = Mathf.Max(min, LayoutUtility.GetPreferredSize(child, axis));
            flex = Mathf.Max(LayoutUtility.GetFlexibleSize(child, axis), Expand(axis) ? 1 : 0);
        }
        private void Place(RectTransform child, int axis, float position, float size)
        {
            var item = Item(child);
            position += item ? item.Offset[axis] : 0;
            if (item && !item.Control(axis)) SetChildAlongAxis(child, axis, position);
            else SetChildAlongAxis(child, axis, position, size);
        }
        public override void SetLayoutHorizontal() { Arrange(0); }
        public override void SetLayoutVertical() { Arrange(1); }
        private void Arrange(int axis)
        {
            float available = rectTransform.rect.size[axis];
            float pad = axis == 0 ? padding.horizontal : padding.vertical;
            if (axis != MainAxis)
            {
                foreach (var child in rectChildren)
                {
                    Read(child, axis, out float min, out float pref, out float flex);
                    float size = flex > 0
                        ? Mathf.Max(min, available - pad) : Mathf.Clamp(available - pad, min, pref);
                    var item = Item(child);
                    float crossPosition = item && item.OverrideAlignment
                        ? (axis == 0 ? padding.left : padding.top) + (available - pad - size) * item.Align(axis)
                        : GetStartOffset(axis, size);
                    Place(child, axis, crossPosition, size);
                }
                return;
            }
            float totalMin = GetTotalMinSize(axis), totalPref = GetTotalPreferredSize(axis);
            float lerp = totalPref > totalMin ? Mathf.Clamp01((available - totalMin) / (totalPref - totalMin)) : 0;
            float flexTotal = GetTotalFlexibleSize(axis);
            float extra = Mathf.Max(0, available - totalPref);
            float position = GetStartOffset(axis, (flexTotal > 0 ? Mathf.Max(totalPref, available) : totalPref) - pad);
            if (available < totalPref) position = axis == 0 ? padding.left : padding.top;
            foreach (var child in rectChildren)
            {
                Read(child, axis, out float min, out float pref, out float flex);
                float size = Mathf.Lerp(min, pref, lerp) + (flexTotal > 0 ? extra * flex / flexTotal : 0);
                Place(child, axis, position, size); position += size + m_spacing;
            }
        }
    }
}
