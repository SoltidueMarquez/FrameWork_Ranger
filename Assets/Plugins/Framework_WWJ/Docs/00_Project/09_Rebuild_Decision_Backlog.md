# Framework_WWJ 重建设计待办

> 状态：第一阶段骨架、Phase 1.1 中央启动/编辑器中心、Phase 1.2 编辑器 UX 与 Phase 1.3 预览/固定页签已完成；等待用户补充下一阶段需求和第一个游戏目标。<br>
> 本文是决策入口，不是已经批准的阶段路线。

当前骨架设计的用户输入、问题清单和计划格式已拆分到 [Core 架构设计入口](../03_Architecture/Core/README.md)。
第一阶段的实际结果见[验收与复盘](../03_Architecture/Core/04_Phase1_Core_Skeleton_Acceptance_And_Review.md)。
Phase 1.1 的当前结果见[中央启动与 Editor Center 验收](../03_Architecture/EditorCenter/03_Phase1_1_Acceptance_And_Review.md)。
Phase 1.2 的当前结果见[Editor Center UX 验收](../03_Architecture/EditorCenter/05_Phase1_2_Editor_Center_UX_Acceptance_And_Review.md)。
Phase 1.3 的当前结果见[预览与固定页签验收](../03_Architecture/EditorCenter/07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md)。

## 1. 已确认边界

- 从零设计 Framework_WWJ，不要求兼容旧框架或 HTY API。
- 框架采用模块化思维，主体负责模块装配、卸载和统一时序驱动。
- HTY / ActFramework 是主要参考目标，但只复用经过验证的问题定义与设计思想。
- 优先追求轻量、可理解、可测试，再随真实游戏目标逐步扩展。
- `Main` 与 `Utils` 的旧代码已经清理；正式设计确认前不恢复旧类型或建立并行 V2。

## 2. 第一阶段已经关闭的决策

| 主题 | 必须回答的问题 | 所需证据 | 状态 |
| --- | --- | --- | --- |
| 模块载体 | Odin SO 模板，Scope 运行时克隆 | [ADR-001](../03_Architecture/Core/ADR/ADR-001_SO_Module_Templates_And_Runtime_Clones.md) | 已接受 |
| 配置分离 | 模板资产不保存运行状态；Module 与 GlobalConfig 按规则克隆 | [ADR-001](../03_Architecture/Core/ADR/ADR-001_SO_Module_Templates_And_Runtime_Clones.md) | 已接受 |
| 作用域 | 一个常驻 GlobalScope + 一个当前 SceneScope | [ADR-002](../03_Architecture/Core/ADR/ADR-002_Global_And_Scene_Scope_Ownership.md) | 已接受 |
| 生命周期 | UniTask 串行 Load；Load 可取消，Unload 不可取消 | [ADR-003](../03_Architecture/Core/ADR/ADR-003_Dependencies_Lifecycle_And_Rollback.md) | 已接受 |
| 初始化顺序 | 具体类型依赖 + 稳定拓扑排序 + 同层优先级 | [ADR-003](../03_Architecture/Core/ADR/ADR-003_Dependencies_Lifecycle_And_Rollback.md) | 已接受 |
| Tick | Update/FixedUpdate/LateUpdate 显式能力接口 | [ADR-004](../03_Architecture/Core/ADR/ADR-004_Module_Access_And_Explicit_Tick.md) | 已接受 |
| 模块访问 | 业务静态只读门面；模块内部受限 Context | [ADR-004](../03_Architecture/Core/ADR/ADR-004_Module_Access_And_Explicit_Tick.md) | 已接受 |
| 重复规则 | 活动框架内按具体 Module 类型唯一 | [ADR-003](../03_Architecture/Core/ADR/ADR-003_Dependencies_Lifecycle_And_Rollback.md) | 已接受 |
| 错误策略 | 当前 Scope 失败并逆序回滚；Global/Scene 状态分治 | [ADR-003](../03_Architecture/Core/ADR/ADR-003_Dependencies_Lifecycle_And_Rollback.md) | 已接受 |
| Handler | 可选组合；Direct 与 Handler Module 并存 | [ADR-001](../03_Architecture/Core/ADR/ADR-001_SO_Module_Templates_And_Runtime_Clones.md) | 已验证 |
| 程序集 | Runtime、Editor、EditMode、PlayMode、Sample 分离 | [验收复盘](../03_Architecture/Core/04_Phase1_Core_Skeleton_Acceptance_And_Review.md) | 已验证 |
| 诊断 | 共享 Resolver、中文诊断、Odin/IMGUI 只读图 | [ADR-005](../03_Architecture/Core/ADR/ADR-005_Fixed_Runtime_And_Driver_Hooks.md) | 已验证 |

这些决策是第一阶段核心契约。未来若真实游戏目标要求多 SceneScope、接口绑定、并行加载或多实例模块，应新增 ADR 替代对应决定，而不是静默改变语义。

## 3. Phase 1.1 已经关闭的决策

| 主题 | 已确认决策 | 依据 | 状态 |
| --- | --- | --- | --- |
| 项目级配置 | 固定 Resources 资产统一保存 GlobalConfig、默认 SceneConfig 与场景覆盖 | [ADR-006](../03_Architecture/Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md) | 已接受 |
| 启动入口 | 删除 `FrameworkEntry`；首场景加载后由 `FrameworkBootstrap` 自动启动 | [ADR-006](../03_Architecture/Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md) | 已接受 |
| Scene 所有权 | 当前活动场景的 Scene Handle 是 SceneScope 所有者令牌 | [ADR-006](../03_Architecture/Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md) | 已接受 |
| 未登记场景 | 精确覆盖优先；其次默认配置；二者均空时为合法空 SceneScope | [Phase 1.1 计划](../03_Architecture/EditorCenter/00_Phase1_1_Implementation_Plan.md) | 已验证 |
| 统一编辑器入口 | `Framework_WWJ/Framework Center` 集成配置、依赖图、架构图、帮助和示例 | [Editor Center 设计](../03_Architecture/EditorCenter/01_Editor_Center_Architecture.md) | 已验证 |
| 页面扩展 | 公共 `FrameworkCenterPage` + 显式 `[FrameworkCenterPageExtension]` + `TypeCache`，稳定 PageId 与持久标签 | [ADR-EC-001](../03_Architecture/EditorCenter/ADR/ADR-EC-001_Discoverable_Framework_Center_Pages.md) | 已验证 |
| 代码架构元数据 | Attribute 声明名称、中文职责、层级与显式协作；继承/接口关系自动生成 | [代码架构图设计](../03_Architecture/EditorCenter/02_Declarative_Code_Graph.md) | 已验证 |
| 架构图边界 | 只绘制直接关系，IMGUI + Handles 固定分层，不引入 GraphView | [代码架构图设计](../03_Architecture/EditorCenter/02_Declarative_Code_Graph.md) | 已验证 |

这些决定只改变项目启动与编辑器工作流，不改变 Module/Handler 生命周期、排序、回滚或公开查询契约。ADR-002 中的 Entry 所有权部分已被 ADR-006 取代，Global/Scene 双 Scope 结论仍有效。

## 4. Phase 1.2 已经关闭的决定

| 主题 | 已确认决定 | 依据 | 状态 |
| --- | --- | --- | --- |
| Center 视觉 | 采用紧凑顶部栏、扁平导航、动态标题卡片和深浅主题样式；Phase 1.2 的最近访问/多标签交互已由 Phase 1.3 取代 | [Phase 1.2 计划](../03_Architecture/EditorCenter/04_Phase1_2_Editor_Center_UX_Implementation_Plan.md) | 视觉已验证，页签部分已取代 |
| 页面发现边界 | 只有显式标记的正式页面参与生产自动发现；测试替身仅用于显式候选测试 | [ADR-EC-001](../03_Architecture/EditorCenter/ADR/ADR-EC-001_Discoverable_Framework_Center_Pages.md) | 已验证 |
| 图视口 | 架构图与模块图共享 35%–200% 缩放、平移、适配与 100% 重置，节点仍固定自动布局 | [ADR-EC-003](../03_Architecture/EditorCenter/ADR/ADR-EC-003_Shared_Navigable_Graph_Viewport.md) | 已验证 |
| 场景组合预览 | 任意 SceneAsset 按 Runtime 精确覆盖、默认、空 Scope 顺序解析；选择只写 SessionState | [Phase 1.2 验收](../03_Architecture/EditorCenter/05_Phase1_2_Editor_Center_UX_Acceptance_And_Review.md) | 已验证 |

这些决定只影响 Editor 发现、呈现和只读预览，不改变 Runtime 生命周期、中央设置序列化格式或场景解析优先级。

## 5. Phase 1.3 已经关闭的决定

| 主题 | 已确认决定 | 依据 | 状态 |
| --- | --- | --- | --- |
| 页签模型 | 多个固定页 + 一个临时预览页；未固定页共用预览槽位 | [ADR-EC-004](../03_Architecture/EditorCenter/ADR/ADR-EC-004_Preview_And_Pinned_Page_Tabs.md) | 已验证 |
| 持久化 | v2 只保存固定 PageId 顺序和最后活动固定页；预览页不持久化 | [Phase 1.3 验收](../03_Architecture/EditorCenter/07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md) | 已验证 |
| 旧状态 | 不把旧 `openTabs` / `recentPageIds` 迁移为固定页，首次回到概览预览 | [ADR-EC-004](../03_Architecture/EditorCenter/ADR/ADR-EC-004_Preview_And_Pinned_Page_Tabs.md) | 已验证 |
| 固定顺序 | 固定页可拖拽重排并跨会话保存；预览页不参与拖拽 | [Phase 1.3 计划](../03_Architecture/EditorCenter/06_Phase1_3_Preview_And_Pinned_Tabs_Implementation_Plan.md) | 状态逻辑已验证，GUI 手感待人工复核 |
| 帮助页 | 使用普通预览/固定规则，不持久化阅读位置或临时文档状态 | [ADR-EC-004](../03_Architecture/EditorCenter/ADR/ADR-EC-004_Preview_And_Pinned_Page_Tabs.md) | 已验证 |

这些决定只改变 Framework Center 的 Editor 交互和 Library 本地状态，不改变页面扩展公开签名或 Runtime 行为。

## 6. 基础模块候选待办

以下不是首批实现清单。每个模块都必须由游戏目标触发后单独定义契约：

- 日志与诊断
- 时间与 Tick
- 场景流程
- 资源访问
- 对象池
- 音频
- 输入
- UI
- 存档

为每个候选补齐：使用者、公开 API、所有权、依赖、同步/异步语义、失败行为、资源释放、EditMode/PlayMode 验收和明确非目标。

## 7. 阶段需求输入模板

用户提出下一阶段时，先记录以下内容；缺失且会改变架构的部分必须补齐后再实施。

```markdown
# 阶段名称

## 目标
- 这一阶段要解决的具体问题：
- 玩家或开发者可观察到的结果：

## 游戏场景
- 核心循环：
- 场景数量与切换方式：
- 目标平台：
- 涉及的资源和数据：

## 范围
- 必须实现：
- 明确不实现：
- 可接受的破坏性变化：

## 参考
- 希望借鉴的 HTY 机制：
- 希望避免的复杂度：
- 其他参考项目：

## 验收
- EditMode：
- PlayMode：
- 垂直切片：
- 性能或内存指标：
```

## 8. 架构决策记录模板

每项影响核心契约的决定都在 Docs 中留下 ADR：

```markdown
# ADR-XXX：标题

- 状态：提议 / 已接受 / 已替代
- 日期：
- 所属阶段：

## 背景与约束

## 候选方案

## 决定

## 影响与明确非目标

## 验证方式
```

一个 ADR 只解决一个主要选择。被替代时保留原文并链接新 ADR，避免历史原因再次丢失。

## 9. 单阶段门禁

### 设计就绪

- [x] 用户目标、范围和非目标已记录
- [x] 关键术语与所有权没有歧义
- [x] 公共接口和数据流已描述
- [x] 失败、取消和卸载行为已描述
- [x] 验收测试能够证明阶段目标
- [x] 与 HTY 的关系已标记为借鉴、简化或舍弃

### 实现完成

- [x] 只实现本阶段批准范围
- [x] Runtime、Editor、Tests 依赖方向正确
- [x] Unity 编译通过
- [x] 最新 EditMode 33/33、PlayMode 13/13 与无 Entry 的 A/B 示例切换通过
- [x] 文档、ADR 和索引已回写
- [x] 已记录未观察就绪失败等真实摩擦点，没有扩张到业务模块

## 10. 下一步需要用户提供

1. 第一个可验证游戏目标；
2. 该目标必须使用的最少模块；
3. 场景与跨场景状态模型；
4. 对异步加载、取消和错误恢复的最低要求；
5. 希望优先借鉴或明确避免的 HTY 设计。

收到这些信息后，再形成下一阶段的正式设计与验收计划。第一阶段骨架本身不预设必须先实现资源、对象池或音频中的哪一个。
