using UnityEngine;

namespace FrameWork_Ranger.Pooling.Editor
{
    [FrameworkArchitecture(
        "对象池配置诊断级别",
        "区分对象池配置警告与阻断性错误。",
        FrameworkArchitectureLayer.EditorIntegration,
        550)]
    internal enum PoolingDiagnosticSeverity
    {
        Warning,
        Error,
    }

    /// <summary>
    /// Pooling Center 与构建前检查共享的中文诊断。
    /// </summary>
    [FrameworkArchitecture(
        "对象池配置诊断",
        "携带双 Module 配置问题的级别、中文消息与可定位 Unity 对象。",
        FrameworkArchitectureLayer.EditorIntegration,
        551,
        typeof(PoolingDiagnosticSeverity))]
    internal sealed class PoolingConfigurationDiagnostic
    {
        internal PoolingDiagnosticSeverity Severity { get; }

        internal string Message { get; }

        internal Object Context { get; }

        internal PoolingConfigurationDiagnostic(
            PoolingDiagnosticSeverity severity,
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
