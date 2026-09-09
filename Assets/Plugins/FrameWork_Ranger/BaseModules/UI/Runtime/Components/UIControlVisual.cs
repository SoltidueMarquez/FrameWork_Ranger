using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [Serializable] public sealed class UIColorTarget { [LabelText("图片或文字")] public Graphic Target; [LabelText("颜色")] public Color Color = Color.white; }
    [Serializable] public sealed class UISpriteTarget { [LabelText("目标图片")] public Image Target; [LabelText("精灵")] public Sprite Sprite; }
    [Serializable] public sealed class UIActiveTarget { [LabelText("附属物体")] public GameObject Target; [LabelText("显示")] public bool Active; }
    [Serializable] public sealed class UIVisualStyle
    {
        [LabelText("颜色目标")] public List<UIColorTarget> Colors = new List<UIColorTarget>();
        [LabelText("换图目标")] public List<UISpriteTarget> Sprites = new List<UISpriteTarget>();
        [LabelText("显隐目标")] public List<UIActiveTarget> Objects = new List<UIActiveTarget>();
        [LabelText("动画器")] public Animator Animator;
        [LabelText("动画状态名")] public string AnimatorState;
        internal void Apply(Transform owner)
        {
            foreach (var item in Colors) if (item?.Target) item.Target.color = item.Color;
            foreach (var item in Sprites) if (item?.Target) item.Target.sprite = item.Sprite;
            foreach (var item in Objects)
                if (item?.Target && item.Target.transform != owner && !owner.IsChildOf(item.Target.transform))
                    item.Target.SetActive(item.Active);
            if (Animator && !string.IsNullOrEmpty(AnimatorState) && Animator.isActiveAndEnabled)
                Animator.Play(AnimatorState, 0, 0);
        }
    }
    [Serializable] public sealed class UIVisualPalette
    {
        [LabelText("普通")] public UIVisualStyle Normal = new UIVisualStyle();
        [LabelText("悬停")] public UIVisualStyle Hover = new UIVisualStyle();
        [LabelText("按下")] public UIVisualStyle Pressed = new UIVisualStyle();
        [LabelText("导航焦点")] public UIVisualStyle Focused = new UIVisualStyle();
        [LabelText("禁用")] public UIVisualStyle Disabled = new UIVisualStyle();
        internal UIVisualStyle Get(UIControlState state) => state switch
        { UIControlState.Hover => Hover, UIControlState.Pressed => Pressed, UIControlState.Focused => Focused, UIControlState.Disabled => Disabled, _ => Normal };
    }
    [AddComponentMenu("Ranger UI/控件状态表现")]
    [FrameworkArchitecture("控件状态表现", "多目标外观与开关值联动，静默数据刷新仍更新显示。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIControlVisual : MonoBehaviour
    {
        [LabelText("目标控件")] public Selectable Control;
        [LabelText("普通或关闭值样式")] public UIVisualPalette Off = new UIVisualPalette();
        [LabelText("切换项开启样式")] public UIVisualPalette On = new UIVisualPalette();
        private UIControlState m_state;
        private bool m_on, m_initialized;
        private void Reset() { Control = GetComponent<Selectable>(); if (Control) Control.transition = Selectable.Transition.None; }
        private void OnEnable() { if (!Control) Control = GetComponent<Selectable>(); Refresh(); }
        private void LateUpdate() => Apply(false);
        public void Refresh() => Apply(true);
        private void Apply(bool force)
        {
            var state = !Control || !UIInputUtility.IsAllowed(Control) ? UIControlState.Disabled :
                Control is IUIControlState source ? source.VisualState : UIControlState.Normal;
            bool on = Control is Toggle toggle && toggle.isOn;
            if (!force && m_initialized && m_state == state && m_on == on) return;
            m_initialized = true; m_state = state; m_on = on;
            (on ? On : Off)?.Get(state)?.Apply(transform);
        }
    }
}
