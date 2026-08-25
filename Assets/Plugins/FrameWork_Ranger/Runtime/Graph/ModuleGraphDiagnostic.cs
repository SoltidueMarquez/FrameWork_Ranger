using System;

namespace FrameWork_Ranger
{
    [FrameworkArchitecture(
        "模块图诊断级别",
        "区分模块依赖图诊断的信息、警告与错误级别。",
        FrameworkArchitectureLayer.GraphAndScope,
        6)]
    internal enum ModuleGraphDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    [FrameworkArchitecture(
        "模块图诊断代码",
        "定义空模板、缺失依赖、重复类型、方向错误和依赖环等稳定错误代码。",
        FrameworkArchitectureLayer.GraphAndScope,
        7,
        typeof(ModuleGraphDiagnostic))]
    internal enum ModuleGraphDiagnosticCode
    {
        MissingConfig,
        MissingTemplate,
        MissingHandler,
        DuplicateType,
        InvalidDependencyType,
        MissingDependency,
        InvalidScopeDirection,
        DependencyCycle,
    }

    /// <summary>
    /// Resolver 生成的结构化诊断。Runtime 和 Inspector 使用同一结果，避免编辑器提示与实际装配规则漂移。
    /// </summary>
    [FrameworkArchitecture(
        "模块图诊断",
        "描述模块图错误代码、作用域、配置位置和中文原因。",
        FrameworkArchitectureLayer.GraphAndScope,
        10)]
    internal sealed class ModuleGraphDiagnostic
    {
        internal ModuleGraphDiagnosticSeverity Severity { get; }

        internal ModuleGraphDiagnosticCode Code { get; }

        internal ModuleScopeKind ScopeKind { get; }

        internal int ConfigIndex { get; }

        internal Type ModuleType { get; }

        internal string Message { get; }

        internal ModuleGraphDiagnostic(
            ModuleGraphDiagnosticSeverity severity,
            ModuleGraphDiagnosticCode code,
            ModuleScopeKind scopeKind,
            int configIndex,
            Type moduleType,
            string message)
        {
            Severity = severity;
            Code = code;
            ScopeKind = scopeKind;
            ConfigIndex = configIndex;
            ModuleType = moduleType;
            Message = message;
        }

        public override string ToString()
        {
            var typeName = ModuleType == null ? "<空模板>" : ModuleType.FullName;
            return $"[{Severity}] {ScopeKind}[{ConfigIndex}] {typeName}: {Message}";
        }
    }
}
