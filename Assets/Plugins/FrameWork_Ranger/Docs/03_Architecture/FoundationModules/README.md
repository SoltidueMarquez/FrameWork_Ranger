# 基础模块建设入口

> 状态：Resource Management 已关闭；Pooling 已实施并等待最终 Unity 全门禁；Event Center 于 2026-09-05 重新进入需求与参考研究，尚未实现。<br>
> 已确认顺序：Resource → Pooling → Event，一次只推进一个模块。

本目录管理基础模块的共同计划、AI 开发流水线、交付契约，以及后续每个模块自己的需求、ADR、实施计划和验收复盘。

## 文档入口

1. [基础模块建设纲领](./00_Foundation_Modules_Program_Charter.md)
2. [AI 模块开发流水线](./01_AI_Module_Development_Pipeline.md)
3. [模块交付契约与模板](./02_Module_Delivery_Contract_And_Templates.md)
4. [HTY 参考架构](../../02_References/HTY/06_HTY_Reference_Architecture.md)
5. [YokiFrame Kit 架构与源码索引](../../02_References/YokiFrame/00_YokiFrame_Kit_Architecture_And_Source_Map.md)
6. [核心契约与 ADR](../Core/README.md)
7. [Resource Management 模块入口](./ResourceManagement/README.md)
8. [Pooling 对象池/引用池模块入口](./Pooling/README.md)
9. [AI 开发工作流优化](../../00_Project/10_AI_Development_Workflow_Optimization.md)（2026-09-05 已落实自然语言入口、JSON 任务记忆与轻量验证）
10. [Event Center 事件中心](./EventCenter/README.md)

## 当前推进规则

- Resource Management 已关闭；Pooling 的已实现内容和未完成验证以模块验收页为准，不能把流程变更当成验证通过。
- 默认保持单个活动模块的工作范围。用户明确提出下一模块时，立即进入其需求和设计；历史模块的未验收项不阻止新需求整理。
- 2026-09-05 用户指定事件中心作为下一次新对话的流程试点。实施前检查它实际依赖的 Reference Pool 契约，只处理影响当前功能的依赖问题，不自动接管无关 Pooling/GameObject/资源后端验收。
- 公共契约的关键未决选择需确认；已有授权和回答持续有效，不要求重新审批全部逐脚本细节。
- 持续操作见[模块流水线](./01_AI_Module_Development_Pipeline.md)与[任务记忆协议](../../05_Skills/01_Task_Memory_And_Recovery.md)。

## 后续模块目录约定

每个模块进入设计阶段时，在本目录创建独立子目录。Resource Management 与 Pooling 已形成正式实例：

```text
FoundationModules/
├─ ResourceManagement/
│  ├─ README.md
│  ├─ 00_Requirement_Brief.md
│  ├─ 01_Reference_Research.md
│  ├─ 02_Architecture_And_Public_Contracts.md
│  ├─ 03_Implementation_Plan.md
│  ├─ 04_Acceptance_And_Review.md
│  └─ ADR/
└─ Pooling/
   ├─ README.md
   ├─ 00_Requirement_Brief.md
   ├─ 01_Reference_Research.md
   ├─ 02_Architecture_And_Public_Contracts.md
   ├─ 03_Implementation_Plan.md
   ├─ 04_Acceptance_And_Review.md
   └─ ADR/
```

目录示例表示文档层次，不预设 C# 类型名或模块最终名称。
