using System.Runtime.CompilerServices;
using Framework_WWJ;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/resource-management/runtime",
    "基础模块/资源管理/Runtime",
    "提供资源键、租约、Provider 契约、缓存 Store、Handler 与 ResourceModule 公开门面。",
    100,
    0,
    0)]

[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Editor")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Tests.EditMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Tests.PlayMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Samples.Editor")]
