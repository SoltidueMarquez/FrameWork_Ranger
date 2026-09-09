using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/折叠项")]
    [FrameworkArchitecture("折叠项", "即时展开收起，保留内容，刷新父布局与标题状态。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIFoldout : MonoBehaviour
    {
        [LabelText("标题按钮")] public Button Header;
        [LabelText("内容区域")] public GameObject Content;
        [LabelText("默认展开")] public bool DefaultExpanded = true;
        [LabelText("手风琴组（可选）")] public UIAccordionGroup Group;
        [LabelText("展开指示图标")] public Graphic Indicator;
        [LabelText("展开角度")] public float ExpandedAngle = -90;
        [LabelText("收起角度")] public float CollapsedAngle;
        [LabelText("状态变化")] public UnityEvent<bool> Changed = new UnityEvent<bool>();
        public bool IsExpanded { get; private set; }
        private bool m_initialized;
        private Button m_boundHeader;
        private UIAccordionGroup m_boundGroup;
        private void OnEnable()
        {
            m_boundHeader = Header; m_boundHeader?.onClick.AddListener(Toggle);
            m_boundGroup = Group; m_boundGroup?.Register(this);
            SetExpanded(m_initialized ? IsExpanded : DefaultExpanded, false); m_initialized = true;
        }
        private void OnDisable()
        { m_boundHeader?.onClick.RemoveListener(Toggle); m_boundGroup?.Unregister(this); m_boundHeader = null; m_boundGroup = null; }
        private void Toggle() { if (UIInputUtility.IsAllowed(this)) SetExpanded(!IsExpanded); }
        public void SetExpanded(bool expanded, bool notify = true)
        {
            bool changed = IsExpanded != expanded; IsExpanded = expanded;
            if (expanded) m_boundGroup?.Expanded(this);
            using (UILayoutScheduler.BeginBatch())
            {
                if (Content && Content.transform != transform && !transform.IsChildOf(Content.transform)) Content.SetActive(expanded);
                if (Indicator) Indicator.rectTransform.localRotation = Quaternion.Euler(0, 0, expanded ? ExpandedAngle : CollapsedAngle);
                if (transform is RectTransform rect) UILayoutScheduler.MarkDirty(rect);
            }
            if (notify && changed) Changed.Invoke(expanded);
        }
    }
}
