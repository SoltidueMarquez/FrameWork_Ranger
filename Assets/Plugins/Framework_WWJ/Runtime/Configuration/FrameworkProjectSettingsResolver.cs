using System;
using System.Collections.Generic;

namespace Framework_WWJ
{
    /// <summary>
    /// 中央项目设置的唯一 Runtime 校验与场景解析算法。
    /// 该类型不访问 UnityEditor，因此 Editor、Runtime 与测试能够共享完全相同的结论。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置解析器",
        "校验中央设置并按活动场景路径解析 SceneConfig。",
        FrameworkArchitectureLayer.Configuration,
        60,
        typeof(FrameworkProjectSettings),
        typeof(FrameworkProjectSettingsResult))]
    internal static class FrameworkProjectSettingsResolver
    {
        #region 解析入口

        internal static FrameworkProjectSettingsResult Resolve(
            FrameworkProjectSettings settings,
            string activeScenePath)
        {
            var diagnostics = new List<FrameworkProjectSettingsDiagnostic>();
            if (settings == null)
            {
                diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                    FrameworkProjectSettingsDiagnosticSeverity.Error,
                    FrameworkProjectSettingsDiagnosticCode.MissingSettings,
                    -1,
                    "找不到固定的 FrameworkProjectSettings 资产。"));
                return new FrameworkProjectSettingsResult(null, null, false, diagnostics);
            }

            if (settings.GlobalConfig == null)
            {
                diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                    FrameworkProjectSettingsDiagnosticSeverity.Error,
                    FrameworkProjectSettingsDiagnosticCode.MissingGlobalConfig,
                    -1,
                    "没有指定 FrameworkGlobalConfig。"));
            }

            var guidIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var pathIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var normalizedActivePath = NormalizeScenePath(activeScenePath);
            FrameworkSceneConfig resolvedSceneConfig = null;
            var usesOverride = false;

            var bindings = settings.SceneBindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null)
                {
                    diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                        FrameworkProjectSettingsDiagnosticSeverity.Error,
                        FrameworkProjectSettingsDiagnosticCode.MissingBinding,
                        i,
                        "场景覆盖条目为空。"));
                    continue;
                }

                ValidateIdentity(binding, i, guidIndices, pathIndices, diagnostics);
                if (binding.SceneConfig == null)
                {
                    diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                        FrameworkProjectSettingsDiagnosticSeverity.Error,
                        FrameworkProjectSettingsDiagnosticCode.MissingSceneConfig,
                        i,
                        "场景覆盖没有指定 FrameworkSceneConfig。"));
                }

                if (!usesOverride &&
                    !string.IsNullOrEmpty(normalizedActivePath) &&
                    string.Equals(NormalizeScenePath(binding.ScenePath), normalizedActivePath, StringComparison.Ordinal))
                {
                    resolvedSceneConfig = binding.SceneConfig;
                    usesOverride = true;
                }
            }

            if (!usesOverride)
            {
                resolvedSceneConfig = settings.DefaultSceneConfig;
            }

            return new FrameworkProjectSettingsResult(
                settings.GlobalConfig,
                resolvedSceneConfig,
                usesOverride,
                diagnostics);
        }

        internal static string NormalizeScenePath(string scenePath)
        {
            return string.IsNullOrWhiteSpace(scenePath)
                ? string.Empty
                : scenePath.Trim().Replace('\\', '/');
        }

        #endregion

        #region 内部校验

        private static void ValidateIdentity(
            FrameworkSceneBinding binding,
            int index,
            IDictionary<string, int> guidIndices,
            IDictionary<string, int> pathIndices,
            ICollection<FrameworkProjectSettingsDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(binding.SceneGuid))
            {
                diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                    FrameworkProjectSettingsDiagnosticSeverity.Error,
                    FrameworkProjectSettingsDiagnosticCode.MissingSceneGuid,
                    index,
                    "场景 GUID 为空，请在 Framework Center 中重新选择场景。"));
            }
            else if (guidIndices.TryGetValue(binding.SceneGuid, out var existingGuidIndex))
            {
                diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                    FrameworkProjectSettingsDiagnosticSeverity.Error,
                    FrameworkProjectSettingsDiagnosticCode.DuplicateSceneGuid,
                    index,
                    $"场景 GUID 已由 SceneBindings[{existingGuidIndex}] 使用。"));
            }
            else
            {
                guidIndices.Add(binding.SceneGuid, index);
            }

            var normalizedPath = NormalizeScenePath(binding.ScenePath);
            if (string.IsNullOrEmpty(normalizedPath))
            {
                diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                    FrameworkProjectSettingsDiagnosticSeverity.Error,
                    FrameworkProjectSettingsDiagnosticCode.MissingScenePath,
                    index,
                    "场景缓存路径为空，请在 Framework Center 中重新选择场景。"));
            }
            else if (pathIndices.TryGetValue(normalizedPath, out var existingPathIndex))
            {
                diagnostics.Add(new FrameworkProjectSettingsDiagnostic(
                    FrameworkProjectSettingsDiagnosticSeverity.Error,
                    FrameworkProjectSettingsDiagnosticCode.DuplicateScenePath,
                    index,
                    $"场景路径已由 SceneBindings[{existingPathIndex}] 使用。"));
            }
            else
            {
                pathIndices.Add(normalizedPath, index);
            }
        }

        #endregion
    }
}
