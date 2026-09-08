using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/pooling/game-object/runtime",
    "基础模块/对象池/GameObject 池/Runtime",
    "提供 SceneScope ResourceKey Prefab 池、Lease 生命周期、分帧预热和严格归还。",
    100,
    10,
    20,
    0)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Editor")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Tests.EditMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Tests.PlayMode")]
[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Samples.Editor")]
