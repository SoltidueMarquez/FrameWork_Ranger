using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
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
        private FrameworkConfigurationWorkspace m_workspace;
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
        public override bool UseHostContentScroll => false;
        public override string HelpDocumentPath =>
            "Assets/Plugins/FrameWork_Ranger/Docs/03_Architecture/EditorCenter/ADR/ADR-EC-009_HTY_Style_Configuration_Workspace.md";

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

            EnsureWorkspace(settings);
            m_workspace.Draw(settings, context);
        }

        public override void OnDeactivated(FrameworkCenterPageContext context)
        {
            ReleaseWorkspace();
        }

        private void EnsureWorkspace(FrameworkProjectSettings settings)
        {
            if (m_editingSettings == settings && m_workspace != null)
            {
                return;
            }

            ReleaseWorkspace();
            m_editingSettings = settings;
            m_workspace = new FrameworkConfigurationWorkspace(settings);
        }

        private void ReleaseWorkspace()
        {
            m_workspace?.Dispose();
            m_workspace = null;
            m_editingSettings = null;
        }
    }
}
