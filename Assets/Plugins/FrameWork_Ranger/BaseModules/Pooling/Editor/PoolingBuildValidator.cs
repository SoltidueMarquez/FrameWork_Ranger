using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Editor
{
    /// <summary>
    /// 在 Player 构建前阻止错误作用域、无效容量和无效 Prefab Key 进入构建。
    /// </summary>
    [FrameworkArchitecture(
        "对象池构建校验器",
        "在 Player 构建前复用双 Module 配置诊断并保留无限缓存风险警告。",
        FrameworkArchitectureLayer.EditorIntegration,
        540,
        typeof(PoolingConfigurationValidator))]
    internal sealed class PoolingBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 30;

        public void OnPreprocessBuild(BuildReport report)
        {
            var diagnostics = PoolingConfigurationValidator.ValidateCurrentProject();
            var errors = new List<string>();
            for (var i = 0; i < diagnostics.Count; i++)
            {
                var diagnostic = diagnostics[i];
                if (diagnostic.Severity == PoolingDiagnosticSeverity.Error)
                {
                    errors.Add(diagnostic.Message);
                }
                else
                {
                    Debug.LogWarning($"[FrameWork_Ranger] {diagnostic.Message}", diagnostic.Context);
                }
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"FrameWork_Ranger Pooling 配置校验失败：{string.Join(" | ", errors)}");
            }
        }
    }
}
