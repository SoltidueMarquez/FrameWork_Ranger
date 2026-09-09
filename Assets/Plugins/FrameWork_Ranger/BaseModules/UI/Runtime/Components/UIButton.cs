using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    public enum UIControlState { Normal, Hover, Pressed, Focused, Disabled }
    public interface IUIControlState { UIControlState VisualState { get; } }
    [AddComponentMenu("Ranger UI/按钮 UIButton")]
    [FrameworkArchitecture("UI 按钮", "点击、长按和按需手势；中断不补发点击。", FrameworkArchitectureLayer.PublicFacade, 130)]
    public sealed class UIButton : Button, IUIControlState
    {
        [LabelText("长按阈值"), Min(0.01f)] public float LongPressSeconds = 0.5f;
        [LabelText("长按事件")] public UnityEvent OnLongPress = new UnityEvent();
        [LabelText("启用双击")] public bool EnableDoubleClick;
        [LabelText("双击间隔"), Min(0.01f)] public float DoubleClickSeconds = 0.25f;
        [LabelText("启用按住连发")] public bool EnableRepeat;
        [LabelText("连发间隔"), Min(0.01f)] public float RepeatSeconds = 0.1f;
        [LabelText("双击事件")] public UnityEvent DoubleClicked = new UnityEvent();
        [LabelText("连续触发")] public UnityEvent Repeated = new UnityEvent();
        [LabelText("右键点击")] public UnityEvent RightClicked = new UnityEvent();
        [LabelText("左键按下")] public UnityEvent LeftDown = new UnityEvent();
        [LabelText("左键松开")] public UnityEvent LeftUp = new UnityEvent();
        [LabelText("右键按下")] public UnityEvent RightDown = new UnityEvent();
        [LabelText("右键松开")] public UnityEvent RightUp = new UnityEvent();
        [LabelText("指针进入")] public UnityEvent PointerEntered = new UnityEvent();
        [LabelText("指针离开")] public UnityEvent PointerExited = new UnityEvent();
        [LabelText("导航选中")] public UnityEvent FocusGained = new UnityEvent();
        [LabelText("导航取消")] public UnityEvent FocusLost = new UnityEvent();
        [LabelText("按压中断")] public UnityEvent PressCanceled = new UnityEvent();
        private bool m_pressed, m_longPressed, m_rightPressed, m_rightCanceled;
        private float m_pressedAt, m_nextRepeat, m_lastClick = float.NegativeInfinity;
        private int m_pointer, m_lastClickPointer;
        public UIControlState VisualState => (UIControlState)(int)currentSelectionState;
        public void SetInteractable(bool value) { interactable = value; if (!value) CancelPress(); }
        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            if (!UIInputUtility.IsAllowed(this)) return;
            if (data.button == PointerEventData.InputButton.Left)
            {
                m_pressed = true; m_longPressed = false; m_pressedAt = Time.unscaledTime;
                m_nextRepeat = m_pressedAt + Mathf.Max(0.01f, LongPressSeconds); m_pointer = data.pointerId; LeftDown.Invoke();
            }
            else if (data.button == PointerEventData.InputButton.Right) { m_rightPressed = true; m_rightCanceled = false; RightDown.Invoke(); }
        }
        public override void OnPointerUp(PointerEventData data)
        {
            base.OnPointerUp(data);
            if (data.button == PointerEventData.InputButton.Left && data.pointerId == m_pointer)
            { bool valid = m_pressed && UIInputUtility.IsAllowed(this); m_pressed = false; if (valid) LeftUp.Invoke(); }
            else if (data.button == PointerEventData.InputButton.Right)
            { bool valid = m_rightPressed && UIInputUtility.IsAllowed(this); m_rightPressed = false; if (valid) RightUp.Invoke(); }
        }
        public override void OnPointerClick(PointerEventData data)
        {
            if (!UIInputUtility.IsAllowed(this)) return;
            if (data.button == PointerEventData.InputButton.Right) { if (!m_rightCanceled) RightClicked.Invoke(); return; }
            if (data.pointerId == m_pointer && m_longPressed) { m_longPressed = false; return; }
            if (data.button != PointerEventData.InputButton.Left) return;
            base.OnPointerClick(data);
            if (EnableDoubleClick && m_lastClickPointer == data.pointerId && Time.unscaledTime - m_lastClick <= DoubleClickSeconds)
            { m_lastClick = float.NegativeInfinity; DoubleClicked.Invoke(); }
            else { m_lastClick = Time.unscaledTime; m_lastClickPointer = data.pointerId; }
        }
        public override void OnPointerEnter(PointerEventData data) { base.OnPointerEnter(data); if (UIInputUtility.IsAllowed(this)) PointerEntered.Invoke(); }
        public override void OnPointerExit(PointerEventData data)
        { base.OnPointerExit(data); PointerExited.Invoke(); if (data.pointerId == m_pointer) CancelPress(); }
        public override void OnSelect(BaseEventData data) { base.OnSelect(data); if (UIInputUtility.IsAllowed(this)) FocusGained.Invoke(); }
        public override void OnDeselect(BaseEventData data) { base.OnDeselect(data); FocusLost.Invoke(); }
        private void Update()
        {
            if (!m_pressed && !m_rightPressed) return;
            if (!UIInputUtility.IsAllowed(this)) { CancelPress(); return; }
            if (!m_pressed) return;
            if (EnableRepeat)
            {
                if (Time.unscaledTime >= m_nextRepeat) { m_longPressed = true; m_nextRepeat = Time.unscaledTime + Mathf.Max(0.01f, RepeatSeconds); Repeated.Invoke(); }
            }
            else if (!m_longPressed && Time.unscaledTime - m_pressedAt >= LongPressSeconds)
            { m_longPressed = true; OnLongPress.Invoke(); }
        }
        public void CancelPress()
        {
            bool wasPressed = m_pressed || m_rightPressed;
            m_pressed = m_rightPressed = false; m_rightCanceled = true; m_longPressed = true; m_lastClick = float.NegativeInfinity;
            if (wasPressed) PressCanceled.Invoke();
        }
        protected override void OnDisable() { CancelPress(); base.OnDisable(); }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelPress(); }
    }
}
