using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/resource-management/runtime",
    "基础模块/资源管理/Runtime",
    "提供资源键、租约、Provider 契约、缓存 Store、Handler 与 ResourceModule 公开门面。",
    100,
    0,
    0)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Editor")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Tests.PlayMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Samples.Editor")]
