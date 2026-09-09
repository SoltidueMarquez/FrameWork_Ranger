using Sirenix.OdinInspector;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [ExecuteAlways, AddComponentMenu("Ranger UI/Layout/子项设置")]
    [FrameworkArchitecture("布局子项设置", "子项对齐、偏移与尺寸控制覆盖。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UILayoutItem : MonoBehaviour
    {
        [LabelText("布局控制宽度")] public bool ControlWidth = true;
        [LabelText("布局控制高度")] public bool ControlHeight = true;
        [LabelText("独立对齐")] public bool OverrideAlignment;
        [LabelText("对齐方式")] public TextAnchor Alignment = TextAnchor.UpperLeft;
        [LabelText("位置偏移")] public Vector2 Offset;
        internal bool Control(int axis) => axis == 0 ? ControlWidth : ControlHeight;
        internal float Align(int axis) => axis == 0 ? (int)Alignment % 3 * 0.5f : (int)Alignment / 3 * 0.5f;
        private void OnEnable() => Refresh();
        private void OnDisable() => Refresh();
        private void OnValidate() => Refresh();
        public void Refresh() { if (transform.parent is RectTransform parent) UILayoutScheduler.MarkDirty(parent); }
    }
}
