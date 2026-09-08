using System.Collections.Generic;
using FrameWork_Ranger.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Samples.Editor
{
    /// <summary>
    /// Pooling 独立示例的创建、打开和人工验收入口。
    /// </summary>
    [FrameworkCenterPageExtension]
    public sealed class PoolingSamplePage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords =
        {
            "Pooling", "Sample", "Reference", "GameObject", "Addressables", "Resources",
        };

        public override string PageId => "framework.sample.pooling";
        public override string DisplayName => "Pooling 示例";
        public override string Description => "构建并验收双后端 GameObject 池和 Global 引用池。";
        public override string Category => "示例";
        public override int Order => 20;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/FrameWork_Ranger/Docs/03_Architecture/FoundationModules/Pooling/README.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建/刷新 Pooling 示例", GUILayout.Height(34f)))
            {
                PoolingSampleAssetBuilder.Build();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("打开独立验收场景", GUILayout.Height(34f)))
                {
                    OpenScene();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("人工验收", EditorStyles.boldLabel);
            DrawStep("1", "进入 Play Mode，确认两个 Prefab Lease 均在 SceneScope 就绪前取得。" );
            DrawStep("2", "分别 Spawn/Despawn Resources 与 Addressables Prefab，确认实例复用和回调顺序。" );
            DrawStep("3", "Rent/Return 引用载荷，确认 Return 后业务字段被重置。" );
            DrawStep("4", "切换离开场景，确认 GameObject 池销毁而 Global Reference 池保持。" );
            DrawStep("5", "在 Pooling 页面核对空闲、借出、总量与循环缩容倒计时。" );
        }

        private static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(PoolingSampleAssetBuilder.SampleScenePath, OpenSceneMode.Single);
        }

        private static void DrawStep(string index, string text)
        {
            EditorGUILayout.LabelField($"{index}. {text}", EditorStyles.wordWrappedLabel);
        }
    }
}
