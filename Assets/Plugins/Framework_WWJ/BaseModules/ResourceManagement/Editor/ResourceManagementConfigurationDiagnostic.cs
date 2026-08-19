using UnityEngine;

namespace Framework_WWJ.ResourceManagement.Editor
{
    internal enum ResourceManagementDiagnosticSeverity
    {
        Warning,
        Error,
    }

    /// <summary>
    /// Resource 配置页与构建前检查共享的中文诊断。
    /// </summary>
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
