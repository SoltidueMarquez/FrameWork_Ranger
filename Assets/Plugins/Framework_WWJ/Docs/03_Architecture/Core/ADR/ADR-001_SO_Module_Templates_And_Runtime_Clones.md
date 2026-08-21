# ADR-001：SO 模块模板与运行时克隆

- 状态：已接受
- 日期：2026-08-07
- 所属阶段：第一阶段——模块模型与驱动骨架

## 背景与约束

框架需要利用 Odin 在 Inspector 中配置模块及多态 Handler，同时不能把生命周期状态、运行计数和依赖引用写回项目资产。普通 C# 对象缺少直接资产化体验；直接运行模板 SO 又会污染资产，并让跨场景所有权难以判断。

## 候选方案

1. 普通 C# 模块，由独立 SO 只保存配置。
2. SO 同时作为模板和运行对象。
3. Odin `SerializedScriptableObject` 作为模板，Scope 启动时克隆。

## 决定

采用方案 3。`ModuleBase` 是 Odin SO 模板；启用条目在装配前完成校验，随后通过 `Object.Instantiate` 克隆。生命周期状态、`ModuleContext`、来源模板和 Tick 状态只存在于克隆。Scope 卸载完成后统一销毁克隆。

同时保留两种实现形态：

- `DirectModuleBase`：小型模块直接实现生命周期。
- `HandlerModuleBase<THandler>`：Module 提供稳定公开门面，Odin 多态内嵌 Handler 承载可替换实现。

## 影响与明确非目标

- 模板资产可复用，运行状态不会被保存。
- Handler 不注册为全局服务，也不单独拥有 Unity 对象生命周期。
- 第一阶段不支持运行时替换模板、热插拔模块或把 Handler 暴露给业务层。

## 验证方式

- EditMode 克隆隔离与 Handler 克隆测试。
- PlayMode 自动启动、Shutdown 后克隆销毁测试。
- A/B 示例使用同一 Module 类型和不同 Handler 类型。
