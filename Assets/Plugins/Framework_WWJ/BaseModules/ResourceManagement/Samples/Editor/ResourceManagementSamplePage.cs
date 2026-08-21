using System.Collections.Generic;
using Framework_WWJ.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Framework_WWJ.ResourceManagement.Samples.Editor
{
    /// <summary>
    /// Resource 双后端示例的生成、打开与人工验收入口。
    /// </summary>
    [FrameworkCenterPageExtension]
    public sealed class ResourceManagementSamplePage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords =
        {
            "Resource", "Sample", "Addressables", "Resources", "Prefab",
        };

        public ResourceManagementSamplePage()
        {
        }

        public override string PageId => "framework.sample.resource-management";
        public override string DisplayName => "Resource Management 示例";
        public override string Description => "构建并验收 Addressables 与 Unity Resources Prefab。";
        public override string Category => "示例";
        public override int Order => 10;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/Framework_WWJ/Docs/03_Architecture/FoundationModules/ResourceManagement/README.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建/刷新双后端示例", GUILayout.Height(34f)))
            {
                ResourceManagementSampleAssetBuilder.Build();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("打开默认验收场景", GUILayout.Height(34f)))
                {
                    OpenScene();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("人工验收", EditorStyles.boldLabel);
            DrawStep("1", "生成示例后进入 Play Mode，确认 Framework Ready。" );
            DrawStep("2", "分别点击两个后端的 Acquire，确认出现两个 Prefab 实例。" );
            DrawStep("3", "分别 Release，确认先销毁实例，再使对应缓存与 Lease 归零。" );
            DrawStep("4", "在 Resource Management 页面确认后端之间没有自动回退。" );
            DrawStep("5", "重新加载默认场景，确认 Global ResourceModule 保持同一运行时实例。" );
        }

        private static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ResourceManagementSampleAssetBuilder.DefaultScenePath, OpenSceneMode.Single);
        }

        private static void DrawStep(string index, string text)
        {
            EditorGUILayout.LabelField($"{index}. {text}", EditorStyles.wordWrappedLabel);
        }
    }
}
