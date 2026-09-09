using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace FrameWork_Ranger.UI
{
    [Serializable] public sealed class UIPointerEvent : UnityEvent<PointerEventData> { }
    [AddComponentMenu("Ranger UI/可选拖拽事件")]
    [FrameworkArchitecture("拖拽事件扩展", "仅启用此组件时接管拖动；普通按钮保留 ScrollRect 路由。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIDragEvents : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [LabelText("开始拖拽")] public UIPointerEvent Began = new UIPointerEvent();
        [LabelText("拖拽移动")] public UIPointerEvent Moved = new UIPointerEvent();
        [LabelText("结束拖拽")] public UIPointerEvent Ended = new UIPointerEvent();
        [LabelText("拖拽中断")] public UnityEvent Canceled = new UnityEvent();
        private PointerEventData m_drag;
        public void OnBeginDrag(PointerEventData data)
        {
            if (!UIInputUtility.IsAllowed(this)) return;
            m_drag = data; GetComponent<UIButton>()?.CancelPress(); Began.Invoke(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (m_drag == null || m_drag.pointerId != data.pointerId) return;
            if (!UIInputUtility.IsAllowed(this)) { Cancel(); return; } Moved.Invoke(data);
        }
        public void OnEndDrag(PointerEventData data)
        { if (m_drag == null || m_drag.pointerId != data.pointerId) return; m_drag = null; Ended.Invoke(data); }
        private void Update() { if (m_drag != null && !UIInputUtility.IsAllowed(this)) Cancel(); }
        private void Cancel() { if (m_drag == null) return; m_drag = null; Canceled.Invoke(); }
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
    }
}
