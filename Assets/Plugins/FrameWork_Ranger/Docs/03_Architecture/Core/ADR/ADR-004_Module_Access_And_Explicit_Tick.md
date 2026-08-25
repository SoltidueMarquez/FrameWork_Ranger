# ADR-004：只读模块访问与显式 Tick 能力

- 状态：已接受
- 日期：2026-08-07
- 所属阶段：第一阶段——模块模型与驱动骨架

## 背景与约束

业务代码需要低摩擦地读取模块，模块内部又需要遵守作用域依赖方向。并非所有模块都需要逐帧执行，默认空 Tick 会扩大无意义 API 面。

## 候选方案

1. 全局可写 Service Locator 与默认 Tick 方法。
2. 构造注入/DI 容器。
3. 静态只读门面、受限 `ModuleContext` 与显式 Tick 接口。

## 决定

采用方案 3。业务通过 `Framework.GetModule<T>` / `TryGetModule<T>` 按精确类型读取已加载模块；模块内部使用 `ModuleContext`。Global 模块只能访问 GlobalScope，Scene 模块可访问自身 Scope 和 GlobalScope。

Update、FixedUpdate、LateUpdate 分别由能力接口选择加入。Module 与 Handler 都可实现接口；同一阶段固定按 Module 后 Handler，GlobalScope 先于 SceneScope。每个 Tick 目标独立捕获异常并继续执行其他目标。

## 影响与明确非目标

- 没有公开模块注册、移除或 Handler 查询 API。
- 查询不会返回 Created/Loading/Failed 状态的模块。
- Tick 异常只记录，不改变 Framework 状态和 `LastException`。
- 第一阶段不引入 DI、反射扫描、Key 查询和日志抽象。

## 验证方式

- EditMode Context 可见性与生命周期状态测试。
- PlayMode Get/TryGet、三类 Tick、Module/Handler 次序和异常隔离测试。
