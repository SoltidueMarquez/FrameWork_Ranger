using System.Collections.Generic;
using FrameWork_Ranger.Editor;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Editor
{
    /// <summary>
    /// Framework Center 中 Reference/GameObject 双 Module 的配置与运行诊断页。
    /// </summary>
    [FrameworkArchitecture(
        "对象池模块中心页",
        "汇总双 Module 作用域、Resource 依赖、配置错误、运行数量和未归还计数。",
        FrameworkArchitectureLayer.EditorIntegration,
        530,
        typeof(PoolingConfigurationValidator),
        typeof(ReferencePoolModule),
        typeof(GameObjectPoolModule))]
    [FrameworkCenterPageExtension]
    public sealed class PoolingCenterPage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords =
        {
            "Pooling", "Pool", "Reference", "GameObject", "对象池", "引用池", "缩容",
        };

        public override string PageId => "framework.module.pooling";
        public override string DisplayName => "Pooling";
        public override string Description => "检查 Global 引用池与 Scene GameObject 池的配置和运行所有权。";
        public override string Category => "模块";
        public override int Order => 10;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/FrameWork_Ranger/Docs/03_Architecture/FoundationModules/Pooling/README.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            DrawConfiguration(context);
            EditorGUILayout.Space(10f);
            DrawRuntime();
        }

        private static void DrawConfiguration(FrameworkCenterPageContext context)
        {
            EditorGUILayout.LabelField("配置与作用域", EditorStyles.boldLabel);
            var settings = FrameworkProjectSettingsAssetUtility.Load();
            var reference = PoolingConfigurationValidator.FindGlobalReferenceModule(settings);
            var gameObjects = PoolingConfigurationValidator.FindSceneGameObjectModules(settings);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位 Reference 模板", GUILayout.Width(150f)) && reference != null)
            {
                context.SelectObject(reference);
            }

            if (GUILayout.Button("定位 GameObject 模板", GUILayout.Width(160f)) && gameObjects.Count > 0)
            {
                context.SelectObject(gameObjects[0]);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "ReferencePoolModule：GlobalScope。GameObjectPoolModule：SceneScope，并精确依赖 Global ResourceModule。",
                MessageType.Info);

            var diagnostics = PoolingConfigurationValidator.Validate(settings);
            if (diagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox("Pooling 双 Module 配置有效。", MessageType.Info);
                return;
            }

            for (var i = 0; i < diagnostics.Count; i++)
            {
                var diagnostic = diagnostics[i];
                EditorGUILayout.HelpBox(
                    diagnostic.Message,
                    diagnostic.Severity == PoolingDiagnosticSeverity.Error
                        ? MessageType.Error
                        : MessageType.Warning);
            }
        }

        private static void DrawRuntime()
        {
            EditorGUILayout.LabelField("Play Mode 运行状态", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 且 Framework Ready 后显示运行数据。", MessageType.None);
                return;
            }

            if (Framework.TryGetModule<ReferencePoolModule>(out var reference))
            {
                EditorGUILayout.LabelField("Global Reference Pool", EditorStyles.boldLabel);
                var snapshots = reference.CreateDiagnosticsSnapshots();
                for (var i = 0; i < snapshots.Count; i++)
                {
                    var snapshot = snapshots[i];
                    EditorGUILayout.LabelField(
                        $"{snapshot.ItemType.FullName} | 空闲 {snapshot.InactiveCount} / " +
                        $"借出 {snapshot.BorrowedCount} / 总量 {snapshot.TotalCount} / " +
                        $"缩容 {snapshot.IdleSecondsRemaining:0.0}s",
                        EditorStyles.wordWrappedLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Global ReferencePoolModule 尚未就绪。", MessageType.Warning);
            }

            EditorGUILayout.Space(6f);
            if (Framework.TryGetModule<GameObjectPoolModule>(out var gameObjectPool))
            {
                EditorGUILayout.LabelField("Scene GameObject Pool", EditorStyles.boldLabel);
                var snapshots = gameObjectPool.CreateDiagnosticsSnapshots();
                for (var i = 0; i < snapshots.Count; i++)
                {
                    var snapshot = snapshots[i];
                    EditorGUILayout.LabelField(
                        $"{snapshot.PoolName} [{snapshot.ResourceKey}] | " +
                        $"空闲 {snapshot.InactiveCount} / 借出 {snapshot.BorrowedCount} / " +
                        $"总量 {snapshot.TotalCount} / 缩容 {snapshot.IdleSecondsRemaining:0.0}s",
                        EditorStyles.wordWrappedLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "当前 SceneScope 未安装或尚未加载 GameObjectPoolModule。",
                    MessageType.None);
            }
        }
    }
}
