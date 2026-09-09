using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/图片 UIImage")]
    [FrameworkArchitecture("UI 图片", "兼容原生图片，封装换图、适配和透明度。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public class UIImage : Image
    {
        public void SetSprite(Sprite value, bool nativeSize = false) { sprite = value; if (nativeSize && value) SetNativeSize(); UILayoutScheduler.MarkDirty(rectTransform); }
        public void SetAlpha(float alpha) { var next = color; next.a = Mathf.Clamp01(alpha); color = next; }
        public void SetRaycast(bool value) => raycastTarget = value;
        public void FitInside(Vector2 available)
        {
            if (!sprite || available.x <= 0 || available.y <= 0) return;
            var size = sprite.rect.size;
            rectTransform.SetSize(size * Mathf.Min(available.x / size.x, available.y / size.y));
        }
    }
}
