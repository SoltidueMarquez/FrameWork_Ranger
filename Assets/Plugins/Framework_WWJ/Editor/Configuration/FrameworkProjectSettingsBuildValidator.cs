using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 在 Player 构建前同步场景路径并阻止无效中央配置进入构建产物。
    /// </summary>
    [FrameworkArchitecture(
        "构建前配置校验",
        "在 Player 构建前验证固定设置、场景映射和全部组合模块图。",
        FrameworkArchitectureLayer.EditorIntegration,
        50,
        typeof(FrameworkProjectSettingsAssetUtility))]
    internal sealed class FrameworkProjectSettingsBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = FrameworkProjectSettingsAssetUtility.Load();
            FrameworkProjectSettingsAssetUtility.SyncScenePaths(settings);
            FrameworkProjectSettingsAssetUtility.Validate(settings, out var errors, out var warnings);

            for (var i = 0; i < warnings.Count; i++)
            {
                Debug.LogWarning($"[Framework_WWJ] {warnings[i]}");
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"Framework_WWJ 中央配置校验失败：{string.Join(" | ", errors)}");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
