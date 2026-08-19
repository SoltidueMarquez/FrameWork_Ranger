# Framework 仓库、模块分发与管理 App

> 状态：问题已纳入设计，等待用户提供参考软件后继续研究。  
> 当前不创建 App、不迁移仓库、不改变 Unity 包结构。

## 入口

1. [分发 App 探索与决策待办](./00_Framework_Repository_Distribution_App_Exploration.md)
2. [基础模块建设入口](../FoundationModules/README.md)
3. [YokiFrame Kit 架构与工具链](../../02_References/YokiFrame/00_YokiFrame_Kit_Architecture_And_Source_Map.md)
4. 自动发现 Skill：`$plan-framework-wwj-distribution`

## 当前边界

- 先定义问题、角色、数据流、风险和候选方案。
- 不预设 App 技术栈、包管理协议、Git 托管平台或版本策略。
- 不为了未来 App 立即移动现有 Runtime/Editor/Samples。
- 基础模块阶段应保留清晰目录与程序集边界，为未来分发提供证据，但不提前实现安装器。
