# FrameWork_Ranger 当前项目状态

> 盘点时间：2026-08-25<br>
> Unity 项目：`D:\unityhub\UnityProjects\FrameWork\FrameWork_Ranger`  
> 框架目录：`Assets/Plugins/FrameWork_Ranger`

## 1. 当前结论

FrameWork_Ranger 已迁入 Unity 6000.5.9f1 URP 工程与同名 GitHub 仓库工作树，同时保留原框架完整 Git 历史。2026-08-25 起现行产品身份、命名空间、插件目录与 Skills 统一为 `FrameWork_Ranger`；旧名 `Framework_WWJ` 只保留在历史文档中，见 [ADR-DIST-002](../03_Architecture/Distribution/ADR-DIST-002_Identity_Rename_To_FrameWork_Ranger.md)。框架已完成核心骨架、Editor Center Phase 1.1–1.8，以及首个正式基础模块 Resource Management。当前代码可以通过固定项目设置自动装配 GlobalScope 与活动场景的 SceneScope，并提供配置校验、确定性生命周期、模块查询、Tick 驱动、失败回滚、统一编辑器中心、紧凑可展开代码架构图、HTY 式主从配置工作台，以及 Resource Management 模块目录中的 Resources/Addressables 双后端验收场景。

Resource Management 已按批准契约实现并验收关闭；对象池/引用池与事件中心仍未开始，音频、输入、UI、存档等继续是未来候选。下一阶段只能先讨论 Pooling 的需求和公共契约，不能直接实现或并行推进 Event Center。

## 2. 环境与依赖

| 项目 | 当前值 |
| --- | --- |
| Unity | 6000.5.9f1，URP 17.5.0 |
| Runtime 根命名空间 | `FrameWork_Ranger` |
| 模块与配置序列化 | Sirenix Odin 4.0.2.3，`SerializedScriptableObject` / `[OdinSerialize]` |
| 异步生命周期 | UniTask 2.5.11，Git UPM 标签锁定 |
| 补间 | DOTween 1.2.825 |
| 资源后端 | Unity Resources + Addressables 2.9.1（本地内容） |
| 中央设置资产 | `Assets/Plugins/FrameWork_Ranger/Resources/FrameworkProjectSettings.asset` |
| 场景入口 | 无场景组件；`FrameworkBootstrap` 自动启动 |
| SceneScope 所有者 | 当前活动场景的 Scene Handle |
| 旧代码 | `Main`、`Utils` 仍保持清理状态，未恢复 |
| 参考项目 | LyingBottle/HTY 只读，未修改 |
| 第二参考项目 | YokiFrame 只读，已整理 Kit、所有权、Provider/Handle 与工具链资料 |

## 3. 代码与程序集快照

按 2026-08-22 清理后的工作区 `.cs` 文件与物理行统计：

| 区域 | C# 文件 | 代码行 | 主要职责 |
| --- | ---: | ---: | --- |
| `Runtime` | 42 | 3,760 | 抽象、中央配置、Module/Handler、Graph、Scope、Bootstrap 与 Runtime |
| `Editor` | 40 | 10,506 | Framework Center、主从配置工作台、紧凑模块列表、真实 Inspector 宿主、生产程序集目录、复合架构图与共享图视口 |
| `Tests/EditMode` | 12 | 1,981 | 图解析、中央设置、配置工作台状态/结构编辑/Inspector 生命周期、架构目录、复合布局与 Center 基础设施 |
| `Tests/PlayMode` | 6 | 670 | 自动启动、场景、失败、Tick 与 Shutdown；A/B 专用示例测试已删除 |
| `BaseModules/ResourceManagement` 生产代码 | 26 | 1,952 | Resource Runtime、双 Integration 与 Editor；不含 Samples/Tests |
| `BaseModules/ResourceManagement/Tests` | 7 | 1,018 | 资源契约、缓存/取消、配置、生命周期与真实双后端集成 |

独立程序集：

- `FrameWork_Ranger.Runtime`
- `FrameWork_Ranger.Editor`
- `FrameWork_Ranger.Tests.EditMode`
- `FrameWork_Ranger.Tests.PlayMode`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Runtime`
- `FrameWork_Ranger.BaseModules.ResourceManagement.UnityResources`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Addressables`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Editor`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Samples`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Samples.Editor`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Tests.EditMode`
- `FrameWork_Ranger.BaseModules.ResourceManagement.Tests.PlayMode`

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

- 菜单 `FrameWork_Ranger/Framework Center` 是项目配置、架构图、帮助和示例验收的统一入口。
- 页面通过 `FrameworkCenterPage`、`[FrameworkCenterPageExtension]` 和 `TypeCache` 显式发现，支持搜索、单预览页、可排序持久固定页和页面级错误隔离；测试替身不会进入生产目录。
- 生产程序集通过 `FrameworkArchitectureAssemblyAttribute` 显式加入架构目录，避免核心维护模块白名单；Tests、Samples 和第三方程序集默认排除。
- 所有接入生产程序集中的顶层类、接口、结构体与枚举通过 `FrameworkArchitectureAttribute` 维护中文名称、职责、层级与关键协作关系。
- 当前正式目录包含 11 个分组、115 个类型节点且诊断为零；其中 Resource Management 为 25 个节点，覆盖公开契约、Module/Handler、缓存状态、Provider、双后端与 Editor 工具。
- 架构页使用一张 Compound Graph：折叠分组是互相连接的紧凑大卡片，原地展开后只包围直接子组与直属类型；每个分组只创建实际使用的局部逻辑层列。
- 新 Unity 会话默认全部收起并启用自动布局；当前会话保存用户展开集合和非零位置偏移。折叠关系聚合到可见分组代理，搜索使用不污染用户状态或位置的临时展开。
- 可见卡片支持缩放换算拖动、子树整体移动、嵌套左上边界约束和父容器右下扩展；布局按目录与状态 revision 缓存，普通 Repaint 复用结果。
- Center 使用 42px 顶部栏、30px 标签栏、208px 扁平导航、动态页面标题卡片和深浅主题自适应样式。
- 代码架构图与模块依赖图共用可缩放、可平移视口，提供适配和 100% 重置；代码架构图为 10%–200%，其他图保持 35%–200%。
- 固定项目设置的 Inspector 与 Framework Center 共用主从配置工作台：左侧选择项目、Global、Default Scene 或场景覆盖，右侧在配置编辑与组合依赖图之间切换，并按当前上下文执行 Global 或 Global + Scene 解析。
- Global/Scene Config 与独立 Inspector 共用 32px 紧凑模块列表；搜索和全部/启用/异常筛选使用源索引映射，筛选期间禁止重排，同一配置最多通过眼睛按钮展开一个真实 Module Inspector。
- 配置工作台导航、页签、筛选、搜索和详情状态只存在当前 Unity 会话；真实子 Editor 使用缓存并在目标替换、选择失效或宿主生命周期结束时释放。

## 6. 示例与资产接线

2026-08-22 已按用户确认删除 CoreSkeleton A/B 示例场景及其专用 Runtime、Editor、asmdef、SceneConfig、Module SO 和测试。原先重复存在的 Resource 空壳场景也已删除；保留下来的唯一双后端验收场景随后迁入 `BaseModules/ResourceManagement/Samples/Scenes/ResourceManagementSample.unity`，并保留原场景 GUID。历史阶段文档继续记录旧资产曾经存在和通过验收的事实。

固定 `FrameworkProjectSettings.asset` 现在指向 `Resources/FrameworkGlobalConfig.asset`；该 GlobalConfig 只安装 `ResourceModule.asset`。场景覆盖表使用验收场景原 GUID，并将新模块内路径绑定到 `Assets/Scenes/DefaultSceneConfig.asset`；该 SceneConfig 不安装 ResourceModule，因此资源模块仍只存在于 GlobalScope。

`ProjectSettings/EditorBuildSettings.asset` 只保留启用的模块内 `ResourceManagementSample.unity`。该场景挂载 `ResourceManagementSampleView`，可以直接人工 Acquire、Instantiate、Destroy 与 Release 两个后端的 Prefab；Addressables 本地 Group/Entry、两个验收 Prefab、Resource Sample 代码和 Player Smoke Runner 继续保留。

## 7. 验证状态

Unity 6000.5.9f1 的当前清理后验证结果如下。由于权威工程当时由 GUI Editor 打开，本次通过由当前 `Assets`、`Packages`、`ProjectSettings` 建立的隔离副本执行：

| 测试集 | 结果 | 用例 | NUnit 时长 |
| --- | --- | ---: | ---: |
| EditMode（含 Addressables 包附加用例） | Passed | 93/93 | 结果 XML 失败 0 |
| PlayMode | Passed | 17/17 | 结果 XML 失败 0 |

隔离 Import/C# 编译、Addressables 2.9.1 本地内容、只含模块内 `ResourceManagementSample` 的 StandaloneWindows64 Player 和 Player 双后端命令行冒烟均通过，所有进程退出码为 0；冒烟日志包含 `PASS 双后端 Acquire/Instantiate/Destroy/Release`。完整迁移历史证据见 [Unity 6000 与仓库迁移验收](../03_Architecture/Distribution/01_Unity6_Migration_Acceptance.md)，命令见 [Unity 6000 CLI 验证命令](../04_Standards/Unity_6000_CLI.md)。

迁移前 Unity 2022.3.62f3 的历史结果仍保留在原阶段文档中，不用 Unity 6000 数值覆写。Resource 原阶段证据见 [Resource Management 验收与复盘](../03_Architecture/FoundationModules/ResourceManagement/04_Acceptance_And_Review.md)，Editor Center 最新证据见 [Phase 1.8 验收与复盘](../03_Architecture/EditorCenter/17_Phase1_8_HTY_Style_Configuration_Workspace_Acceptance_And_Review.md)。

Editor Center 前序页签验收见 [Phase 1.3 验收与复盘](../03_Architecture/EditorCenter/07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md)；紧凑复合架构图证据见 [Phase 1.6 验收与复盘](../03_Architecture/EditorCenter/13_Phase1_6_Compact_Expandable_Architecture_Graph_Acceptance_And_Review.md)，最新主从配置工作台证据与人工步骤见 [Phase 1.8 验收与复盘](../03_Architecture/EditorCenter/17_Phase1_8_HTY_Style_Configuration_Workspace_Acceptance_And_Review.md)。

## 8. 当前未决范围

- 第一个可验证游戏目标及其核心循环。
- 对象池/引用池的准确边界，以及 Event Center 对最小引用复用契约的依赖。
- EventCenter 对 Pooling 的最小依赖接口，以及 GameObject Pool 对 Resource 的依赖方式。
- Pooling 是否复用 Resource 的垂直胶囊原则，以及自身需要的程序集拆分。
- 是否由真实需求扩展 Additive Scene、多实例、接口绑定或并行初始化。
- 各业务模块的公开 API、数据所有权、失败策略与性能指标。
- 模块分发、游戏项目可编辑维护与未来管理 App 的领域模型；工程仓库与 Unity 版本迁移已由 [ADR-DIST-001](../03_Architecture/Distribution/ADR-DIST-001_Unity6_Repository_Migration.md) 确认。

这些问题继续集中到[重建设计待办](./09_Rebuild_Decision_Backlog.md)。下一步只围绕 Pooling 填写需求简报并形成正式计划；没有新的用户批准前不创建 Pooling 或 Event 代码。
