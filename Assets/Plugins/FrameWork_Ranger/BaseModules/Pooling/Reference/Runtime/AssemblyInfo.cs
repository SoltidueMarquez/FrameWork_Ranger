using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/pooling/reference/runtime",
    "基础模块/对象池/引用池/Runtime",
    "提供 GlobalScope 精确类型引用池、严格归还、循环缩容与诊断门面。",
    100,
    10,
    10,
    0)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Editor")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Tests.PlayMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Samples.Editor")]
