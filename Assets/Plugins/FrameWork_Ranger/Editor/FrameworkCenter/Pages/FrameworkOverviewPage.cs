using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// Framework Center 首页，汇总当前状态、中央配置和常用入口。
    /// </summary>
    [FrameworkArchitecture(
        "Center 概览页",
        "汇总框架状态、中央配置诊断和常用编辑器入口。",
        FrameworkArchitectureLayer.EditorIntegration,
        200,
        typeof(FrameworkCenterPage),
        typeof(FrameworkProjectSettingsAssetUtility))]
    [FrameworkCenterPageExtension]
    internal sealed class FrameworkOverviewPage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords = { "首页", "状态", "配置", "dashboard" };

        public FrameworkOverviewPage()
        {
        }

        public override string PageId => "framework.overview";
        public override string DisplayName => "概览";
        public override string Description => "查看框架状态、中央配置健康度与常用入口。";
        public override string Category => "基础";
        public override int Order => 0;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/FrameWork_Ranger/Docs/03_Architecture/EditorCenter/01_Editor_Center_Architecture.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginHorizontal();
            DrawRuntimeCard();
            DrawSettingsCard(context);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("常用入口", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("项目配置", GUILayout.Height(34f)))
            {
                context.OpenPage("framework.settings");
            }

            if (GUILayout.Button("代码架构", GUILayout.Height(34f)))
            {
                context.OpenPage("framework.architecture");
            }

            if (GUILayout.Button("打开设计文档", GUILayout.Height(34f)))
            {
                context.OpenHelp(HelpDocumentPath);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRuntimeCard()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(300f), GUILayout.Height(126f));
            EditorGUILayout.LabelField("Runtime", FrameworkCenterStyles.CardTitle);
            EditorGUILayout.LabelField($"状态：{Framework.State}");
            EditorGUILayout.LabelField($"Ready：{Framework.IsReady}");
            EditorGUILayout.LabelField(
                Framework.LastException == null ? "最近异常：无" : $"最近异常：{Framework.LastException.Message}",
                FrameworkCenterStyles.Description);
            EditorGUILayout.EndVertical();
        }

        private static void DrawSettingsCard(FrameworkCenterPageContext context)
        {
            var settings = FrameworkProjectSettingsAssetUtility.Load();
            FrameworkProjectSettingsAssetUtility.Validate(settings, out var errors, out var warnings);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(360f), GUILayout.Height(126f));
            EditorGUILayout.LabelField("中央项目设置", FrameworkCenterStyles.CardTitle);
            EditorGUILayout.LabelField(settings == null ? "固定资产：缺失" : $"固定资产：{settings.name}");
            EditorGUILayout.LabelField($"错误 {errors.Count} / 警告 {warnings.Count}");
            if (settings != null && GUILayout.Button("定位设置资产"))
            {
                context.SelectObject(settings);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
