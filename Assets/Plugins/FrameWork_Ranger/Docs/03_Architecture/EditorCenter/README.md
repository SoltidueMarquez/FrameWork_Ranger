# FrameWork_Ranger Editor Center 设计入口

本目录记录 Phase 1.1 的中央启动、统一编辑器入口与声明式代码架构图设计，Phase 1.2 的视觉层级、显式页面发现边界和共享可导航图视口，Phase 1.3 的预览/固定页签交互，Phase 1.4 的生产程序集接入，Phase 1.5 的单画布可展开代码架构图，Phase 1.6 的紧凑复合节点与会话拖动，Phase 1.7 的配置资产内联 Inspector，以及 Phase 1.8 的 HTY 式主从配置工作台。具体运行时代码仍以 Core 契约为基础；本目录只增加项目级配置和编辑器工作流，不改变模块生命周期、依赖排序与回滚语义。

## 阅读顺序

1. [Phase 1.1 实施计划](./00_Phase1_1_Implementation_Plan.md)
2. [统一编辑器中心设计](./01_Editor_Center_Architecture.md)
3. [声明式代码架构图设计](./02_Declarative_Code_Graph.md)
4. [Phase 1.1 验收与复盘](./03_Phase1_1_Acceptance_And_Review.md)
5. [Phase 1.2 Editor Center UX 实施计划](./04_Phase1_2_Editor_Center_UX_Implementation_Plan.md)
6. [Phase 1.2 验收与复盘](./05_Phase1_2_Editor_Center_UX_Acceptance_And_Review.md)
7. [Phase 1.3 预览与固定页签实施计划](./06_Phase1_3_Preview_And_Pinned_Tabs_Implementation_Plan.md)
8. [Phase 1.3 验收与复盘](./07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md)
9. [Phase 1.4 分层代码架构导航实施计划](./08_Phase1_4_Hierarchical_Architecture_Navigator_Implementation_Plan.md)
10. [Phase 1.4 验收与复盘](./09_Phase1_4_Hierarchical_Architecture_Navigator_Acceptance_And_Review.md)
11. [Phase 1.5 单画布架构图实施计划](./10_Phase1_5_Expandable_Compound_Architecture_Graph_Implementation_Plan.md)
12. [Phase 1.5 验收与复盘](./11_Phase1_5_Expandable_Compound_Architecture_Graph_Acceptance_And_Review.md)
13. [Phase 1.6 紧凑可展开架构节点图实施计划](./12_Phase1_6_Compact_Expandable_Architecture_Graph_Implementation_Plan.md)
14. [Phase 1.6 验收与复盘](./13_Phase1_6_Compact_Expandable_Architecture_Graph_Acceptance_And_Review.md)
15. [Phase 1.7 配置资产内联 Inspector 实施计划](./14_Phase1_7_Configuration_Inline_Inspector_Implementation_Plan.md)
16. [Phase 1.7 验收与复盘](./15_Phase1_7_Configuration_Inline_Inspector_Acceptance_And_Review.md)
17. [Phase 1.8 HTY 式主从配置工作台实施计划](./16_Phase1_8_HTY_Style_Configuration_Workspace_Implementation_Plan.md)
18. [Phase 1.8 验收与复盘](./17_Phase1_8_HTY_Style_Configuration_Workspace_Acceptance_And_Review.md)
19. [中央启动 ADR](../Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md)
20. [Editor Center ADR](./ADR/README.md)

## 当前状态

- **状态**：Phase 1.1–1.8 已实现；Phase 1.8 自动化为 EditMode 93/93、PlayMode 17/17，真实窗口视觉交互等待用户确认。
- **决定**：正常场景不再放置 `FrameworkEntry`，由固定项目设置资产与自动 Bootstrap 驱动。
- **决定**：Framework Center 只自动发现同时继承页面基类并声明 `[FrameworkCenterPageExtension]` 的正式页面，集成配置、架构图、帮助与示例验收。
- **决定**：生产程序集通过程序集 Attribute 显式接入分层目录；其顶层类、接口、结构体和枚举通过类型 Attribute 维护名称、职责、层级与关键协作关系。
- **决定**：节点图共享可缩放、可平移视口；模块依赖图保持 35%–200%，代码架构大图使用 10%–200%。
- **决定**：代码架构使用一张单画布 Compound Graph；折叠分组是互相连接的紧凑卡片，原地展开后只包围直接子组与直属类型，类型按分组实际使用的局部逻辑层排列。
- **决定**：可见卡片允许会话内拖动，偏移只写入 `SessionState`；布局按状态 revision 缓存，普通 Repaint 复用结果。
- **决定**：项目设置 Config 引用和 Global/Scene Config 的 Module 引用使用眼睛按钮按需展开真实 Inspector；状态只写入 `SessionState`，子 Editor 按宿主生命周期缓存与释放。
- **决定**：项目配置页使用左侧作用域导航与右侧配置编辑/组合依赖图工作区；Global/Scene Config 和独立 Inspector 共用 32px 紧凑模块列表，同一配置最多展开一个真实 Module Inspector。
- **决定**：Center 顶部使用多个可排序固定页与一个临时预览页；只持久化用户显式固定的页面。
