using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FrameWork_Ranger.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UIInspectorPreviewWindow : EditorWindow
    {
        internal UnityEditor.Editor Inspector;
        internal Exception Error;
        internal int DrawCount;
        private Vector2 m_scroll;
        private void OnGUI()
        {
            if (!Inspector) return;
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            try { Inspector.OnInspectorGUI(); DrawCount++; }
            catch (Exception ex) { Error = ex; }
            finally { EditorGUILayout.EndScrollView(); }
        }
    }
    public sealed class UIInspectorDrawingTests
    {
        [UnityTest] public IEnumerator IndependentCatalog_DrawsInlinePanelAndLogic_InNarrowAndWideWindows()
        {
            var resource = ScriptableObject.CreateInstance<UIResourcesConfig>(); resource.name = "UI 资源总配置";
            var panel = ScriptableObject.CreateInstance<UIPanelConfig>(); panel.Definition.Key = "Notice";
            panel.Definition.UseDirectPrefab = false; panel.Definition.PrefabAddress = "RangerUI/Notice";
            var domain = new UIDomainDefinition(); resource.Global.Domains.Add(domain);
            var entry = new UIPanelEntry { Panel = panel }; resource.Global.Panels.Add(entry);
            var inspector = UnityEditor.Editor.CreateEditor(resource);
            var window = ScriptableObject.CreateInstance<UIInspectorPreviewWindow>(); bool shown = false;
            try
            {
                window.Inspector = inspector;
                var expanded = (HashSet<object>)typeof(UIResourcesConfigInspector).GetField("m_expanded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inspector);
                expanded.Add(domain); expanded.Add(entry); window.ShowUtility(); shown = true;
                foreach (int width in new[] { 360, 800 })
                {
                    window.position = new Rect(20,20,width,1000);
                    foreach (RenderMode mode in Enum.GetValues(typeof(RenderMode)))
                    {
                        domain.RenderMode = mode; int before = window.DrawCount;
                        for (int frame=0;frame<8;frame++) { window.Repaint(); yield return null; }
                        Assert.That(window.Error, Is.Null, window.Error?.ToString()); Assert.That(window.DrawCount, Is.GreaterThan(before));
                        Assert.That(panel.Definition.Logic, Is.Not.Null); Assert.That(panel.Definition.PrefabAddress, Is.EqualTo("RangerUI/Notice"));
                    }
                }
            }
            finally
            {
                if (shown) window.Close(); else UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(inspector); UnityEngine.Object.DestroyImmediate(resource); UnityEngine.Object.DestroyImmediate(panel);
            }
        }
        [UnityTest]
        public IEnumerator Inspector_DrawsExpandedLogicAndAllModesAtNarrowAndWideWidths()
        {
            var module = ScriptableObject.CreateInstance<GlobalUIModule>();
            var domain = new UIDomainDefinition();
            var panel = new UIPanelDefinition { Key = "Notice", PrefabAddress = "RangerUI/Notice" };
            module.GlobalConfiguration.Domains.Add(domain);
            module.GlobalConfiguration.Panels.Add(panel);
            var inspector = UnityEditor.Editor.CreateEditor(module);
            var window = ScriptableObject.CreateInstance<UIInspectorPreviewWindow>();
            bool shown = false;
            try
            {
                window.Inspector = inspector;
                var expanded = (HashSet<object>)typeof(GlobalUIModuleInspector).GetField("m_expanded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inspector);
                expanded.Add(domain); expanded.Add(panel);
                var tree = typeof(GlobalUIModuleInspector).GetField("m_tree", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inspector);
                var getProperty = tree.GetType().GetMethod("GetPropertyAtPath", new[] { typeof(string) });
                Assert.That(getProperty.Invoke(tree, new object[] { "GlobalConfiguration.Panels" }), Is.Not.Null, "Odin 面板属性树必须可访问。");
                Assert.That(getProperty.Invoke(tree, new object[] { "m_loadPriority" }), Is.Not.Null, "高级设置保留加载优先级。");
                window.ShowUtility();
                shown = true;
                foreach (int width in new[] { 360, 800 })
                {
                    window.position = new Rect(20, 20, width, 900);
                    foreach (RenderMode mode in Enum.GetValues(typeof(RenderMode)))
                    {
                        domain.RenderMode = mode;
                        int before = window.DrawCount;
                        for (int frame = 0; frame < 8; frame++) { window.Repaint(); yield return null; }
                        Assert.That(window.Error, Is.Null, window.Error?.ToString());
                        Assert.That(window.DrawCount, Is.GreaterThan(before), "必须执行真实 Inspector GUI 绘制。");
                        Assert.That(panel.Logic, Is.Not.Null);
                        Assert.That(panel.MutexGroup, Is.Null, "仅查看不能将原有 null 字段改写为空字符串。");
                        Assert.That(typeof(GlobalUIModuleInspector).GetField("m_narrow", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inspector), Is.EqualTo(width < 480));
                    }
                }
            }
            finally
            {
                if (shown) window.Close(); else UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(inspector); UnityEngine.Object.DestroyImmediate(module);
            }
        }
    }
}
