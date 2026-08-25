# FrameWork_Ranger 文档与 Skill 索引

本目录是 FrameWork_Ranger 的项目事实、历史、参考研究、架构设计、开发规范和 Skill 路由的默认存放地。根索引保留稳定；其余资料按功能与层次分目录管理。

现行产品身份是 `FrameWork_Ranger`（命名空间、插件目录、GitHub 仓库与本地工程目录一致）。`Framework_WWJ` / `FrameWork_WWJ` 只作为历史名称出现在 `01_History` 和已关闭阶段证据中，见 [产品身份说明](./01_History/00_Product_Identity_Note.md) 与 [ADR-DIST-002](./03_Architecture/Distribution/ADR-DIST-002_Identity_Rename_To_FrameWork_Ranger.md)。

## 信息状态

- **事实**：已从代码、资产、版本历史或测试验证。
- **历史方案**：曾经计划或实现过，不自动成为新框架约束。
- **候选**：用户倾向或值得讨论的方向，仍需补齐精确语义。
- **已确认决策**：针对正式阶段接受，并通过 ADR 与验收标准记录。

## 目录结构

```text
Docs/
├─ 00_Project/              当前状态、重建基线、清理记录、总决策待办
├─ 01_History/              旧框架、历代计划、Loader 与对象池历史原文
├─ 02_References/HTY/       HTY / ActFramework 架构、提炼矩阵、源码索引
├─ 02_References/YokiFrame/ Kit 架构、基础能力与工具链参考
├─ 03_Architecture/Core/    模块骨架设计、阶段计划、ADR 与验收复盘
├─ 03_Architecture/EditorCenter/ 中央启动、统一编辑器中心与代码架构图
├─ 03_Architecture/FoundationModules/ 基础模块纲领、AI 流水线与模块交付契约
├─ 03_Architecture/Distribution/ 框架仓库、模块分发与管理 App 探索
├─ 04_Standards/            代码、注释、Unity CLI、目录和实现规范
└─ 05_Skills/               项目内 Skill 路由与自动发现 Skill 的同步入口
```

未来新增基础模块时，在 `03_Architecture/FoundationModules/<ModuleName>/` 建立对应设计资料，不把所有模块混入 Core 文档。

## 当前设计入口

1. [Editor Center 与中央启动入口](./03_Architecture/EditorCenter/README.md)
2. [Phase 1.1 实现计划](./03_Architecture/EditorCenter/00_Phase1_1_Implementation_Plan.md)
3. [Phase 1.1 验收与复盘](./03_Architecture/EditorCenter/03_Phase1_1_Acceptance_And_Review.md)
4. [Phase 1.2 Editor Center UX 实现计划](./03_Architecture/EditorCenter/04_Phase1_2_Editor_Center_UX_Implementation_Plan.md)
5. [Phase 1.2 验收与复盘](./03_Architecture/EditorCenter/05_Phase1_2_Editor_Center_UX_Acceptance_And_Review.md)
6. [Phase 1.3 预览与固定页签实施计划](./03_Architecture/EditorCenter/06_Phase1_3_Preview_And_Pinned_Tabs_Implementation_Plan.md)
7. [Phase 1.3 验收与复盘](./03_Architecture/EditorCenter/07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md)
8. [Phase 1.4 分层代码架构导航实施计划](./03_Architecture/EditorCenter/08_Phase1_4_Hierarchical_Architecture_Navigator_Implementation_Plan.md)
9. [Phase 1.4 验收与复盘](./03_Architecture/EditorCenter/09_Phase1_4_Hierarchical_Architecture_Navigator_Acceptance_And_Review.md)
10. [Phase 1.5 单画布架构图实施计划](./03_Architecture/EditorCenter/10_Phase1_5_Expandable_Compound_Architecture_Graph_Implementation_Plan.md)
11. [Phase 1.5 验收与复盘](./03_Architecture/EditorCenter/11_Phase1_5_Expandable_Compound_Architecture_Graph_Acceptance_And_Review.md)
12. [Phase 1.6 紧凑可展开架构节点图实施计划](./03_Architecture/EditorCenter/12_Phase1_6_Compact_Expandable_Architecture_Graph_Implementation_Plan.md)
13. [Phase 1.6 验收与复盘](./03_Architecture/EditorCenter/13_Phase1_6_Compact_Expandable_Architecture_Graph_Acceptance_And_Review.md)
14. [Phase 1.7 配置资产内联 Inspector 实施计划](./03_Architecture/EditorCenter/14_Phase1_7_Configuration_Inline_Inspector_Implementation_Plan.md)
15. [Phase 1.7 验收与复盘](./03_Architecture/EditorCenter/15_Phase1_7_Configuration_Inline_Inspector_Acceptance_And_Review.md)
16. [Phase 1.8 HTY 式主从配置工作台实施计划](./03_Architecture/EditorCenter/16_Phase1_8_HTY_Style_Configuration_Workspace_Implementation_Plan.md)
17. [Phase 1.8 验收与复盘](./03_Architecture/EditorCenter/17_Phase1_8_HTY_Style_Configuration_Workspace_Acceptance_And_Review.md)
18. [Core 架构设计入口](./03_Architecture/Core/README.md)
19. [第一阶段实现计划（历史启动模型）](./03_Architecture/Core/03_Phase1_Core_Skeleton_Implementation_Plan.md)
20. [第一阶段验收与复盘（历史基线）](./03_Architecture/Core/04_Phase1_Core_Skeleton_Acceptance_And_Review.md)
21. [Core ADR](./03_Architecture/Core/ADR/README.md)
22. [代码、注释与分区规范](./04_Standards/Code_Style_And_Comments.md)
23. [Unity CLI 开发强制规则](./04_Standards/Unity_CLI_Development_Rules.md)
24. [Unity 6000 CLI 命令速查](./04_Standards/Unity_6000_CLI.md)
25. [Unity CLI 技术参考与排障](./04_Standards/Unity_CLI_Technical_Reference.md)
26. [Skill 路由](./05_Skills/README.md)
27. [基础模块建设入口](./03_Architecture/FoundationModules/README.md)
28. [AI 模块开发流水线](./03_Architecture/FoundationModules/01_AI_Module_Development_Pipeline.md)
29. [框架仓库、分发与管理 App](./03_Architecture/Distribution/README.md)
30. [Unity 6000 与仓库迁移 ADR](./03_Architecture/Distribution/ADR-DIST-001_Unity6_Repository_Migration.md)
31. [产品身份重命名 ADR](./03_Architecture/Distribution/ADR-DIST-002_Identity_Rename_To_FrameWork_Ranger.md)
32. [Unity 6000 与仓库迁移验收](./03_Architecture/Distribution/01_Unity6_Migration_Acceptance.md)
33. [Resource Management 模块入口](./03_Architecture/FoundationModules/ResourceManagement/README.md)

## 项目与历史

- [当前项目状态](./00_Project/01_Current_Project_Status.md)
- [历史目录的产品身份说明](./01_History/00_Product_Identity_Note.md)
- [全量重建基线](./00_Project/04_Rebuild_Baseline.md)
- [旧代码清理记录](./00_Project/05_Cleanup_Log.md)
- [重建设计待办](./00_Project/09_Rebuild_Decision_Backlog.md)
- [旧框架架构](./01_History/02_Legacy_Architecture.md)
- [历史计划与设计演进](./01_History/03_Previous_Plans_And_Design_Evolution.md)
- [旧对象池设计归档](./01_History/Legacy_Object_Pool_Design.md)
- [旧 Loader 原始设计](./01_History/LoaderDesign.md)

## HTY 参考

- [HTY / ActFramework 参考架构](./02_References/HTY/06_HTY_Reference_Architecture.md)
- [HTY 轻量化提炼矩阵](./02_References/HTY/07_HTY_Lightweight_Extraction_Matrix.md)
- [HTY 参考源码索引](./02_References/HTY/08_HTY_Reference_Source_Map.md)
- [YokiFrame Kit 架构与源码索引](./02_References/YokiFrame/00_YokiFrame_Kit_Architecture_And_Source_Map.md)

## 维护约定

- 关键设计优先写入本目录，不只保留在聊天或代码注释中。
- 历史文档保留原意；用状态说明和新文档链接替代篡改历史。
- 候选方向不能直接变成代码约束，正式实现前必须进入阶段决策。
- 新建或移动 Unity 资产时同步维护 `.meta` 与 GUID。
- 文档路径改变后同步更新个人 Skills 和本目录 `05_Skills` 路由。
- 第三方依赖是否进入核心必须单独决策。
