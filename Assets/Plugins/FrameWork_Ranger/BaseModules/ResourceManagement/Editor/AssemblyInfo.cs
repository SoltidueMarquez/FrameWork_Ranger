using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/resource-management/editor",
    "基础模块/资源管理/Editor",
    "提供资源模块的 Framework Center 页面、配置诊断和构建前校验。",
    100,
    0,
    20)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Tests.EditMode")]
