using System.Runtime.CompilerServices;
using Framework_WWJ;

[assembly: FrameworkArchitectureAssembly(
    "core/runtime",
    "框架核心/Runtime",
    "提供框架公开契约、配置资产、模块模型、依赖图、作用域与运行时驱动。",
    0,
    0)]

[assembly: InternalsVisibleTo("Framework_WWJ.Editor")]
[assembly: InternalsVisibleTo("Framework_WWJ.Tests.EditMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.Tests.PlayMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.Samples.CoreSkeleton.Editor")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Editor")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Tests.EditMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Tests.PlayMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Samples.Editor")]
