using System;
using System.Collections.Generic;

namespace Framework_WWJ
{
    /// <summary>
    /// 表示模块配置图无法安全装配。异常中的诊断文本已经包含作用域、条目位置和错误原因。
    /// </summary>
    [FrameworkArchitecture(
        "框架配置异常",
        "向等待者传递克隆前发现的结构化配置错误。",
        FrameworkArchitectureLayer.Contracts,
        40)]
    public sealed class FrameworkConfigurationException : Exception
    {
        /// <summary>
        /// 获取本次配置校验产生的只读诊断文本。
        /// </summary>
        public IReadOnlyList<string> Diagnostics { get; }

        internal FrameworkConfigurationException(IReadOnlyList<string> diagnostics)
            : base(BuildMessage(diagnostics))
        {
            Diagnostics = diagnostics;
        }

        private static string BuildMessage(IReadOnlyList<string> diagnostics)
        {
            return diagnostics == null || diagnostics.Count == 0
                ? "Framework 模块配置无效。"
                : $"Framework 模块配置无效：{string.Join(" | ", diagnostics)}";
        }
    }
}
