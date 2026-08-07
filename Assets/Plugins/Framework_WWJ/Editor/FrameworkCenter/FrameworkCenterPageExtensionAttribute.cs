using System;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 标记一个可以由 Framework Center 自动发现的正式扩展页面。
    /// 仅继承页面基类不会进入生产目录，从而避免测试替身和临时类型污染编辑器入口。
    /// </summary>
    [FrameworkArchitecture(
        "Center 页面扩展标记",
        "区分正式可发现页面与测试替身、临时派生类型。",
        FrameworkArchitectureLayer.EditorIntegration,
        95,
        typeof(FrameworkCenterPage))]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class FrameworkCenterPageExtensionAttribute : Attribute
    {
    }
}
