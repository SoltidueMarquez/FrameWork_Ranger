# 基础模块建设入口

> 状态：Resource Management 已实现并完成验收；Pooling 与 Event Center 尚未开始。  
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

## 当前门禁

- Resource Management 已按批准计划关闭；当前不得顺带实现 Pooling 或 Event。
- 三个模块一次只设计和实现一个；进入 Pooling 前仍需重新完成需求确认、参考研究、详细计划和用户批准。
- 每个模块的正式计划都必须先取得用户确认，不能从参考项目直接推导为 Framework_WWJ 决策。
- 模块目录、程序集、SO 资产与可分发单元边界，要在第一个模块计划中先行决定。

## 后续模块目录约定

每个模块进入设计阶段时，在本目录创建独立子目录。Resource Management 已形成首个正式实例：

```text
FoundationModules/
└─ ResourceManagement/
   ├─ README.md
   ├─ 00_Requirement_Brief.md
   ├─ 01_Architecture_And_Public_Contracts.md
   ├─ 02_Implementation_Plan.md
   ├─ 03_Acceptance_And_Review.md
   └─ ADR/
```

目录示例表示文档层次，不预设 C# 类型名或模块最终名称。
