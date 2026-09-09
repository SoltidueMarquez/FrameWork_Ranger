using System;
using System.Linq;
using FrameWork_Ranger.Editor;
using UnityEditor;
using UnityEngine;
namespace FrameWork_Ranger.UI.Editor
{
    [FrameworkCenterPageExtension]
    [FrameworkArchitecture("UI 配置页", "下拉选择资源总配置，明确项目使用与演示状态。", FrameworkArchitectureLayer.EditorIntegration, 140)]
    public sealed class UIFrameworkCenterPage : FrameworkCenterPage
    {
        public override string PageId => "base-modules/ui";
        public override string DisplayName => "UI";
        public override string Description => "指定 UI 资源总配置，配置域、独立面板和场景启用规则。";
        public override string Category => "基础模块";
        public override int Order => 40;
        public override string HelpDocumentPath => "Assets/Plugins/FrameWork_Ranger/BaseModules/UI/README.md";
        private const string SelectionKey = "FrameWork_Ranger.UI.ResourcesSelection";
        private UIResourcesConfig m_config;
        private UIResourcesConfig[] m_assets = Array.Empty<UIResourcesConfig>();
        private UnityEditor.Editor m_editor;
        private bool m_dirty = true, m_tools;
        private GameObject m_prefab;
        private string m_message;
        private const int Picker = 184703;
        public override void OnActivated(FrameworkCenterPageContext context) { m_dirty = true; EditorApplication.projectChanged += MarkDirty; }
        private void MarkDirty() { m_dirty = true; }
        private void Select(UIResourcesConfig config)
        {
            if (m_editor) UnityEngine.Object.DestroyImmediate(m_editor);
            m_config = config; m_editor = config ? UnityEditor.Editor.CreateEditor(config) : null;
            SessionState.SetString(SelectionKey, config ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config)) : "");
        }
        public override void OnGUI(FrameworkCenterPageContext context)
        {
            var settings = UIConfigurationAssets.Settings;
            var module = UIConfigurationAssets.Active(settings?.GlobalConfig);
            var active = module ? module.UIResources : null;
            if (m_dirty)
            {
                m_dirty = false; m_assets = UIResourceAssetTools.FindAll();
                string guid = SessionState.GetString(SelectionKey, "");
                Select(UIResourceAssetTools.ResolveSelection(m_assets, guid, active));
            }
            EditorGUILayout.LabelField("UI 资源总配置", EditorStyles.boldLabel);
            bool canInstall = settings && settings.GlobalConfig && UIConfigurationAssets.UIEntries(settings.GlobalConfig).Count <= 1;
            if (!settings || !settings.GlobalConfig) EditorGUILayout.HelpBox("中央 GlobalConfig 未指定，请先在项目配置页设置。仍可编辑资源总配置。", MessageType.Warning);
            else if (!canInstall) EditorGUILayout.HelpBox("中央配置存在多个 UI 模块条目，请先整理为一个，当前不会猜测替换对象。", MessageType.Error);
            int current = Array.IndexOf(m_assets, m_config) + 1;
            int next = EditorGUILayout.Popup(current, new[] { "请选择总配置" }.Concat(m_assets.Select(a =>
                (a == active ? "【项目使用】" : "") + (a.IsSample ? "【演示】" : "") + a.name + " — " + AssetDatabase.GetAssetPath(a))).ToArray());
            if (next != current) Select(next == 0 ? null : m_assets[next - 1]);
            EditorGUILayout.HelpBox(m_config && active == m_config ? "项目当前使用此总配置。下方修改会保存到这份资产。" :
                "选择只切换编辑对象；“设为项目使用”才会修改唯一 UI 模块的资源引用。", MessageType.Info);
            if (module && !module.UIResources)
            {
                EditorGUILayout.HelpBox("项目仍使用旧版内嵌目录。可保留模块和 Prefab GUID，提取独立总配置与面板 SO。", MessageType.Warning);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("提取当前项目的旧 UI 配置")) Run(() => { Select(UIResourceAssetTools.Extract(module)); MarkDirty(); });
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!m_config))
                    if (GUILayout.Button("定位总配置")) EditorGUIUtility.PingObject(m_config);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("创建空白总配置"))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("创建 UI 资源总配置", "UIResources", "asset", "仅含默认域，不包含演示面板，不自动安装。");
                        if (!string.IsNullOrEmpty(path)) Run(() => { Select(UIResourceAssetTools.Create(path)); MarkDirty(); });
                    }
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || !canInstall || !m_config || active == m_config))
                if (GUILayout.Button("设为项目使用"))
                {
                    string path = null;
                    var entries = UIConfigurationAssets.UIEntries(settings?.GlobalConfig);
                    if (entries.Count == 0)
                    {
                        path = EditorUtility.SaveFilePanelInProject("保存唯一 UI 模块", "GlobalUIModule", "asset", "校验通过后安装模块并指定这份总配置。");
                        if (string.IsNullOrEmpty(path)) return;
                    }
                    Run(() => { UIResourceAssetTools.Use(m_config, path); m_message = "已指定项目使用，可撤销。"; MarkDirty(); });
                }
            if (!string.IsNullOrEmpty(m_message)) EditorGUILayout.HelpBox(m_message, MessageType.Info);
            if (m_editor) m_editor.OnInspectorGUI();
            m_tools = EditorGUILayout.Foldout(m_tools, "辅助工具：Prefab 创建与校验（可选）", true);
            if (m_tools)
            {
                EditorGUILayout.HelpBox("此处只提供独立检查。实际运行 Prefab 在各面板 SO 的“资源与逻辑”中指定。", MessageType.None);
                EditorGUILayout.LabelField(m_prefab ? AssetDatabase.GetAssetPath(m_prefab) : "尚未选择待校验 Prefab", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("选择待校验 Prefab")) EditorGUIUtility.ShowObjectPicker<GameObject>(m_prefab, false, "t:Prefab", Picker);
                if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == Picker)
                { m_prefab = EditorGUIUtility.GetObjectPickerObject() as GameObject; context.Repaint(); }
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("创建 UIView Prefab 骨架"))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("创建面板 Prefab", "Panel", "prefab", "创建后在面板 SO 中指定。");
                        if (!string.IsNullOrEmpty(path)) Run(() => m_prefab = UIEditorTools.CreatePanelPrefab(path));
                    }
                using (new EditorGUI.DisabledScope(!m_prefab))
                    if (GUILayout.Button("校验 Prefab")) m_message = string.Join("\n", UIEditorTools.ValidatePrefab(m_prefab));
            }
        }
        private void Run(Action action) { try { action(); } catch (Exception ex) { m_message = ex.Message; } }
        public override void OnDeactivated(FrameworkCenterPageContext context)
        {
            EditorApplication.projectChanged -= MarkDirty;
            if (m_editor) UnityEngine.Object.DestroyImmediate(m_editor);
            m_editor = null;
        }
    }
}
