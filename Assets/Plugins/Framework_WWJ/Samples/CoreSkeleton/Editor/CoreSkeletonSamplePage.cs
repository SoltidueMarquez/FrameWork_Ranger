using System.Collections.Generic;
using Framework_WWJ.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Framework_WWJ.Samples.Editor
{
    /// <summary>
    /// 通过 Framework Center 页面扩展机制提供骨架示例的构建、打开与人工验收入口。
    /// </summary>
    [FrameworkCenterPageExtension]
    public sealed class CoreSkeletonSamplePage : FrameworkCenterPage
    {
        private const string SceneAPath =
            "Assets/Plugins/Framework_WWJ/Samples/CoreSkeleton/Scenes/CoreSkeleton_A.unity";
        private const string SceneBPath =
            "Assets/Plugins/Framework_WWJ/Samples/CoreSkeleton/Scenes/CoreSkeleton_B.unity";

        private static readonly string[] s_keywords =
        {
            "Sample", "Scene A", "Scene B", "验收", "Counter", "Pulse",
        };

        public CoreSkeletonSamplePage()
        {
        }

        public override string PageId => "framework.sample.core-skeleton";
        public override string DisplayName => "Core Skeleton 示例";
        public override string Description => "构建、打开并验收中央启动后的 A/B 骨架示例。";
        public override string Category => "示例";
        public override int Order => 0;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/Framework_WWJ/Docs/03_Architecture/EditorCenter/00_Phase1_1_Implementation_Plan.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重建示例资产与场景", GUILayout.Height(34f)))
            {
                CoreSkeletonSampleAssetBuilder.Build();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("打开 Scene A", GUILayout.Height(34f)))
                {
                    OpenScene(SceneAPath);
                }

                if (GUILayout.Button("打开 Scene B", GUILayout.Height(34f)))
                {
                    OpenScene(SceneBPath);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10f);

            var settings = FrameworkProjectSettingsAssetUtility.Load();
            FrameworkProjectSettingsAssetUtility.Validate(settings, out var errors, out var warnings);
            EditorGUILayout.HelpBox(
                settings == null
                    ? "固定 FrameworkProjectSettings 尚未生成。"
                    : $"中央配置：错误 {errors.Count} / 警告 {warnings.Count}",
                errors.Count > 0 || settings == null ? MessageType.Error :
                warnings.Count > 0 ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.LabelField("人工验收", EditorStyles.boldLabel);
            DrawStep("1", "打开 Scene A 并进入 Play Mode，等待 Framework State = Ready。");
            DrawStep("2", "确认 Handler Type 为 SampleCounterHandler，并记录 Global Clone ID。");
            DrawStep("3", "点击“加载场景 B”，确认 Handler 变为 SamplePulseHandler。");
            DrawStep("4", "确认 Global Clone ID 不变，Scene Tick 与 Value 继续更新。");
            DrawStep("5", "切回 Scene A，确认 Global 实例仍未重建。");
            DrawStep("6", "点击 Framework.ShutdownAsync，确认状态为 Shutdown 且 Host 消失。");
        }

        private static void OpenScene(string path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static void DrawStep(string index, string text)
        {
            EditorGUILayout.LabelField($"{index}. {text}", EditorStyles.wordWrappedLabel);
        }
    }
}
