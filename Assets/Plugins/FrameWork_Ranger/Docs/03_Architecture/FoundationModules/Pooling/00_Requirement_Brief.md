# Pooling 需求简报

> 日期：2026-08-31  
> 状态：用户已确认并进入实施

## 目标

在 Resource Management 之后提供两类可独立消费的复用能力：跨场景引用对象池，以及随场景创建和销毁的 GameObject Prefab 池。模块必须沿用现有 SO 模板、Handler 运行克隆、Global/Scene Scope、依赖图、失败回滚、Framework Center 与 Unity CLI 验收体系。

## 必须实现

- Global `ReferencePoolModule`：精确类型惰性建池、SO 类型覆盖、严格借还、`IDisposable` 清理与循环缩容。
- Scene `GameObjectPoolModule`：配置池名、ResourceKey Prefab、Prefab Lease、分帧预热、回调缓存、严格错池检查与场景卸载清理。
- 共享 `PoolCapacitySettings` 与 `TrackedPool<T>`，统一批量扩容、有限空闲容量和循环缩容语义。
- 所有公开运行操作只允许加载 Handler 的 Unity 主线程调用。
- Editor 配置诊断、Build Validator、统一 Pooling Center 页面、独立 Sample、EditMode/PlayMode 与 Player Smoke。

## 失败与清理

- 配置错误在 Scope Load 前失败；GameObject 定义全部无副作用校验后才取得资源。
- 任一资源、实例化、预热或回调失败，清理当前对象并向调用方传播；Scene Load 失败时逆序销毁池并释放 Lease。
- 卸载时未归还对象只输出包含类型或池名与数量的警告，随后回调并强制清理；真实清理异常聚合上报。
- `Try` API 只吸收预期的池名/所有权校验失败，不吞资源、构造或业务回调异常。

## 非目标

不实现 Event Center、动态注册/注销、后台预热、整池空闲卸载、ScriptableObject/集合池、Component 快捷 API、自动 `UnloadUnusedAssets`、CSV、历史追踪、堆栈追踪或多线程借还。
