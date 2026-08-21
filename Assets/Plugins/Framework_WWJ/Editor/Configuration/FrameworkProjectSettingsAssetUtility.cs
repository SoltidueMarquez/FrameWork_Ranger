using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 集中处理固定项目设置资产的创建、定位、场景路径同步与编辑器校验。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置资产工具",
        "维护固定 Resources 设置资产及场景 GUID/Path 一致性。",
        FrameworkArchitectureLayer.EditorIntegration,
        10,
        typeof(FrameworkProjectSettings))]
    internal static class FrameworkProjectSettingsAssetUtility
    {
        internal const string FixedAssetPath =
            "Assets/Plugins/Framework_WWJ/Resources/FrameworkProjectSettings.asset";

        #region 资产操作

        internal static FrameworkProjectSettings Load()
        {
            return AssetDatabase.LoadAssetAtPath<FrameworkProjectSettings>(FixedAssetPath);
        }

        internal static FrameworkProjectSettings CreateOrLoad()
        {
            var settings = Load();
            if (settings != null)
            {
                return settings;
            }

            const string resourcesFolder = "Assets/Plugins/Framework_WWJ/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Plugins/Framework_WWJ", "Resources");
            }

            settings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
            settings.name = "FrameworkProjectSettings";
            AssetDatabase.CreateAsset(settings, FixedAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        internal static void Ping(FrameworkProjectSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        #endregion

        #region 场景身份同步

        internal static bool SyncScenePaths(FrameworkProjectSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var changed = false;
            var bindings = settings.SceneBindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.SceneGuid))
                {
                    continue;
                }

                var currentPath = AssetDatabase.GUIDToAssetPath(binding.SceneGuid);
                if (string.Equals(
                        FrameworkProjectSettingsResolver.NormalizeScenePath(binding.ScenePath),
                        FrameworkProjectSettingsResolver.NormalizeScenePath(currentPath),
                        StringComparison.Ordinal))
                {
                    continue;
                }

                binding.SetScene(binding.SceneGuid, currentPath, binding.SceneConfig);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(settings);
            }

            return changed;
        }

        internal static SceneAsset LoadSceneAsset(FrameworkSceneBinding binding)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.SceneGuid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(binding.SceneGuid);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        #endregion

        #region 编辑器校验

        internal static void Validate(
            FrameworkProjectSettings settings,
            out List<string> errors,
            out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            var result = FrameworkProjectSettingsResolver.Resolve(settings, string.Empty);
            for (var i = 0; i < result.Diagnostics.Count; i++)
            {
                var diagnostic = result.Diagnostics[i];
                if (diagnostic.Severity == FrameworkProjectSettingsDiagnosticSeverity.Error)
                {
                    errors.Add(diagnostic.ToString());
                }
                else if (diagnostic.Severity == FrameworkProjectSettingsDiagnosticSeverity.Warning)
                {
                    warnings.Add(diagnostic.ToString());
                }
            }

            if (settings == null)
            {
                return;
            }

            ValidateUniqueAsset(settings, errors);
            ValidateSceneAssetsAndBuildSettings(settings, errors, warnings);
            ValidateModuleGraphs(settings, errors, warnings);
        }

        private static void ValidateUniqueAsset(
            FrameworkProjectSettings settings,
            ICollection<string> errors)
        {
            var actualPath = AssetDatabase.GetAssetPath(settings);
            if (!string.Equals(actualPath, FixedAssetPath, StringComparison.Ordinal))
            {
                errors.Add($"[Error] ProjectSettings 必须位于固定路径：{FixedAssetPath}");
            }

            var guids = AssetDatabase.FindAssets($"t:{nameof(FrameworkProjectSettings)}");
            var resourceAssets = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Contains("/Resources/"))
                .ToArray();
            if (resourceAssets.Length > 1)
            {
                errors.Add($"[Error] Resources 中存在 {resourceAssets.Length} 个 FrameworkProjectSettings，运行时加载结果不唯一。" );
            }
        }

        private static void ValidateSceneAssetsAndBuildSettings(
            FrameworkProjectSettings settings,
            ICollection<string> errors,
            ICollection<string> warnings)
        {
            var buildPaths = new HashSet<string>(
                EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path),
                StringComparer.Ordinal);

            var bindings = settings.SceneBindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.SceneGuid))
                {
                    continue;
                }

                var assetPath = AssetDatabase.GUIDToAssetPath(binding.SceneGuid);
                if (string.IsNullOrEmpty(assetPath) ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath) == null)
                {
                    errors.Add($"[Error] SceneBindings[{i}] 的场景 GUID 已失效：{binding.SceneGuid}");
                    continue;
                }

                if (!buildPaths.Contains(assetPath))
                {
                    warnings.Add($"[Warning] SceneBindings[{i}] 的场景未加入已启用的 Build Settings：{assetPath}");
                }
            }
        }

        private static void ValidateModuleGraphs(
            FrameworkProjectSettings settings,
            ICollection<string> errors,
            ICollection<string> warnings)
        {
            ValidateGraph(
                ModuleGraphResolver.Resolve(settings.GlobalConfig, settings.DefaultSceneConfig),
                "DefaultSceneConfig",
                errors,
                warnings);

            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                var binding = settings.SceneBindings[i];
                if (binding?.SceneConfig == null)
                {
                    continue;
                }

                ValidateGraph(
                    ModuleGraphResolver.Resolve(settings.GlobalConfig, binding.SceneConfig),
                    $"SceneBindings[{i}]",
                    errors,
                    warnings);
            }

            if (settings.GlobalConfig != null && settings.GlobalConfig.DriverHandler == null)
            {
                errors.Add("[Error] FrameworkGlobalConfig 没有配置 FrameworkDriverHandler。");
            }
        }

        private static void ValidateGraph(
            ModuleGraphResult graph,
            string location,
            ICollection<string> errors,
            ICollection<string> warnings)
        {
            for (var i = 0; i < graph.Diagnostics.Count; i++)
            {
                var diagnostic = graph.Diagnostics[i];
                if (diagnostic.Severity == ModuleGraphDiagnosticSeverity.Error)
                {
                    errors.Add($"[Error] {location}: {diagnostic}");
                }
                else if (diagnostic.Severity == ModuleGraphDiagnosticSeverity.Warning)
                {
                    warnings.Add($"[Warning] {location}: {diagnostic}");
                }
            }
        }

        #endregion
    }
}
