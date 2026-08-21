using System.Collections.Generic;
using Framework_WWJ.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Framework_WWJ.ResourceManagement.Editor
{
    /// <summary>
    /// Framework Center 中 Resource Management 的配置与运行时只读诊断页。
    /// </summary>
    [FrameworkArchitecture(
        "资源模块中心页",
        "在 Framework Center 汇总资源配置、双后端健康度与 Play Mode 缓存/Pending/Lease 快照。",
        FrameworkArchitectureLayer.EditorIntegration,
        500,
        typeof(ResourceManagementConfigurationValidator),
        typeof(ResourceModule),
        typeof(ResourceDiagnosticsSnapshot))]
    [FrameworkCenterPageExtension]
    public sealed class ResourceManagementCenterPage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords =
        {
            "Resource", "Addressables", "Resources", "Lease", "缓存", "资源",
        };

        public ResourceManagementCenterPage()
        {
        }

        public override string PageId => "framework.module.resource-management";
        public override string DisplayName => "Resource Management";
        public override string Description => "检查双后端配置、缓存、Pending 与 Lease 所有权。";
        public override string Category => "模块";
        public override int Order => 0;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/Framework_WWJ/Docs/03_Architecture/FoundationModules/ResourceManagement/README.md";

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            DrawConfiguration(context);
            EditorGUILayout.Space(10f);
            DrawRuntime();
        }

        private static void DrawConfiguration(FrameworkCenterPageContext context)
        {
            EditorGUILayout.LabelField("配置", EditorStyles.boldLabel);
            var settings = FrameworkProjectSettingsAssetUtility.Load();
            var module = ResourceManagementConfigurationValidator.FindGlobalModule(settings);
            var diagnostics = ResourceManagementConfigurationValidator.Validate(settings);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("定位 Module 模板", GUILayout.Width(130f)) && module != null)
            {
                context.SelectObject(module);
            }

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (GUILayout.Button("定位 Addressables 设置", GUILayout.Width(150f)) && addressableSettings != null)
            {
                context.SelectObject(addressableSettings);
            }

            EditorGUILayout.EndHorizontal();

            if (diagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox("Resource 双后端配置有效。", MessageType.Info);
            }
            else
            {
                for (var i = 0; i < diagnostics.Count; i++)
                {
                    var diagnostic = diagnostics[i];
                    EditorGUILayout.HelpBox(
                        diagnostic.Message,
                        diagnostic.Severity == ResourceManagementDiagnosticSeverity.Error
                            ? MessageType.Error
                            : MessageType.Warning);
                }
            }

            EditorGUILayout.HelpBox(
                "Unity Resources 的 Lease 归零只表示框架解除持有，不保证 Unity 立即释放物理内存。",
                MessageType.Info);
        }

        private static void DrawRuntime()
        {
            EditorGUILayout.LabelField("Play Mode 运行状态", EditorStyles.boldLabel);
            if (!EditorApplication.isPlaying || !Framework.TryGetModule<ResourceModule>(out var module))
            {
                EditorGUILayout.HelpBox("进入 Play Mode 且 Framework Ready 后显示运行数据。", MessageType.None);
                return;
            }

            var snapshot = module.CreateDiagnosticsSnapshot();
            if (snapshot == null)
            {
                EditorGUILayout.HelpBox("ResourceModule 尚未完成加载或正在关闭。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"接受请求：{snapshot.IsAcceptingRequests}");
            EditorGUILayout.LabelField(
                $"缓存 {snapshot.CacheCount} / Pending {snapshot.PendingCount} / Lease {snapshot.LeaseCount}");
            for (var i = 0; i < snapshot.Backends.Count; i++)
            {
                var backend = snapshot.Backends[i];
                EditorGUILayout.LabelField($"• {backend.Backend}: {backend.ProviderName}");
            }

            for (var i = 0; i < snapshot.Entries.Count; i++)
            {
                var entry = snapshot.Entries[i];
                EditorGUILayout.LabelField(
                    $"{entry.Key} <{entry.AssetType.Name}> | " +
                    (entry.IsPending ? $"等待者 {entry.WaiterCount}" : $"Lease {entry.RefCount}"),
                    EditorStyles.wordWrappedLabel);
            }
        }
    }
}
