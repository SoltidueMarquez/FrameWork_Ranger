using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "core/runtime",
    "框架核心/Runtime",
    "提供框架公开契约、配置资产、模块模型、依赖图、作用域与运行时驱动。",
    0,
    0)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.Editor")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.Tests.PlayMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Editor")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Tests.PlayMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Samples.Editor")]
