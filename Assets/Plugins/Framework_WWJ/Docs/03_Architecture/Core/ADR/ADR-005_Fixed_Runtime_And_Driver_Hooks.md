# ADR-005：固定 Runtime 算法与受限 Driver 钩子

- 状态：已接受
- 日期：2026-08-07
- 所属阶段：第一阶段——模块模型与驱动骨架

## 背景与约束

框架级流程需要保留扩展点，但若把排序、状态机或所有权整体交给可替换实现，骨架行为将难以验证，并容易重现 HTY 中较重的装配复杂度。

## 候选方案

1. Framework 自身作为普通 Module。
2. 整个 Runtime/Loader 可替换。
3. 固定 `FrameworkRuntime`，只开放 Driver 前后置异步钩子。

## 决定

采用方案 3。`FrameworkRuntime` 唯一决定校验、克隆、状态迁移、Scope 所有权、回滚和 Shutdown。`FrameworkDriverHandlerBase` 只观察 Scope/Module 加载卸载的前后位置，可执行异步包装逻辑，但不能修改装配集合和顺序。

GlobalConfig 首次有效初始化时被克隆，使 DriverHandler 运行绑定不写入原资产。默认 Handler 是无行为实现。

## 影响与明确非目标

- Driver Load 钩子失败等同当前 Scope 加载失败；Unload 钩子失败会被聚合并继续清理。
- Framework 不加入模块注册表，也不被普通模块依赖。
- 第一阶段不提供替换状态机、排序器或 Scope 策略的插件点。

## 验证方式

- EditMode 共享 Resolver 测试。
- PlayMode 生命周期记录、失败回滚和 Shutdown 测试。
- Inspector 与 Runtime 直接调用同一个 `ModuleGraphResolver`。
