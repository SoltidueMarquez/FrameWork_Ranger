# Framework_WWJ 当前项目状态

> 盘点时间：2026-08-18<br>
> Unity 项目：`D:\unityhub\UnityProjects\Framework_Test`  
> 框架目录：`Assets/Plugins/Framework_WWJ`

## 1. 当前结论

Framework_WWJ 已完成第一阶段“模块模型与驱动骨架”、Phase 1.1“中央启动、统一编辑器中心与架构类图”、Phase 1.2“Editor Center 视觉与节点图交互优化”和 Phase 1.3“预览页签与固定快捷页签”。当前代码可以通过固定项目设置自动装配 GlobalScope 与活动场景的 SceneScope，并提供配置校验、确定性生命周期、模块查询、Tick 驱动、失败回滚、统一编辑器中心、可导航节点图和 A/B 示例场景。

当前仍是**骨架阶段**：资源管理、对象池、音频、输入、UI、存档等正式功能模块均未实现，下一阶段也尚未选定。

## 2. 环境与依赖

| 项目 | 当前值 |
| --- | --- |
| Unity | 2022.3.62f3 |
| Runtime 根命名空间 | `Framework_WWJ` |
| 模块与配置序列化 | Sirenix Odin `SerializedScriptableObject` / `[OdinSerialize]` |
| 异步生命周期 | UniTask 2.5.10，Git UPM 锁定 |
| UniTask 锁文件提交 | `7c0f199fe0d3fc528024488ccd671e6c7b27745b` |
| 中央设置资产 | `Assets/Plugins/Framework_WWJ/Resources/FrameworkProjectSettings.asset` |
| 场景入口 | 无场景组件；`FrameworkBootstrap` 自动启动 |
| SceneScope 所有者 | 当前活动场景的 Scene Handle |
| 旧代码 | `Main`、`Utils` 仍保持清理状态，未恢复 |
| 参考项目 | LyingBottle/HTY 只读，未修改 |

## 3. 代码与程序集快照

按 2026-08-18 工作区中的 `.cs` 文件与物理行统计：

| 区域 | C# 文件 | 代码行 | 主要职责 |
| --- | ---: | ---: | --- |
| `Runtime` | 41 | 3,479 | 抽象、中央配置、Module/Handler、Graph、Scope、Bootstrap 与 Runtime |
| `Editor` | 30 | 4,200 | Framework Center、预览/固定页签、显式页面发现、共享图视口、设置工具、依赖图、代码架构图与源码定位 |
| `Tests/EditMode` | 9 | 1,037 | 图解析、克隆、中央设置、架构目录、Center 页签状态/持久化与图视口数学 |
| `Tests/PlayMode` | 7 | 707 | 自动启动、场景、失败、Tick、Shutdown 与示例集成 |
| `Samples/CoreSkeleton/Runtime` | 6 | 275 | 全局时钟、场景 Module、两种 Handler 与 IMGUI View |
| `Samples/CoreSkeleton/Editor` | 2 | 263 | 示例资产构建器与显式标记的 Framework Center 扩展页 |

独立程序集：

- `Framework_WWJ.Runtime`
- `Framework_WWJ.Editor`
- `Framework_WWJ.Tests.EditMode`
- `Framework_WWJ.Tests.PlayMode`
- `Framework_WWJ.Samples.CoreSkeleton`
- `Framework_WWJ.Samples.CoreSkeleton.Editor`

Runtime 不引用 `UnityEditor`；Editor、Tests 和 Sample 单向依赖 Runtime。内部算法通过 `InternalsVisibleTo` 只开放给受控程序集。

## 4. 已实现的核心模型

- `ModuleBase` 是 Odin SO 模板；Scope 运行时克隆并在卸载后销毁。
- `DirectModuleBase` 与 `HandlerModuleBase<THandler>` 并存；Handler 是 Odin 多态内嵌托管对象。
- `FrameworkRuntime` 固定校验、装配、状态机、回滚和所有权算法。
- `FrameworkDriverHandlerBase` 只提供 Scope/Module 生命周期前后置钩子。
- `ModuleGraphResolver` 是 Runtime、Inspector 和 EditMode 测试共享的唯一模块图算法。
- `FrameworkProjectSettings` 统一保存 GlobalConfig、默认 SceneConfig 和场景覆盖；Runtime 不静默创建该资产。
- `FrameworkBootstrap` 与 `FrameworkSceneCoordinator` 自动跟踪活动场景，一个常驻 GlobalScope 配合一个当前 SceneScope；Scene Handle 防止迟到卸载破坏新 Scope。
- 依赖按具体 Module 类型声明，稳定拓扑排序，严格串行 Load，成功顺序逆序 Unload。
- `Framework` 提供静态只读访问；`ModuleContext` 限制模块内部的作用域可见性。
- Update、FixedUpdate、LateUpdate 通过显式能力接口 opt-in，单目标异常隔离。

详细决策见 [Core ADR](../03_Architecture/Core/ADR/README.md)。

## 5. 编辑器中心与代码架构图

- 菜单 `Framework_WWJ/Framework Center` 是项目配置、架构图、帮助和示例验收的统一入口。
- 页面通过 `FrameworkCenterPage`、`[FrameworkCenterPageExtension]` 和 `TypeCache` 显式发现，支持搜索、单预览页、可排序持久固定页和页面级错误隔离；测试替身不会进入生产目录。
- Runtime/Editor 顶层类及关键接口通过 `FrameworkArchitectureAttribute` 维护名称、中文职责、层级与关键协作关系；当前共有 65 个声明。
- Center 使用 42px 顶部栏、30px 标签栏、208px 扁平导航、动态页面标题卡片和深浅主题自适应样式。
- 代码架构图与模块依赖图共用 35%–200% 的可缩放、可平移视口，提供适配和 100% 重置；节点仍使用固定自动布局。
- 固定项目设置的 Inspector、Framework Center 和构建前校验共享同一套设置诊断和模块图结果；项目配置页可按任意 SceneAsset 预览真实 Global + Scene 组合。

## 6. 示例与资产接线

`Samples/CoreSkeleton/Configs` 包含一个 GlobalConfig、一个全局时钟模块模板，以及 Scene A/B 各自的 SceneConfig 与场景模块模板。A 使用 `SampleCounterHandler`，B 使用 `SamplePulseHandler`。

固定 `FrameworkProjectSettings.asset` 指向该 GlobalConfig，并使用场景 GUID/缓存路径将 A/B 映射到对应 SceneConfig；默认 SceneConfig 为空，因此未登记场景获得合法的零模块 SceneScope。示例场景中没有 `FrameworkEntry`。

`ProjectSettings/EditorBuildSettings.asset` 保留现有条目和顺序，并包含启用的 `CoreSkeleton_A.unity`、`CoreSkeleton_B.unity`。原 `Assets/Scenes/SampleScene.unity` 未被覆盖。

## 7. 验证状态

最终验证使用 Unity 2022.3.62f3 官方批处理 Test Runner，在临时项目副本中执行：

| 测试集 | 结果 | 用例 | NUnit 时长 |
| --- | --- | ---: | ---: |
| EditMode | Passed | 33/33 | 0.305 s |
| PlayMode | Passed | 13/13 | 0.295 s |

最终编译和测试日志无 C# 编译诊断。EditMode 已验证正式页面发现、v2 状态清理/往返、单预览、固定/取消固定、关闭回退、顺序调整、共享视口数学和既有图解析；PlayMode 已验证无 Entry 自动启动、A/B 真实场景切换、Global Module 克隆保持不变、Scene Handler 切换、空 SceneScope、失败前零克隆和完整 Shutdown。

完整证据、计划偏差和人工步骤见 [Phase 1.3 验收与复盘](../03_Architecture/EditorCenter/07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md)。

## 8. 当前未决范围

- 第一个可验证游戏目标及其核心循环。
- 下一阶段最少需要的正式业务模块。
- 是否由真实需求扩展 Additive Scene、多实例、接口绑定或并行初始化。
- 各业务模块的公开 API、数据所有权、失败策略与性能指标。

这些问题继续集中到[重建设计待办](./09_Rebuild_Decision_Backlog.md)，没有用户阶段输入前不自动扩张核心。
