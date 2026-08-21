# Framework_WWJ Phase 1.1 验收与复盘

> 状态：已实现并通过自动化验收  
> 验收日期：2026-08-07  
> Unity：2022.3.62f3  
> 范围：中央项目配置、自动场景启动、Framework Center、声明式代码架构图、示例迁移与测试。

## 1. 交付结论

Phase 1.1 已完成。框架不再要求任何场景放置 `FrameworkEntry` 或对应预制体；固定的 `FrameworkProjectSettings` 资产负责 GlobalConfig、默认 SceneConfig 和场景覆盖配置，`FrameworkBootstrap` 在首个场景加载后自动创建 Host 并驱动一个 GlobalScope 与当前活动场景的 SceneScope。

同时已交付统一菜单 `Framework_WWJ/Framework Center`，包含概览、项目配置、代码架构、帮助和可自动发现的 Core Skeleton 示例页面。代码架构页使用 Attribute 声明名称、中文职责、层级和关键协作关系，并支持节点选择、源码定位与使用 Unity 当前外部编辑器打开源码。

本次未修改 Module/Handler 生命周期、精确类型依赖、稳定拓扑排序、失败回滚和反向卸载契约，也未实现任何正式业务模块。

## 2. 实际代码快照

按 2026-08-07 工作区中的 `.cs` 文件与物理行统计：

| 区域 | C# 文件 | 代码行 | 主要职责 |
| --- | ---: | ---: | --- |
| `Runtime` | 41 | 3,479 | 模块骨架、中央设置、Bootstrap、场景协调、Scope 与 Runtime |
| `Editor` | 25 | 2,795 | Framework Center、设置工具、依赖图、代码架构图与源码定位 |
| `Tests/EditMode` | 7 | 715 | 图算法、克隆、中央设置、架构目录与中心基础设施 |
| `Tests/PlayMode` | 7 | 707 | 自动启动、场景切换、失败、Tick、Shutdown 与示例集成 |
| `Samples/CoreSkeleton/Runtime` | 6 | 275 | 全局时钟、场景 Module、Handler 与运行时验收界面 |
| `Samples/CoreSkeleton/Editor` | 2 | 262 | 示例资产构建器与中心扩展页 |

Runtime 与 Editor 范围内共有 60 个架构 Attribute 声明；活动 Runtime、Editor、Tests 与 Samples 源码中没有 `FrameworkEntry` 引用。历史计划和 ADR 中仍保留该名称，用于说明设计演进。

## 3. 自动化验证证据

为避免关闭或污染用户当前打开的 Unity，最终验证在只复制 `Assets`、`Packages` 和 `ProjectSettings` 的临时项目中，使用 Unity 官方批处理 Test Runner 执行。临时项目不向正式项目回写 `Library`、测试结果或导入缓存。

| 测试集 | 结果 | 用例 | 失败 / 跳过 | NUnit 时长 |
| --- | --- | ---: | ---: | ---: |
| EditMode | Passed | 17/17 | 0 / 0 | 0.196 s |
| PlayMode | Passed | 13/13 | 0 / 0 | 0.252 s |

最终编译与两组测试日志均没有 C# 编译诊断。验证覆盖：

- 场景覆盖、默认 SceneConfig 和合法空 SceneScope 的解析优先级；
- 缺失 GlobalConfig、重复映射、空覆盖配置和无效场景身份诊断；
- 不放置 `FrameworkEntry` 时的自动启动；
- GlobalScope 只初始化一次，Scene A/B 切换时 Global 克隆保持不变；
- Scene A 的 Counter Handler 与 Scene B 的 Pulse Handler 正确切换；
- Scene Handle 所有权、迟到 Unload、加载取消和 Global Tick 连续性；
- 无效项目设置在任何模块克隆前进入 `Failed`；
- Shutdown 清理 Host、Coordinator、CTS、Scope 和运行时克隆；
- 架构 Attribute 发现、层级排序、三类关系和 Type 到 `MonoScript` 定位；
- 中心页面发现、排序、重复 PageId 诊断以及状态 JSON 回退。

## 4. 示例人工验收

1. 在 Unity 菜单打开 `Framework_WWJ → Framework Center`。
2. 进入“项目配置”，确认固定设置资产存在，GlobalConfig 以及 Scene A/B 两条映射均无错误。
3. 进入“代码架构”，选择 `FrameworkRuntime`：右侧应显示中文名称、职责和关系；`Ping` 应在 Project 窗口定位脚本，`打开源码` 应使用当前外部编辑器（项目配置为 Rider 时即打开 Rider）。
4. 搜索任意页面并打开多个标签，关闭后重新打开 Framework Center，确认标签与最近访问恢复。
5. 在“Core Skeleton 示例”页打开 Scene A，进入 Play Mode：框架最终为 `Ready`，Handler 为 `SampleCounterHandler`；记录 Global 与 Scene Module 的克隆 InstanceID。
6. 点击运行界面的“切换到 Scene B”：等待再次进入 `Ready`，Handler 应为 `SamplePulseHandler`；Global InstanceID 不变，Scene InstanceID 变化。
7. 切回 Scene A，再次确认 Global 实例没有重建。
8. 点击 `Shutdown`：状态应为 `Shutdown`，Hierarchy 中 `[Framework_WWJ]` 消失，模块 Tick 停止。
9. 在 Unity Test Runner 分别运行 Framework_WWJ EditMode 和 PlayMode：当前基线应为 EditMode 17/17、PlayMode 13/13，Console 不应出现非预期异常。

如果示例资产被删除或映射失效，可在示例页点击“重建示例资产”；该操作会通过 Unity/Odin 正常序列化流程重建配置、场景和中央映射。

## 5. 与计划的偏差

- 场景绑定的 `SceneAsset` 选择逻辑实现为中心页面和设置 Inspector 共用的 IMGUI 辅助绘制，而不是额外注册全局 Odin ValueDrawer，避免影响项目内同类型字段。
- 示例构建入口集中到 Framework Center 扩展页，不再保留独立的 Tools 菜单；这与“所有操作进入统一入口”的目标一致。
- 架构图采用计划内的 IMGUI + Handles 固定分层布局，没有引入 GraphView、缩放或自由拖动。
- 自动测试验证了目录、关系和源码索引；窗口的视觉布局、快捷键手感以及 Rider 打开行为仍需按上一节在用户当前编辑器中人工确认。

## 6. 已知边界

- Additive 场景仍只维护活动场景对应的一个 SceneScope；未激活的已加载场景不参与框架装配。
- 场景匹配在 Runtime 使用缓存路径，因此移动或改名后必须由 Postprocessor、Inspector 或中心页刷新；构建前验证会阻止失效映射进入构建。
- Framework Center 状态只写入 `Library/Framework_WWJ/FrameworkCenterState.json`，不会纳入版本控制，也不承担多人共享布局。
- 代码架构图只描述直接继承、直接接口实现和显式关键协作关系，不试图成为完整调用图。
- 显式调用 `Framework.ShutdownAsync()` 后，本次 Play Session 不会自动重启；重新进入 Play Mode 才会重新 Bootstrap。
- 正式业务模块仍为空，下一阶段需由具体游戏目标决定最小模块集合和验收标准。

## 7. 相关资料

- [Phase 1.1 实施计划](./00_Phase1_1_Implementation_Plan.md)
- [统一编辑器中心设计](./01_Editor_Center_Architecture.md)
- [声明式代码架构图设计](./02_Declarative_Code_Graph.md)
- [中央项目设置与场景所有权 ADR](../Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md)
- [Editor Center 架构决策](./ADR/README.md)
- [第一阶段骨架验收（历史基线）](../Core/04_Phase1_Core_Skeleton_Acceptance_And_Review.md)
