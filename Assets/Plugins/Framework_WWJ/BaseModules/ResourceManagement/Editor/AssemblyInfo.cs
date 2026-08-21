using System.Runtime.CompilerServices;
using Framework_WWJ;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/resource-management/editor",
    "基础模块/资源管理/Editor",
    "提供资源模块的 Framework Center 页面、配置诊断和构建前校验。",
    100,
    0,
    20)]

[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Tests.EditMode")]
