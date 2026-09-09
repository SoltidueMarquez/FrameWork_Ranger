using System;
using System.Collections.Generic;
using System.Linq;
using FrameWork_Ranger.ResourceManagement;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
namespace FrameWork_Ranger.UI.Editor
{
    [CustomEditor(typeof(UIResourcesConfig))]
    [FrameworkArchitecture("UI 总配置 Inspector", "中文编辑域、面板引用及场景启用规则。", FrameworkArchitectureLayer.EditorIntegration, 145)]
    public sealed class UIResourcesConfigInspector : UnityEditor.Editor
    {
        private int m_owner;
        private readonly HashSet<object> m_expanded = new HashSet<object>();
        private UnityEditor.Editor m_panelEditor;
        private string m_message;
        private void OnDisable() { if (m_panelEditor) DestroyImmediate(m_panelEditor); }
        public override void OnInspectorGUI()
        {
            var asset = (UIResourcesConfig)target;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                float width = EditorGUIUtility.labelWidth; EditorGUIUtility.labelWidth = 132;
                try
                {
                    if (asset.IsSample) EditorGUILayout.HelpBox("演示配置：用于学习和验证。新建业务总配置不会包含这些面板。", MessageType.Info);
                    m_owner = GUILayout.Toolbar(m_owner, new[] { "全局 UI", "场景 UI" });
                    EditorGUILayout.HelpBox(m_owner == 0 ? "跨场景保留的窗口。这里收录独立面板 SO；由业务按需打开。" :
                        "随 SceneScope 结束清理。按面板选择适用场景，可独立设置进入场景自动打开。", MessageType.None);
                    var dir = m_owner == 0 ? asset.Global : asset.Scene;
                    Undo.RecordObject(asset, "编辑 UI 资源目录"); EditorGUI.BeginChangeCheck();
                    EditorGUILayout.LabelField("UI 域 — 决定在哪里、如何显示", EditorStyles.boldLabel);
                    foreach (var domain in dir.Domains.ToArray())
                    {
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            if (domain == null) { if (GUILayout.Button("移除空域")) dir.Domains.Remove(domain); continue; }
                            if (Fold(domain, domain.Key + " · " + GlobalUIModuleInspector.RenderNames[(int)domain.RenderMode] + " · 排序 " + domain.SortingOrder))
                            {
                                UIConfigurationGUI.Domain(domain);
                                if (GUILayout.Button("移除此域")) dir.Domains.Remove(domain);
                            }
                        }
                    }
                    if (GUILayout.Button("添加 UI 域")) dir.Domains.Add(new UIDomainDefinition { Key = "Domain" + dir.Domains.Count, SortingOrder = m_owner == 0 ? 100 + dir.Domains.Count : dir.Domains.Count });
                    EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField("面板目录 — 引用独立面板配置 SO", EditorStyles.boldLabel);
                    foreach (var entry in dir.Panels.ToArray())
                    {
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            if (entry == null) { if (GUILayout.Button("移除空条目")) dir.Panels.Remove(entry); continue; }
                            string key = entry.Panel ? entry.Panel.Definition.Key : "未选择面板";
                            string summary = entry.Panel ? " · " + entry.Panel.Definition.DomainKey +
                                (entry.Panel.Definition.ClosePolicy == UIClosePolicy.KeepAlive ? " · 关闭保留" : " · 关闭销毁") : "";
                            if (!Fold(entry, (entry.Enabled ? "" : "【停用】") + key + summary + (entry.AutoOpen && m_owner == 1 ? " · 自动打开" : ""))) continue;
                            entry.Enabled = EditorGUILayout.Toggle("收录启用", entry.Enabled);
                            entry.Panel = (UIPanelConfig)EditorGUILayout.ObjectField("面板配置 SO", entry.Panel, typeof(UIPanelConfig), false);
                            if (GUILayout.Button("创建面板配置 SO"))
                            {
                                string path = EditorUtility.SaveFilePanelInProject("创建面板配置", "UIPanel", "asset", "创建后在面板中指定实际 Prefab");
                                if (!string.IsNullOrEmpty(path) && !AssetDatabase.LoadMainAssetAtPath(path))
                                {
                                    entry.Panel = CreateInstance<UIPanelConfig>();
                                    entry.Panel.Definition.Key = System.IO.Path.GetFileNameWithoutExtension(path);
                                    entry.Panel.Definition.DomainKey = dir.Domains.FirstOrDefault()?.Key ?? "";
                                    AssetDatabase.CreateAsset(entry.Panel, path);
                                }
                            }
                            if (m_owner == 1)
                            {
                                entry.AllScenes = EditorGUILayout.Toggle("适用全部场景", entry.AllScenes);
                                if (!entry.AllScenes)
                                {
                                    for (int i = 0; i < entry.Scenes.Count; i++)
                                    {
                                        var scene = entry.Scenes[i] ??= new UISceneReference();
                                        var old = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(scene.Guid ?? ""));
                                        var next = (SceneAsset)EditorGUILayout.ObjectField("适用场景 " + (i + 1), old, typeof(SceneAsset), false);
                                        if (next != old) { scene.Path = next ? AssetDatabase.GetAssetPath(next) : ""; scene.Guid = next ? AssetDatabase.AssetPathToGUID(scene.Path) : ""; }
                                        if (!old) EditorGUILayout.HelpBox("场景引用未指定或已失效。", MessageType.Warning);
                                        if (GUILayout.Button("移除此场景 " + (i + 1))) { entry.Scenes.RemoveAt(i); break; }
                                    }
                                    if (GUILayout.Button("添加适用场景")) entry.Scenes.Add(new UISceneReference());
                                    if (entry.Scenes.Count == 0) EditorGUILayout.HelpBox("当前没有适用场景，此面板不能打开。", MessageType.Info);
                                }
                                entry.AutoOpen = EditorGUILayout.Toggle("进入场景自动打开", entry.AutoOpen);
                                if (entry.AutoOpen)
                                {
                                    entry.Required = EditorGUILayout.Toggle("必须成功", entry.Required);
                                    EditorGUILayout.HelpBox(entry.Required ? "失败会回滚本轮 SceneScope，保留 Global。" : "失败报告原因并继续；已由业务打开则跳过。", MessageType.None);
                                }
                            }
                            if (entry.Panel)
                            {
                                UnityEditor.Editor.CreateCachedEditor(entry.Panel, typeof(UIPanelConfigInspector), ref m_panelEditor);
                                var editor = (UIPanelConfigInspector)m_panelEditor;
                                editor.Directory = dir;
                                editor.ContextLabel = asset.name + (m_owner == 0 ? " / 全局" : " / 场景");
                                editor.OnInspectorGUI();
                            }
                            if (GUILayout.Button("从目录移除（保留 SO）")) dir.Panels.Remove(entry);
                        }
                    }
                    if (GUILayout.Button("添加面板引用")) dir.Panels.Add(new UIPanelEntry());
                    if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(asset); m_message = null; }
                    if (GUILayout.Button("校验资源总配置"))
                    {
                        UIResourceAssetTools.SyncScenes(asset);
                        var errors = asset.Validate();
                        foreach (var directory in new[] { asset.Global, asset.Scene })
                            foreach (var item in directory.Panels)
                                if (item?.Enabled == true && item.Panel && item.Panel.Definition.UseDirectPrefab)
                                    errors.AddRange(UIEditorTools.ValidatePrefab(item.Panel.Definition.DirectPrefab));
                        m_message = errors.Count == 0 ? "配置校验通过。" : string.Join("\n", errors);
                    }
                    if (!string.IsNullOrEmpty(m_message)) EditorGUILayout.HelpBox(m_message, MessageType.Info);
                }
                finally { EditorGUIUtility.labelWidth = width; }
            }
        }
        private bool Fold(object key, string title)
        {
            bool open = EditorGUILayout.Foldout(m_expanded.Contains(key), title, true);
            if (open) m_expanded.Add(key); else m_expanded.Remove(key);
            return open;
        }
    }
    [CustomEditor(typeof(UIPanelConfig))]
    [FrameworkArchitecture("UI 面板 Inspector", "中文资源与行为编辑并保留 Odin 多态逻辑选择。", FrameworkArchitectureLayer.EditorIntegration, 146)]
    public sealed class UIPanelConfigInspector : UnityEditor.Editor
    {
        internal UIResourceDirectory Directory;
        internal string ContextLabel;
        private PropertyTree m_tree;
        private int m_context;
        private string m_message;
        private void OnEnable() { m_tree = PropertyTree.Create(serializedObject); }
        private void OnDisable() { m_tree?.Dispose(); }
        public override void OnInspectorGUI()
        {
            var asset = (UIPanelConfig)target;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                var contexts = new List<(UIResourcesConfig config, UIResourceDirectory dir, string name)>();
                if (Directory == null)
                {
                    foreach (var config in UIResourceAssetTools.FindAll())
                        foreach (var owner in new[] { UIOwner.Global, UIOwner.Scene })
                        {
                            var dir = owner == UIOwner.Global ? config.Global : config.Scene;
                            if (dir.Panels.Any(e => e?.Panel == asset)) contexts.Add((config, dir, config.name + (owner == UIOwner.Global ? " / 全局" : " / 场景")));
                        }
                    if (contexts.Count > 0)
                    {
                        m_context = EditorGUILayout.Popup("域校验依据", Mathf.Clamp(m_context, 0, contexts.Count - 1), contexts.Select(x => x.name).ToArray());
                        ContextLabel = contexts[m_context].name;
                    }
                }
                var directory = Directory ?? (contexts.Count > 0 ? contexts[m_context].dir : null);
                if (!string.IsNullOrEmpty(ContextLabel)) EditorGUILayout.LabelField("域校验依据：" + ContextLabel, EditorStyles.wordWrappedMiniLabel);
                m_tree ??= PropertyTree.Create(serializedObject);
                m_tree.BeginDraw(true);
                try
                {
                    Undo.RecordObject(asset, "编辑 UI 面板"); EditorGUI.BeginChangeCheck();
                    var p = asset.Definition;
                    UIConfigurationGUI.Section("身份与所属域");
                    p.Key = UIConfigurationGUI.Text("面板标识", p.Key);
                    if (directory != null)
                    {
                        var keys = directory.Domains.Where(d => d != null).Select(d => d.Key).ToArray();
                        int index = Array.IndexOf(keys, p.DomainKey);
                        int next = EditorGUILayout.Popup("所属 UI 域", index + 1, new[] { "请选择有效域" }.Concat(keys).ToArray());
                        if (next > 0) p.DomainKey = keys[next - 1];
                        if (index < 0) EditorGUILayout.HelpBox("当前目录不存在此域：" + p.DomainKey, MessageType.Error);
                    }
                    else { p.DomainKey = UIConfigurationGUI.Text("域标识", p.DomainKey); EditorGUILayout.HelpBox("尚未被总配置收录；收录后可从该目录下拉选域。", MessageType.Info); }
                    UIConfigurationGUI.Section("资源与逻辑");
                    int source = p.UseDirectPrefab ? 0 : p.Backend == ResourceBackendKind.UnityResources ? 1 : 2;
                    source = EditorGUILayout.Popup("Prefab 加载方式", source, new[] { "直接引用 Prefab", "Resources 按地址加载", "Addressables 按地址加载" });
                    p.UseDirectPrefab = source == 0;
                    if (source > 0) p.Backend = source == 1 ? ResourceBackendKind.UnityResources : ResourceBackendKind.Addressables;
                    // An address-backed asset must not retain a hidden hard reference.
                    if (!p.UseDirectPrefab && p.DirectPrefab) { p.DirectPrefab = null; EditorUtility.SetDirty(asset); }
                    if (p.UseDirectPrefab)
                    {
                        p.DirectPrefab = (GameObject)EditorGUILayout.ObjectField("实际 Prefab", p.DirectPrefab, typeof(GameObject), false);
                        EditorGUILayout.HelpBox("直接引用会持有 Prefab 资源；关闭销毁的是窗口实例。", MessageType.None);
                    }
                    else
                    {
                        p.PrefabAddress = UIConfigurationGUI.Text("资源地址", p.PrefabAddress);
                        var picked = (GameObject)EditorGUILayout.ObjectField("从 Prefab 填写地址", null, typeof(GameObject), false);
                        if (picked) { try { p.PrefabAddress = UIConfigurationGUI.Address(picked, p.Backend); m_message = null; } catch (Exception ex) { m_message = ex.Message; } }
                    }
                    m_tree.GetPropertyAtPath("Definition.Logic")?.Draw(new GUIContent("面板逻辑"));
                    UIConfigurationGUI.Section("显示与交互");
                    p.Layer = EditorGUILayout.IntField(new GUIContent("面板层级", "仅决定同域面板的显示层，不是域 Canvas 排序。"), p.Layer);
                    p.MutexGroup = UIConfigurationGUI.Text("互斥组", p.MutexGroup);
                    EditorGUILayout.LabelField("留空可并存；同所有者、同域、同组互斥。", EditorStyles.wordWrappedMiniLabel);
                    p.Modal = (UIModalScope)EditorGUILayout.Popup("模态范围", (int)p.Modal, new[] { "无模态限制", "所属 UI 域", "所有 Global / Scene UI 域" });
                    UIConfigurationGUI.Section("关闭行为与动画");
                    p.ClosePolicy = (UIClosePolicy)EditorGUILayout.Popup("关闭后", (int)p.ClosePolicy, new[] { "保留实例", "销毁实例" });
                    p.EnterAnimation ??= new UIPanelAnimation(); p.ExitAnimation ??= new UIPanelAnimation();
                    UIConfigurationGUI.Animation("入场动画", p.EnterAnimation);
                    UIConfigurationGUI.Animation("退场动画", p.ExitAnimation);
                    if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(asset);
                    if (p.UseDirectPrefab && p.DirectPrefab && GUILayout.Button("校验实际 Prefab"))
                        m_message = string.Join("\n", UIEditorTools.ValidatePrefab(p.DirectPrefab));
                    if (!string.IsNullOrEmpty(m_message)) EditorGUILayout.HelpBox(m_message, MessageType.Warning);
                }
                finally { m_tree.EndDraw(); }
            }
        }
    }
    internal static class UIConfigurationGUI
    {
        internal static void Section(string title) { EditorGUILayout.Space(5); EditorGUILayout.LabelField(title, EditorStyles.boldLabel); }
        internal static string Text(string label, string value)
        {
            if (EditorGUIUtility.currentViewWidth < 480) { EditorGUILayout.LabelField(label); return EditorGUILayout.TextField(value ?? "") is string next && next != (value ?? "") ? next : value; }
            string result = EditorGUILayout.TextField(label, value ?? ""); return result == (value ?? "") ? value : result;
        }
        internal static Vector2 Size(string label, Vector2 value)
        {
            EditorGUILayout.LabelField(label);
            value.x = EditorGUILayout.FloatField("宽", value.x);
            value.y = EditorGUILayout.FloatField("高", value.y);
            return value;
        }
        internal static void Domain(UIDomainDefinition d)
        {
            d.Key = Text("域标识", d.Key);
            d.RenderMode = (RenderMode)EditorGUILayout.Popup("渲染方式", (int)d.RenderMode, GlobalUIModuleInspector.RenderNames);
            d.SortingOrder = EditorGUILayout.IntField("域显示顺序", d.SortingOrder);
            if (d.RenderMode != RenderMode.WorldSpace)
            {
                d.ReferenceResolution = Size("参考分辨率", d.ReferenceResolution);
                EditorGUILayout.LabelField("屏幕适配：按宽度 ← → 按高度");
                d.MatchWidthOrHeight = EditorGUILayout.Slider(d.MatchWidthOrHeight, 0, 1);
            }
            if (d.RenderMode == RenderMode.ScreenSpaceCamera)
            {
                d.PlaneDistance = EditorGUILayout.FloatField("距相机距离", d.PlaneDistance);
                EditorGUILayout.HelpBox("通过场景中的 UIDomainBinding 指定相机。", MessageType.None);
            }
            if (d.RenderMode == RenderMode.WorldSpace)
            {
                d.WorldSize = Size("世界画布尺寸", d.WorldSize);
                d.WorldScale = EditorGUILayout.FloatField("世界缩放", d.WorldScale);
                EditorGUILayout.HelpBox("通过 UIDomainBinding 指定相机与挂载点。", MessageType.None);
            }
        }
        internal static void Animation(string label, UIPanelAnimation a)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            a.Fade = EditorGUILayout.Toggle("淡入淡出", a.Fade);
            a.Scale = EditorGUILayout.Toggle("缩放", a.Scale);
            a.Slide = EditorGUILayout.Toggle("滑动", a.Slide);
            if (!a.Fade && !a.Scale && !a.Slide) return;
            a.Duration = Mathf.Max(0, EditorGUILayout.FloatField("时长（秒）", a.Duration));
            if (a.Scale) a.HiddenScale = EditorGUILayout.Vector3Field("隐藏时缩放", a.HiddenScale);
            if (a.Slide) a.Offset = EditorGUILayout.Vector2Field("隐藏时偏移", a.Offset);
            a.Curve = EditorGUILayout.CurveField("过渡曲线", a.Curve);
        }
        internal static string Address(GameObject prefab, ResourceBackendKind backend)
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab)) throw new InvalidOperationException("请选择 Prefab 资产。");
            string path = AssetDatabase.GetAssetPath(prefab);
            if (backend == ResourceBackendKind.UnityResources)
            {
                int start = path.LastIndexOf("/Resources/", StringComparison.Ordinal);
                if (start < 0) throw new InvalidOperationException("此 Prefab 不在 Resources 目录下。");
                return System.IO.Path.ChangeExtension(path.Substring(start + 11), null);
            }
            // 可选 Addressables 包不形成 Editor 硬依赖。
            var type = Type.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
            var settings = type?.GetProperty("Settings")?.GetValue(null);
            var method = settings?.GetType().GetMethod("FindAssetEntry", new[] { typeof(string), typeof(bool) });
            var entry = method?.Invoke(settings, new object[] { AssetDatabase.AssetPathToGUID(path), false });
            string address = entry?.GetType().GetProperty("address")?.GetValue(entry) as string;
            if (string.IsNullOrEmpty(address)) throw new InvalidOperationException("此 Prefab 尚未配置有效的 Addressables 条目。");
            return address;
        }
    }
}
