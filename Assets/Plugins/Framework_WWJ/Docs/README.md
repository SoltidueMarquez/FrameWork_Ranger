# Framework_WWJ 文档索引

本目录是 Framework_WWJ 从旧实现转向全量重建时的资料基线。文档整理日期为 2026-08-06。

这里刻意区分四类信息：

- **事实**：已经在旧代码、Unity 资产或 Git 历史中验证过的内容。
- **历史方案**：曾经计划或实现过，但不再自动成为新框架约束的内容。
- **暂定方向**：开始新设计时值得讨论的候选方案，必须结合后续参考项目重新确认。
- **已确认决策**：针对某个正式阶段明确接受，并通过 ADR 与验收标准记录的约束。

## 阅读顺序

1. [当前项目状态](./01_Current_Project_Status.md)：环境、工作区状态、代码规模和重构是否已经开始。
2. [旧框架架构](./02_Legacy_Architecture.md)：旧入口、Loader、模块、配置、对象池、工具层的完整说明。
3. [历史计划与设计演进](./03_Previous_Plans_And_Design_Evolution.md)：从参考框架复刻到轻量 SO 架构的几代方案。
4. [全量重建基线](./04_Rebuild_Baseline.md)：本次从零设计的边界和历史占位路线。
5. [旧代码清理记录](./05_Cleanup_Log.md)：旧代码删除清单、保留范围和恢复说明。
6. [HTY / ActFramework 参考架构](./06_HTY_Reference_Architecture.md)：参考项目的宿主、模块、配置、生命周期与复杂度事实。
7. [HTY 轻量化提炼矩阵](./07_HTY_Lightweight_Extraction_Matrix.md)：保留思想、简化验证、延后和默认舍弃项。
8. [HTY 参考源码索引](./08_HTY_Reference_Source_Map.md)：后续按主题继续研究 LyingBottle 的最小阅读路径。
9. [重建设计待办](./09_Rebuild_Decision_Backlog.md)：阶段输入、待决策问题、ADR 与验收门禁。
10. [旧对象池设计归档](./Legacy_Object_Pool_Design.md)：旧对象池中值得保留和需要修正的思想。
11. [旧 Loader 原始设计](./LoaderDesign.md)：2026-03 的原始计划，原文保留用于追溯。

## 文档约定

- 新架构开始实现后，优先更新本目录，而不是把关键设计只留在聊天或代码注释中。
- 历史文档不代表新实现必须兼容旧 API。
- 参考项目文档中的“事实、推断、候选、决策”必须分开标记；候选不能直接变成实现约束。
- 第三方依赖（例如 Sirenix Odin、DOTween）不属于 Framework_WWJ 核心源码；是否继续使用，要在新设计阶段单独决策。
