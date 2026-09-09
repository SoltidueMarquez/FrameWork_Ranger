using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [FrameworkArchitecture("UI 控件工具", "显隐、图文更新和可解除的事件订阅。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public static class UIElementTools
    {
        public static void SetVisible(this Component component, bool visible) { if (component) SetVisible(component.gameObject, visible); }
        public static void SetVisible(this GameObject target, bool visible)
        {
            if (!target || target.activeSelf == visible) return;
            var parent = target.transform.parent as RectTransform;
            target.SetActive(visible);
            if (parent) UILayoutScheduler.MarkDirty(parent);
        }
        public static void SetAlpha(this Graphic graphic, float alpha)
        { var color = graphic.color; color.a = Mathf.Clamp01(alpha); graphic.color = color; }
        public static void SetImage(this Image image, Sprite sprite, bool nativeSize = false)
        { image.sprite = sprite; if (nativeSize && sprite) image.SetNativeSize(); UILayoutScheduler.MarkDirty(image.rectTransform); }
        public static void SetLabel(this TMP_Text text, string value)
        { text.text = value ?? ""; UILayoutScheduler.MarkDirty(text.rectTransform); }
        public static void SetLabel(this Text text, string value)
        { text.text = value ?? ""; UILayoutScheduler.MarkDirty(text.rectTransform); }
        public static void SetUIInteractable(this Selectable control, bool value)
        { if (control is UIButton button) button.SetInteractable(value); else control.interactable = value; }
        public static IDisposable Subscribe(this UnityEvent source, UnityAction listener)
        { source.AddListener(listener); return new Subscription(() => source.RemoveListener(listener)); }
        public static IDisposable Subscribe<T>(this UnityEvent<T> source, UnityAction<T> listener)
        { source.AddListener(listener); return new Subscription(() => source.RemoveListener(listener)); }
        private sealed class Subscription : IDisposable
        {
            private Action m_remove;
            internal Subscription(Action remove) { m_remove = remove; }
            public void Dispose() { var remove = m_remove; m_remove = null; remove?.Invoke(); }
        }
    }
}
