# Framework_WWJ Editor Center 设计入口

本目录记录 Phase 1.1 的中央启动、统一编辑器入口与声明式代码架构图设计，以及 Phase 1.2 的视觉层级、显式页面发现边界和共享可导航图视口。具体运行时代码仍以 Core 契约为基础；本目录只增加项目级配置和编辑器工作流，不改变模块生命周期、依赖排序与回滚语义。

## 阅读顺序

1. [Phase 1.1 实施计划](./00_Phase1_1_Implementation_Plan.md)
2. [统一编辑器中心设计](./01_Editor_Center_Architecture.md)
3. [声明式代码架构图设计](./02_Declarative_Code_Graph.md)
4. [Phase 1.1 验收与复盘](./03_Phase1_1_Acceptance_And_Review.md)
5. [Phase 1.2 Editor Center UX 实施计划](./04_Phase1_2_Editor_Center_UX_Implementation_Plan.md)
6. [Phase 1.2 验收与复盘](./05_Phase1_2_Editor_Center_UX_Acceptance_And_Review.md)
7. [中央启动 ADR](../Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md)
8. [Editor Center ADR](./ADR/README.md)

## 当前状态

- **状态**：Phase 1.1 与 Phase 1.2 均已实现并通过自动化验收；窗口视觉手感按验收清单在当前 Unity 编辑器中确认。
- **决定**：正常场景不再放置 `FrameworkEntry`，由固定项目设置资产与自动 Bootstrap 驱动。
- **决定**：Framework Center 只自动发现同时继承页面基类并声明 `[FrameworkCenterPageExtension]` 的正式页面，集成配置、架构图、帮助与示例验收。
- **决定**：Runtime 与 Editor 顶层类/关键接口通过 Attribute 维护名称、职责、层级与关键协作关系。
- **决定**：代码架构图与模块依赖图共享 35%–200% 的可缩放、可平移视口；节点仍使用确定性自动布局。
