# Framework 仓库、模块分发与管理 App

> 状态：Unity 6000 与 `FrameWork_Ranger` 仓库迁移已接受并实施；产品身份已统一为 `FrameWork_Ranger`；模块分发与管理 App 仍处于研究阶段。  
> 当前不创建 App、不拆 UPM 包、不增加 CLI 包装器或 CI。

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
- 不预设 App 技术栈、包管理协议、Git 托管平台或版本策略。
- 不为了未来 App 立即移动现有 Runtime/Editor/Samples。
- 基础模块阶段应保留清晰目录与程序集边界，为未来分发提供证据，但不提前实现安装器。
