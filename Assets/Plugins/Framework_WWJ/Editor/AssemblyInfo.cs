using System.Runtime.CompilerServices;
using Framework_WWJ;

[assembly: FrameworkArchitectureAssembly(
    "core/editor",
    "框架核心/Editor",
    "提供 Framework Center、配置工具、依赖图、代码架构导航和编辑器验证。",
    0,
    10)]

[assembly: InternalsVisibleTo("Framework_WWJ.Tests.EditMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Editor")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Tests.EditMode")]
[assembly: InternalsVisibleTo("Framework_WWJ.BaseModules.ResourceManagement.Samples.Editor")]
