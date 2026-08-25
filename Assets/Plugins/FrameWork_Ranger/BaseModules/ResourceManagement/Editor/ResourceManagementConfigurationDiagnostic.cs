using UnityEngine;

namespace FrameWork_Ranger.ResourceManagement.Editor
{
    [FrameworkArchitecture(
        "资源配置诊断级别",
        "区分资源模块配置警告与阻断性错误。",
        FrameworkArchitectureLayer.EditorIntegration,
        520)]
    internal enum ResourceManagementDiagnosticSeverity
    {
        Warning,
        Error,
    }

    /// <summary>
    /// Resource 配置页与构建前检查共享的中文诊断。
    /// </summary>
    [FrameworkArchitecture(
        "资源配置诊断",
        "携带资源配置问题的级别、中文消息与可定位 Unity 对象。",
        FrameworkArchitectureLayer.EditorIntegration,
        521,
        typeof(ResourceManagementDiagnosticSeverity))]
    internal sealed class ResourceManagementConfigurationDiagnostic
    {
        internal ResourceManagementDiagnosticSeverity Severity { get; }

        internal string Message { get; }

        internal Object Context { get; }

        internal ResourceManagementConfigurationDiagnostic(
            ResourceManagementDiagnosticSeverity severity,
            string message,
            Object context = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }

        public override string ToString()
        {
            return $"[{Severity}] {Message}";
        }
    }
}
