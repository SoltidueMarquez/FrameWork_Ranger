using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UIOutlineTests
    {
        [UnityTest] public IEnumerator UnionOutline_HasNoOverlapSeam_InAllCanvasModes_AndHonorsNestedMasks() => UniTask.ToCoroutine(async () =>
        {
            foreach (var mode in new[] { RenderMode.ScreenSpaceOverlay, RenderMode.ScreenSpaceCamera, RenderMode.WorldSpace })
            {
                var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
                var cam = new GameObject("Camera", typeof(Camera)); cam.transform.position = new Vector3(0, 0, -10);
                try
                {
                    var canvas = root.GetComponent<Canvas>(); canvas.renderMode = mode; canvas.worldCamera = cam.GetComponent<Camera>(); canvas.planeDistance = 2;
                    if (mode == RenderMode.WorldSpace) { ((RectTransform)root.transform).sizeDelta = new Vector2(300, 200); root.transform.localScale = Vector3.one * .01f; }
                    var group = new GameObject("Union", typeof(RectTransform), typeof(UIOutlineGroup)); group.transform.SetParent(root.transform, false);
                    ((RectTransform)group.transform).sizeDelta = new Vector2(200, 200);
                    var outline = group.GetComponent<UIOutlineGroup>(); outline.Width = 4;
                    Image Square(string name, Vector2 position)
                    {
                        var obj = new GameObject(name, typeof(RectTransform), typeof(UIImage)); obj.transform.SetParent(group.transform, false);
                        var image = obj.GetComponent<UIImage>(); image.rectTransform.sizeDelta = Vector2.one * 60; image.rectTransform.anchoredPosition = position; return image;
                    }
                    var a = Square("A", new Vector2(-20, -10)); var b = Square("B", new Vector2(20, 10));
                    await UniTask.NextFrame(); Canvas.ForceUpdateCanvases(); outline.RefreshNow();
                    var output = group.GetComponentInChildren<RawImage>();
                    Assert.That(output, Is.Not.Null, mode.ToString()); Assert.That(output.raycastTarget, Is.False);
                    float Sample(string field, Vector2 point)
                    {
                        var rt = (RenderTexture)typeof(UIOutlineGroup).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(outline);
                        Assert.That(rt, Is.Not.Null);
                        var old = RenderTexture.active; var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                        try
                        {
                            RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                            System.IO.Directory.CreateDirectory("Logs/UIVisualChecks");
                            System.IO.File.WriteAllBytes("Logs/UIVisualChecks/" + mode + field + ".png", texture.EncodeToPNG());
                            Vector2 min = output.rectTransform.anchoredPosition - output.rectTransform.rect.size * .5f;
                            Vector2 uv = (point - min) / output.rectTransform.rect.size;
                            return texture.GetPixelBilinear(uv.x, uv.y).a;
                        }
                        finally { RenderTexture.active = old; UnityEngine.Object.Destroy(texture); }
                    }
                    Assert.That(Sample("m_mask", new Vector2(-40, -30)), Is.GreaterThan(.9f), mode + " lower left source");
                    Assert.That(Sample("m_mask", new Vector2(-40, 30)), Is.LessThan(.05f), mode + " transparent upper left");
                    Assert.That(Sample("m_result", Vector2.zero), Is.LessThan(.05f), mode + " no internal seam");
                    Assert.That(Sample("m_result", new Vector2(-52, -10)), Is.GreaterThan(.5f), mode + " outside outline");
                    var maskObject = new GameObject("Nested Mask", typeof(RectTransform), typeof(UIImage), typeof(Mask)); maskObject.transform.SetParent(group.transform, false);
                    var maskRect = (RectTransform)maskObject.transform; maskRect.sizeDelta = new Vector2(20, 40); maskRect.anchoredPosition = new Vector2(-20, -10);
                    maskObject.GetComponent<Mask>().showMaskGraphic = false;
                    a.transform.SetParent(maskRect, false); a.rectTransform.anchoredPosition = Vector2.zero; b.enabled = false;
                    await UniTask.NextFrame(); outline.RefreshNow();
                    Assert.That(Sample("m_mask", new Vector2(-20, -10)), Is.GreaterThan(.9f));
                    Assert.That(Sample("m_mask", new Vector2(-40, -10)), Is.LessThan(.05f), "Nested stencil crops source before union");
                    maskObject.GetComponent<Mask>().enabled = false; maskObject.GetComponent<UIImage>().enabled = false;
                    maskObject.AddComponent<RectMask2D>(); await UniTask.NextFrame(); outline.RefreshNow();
                    Assert.That(Sample("m_mask", new Vector2(-40, -10)), Is.LessThan(.05f), "Nested rectangular crop");
                }
                finally { UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(cam); }
                await UniTask.NextFrame();
            }
        });
    }
}
