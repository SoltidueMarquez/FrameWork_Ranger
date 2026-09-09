using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UITmpLayoutTests
    {
        [Test]
        public void TmpWrap_NarrowWidthIncreasesNestedContainerHeight()
        {
            // 临时 TMP 设置与动态字体，只供测试；不改写项目字体/Essential Resources。
            var singleton = typeof(TMP_Settings).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            typeof(TMP_Settings).GetMethod("SetAssetVersion", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(settings, null);
            singleton.SetValue(null, settings);
            var shaderField = typeof(ShaderUtilities).GetField("k_ShaderRef_MobileSDF", BindingFlags.Static | BindingFlags.NonPublic);
            var previousShader = shaderField.GetValue(null);
            TMP_FontAsset font = null;
            var root = new GameObject("Layout", typeof(RectTransform)); root.SetActive(false);
            try
            {
                // 仅测量几何；未导入 Essential Resources 的项目借用 TMP 自带编辑器 SDF 材质。
                var shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.ugui/Editor Resources/Shaders/TMP_SDF Internal Editor.shader");
                Assert.That(shader, Is.Not.Null);
                shaderField.SetValue(null, shader);
                var source = AssetDatabase.LoadAssetAtPath<Font>("Packages/com.unity.searcher/Editor/Resources/FlatSkin/Font/Roboto-Regular.ttf");
                font = source ? TMP_FontAsset.CreateFontAsset(source) : null;
                TMP_Settings.defaultFontAsset = font;
                Assert.That(font, Is.Not.Null, "TMP 测试需要当前 Searcher 包自带的 Roboto 字体。");
                var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(360, 400);
                var layout = root.AddComponent<UIVerticalLayout>(); layout.ExpandWidth = true;
                root.AddComponent<UIContentFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var child = new GameObject("TMP", typeof(RectTransform)); child.transform.SetParent(root.transform, false);
                var label = child.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = 24;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.text = "Dynamic text wraps after its parent assigns a width, then increases the container height.";
                root.SetActive(true); UILayoutScheduler.FlushNow(rect);
                float wide = rect.rect.height;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150);
                UILayoutScheduler.FlushNow(rect);
                Assert.That(rect.rect.height, Is.GreaterThan(wide));
                Assert.That(label.rectTransform.rect.width, Is.EqualTo(150).Within(0.1));
                float settled = rect.rect.height; UILayoutScheduler.FlushNow(rect);
                Assert.That(rect.rect.height, Is.EqualTo(settled).Within(0.1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                if (font)
                {
                    foreach (var texture in font.atlasTextures) if (texture) UnityEngine.Object.DestroyImmediate(texture);
                    if (font.material) UnityEngine.Object.DestroyImmediate(font.material);
                    UnityEngine.Object.DestroyImmediate(font);
                }
                singleton.SetValue(null, previous);
                shaderField.SetValue(null, previousShader);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
