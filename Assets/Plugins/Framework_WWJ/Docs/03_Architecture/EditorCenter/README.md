# Framework_WWJ Editor Center 设计入口

本目录记录 Phase 1.1 的中央启动、统一编辑器入口与声明式代码架构图设计，Phase 1.2 的视觉层级、显式页面发现边界和共享可导航图视口，Phase 1.3 的预览/固定页签交互，Phase 1.4 的生产程序集接入，以及 Phase 1.5 的单画布可展开代码架构图。具体运行时代码仍以 Core 契约为基础；本目录只增加项目级配置和编辑器工作流，不改变模块生命周期、依赖排序与回滚语义。

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
13. [中央启动 ADR](../Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md)
14. [Editor Center ADR](./ADR/README.md)

## 当前状态

- **状态**：Phase 1.1–1.5 已实现；Phase 1.5 自动化为 EditMode 67/67、PlayMode 18/18，真实窗口视觉交互等待用户确认。
- **决定**：正常场景不再放置 `FrameworkEntry`，由固定项目设置资产与自动 Bootstrap 驱动。
- **决定**：Framework Center 只自动发现同时继承页面基类并声明 `[FrameworkCenterPageExtension]` 的正式页面，集成配置、架构图、帮助与示例验收。
- **决定**：生产程序集通过程序集 Attribute 显式接入分层目录；其顶层类、接口、结构体和枚举通过类型 Attribute 维护名称、职责、层级与关键协作关系。
- **决定**：节点图共享可缩放、可平移视口；模块依赖图保持 35%–200%，代码架构大图使用 10%–200%。
- **决定**：代码架构使用一张单画布 Compound Graph；分组作为纵向嵌套泳道原地展开，类型按七个全局逻辑层列对齐。
- **决定**：Center 顶部使用多个可排序固定页与一个临时预览页；只持久化用户显式固定的页面。
