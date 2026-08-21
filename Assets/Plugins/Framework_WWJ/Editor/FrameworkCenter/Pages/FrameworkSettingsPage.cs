using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// Framework Center 中央项目设置页面。
    /// </summary>
    [FrameworkArchitecture(
        "Center 项目配置页",
        "创建、编辑、定位和验证固定 FrameworkProjectSettings。",
        FrameworkArchitectureLayer.EditorIntegration,
        210,
        typeof(FrameworkProjectSettingsInspector))]
    [FrameworkCenterPageExtension]
    internal sealed class FrameworkSettingsPage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords = { "GlobalConfig", "SceneConfig", "场景映射", "依赖图" };
        private UnityEditor.Editor m_embeddedEditor;
        private FrameworkProjectSettings m_editingSettings;

        public FrameworkSettingsPage()
        {
        }

        public override string PageId => "framework.settings";
        public override string DisplayName => "项目配置";
        public override string Description => "维护固定中央设置、场景覆盖关系和组合模块图。";
        public override string Category => "基础";
        public override int Order => 10;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/Framework_WWJ/Docs/03_Architecture/Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            var settings = FrameworkProjectSettingsAssetUtility.Load();
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    $"固定设置资产不存在：{FrameworkProjectSettingsAssetUtility.FixedAssetPath}",
                    MessageType.Error);
                if (GUILayout.Button("创建固定项目设置"))
                {
                    settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
                    context.SelectObject(settings);
                    context.Repaint();
                }

                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位设置资产", GUILayout.Width(120f)))
            {
                context.SelectObject(settings);
            }

            if (GUILayout.Button("保存并重新校验", GUILayout.Width(140f)))
            {
                FrameworkProjectSettingsAssetUtility.SyncScenePaths(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f);

            EnsureEmbeddedEditor(settings);
            m_embeddedEditor?.OnInspectorGUI();
        }

        public override void OnDeactivated(FrameworkCenterPageContext context)
        {
            ReleaseEmbeddedEditor();
        }

        private void EnsureEmbeddedEditor(FrameworkProjectSettings settings)
        {
            if (m_editingSettings == settings && m_embeddedEditor != null)
            {
                return;
            }

            ReleaseEmbeddedEditor();
            m_editingSettings = settings;
            m_embeddedEditor = UnityEditor.Editor.CreateEditor(settings);
        }

        private void ReleaseEmbeddedEditor()
        {
            if (m_embeddedEditor != null)
            {
                Object.DestroyImmediate(m_embeddedEditor);
            }

            m_embeddedEditor = null;
            m_editingSettings = null;
        }
    }
}
