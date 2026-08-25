# FrameWork_Ranger Skill 路由

本目录是 FrameWork_Ranger 的项目内 Skill 规范入口。项目事实和架构要求存放在版本库中；Codex 自动发现的个人 Skill 只负责路由与执行这些规范，避免在项目内外复制两套容易漂移的完整 `SKILL.md`。

## 自动发现的个人 Skills

- `$work-with-framework-ranger`：所有 FrameWork_Ranger 任务的项目入口。
- `$framework-ranger-lightweight-refactor`：参考研究、骨架设计、ADR、分阶段实现与验收路线。
- `$build-framework-ranger-module`：单个正式模块从需求简报、参考研究、详细计划到代码/SO/配置/测试/复盘的门禁式流水线。
- `$plan-framework-ranger-distribution`：框架源码仓库、可选模块分发、游戏项目同步与未来管理 App 的研究和决策流程。

个人 Skill 目录位于 `C:\Users\Maugham\.codex\skills`，不属于 Unity 项目资产。更新本目录的路由或强制规范时，应同步检查个人 Skill 是否仍指向正确文档。

## 当前核心任务加载顺序

1. [Docs 总索引](../README.md)
2. [当前项目状态](../00_Project/01_Current_Project_Status.md)
3. [第一阶段实现计划](../03_Architecture/Core/03_Phase1_Core_Skeleton_Implementation_Plan.md)
4. [第一阶段验收与复盘](../03_Architecture/Core/04_Phase1_Core_Skeleton_Acceptance_And_Review.md)
5. [Core ADR](../03_Architecture/Core/ADR/README.md)
6. [重建设计待办](../00_Project/09_Rebuild_Decision_Backlog.md)
7. [计划交付规范](../03_Architecture/Core/02_Core_Skeleton_Plan_Output_Spec.md)
8. [代码规范](../04_Standards/Code_Style_And_Comments.md)
9. [Unity CLI 开发强制规则](../04_Standards/Unity_CLI_Development_Rules.md)
10. [HTY 参考架构](../02_References/HTY/06_HTY_Reference_Architecture.md)
11. [基础模块建设入口](../03_Architecture/FoundationModules/README.md)
12. [AI 模块开发流水线](../03_Architecture/FoundationModules/01_AI_Module_Development_Pipeline.md)
13. [YokiFrame Kit 参考](../02_References/YokiFrame/00_YokiFrame_Kit_Architecture_And_Source_Map.md)
14. [分发 App 探索](../03_Architecture/Distribution/README.md)
15. [Resource Management 已验收事实](../03_Architecture/FoundationModules/ResourceManagement/README.md)

设计输入与决策问题仍作为历史推导资料保留，但不再代表当前 API 尚未确定。

## 路由规则

- 用户提出想法或偏好：先更新设计输入并标记为已确认方向、候选或待决策。
- 用户要求骨架计划：按决策问题逐组确认，然后严格使用计划交付规范。
- 用户批准计划并要求实现：先核对 ADR、脚本路径和验收，再写代码。
- 用户要求模块功能：加载 `$build-framework-ranger-module`，在 `03_Architecture/FoundationModules/<ModuleName>` 建立模块设计文档，先批准逐脚本计划，再决定 Runtime/Editor/Tests/Assets 位置并实现。
- 用户讨论框架仓库、模块选择安装、跨项目升级、源码回流或专用管理 App：加载 `$plan-framework-ranger-distribution`，先研究并形成 ADR，不直接迁移仓库或开发 App。
- 用户要求修改 HTY：除非明确把 LyingBottle 设为写入目标，否则只读参考并回写 FrameWork_Ranger 文档。
- 用户要求参考 YokiFrame：遵守其项目 Skill 阅读路线，默认只读；Kit API 和安装协议不自动成为 FrameWork_Ranger 决策。
- 任何涉及 Unity C#、资产、设置、测试、内容或 Player 构建的任务：加载并遵守 [Unity CLI 开发强制规则](../04_Standards/Unity_CLI_Development_Rules.md)，优先使用项目根目录 `Tools/UnityCli.ps1`；MCP/EditorMcpAdapter 不作为当前开发或验收入口。

## 文档与 Skill 同步检查

- 路径和文件名改变后，更新 Docs 索引、项目 Skill 路由和个人 Skills。
- 新约束只记录一个权威来源，其他位置使用链接。
- Skill 验证通过不代表 Unity 代码通过；实现阶段仍需 Unity 编译和对应测试。
