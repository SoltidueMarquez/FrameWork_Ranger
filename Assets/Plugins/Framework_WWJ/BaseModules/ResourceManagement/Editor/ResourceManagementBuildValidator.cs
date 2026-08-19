using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Framework_WWJ.ResourceManagement.Editor
{
    /// <summary>
    /// 阻止缺失双后端或错误作用域的 Resource 配置进入 Player 构建。
    /// </summary>
    internal sealed class ResourceManagementBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 20;

        public void OnPreprocessBuild(BuildReport report)
        {
            var diagnostics = ResourceManagementConfigurationValidator.ValidateCurrentProject();
            var errors = new System.Collections.Generic.List<string>();
            for (var i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Severity == ResourceManagementDiagnosticSeverity.Error)
                {
                    errors.Add(diagnostics[i].Message);
                }
                else
                {
                    Debug.LogWarning($"[Framework_WWJ] {diagnostics[i].Message}", diagnostics[i].Context);
                }
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"Framework_WWJ Resource 配置校验失败：{string.Join(" | ", errors)}");
            }
        }
    }
}
