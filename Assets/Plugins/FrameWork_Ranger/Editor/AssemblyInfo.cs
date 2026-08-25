using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "core/editor",
    "框架核心/Editor",
    "提供 Framework Center、配置工具、依赖图、代码架构导航和编辑器验证。",
    0,
    10)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Editor")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.ResourceManagement.Samples.Editor")]
