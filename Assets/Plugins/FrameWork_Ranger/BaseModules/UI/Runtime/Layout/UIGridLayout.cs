using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>固定列数的基础网格，与 Ranger 横纵布局使用一致的忽略标记。</summary>
    [ExecuteAlways, AddComponentMenu("Ranger UI/Layout/Grid")]
    [FrameworkArchitecture("UI 网格布局", "固定列网格与忽略项支持。", FrameworkArchitectureLayer.PublicFacade, 130)]
    public sealed class UIGridLayout : LayoutGroup
    {
        [Min(1)] public int Columns = 3;
        public Vector2 CellSize = new Vector2(100, 100);
        public Vector2 Spacing = new Vector2(8, 8);
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            rectChildren.RemoveAll(child => child.GetComponent<UIIgnoreLayout>() is UIIgnoreLayout ignored && ignored.isActiveAndEnabled);
            int columns = Mathf.Min(Mathf.Max(1, Columns), rectChildren.Count);
            float size = padding.horizontal + columns * CellSize.x + Mathf.Max(0, columns - 1) * Spacing.x;
            SetLayoutInputForAxis(size, size, 0, 0);
        }
        public override void CalculateLayoutInputVertical()
        {
            int rows = Mathf.CeilToInt((float)rectChildren.Count / Mathf.Max(1, Columns));
            float size = padding.vertical + rows * CellSize.y + Mathf.Max(0, rows - 1) * Spacing.y;
            SetLayoutInputForAxis(size, size, 0, 1);
        }
        public override void SetLayoutHorizontal() { Arrange(0); }
        public override void SetLayoutVertical() { Arrange(1); }
        private void Arrange(int axis)
        {
            int columns = Mathf.Max(1, Columns);
            int count = axis == 0 ? Mathf.Min(columns, rectChildren.Count) : Mathf.CeilToInt((float)rectChildren.Count / columns);
            float offset = GetStartOffset(axis, count * CellSize[axis] + Mathf.Max(0, count - 1) * Spacing[axis]);
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i]; var item = child.GetComponent<UILayoutItem>();
                if (item && !item.isActiveAndEnabled) item = null;
                float size = item && !item.Control(axis) ? child.rect.size[axis] : CellSize[axis];
                float align = item && item.OverrideAlignment ? item.Align(axis) : GetAlignmentOnAxis(axis);
                float position = offset + (axis == 0 ? i % columns : i / columns) * (CellSize[axis] + Spacing[axis]) +
                    (CellSize[axis] - size) * align + (item ? item.Offset[axis] : 0);
                if (item && !item.Control(axis)) SetChildAlongAxis(child, axis, position);
                else SetChildAlongAxis(child, axis, position, size);
            }
        }
        public void RefreshLayout() { UILayoutScheduler.MarkDirty((RectTransform)transform); }
    }
}
