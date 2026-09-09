using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/窗口拖动")]
    [FrameworkArchitecture("窗口拖动", "在父坐标中移动窗口，支持三种 Canvas 与可选边界。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [LabelText("移动窗口")] public RectTransform Target;
        [LabelText("移动范围（可选）")] public RectTransform Bounds;
        [LabelText("拖动结束")] public UnityEvent DragEnded = new UnityEvent();
        [LabelText("拖动中断")] public UnityEvent DragCanceled = new UnityEvent();
        private bool m_dragging;
        private int m_pointer;
        private Vector2 m_offset;
        public void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !UIInputUtility.IsAllowed(this)) return;
            if (!Target) Target = transform as RectTransform;
            if (!Target || !(Target.parent is RectTransform parent)) return;
            var pressed = data.pointerPress ? data.pointerPress : data.pointerPressRaycast.gameObject;
            var control = pressed ? pressed.GetComponentInParent<Selectable>() : null;
            if (control && control.transform != transform && control.transform.IsChildOf(transform)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out var point)) return;
            m_pointer = data.pointerId; m_offset = Target.anchoredPosition - point; m_dragging = true;
            GetComponent<UIButton>()?.CancelPress();
        }
        public void OnDrag(PointerEventData data)
        {
            if (!m_dragging || data.pointerId != m_pointer) return;
            if (!UIInputUtility.IsAllowed(this) || !Target) { Cancel(); return; }
            if (!(Target.parent is RectTransform parent)) { Cancel(); return; }
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out var point)) return;
            Target.anchoredPosition = point + m_offset;
            if (Bounds)
            {
                var extent = RectTransformUtility.CalculateRelativeRectTransformBounds(Bounds, Target);
                var rect = Bounds.rect; var delta = Vector3.zero;
                for (int axis = 0; axis < 2; axis++)
                    delta[axis] = extent.size[axis] > rect.size[axis] ? rect.center[axis] - extent.center[axis] :
                        extent.min[axis] < rect.min[axis] ? rect.min[axis] - extent.min[axis] :
                        extent.max[axis] > rect.max[axis] ? rect.max[axis] - extent.max[axis] : 0;
                var local = parent.InverseTransformVector(Bounds.TransformVector(delta));
                Target.anchoredPosition += new Vector2(local.x, local.y);
            }
        }
        public void OnEndDrag(PointerEventData data)
        { if (!m_dragging || data.pointerId != m_pointer) return; m_dragging = false; DragEnded.Invoke(); }
        private void Update() { if (m_dragging && !UIInputUtility.IsAllowed(this)) Cancel(); }
        private void Cancel() { if (!m_dragging) return; m_dragging = false; DragCanceled.Invoke(); }
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
    }
}
