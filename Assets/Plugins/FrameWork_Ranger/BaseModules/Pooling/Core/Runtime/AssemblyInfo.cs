using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/pooling/core/runtime",
    "基础模块/对象池/Core/Runtime",
    "提供容量配置、引用相等比较与严格所有权跟踪池算法。",
    100,
    10,
    0,
    0)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Reference.Runtime")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.GameObject.Runtime")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Tests.EditMode")]
