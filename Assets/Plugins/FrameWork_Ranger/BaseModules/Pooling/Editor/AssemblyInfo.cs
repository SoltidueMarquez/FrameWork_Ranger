using System.Runtime.CompilerServices;
using FrameWork_Ranger;

[assembly: FrameworkArchitectureAssembly(
    "base-modules/pooling/editor",
    "基础模块/对象池/Editor",
    "提供双 Module 配置诊断、构建前校验与 Framework Center 运行快照。",
    100,
    10,
    30)]

[assembly: InternalsVisibleTo("FrameWork_Ranger.BaseModules.Pooling.Tests.EditMode")]
