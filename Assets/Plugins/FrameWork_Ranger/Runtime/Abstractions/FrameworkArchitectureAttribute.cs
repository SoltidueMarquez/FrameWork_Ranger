using System;
using System.Diagnostics;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 声明 Framework Center 代码架构图所需的稳定名称、职责、层级与关键协作关系。
    /// </summary>
    [FrameworkArchitecture(
        "架构元数据 Attribute",
        "为 Runtime 与 Editor 类型声明架构图显示信息和关键协作关系。",
        FrameworkArchitectureLayer.Contracts,
        -100)]
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Struct |
        AttributeTargets.Enum,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class FrameworkArchitectureAttribute : Attribute
    {
        /// <summary>
        /// 获取节点显示名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 获取该类型在框架中的中文职责。
        /// </summary>
        public string Responsibility { get; }

        /// <summary>
        /// 获取节点所在的逻辑层。
        /// </summary>
        public FrameworkArchitectureLayer Layer { get; }

        /// <summary>
        /// 获取节点在同层中的稳定排序值。
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// 获取该类型显式声明的关键协作类型。
        /// </summary>
        public Type[] RelatedTypes { get; }

        /// <summary>
        /// 创建一项声明式架构元数据。
        /// </summary>
        public FrameworkArchitectureAttribute(
            string displayName,
            string responsibility,
            FrameworkArchitectureLayer layer,
            int order = 0,
            params Type[] relatedTypes)
        {
            DisplayName = displayName;
            Responsibility = responsibility;
            Layer = layer;
            Order = order;
            RelatedTypes = relatedTypes ?? Array.Empty<Type>();
        }
    }
}
