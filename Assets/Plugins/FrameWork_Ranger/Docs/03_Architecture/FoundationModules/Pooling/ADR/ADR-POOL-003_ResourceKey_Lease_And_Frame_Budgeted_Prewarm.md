# ADR-POOL-003：ResourceKey、Lease 与分帧预热

- 状态：已接受
- 日期：2026-08-31
- 所属阶段：Pooling

## 背景与约束

GameObject 池需要同步热路径，但 Prefab 可能来自 Resources 或 Addressables。直接引用后端 API 会破坏 Resource Management 的路由和释放所有权；一次性大量预热会形成首帧尖峰。

## 决定

每个配置池只保存后端与 Location 并生成 `ResourceKey`。Scene Load 串行取得 `ResourceLease<GameObject>`，在全部池合计每帧最多 10 个实例的预算下预热；Scope 只有预热完成后进入 Ready。Lease 保持到 Scene 卸载，缩容到零也不提前释放。

## 影响与非目标

运行期 Spawn 不等待异步资源，也不在后端间回退。Load 任一步失败会逆序销毁已建池并释放 Lease。首版不做后台预热、动态注册或整池空闲卸载。

## 验证

PlayMode 覆盖帧预算、双后端、失败回滚和场景卸载；Standalone Smoke 覆盖双后端 Spawn/Despawn。
