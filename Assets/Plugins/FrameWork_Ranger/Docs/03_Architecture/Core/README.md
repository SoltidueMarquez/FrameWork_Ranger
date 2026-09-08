# FrameWork_Ranger Core 架构设计入口

本目录存放框架骨架的设计输入、已确认计划、架构决策与实际验收结果。这里不会把历史实现或 HTY API 自动视为最终方案。

## 阅读顺序

1. [Phase 1.1 中央启动与 Editor Center](../EditorCenter/README.md)：当前启动方式、统一编辑器入口、代码架构图与最新验收。
2. [第一阶段实现计划](./03_Phase1_Core_Skeleton_Implementation_Plan.md)：模块模型与生命周期核心契约；其中 Entry 启动方式属于历史设计。
3. [第一阶段验收与复盘](./04_Phase1_Core_Skeleton_Acceptance_And_Review.md)：Phase 1 的历史测试基线。
4. [ADR 目录](./ADR/README.md)：SO 克隆、Scope、中央启动、依赖生命周期、访问 Tick 与 Driver 扩展的决策依据。
5. [骨架设计输入](./00_Core_Skeleton_Design_Input.md)：第一阶段之前的用户目标与偏好记录。
6. [骨架决策问题](./01_Core_Skeleton_Decision_Questions.md)：第一阶段决策形成前的问题清单，作为历史推导依据。
7. [计划交付规范](./02_Core_Skeleton_Plan_Output_Spec.md)：顶部为新任务的适量计划规则，后续正文保留第一阶段历史规格。
8. [代码规范](../../04_Standards/Code_Style_And_Comments.md)：可读性、中文注释与 `#region` 规则。

项目级未决问题仍集中在[重建设计待办](../../00_Project/09_Rebuild_Decision_Backlog.md)，HTY 事实见[参考架构](../../02_References/HTY/06_HTY_Reference_Architecture.md)。

## 当前事实与历史基线

核心与 Editor 的实际状态见[当前项目状态](../../00_Project/01_Current_Project_Status.md)及相应现行 ADR。第一阶段的测试数字、Entry 与 A/B 示例属于历史结果，不作为新任务的状态或固定验收数。

新的核心任务按[任务记忆协议](../../05_Skills/01_Task_Memory_And_Recovery.md)恢复需求与进度，只设计和实现用户当前目标。业务模块走[模块流水线](../FoundationModules/01_AI_Module_Development_Pipeline.md)。
