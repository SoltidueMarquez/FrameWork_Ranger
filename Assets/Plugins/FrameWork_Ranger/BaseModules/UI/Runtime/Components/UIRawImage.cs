using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/纹理图片 UIRawImage")]
    [FrameworkArchitecture("UI 纹理图片", "兼容原生 RawImage，封装纹理和尺寸操作。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public class UIRawImage : RawImage
    {
        public void SetTexture(Texture value, bool nativeSize = false) { texture = value; if (nativeSize && value) SetNativeSize(); UILayoutScheduler.MarkDirty(rectTransform); }
        public void SetAlpha(float alpha) { var next = color; next.a = Mathf.Clamp01(alpha); color = next; }
        public void SetRaycast(bool value) => raycastTarget = value;
        public void FitInside(Vector2 available)
        {
            if (!texture || available.x <= 0 || available.y <= 0) return;
            var size = new Vector2(texture.width * Mathf.Abs(uvRect.width), texture.height * Mathf.Abs(uvRect.height));
            if (size.x > 0 && size.y > 0) rectTransform.SetSize(size * Mathf.Min(available.x / size.x, available.y / size.y));
        }
    }
}
