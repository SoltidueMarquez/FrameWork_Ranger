# Framework 仓库、模块分发与管理 App

> 状态：Unity 6000 与 `FrameWork_Ranger` 仓库迁移已接受并实施；产品身份已统一为 `FrameWork_Ranger`。2026-09-11 已完成管理 App 的参考调研、需求与技术准备；尚未开始正式实现。<br>
> 当前交付为准备文档，未创建远程 App 仓库、拆 UPM 包或增加安装器。

## 管理 App 准备入口（2026-09-11）

- [后续评估：WispFramework 与 AI 工作流替代方案](./08_AI_Workflow_And_Wisp_Review.md)（新推荐：先验证轻量 AI 工作流，是否暂缓 App 待定）
- [准备工作总览](./02_Manager_App_Preparation.md)
- [HtyHub / HtyApp / HtyFrameworkSync 参考调研](./03_Reference_Apps_Research.md)
- [产品需求与验收场景](./04_Manager_App_Requirements.md)
- [模块拆分、版本与开发回流](./05_Modules_And_Sync_Design.md)
- [技术选型与官方参考](./06_Technology_And_References.md)
- [新仓库筹备与开发路线](./07_Repository_And_Roadmap.md)

## 入口

1. [Unity 6000 与仓库迁移 ADR](./ADR-DIST-001_Unity6_Repository_Migration.md)
2. [产品身份重命名 ADR](./ADR-DIST-002_Identity_Rename_To_FrameWork_Ranger.md)
3. [Unity 6000 与仓库迁移验收](./01_Unity6_Migration_Acceptance.md)
4. [Unity 6000 CLI 验证命令](../../04_Standards/Unity_6000_CLI.md)
5. [分发 App 探索与决策待办](./00_Framework_Repository_Distribution_App_Exploration.md)
6. [基础模块建设入口](../FoundationModules/README.md)
7. [YokiFrame Kit 架构与工具链](../../02_References/YokiFrame/00_YokiFrame_Kit_Architecture_And_Source_Map.md)
8. 自动发现 Skill：`$plan-framework-ranger-distribution`

## 当前边界

- 源工程已迁入 Unity 6000.5.9f1 与 `FrameWork_Ranger`，双历史、备份和清理门禁以 ADR-DIST-001 为准。
- 后续分发先定义问题、角色、数据流、风险和候选方案。
- 用户已提出后续新建 GitHub 仓库；App 技术栈、包协议与版本策略已有候选建议，尚未作为正式决策接受。
- 不为了未来 App 立即移动现有 Runtime/Editor/Samples。
- 基础模块阶段应保留清晰目录与程序集边界，为未来分发提供证据，但不提前实现安装器。
