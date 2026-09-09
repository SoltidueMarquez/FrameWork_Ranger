using System;
using System.Collections.Generic;
using System.Linq;
using FrameWork_Ranger.ResourceManagement;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.UI.Editor
{
    [CustomEditor(typeof(GlobalUIModule))]
    [FrameworkArchitecture("UI 中文配置 Inspector", "统一域和面板编辑，按模式显示参数并保留 Odin 逻辑编辑。", FrameworkArchitectureLayer.EditorIntegration, 140)]
    public sealed class GlobalUIModuleInspector : UnityEditor.Editor
    {
        private PropertyTree m_tree;
        private int m_owner;
        private bool m_advanced;
        private readonly HashSet<object> m_expanded = new HashSet<object>();
        private List<string> m_errors;
        private bool m_narrow;
        private UnityEditor.Editor m_resourceEditor;
        internal static readonly string[] RenderNames = { "屏幕叠加", "相机空间", "世界空间" };
        internal static bool UsesScreenSize(RenderMode mode) => mode != RenderMode.WorldSpace;
        internal static bool UsesCameraDistance(RenderMode mode) => mode == RenderMode.ScreenSpaceCamera;
        private void OnEnable() { m_tree = PropertyTree.Create(serializedObject); }
        private void OnDisable() { m_tree?.Dispose(); m_tree = null; if (m_resourceEditor) DestroyImmediate(m_resourceEditor); }

        public override void OnInspectorGUI()
        {
            var module = (GlobalUIModule)target;
            if (!module) return;
            EditorGUILayout.LabelField("UI 模块 — 指定一份资源总配置", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || module.IsRuntimeInstance))
            {
                var resources = UIResourceAssetTools.FindAll();
                int selected = Array.IndexOf(resources, module.UIResources) + 1;
                int next = EditorGUILayout.Popup("指定资源总配置", selected,
                    new[] { "未指定（兼容旧目录）" }.Concat(resources.Select(a => a.name + " — " + AssetDatabase.GetAssetPath(a))).ToArray());
                if (next != selected) { Undo.RecordObject(module, "指定 UI 资源总配置"); module.UIResources = next == 0 ? null : resources[next - 1]; EditorUtility.SetDirty(module); }
                if (module.UIResources)
                {
                    UnityEditor.Editor.CreateCachedEditor(module.UIResources, typeof(UIResourcesConfigInspector), ref m_resourceEditor);
                    m_resourceEditor.OnInspectorGUI();
                    m_advanced = EditorGUILayout.Foldout(m_advanced, "高级设置", true);
                    if (m_advanced)
                    {
                        m_tree.BeginDraw(true);
                        try { m_tree.GetPropertyAtPath("m_loadPriority")?.Draw(new GUIContent("加载优先级")); }
                        finally { m_tree.EndDraw(); }
                    }
                    return;
                }
                EditorGUILayout.HelpBox("这是旧版内嵌目录。提取后改用独立总配置与面板 SO，旧数据保留用于回退。", MessageType.Info);
                if (GUILayout.Button("提取旧目录为独立 SO")) { UIResourceAssetTools.Extract(module); return; }
            }
            m_tree ??= PropertyTree.Create(serializedObject);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            m_narrow = EditorGUIUtility.currentViewWidth < 480;
            EditorGUIUtility.labelWidth = 132;
            m_owner = GUILayout.Toolbar(m_owner, new[] { "全局 UI", "场景 UI" });
            EditorGUILayout.HelpBox(m_owner == 0
                ? "全局 UI：跨场景保留，例如全局提示和加载遮罩。这里只定义可用面板，打开时才加载资源。"
                : "场景 UI：随当前场景作用域结束清理，例如 HUD 和菜单。每个场景按需打开同一目录中的面板。", MessageType.Info);
            bool readOnly = EditorApplication.isPlayingOrWillChangePlaymode || module.IsRuntimeInstance;
            using (new EditorGUI.DisabledScope(readOnly))
            {
                m_tree.BeginDraw(true);
                try
                {
                    Undo.RecordObject(module, "编辑 UI 配置");
                    EditorGUI.BeginChangeCheck();
                    var config = m_owner == 0 ? module.GlobalConfiguration : module.SceneConfiguration;
                    if (config == null)
                    {
                        EditorGUILayout.HelpBox("配置目录为空。", MessageType.Error);
                        if (GUILayout.Button("建立空目录"))
                        {
                            if (m_owner == 0) module.GlobalConfiguration = new UIConfiguration();
                            else module.SceneConfiguration = new UIConfiguration();
                        }
                    }
                    else
                    {
                        DrawDomains(config);
                        EditorGUILayout.Space(8);
                        DrawPanels(config);
                    }
                    m_advanced = EditorGUILayout.Foldout(m_advanced, "高级设置", true);
                    if (m_advanced) m_tree.GetPropertyAtPath("m_loadPriority")?.Draw(new GUIContent("加载优先级", "数值越小，同依赖层内越早加载。"));
                    if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(module); m_errors = null; }
                }
                finally { m_tree.EndDraw(); EditorGUIUtility.labelWidth = oldLabelWidth; }
            }
            if (GUILayout.Button("校验配置")) m_errors = module.ValidateConfiguration();
            if (m_errors != null)
            {
                if (m_errors.Count == 0) EditorGUILayout.HelpBox("配置校验通过。", MessageType.Info);
                foreach (var error in m_errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }

        private void DrawDomains(UIConfiguration config)
        {
            EditorGUILayout.LabelField("UI 域", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("决定一组面板在哪里、如何显示。面板通过域标识关联到这里。", EditorStyles.wordWrappedMiniLabel);
            if (config.Domains == null)
            {
                if (GUILayout.Button("建立域列表")) config.Domains = new List<UIDomainDefinition>();
                return;
            }
            for (int i = 0; i < config.Domains.Count; i++)
            {
                var domain = config.Domains[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (domain == null)
                    {
                        if (GUILayout.Button("移除空域条目")) { config.Domains.RemoveAt(i); break; }
                        continue;
                    }
                    string mode = Enum.IsDefined(typeof(RenderMode), domain.RenderMode) ? RenderNames[(int)domain.RenderMode] : "无效模式";
                    if (Header(domain, $"{domain.Key} · {mode} · 排序 {domain.SortingOrder}"))
                    {
                        domain.Key = Text("域标识", domain.Key, "面板通过此标识选择所属域；同一目录内不可重复。");
                        domain.RenderMode = (RenderMode)Choice("渲染方式", (int)domain.RenderMode, RenderNames);
                        domain.SortingOrder = Integer("域显示顺序", domain.SortingOrder, "同类 Canvas 排序值越大越靠前；与面板层级分开。");
                        if (UsesScreenSize(domain.RenderMode))
                        {
                            domain.ReferenceResolution = Size("参考分辨率", domain.ReferenceResolution);
                            Label("屏幕适配", "0 按宽度适配，1 按高度适配，0.5 折中。");
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label("按宽度", GUILayout.Width(45));
                                domain.MatchWidthOrHeight = EditorGUILayout.Slider(domain.MatchWidthOrHeight, 0, 1);
                                GUILayout.Label("按高度", GUILayout.Width(45));
                            }
                        }
                        if (UsesCameraDistance(domain.RenderMode))
                        {
                            domain.PlaneDistance = Number("距相机距离", domain.PlaneDistance);
                            EditorGUILayout.HelpBox("在场景的 UI 域绑定组件中指定相机。", MessageType.None);
                        }
                        if (domain.RenderMode == RenderMode.WorldSpace)
                        {
                            domain.WorldSize = Size("世界画布尺寸", domain.WorldSize);
                            domain.WorldScale = Number("世界缩放", domain.WorldScale, "每个画布单位对应的世界单位比例。");
                            EditorGUILayout.HelpBox("通过场景 UI 域绑定组件指定相机与挂载点；画布不会成为挂载点的子对象。", MessageType.None);
                        }
                        if (GUILayout.Button("移除此域")) { m_expanded.Remove(domain); config.Domains.RemoveAt(i); break; }
                    }
                }
            }
            if (GUILayout.Button("添加 UI 域"))
            {
                var module = (GlobalUIModule)target;
                var usedOrders = new HashSet<int>(new[] { module.GlobalConfiguration, module.SceneConfiguration }
                    .Where(c => c?.Domains != null).SelectMany(c => c.Domains).Where(d => d != null).Select(d => d.SortingOrder));
                int order = 0; while (usedOrders.Contains(order) && order < short.MaxValue) order++;
                var domain = new UIDomainDefinition { Key = UniqueKey("Domain", config.Domains.Where(d => d != null).Select(d => d.Key)), SortingOrder = order };
                config.Domains.Add(domain); m_expanded.Add(domain);
            }
        }

        private void DrawPanels(UIConfiguration config)
        {
            EditorGUILayout.LabelField("面板目录", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("具体窗口的资源、逻辑和交互行为；域本身不是面板。", EditorStyles.wordWrappedMiniLabel);
            if (config.Panels == null)
            {
                if (GUILayout.Button("建立面板列表")) config.Panels = new List<UIPanelDefinition>();
                return;
            }
            for (int i = 0; i < config.Panels.Count; i++)
            {
                var panel = config.Panels[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (panel == null)
                    {
                        if (GUILayout.Button("移除空面板条目")) { config.Panels.RemoveAt(i); break; }
                        continue;
                    }
                    if (!Header(panel, $"{panel.Key} · {panel.DomainKey} · {(panel.ClosePolicy == UIClosePolicy.KeepAlive ? "关闭后保留" : "关闭后销毁")}")) continue;
                    Section("身份与所属域");
                    panel.Key = Text("面板标识", panel.Key, "业务 OpenAsync 使用的名字；同一全局/场景目录内唯一。");
                    string[] domains = config.Domains?.Where(d => d != null && !string.IsNullOrEmpty(d.Key)).Select(d => d.Key).Distinct().ToArray() ?? Array.Empty<string>();
                    int found = Array.IndexOf(domains, panel.DomainKey);
                    string[] options = new[] { found < 0 ? "未配置或已失效：" + panel.DomainKey : "请选择所属域" }.Concat(domains).ToArray();
                    int chosen = Choice("所属 UI 域", found + 1, options);
                    if (chosen > 0) panel.DomainKey = domains[chosen - 1];
                    if (found < 0) EditorGUILayout.HelpBox("所属域不存在，请先添加 UI 域或重新选择。", MessageType.Error);
                    Section("资源与逻辑");
                    int backend = panel.Backend == ResourceBackendKind.UnityResources ? 0 : 1;
                    int selectedBackend = Choice("资源后端", backend, new[] { "Unity 资源目录（Resources）", "可寻址资源（Addressables）" });
                    if (selectedBackend != backend) panel.Backend = selectedBackend == 0 ? ResourceBackendKind.UnityResources : ResourceBackendKind.Addressables;
                    panel.PrefabAddress = Text("预制体地址", panel.PrefabAddress, "Resources 地址不含 Resources/ 前缀和 .prefab 后缀；Addressables 使用资源地址。");
                    EditorGUILayout.LabelField(panel.Backend == ResourceBackendKind.UnityResources ? "例：RangerUI/Notice → Resources/RangerUI/Notice.prefab" : "使用 Addressables 中登记的资源地址。", EditorStyles.wordWrappedMiniLabel);
                    var listProperty = m_tree.GetPropertyAtPath((m_owner == 0 ? "GlobalConfiguration" : "SceneConfiguration") + ".Panels");
                    if (listProperty != null && i < listProperty.Children.Count)
                        listProperty.Children[i].Children["Logic"]?.Draw(new GUIContent("面板逻辑", "选择 UILogic 类型；业务 View 引用仍在 Prefab 上配置。"));
                    Section("显示与交互");
                    panel.Layer = Integer("面板层级", panel.Layer, "仅在所属域内比较；数值越大越靠前。置顶不会跨层。");
                    panel.MutexGroup = Text("互斥组", panel.MutexGroup, "留空表示并存；同一所有者、同域、同组面板互斥。");
                    panel.Modal = (UIModalScope)Choice("模态输入范围", (int)panel.Modal, new[] { "不限制（普通面板）", "所属域（常用模态）", "全部 UI 域（全局模态）" });
                    EditorGUILayout.LabelField("模态只限制受管 UI 输入，不暂停游戏。依附弹窗由业务指定父面板。", EditorStyles.wordWrappedMiniLabel);
                    Section("关闭行为");
                    panel.ClosePolicy = (UIClosePolicy)Choice("关闭后处理", (int)panel.ClosePolicy, new[] { "隐藏保留（下次复用）", "销毁释放（下次重建）" });
                    if (GUILayout.Button("移除此面板")) { m_expanded.Remove(panel); config.Panels.RemoveAt(i); break; }
                }
            }
            if (GUILayout.Button("添加面板"))
            {
                var panel = new UIPanelDefinition { Key = UniqueKey("Panel", config.Panels.Where(p => p != null).Select(p => p.Key)), DomainKey = config.Domains?.FirstOrDefault(d => d != null)?.Key ?? "" };
                config.Panels.Add(panel); m_expanded.Add(panel);
            }
        }
        private bool Header(object key, string title)
        {
            bool expanded = EditorGUILayout.Foldout(m_expanded.Contains(key), title, true, EditorStyles.foldoutHeader);
            if (expanded) m_expanded.Add(key); else m_expanded.Remove(key);
            return expanded;
        }
        private static string UniqueKey(string prefix, IEnumerable<string> keys)
        { var used = new HashSet<string>(keys); int n = 1; while (used.Contains(prefix + n)) n++; return prefix + n; }
        private static void Section(string label) { EditorGUILayout.Space(4); EditorGUILayout.LabelField(label, EditorStyles.boldLabel); }
        private static void Label(string label, string tip) => EditorGUILayout.LabelField(new GUIContent(label, tip));
        private GUIContent FieldLabel(string label, string tip)
        { if (m_narrow) Label(label, tip); return m_narrow ? GUIContent.none : new GUIContent(label, tip); }
        private string Text(string label, string value, string tip = "")
        {
            string edited = EditorGUILayout.TextField(FieldLabel(label, tip), value ?? "");
            return value == null && edited.Length == 0 ? null : edited;
        }
        private int Integer(string label, int value, string tip = "") => EditorGUILayout.IntField(FieldLabel(label, tip), value);
        private float Number(string label, float value, string tip = "") => EditorGUILayout.FloatField(FieldLabel(label, tip), value);
        private int Choice(string label, int value, string[] values) => EditorGUILayout.Popup(FieldLabel(label, ""), value, values.Select(v => new GUIContent(v)).ToArray());
        private Vector2 Size(string label, Vector2 value)
        {
            Label(label, "");
            float width = EditorGUIUtility.labelWidth; EditorGUIUtility.labelWidth = 22;
            using (new EditorGUILayout.HorizontalScope())
            { value.x = EditorGUILayout.FloatField("宽", value.x); value.y = EditorGUILayout.FloatField("高", value.y); }
            EditorGUIUtility.labelWidth = width; return value;
        }
    }
}
