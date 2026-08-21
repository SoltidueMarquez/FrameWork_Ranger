using System;
using System.Diagnostics;

namespace Framework_WWJ
{
    /// <summary>
    /// 将一个 Framework_WWJ 生产程序集显式接入代码架构目录，并声明其所在的分组路径。
    /// 未声明该 Attribute 的测试、示例或第三方程序集不会进入正式架构图。
    /// </summary>
    [FrameworkArchitecture(
        "架构程序集元数据",
        "声明生产程序集在分层代码架构目录中的稳定路径、中文名称、职责和逐级顺序。",
        FrameworkArchitectureLayer.Contracts,
        -90,
        typeof(FrameworkArchitectureAttribute))]
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class FrameworkArchitectureAssemblyAttribute : Attribute
    {
        #region 公开属性

        /// <summary>
        /// 获取由小写短横线片段组成的稳定分组路径，例如
        /// <c>base-modules/resource-management/runtime</c>。
        /// </summary>
        public string GroupPath { get; }

        /// <summary>
        /// 获取与稳定路径逐段对应的中文显示路径。
        /// </summary>
        public string DisplayPath { get; }

        /// <summary>
        /// 获取该程序集在框架中的职责说明。
        /// </summary>
        public string Responsibility { get; }

        /// <summary>
        /// 获取每一层分组的稳定排序值；未提供的层级使用零。
        /// </summary>
        public int[] OrderPath { get; }

        #endregion

        /// <summary>
        /// 创建一项生产程序集架构元数据。
        /// </summary>
        /// <param name="groupPath">稳定分组路径。</param>
        /// <param name="displayPath">中文显示路径。</param>
        /// <param name="responsibility">程序集职责。</param>
        /// <param name="orderPath">从根分组到叶分组的逐级排序值。</param>
        public FrameworkArchitectureAssemblyAttribute(
            string groupPath,
            string displayPath,
            string responsibility,
            params int[] orderPath)
        {
            GroupPath = groupPath;
            DisplayPath = displayPath;
            Responsibility = responsibility;
            OrderPath = orderPath ?? Array.Empty<int>();
        }
    }
}
