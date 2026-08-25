namespace FrameWork_Ranger
{
    /// <summary>
    /// 项目设置诊断的严重程度。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置诊断级别",
        "区分中央项目设置诊断的信息、警告与错误级别。",
        FrameworkArchitectureLayer.Configuration,
        46)]
    internal enum FrameworkProjectSettingsDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// 项目设置诊断的稳定代码，供 Editor 和测试判断具体问题。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置诊断代码",
        "定义中央项目设置各类结构错误的稳定机器可读代码。",
        FrameworkArchitectureLayer.Configuration,
        47,
        typeof(FrameworkProjectSettingsDiagnostic))]
    internal enum FrameworkProjectSettingsDiagnosticCode
    {
        MissingSettings,
        MissingGlobalConfig,
        MissingBinding,
        MissingSceneGuid,
        MissingScenePath,
        MissingSceneConfig,
        DuplicateSceneGuid,
        DuplicateScenePath,
    }

    /// <summary>
    /// 描述中央项目设置中的一项结构化问题。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置诊断",
        "描述中央设置中的稳定错误代码、位置和中文消息。",
        FrameworkArchitectureLayer.Configuration,
        50)]
    internal sealed class FrameworkProjectSettingsDiagnostic
    {
        internal FrameworkProjectSettingsDiagnosticSeverity Severity { get; }

        internal FrameworkProjectSettingsDiagnosticCode Code { get; }

        internal int BindingIndex { get; }

        internal string Message { get; }

        internal FrameworkProjectSettingsDiagnostic(
            FrameworkProjectSettingsDiagnosticSeverity severity,
            FrameworkProjectSettingsDiagnosticCode code,
            int bindingIndex,
            string message)
        {
            Severity = severity;
            Code = code;
            BindingIndex = bindingIndex;
            Message = message;
        }

        public override string ToString()
        {
            var location = BindingIndex < 0 ? "ProjectSettings" : $"SceneBindings[{BindingIndex}]";
            return $"[{Severity}] {location}: {Message}";
        }
    }
}
