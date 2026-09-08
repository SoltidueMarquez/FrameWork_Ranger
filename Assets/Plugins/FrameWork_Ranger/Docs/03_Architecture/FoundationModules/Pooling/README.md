# Pooling 模块入口

> 状态：双 Module 实现、Editor、测试与 Sample Builder 已落盘；最终 Unity CLI 全门禁以验收页记录为准。  
> 日期：2026-08-31

本目录是 FrameWork_Ranger 对象池/引用池模块的权威事实源。实现位于 `BaseModules/Pooling`，由 GlobalScope `ReferencePoolModule` 与 SceneScope `GameObjectPoolModule` 组成；二者共享严格所有权、批量扩容和循环缩容算法，但保持程序集与依赖方向独立。

## 文档

1. [需求简报](./00_Requirement_Brief.md)
2. [参考研究](./01_Reference_Research.md)
3. [架构与公共契约](./02_Architecture_And_Public_Contracts.md)
4. [逐脚本实施计划](./03_Implementation_Plan.md)
5. [验收与复盘](./04_Acceptance_And_Review.md)
6. [ADR-POOL-001：双 Module 与双 Scope](./ADR/ADR-POOL-001_Dual_Modules_And_Scopes.md)
7. [ADR-POOL-002：严格所有权与循环缩容](./ADR/ADR-POOL-002_Strict_Ownership_And_Cyclic_Shrink.md)
8. [ADR-POOL-003：ResourceKey、Lease 与分帧预热](./ADR/ADR-POOL-003_ResourceKey_Lease_And_Frame_Budgeted_Prewarm.md)

## 当前边界

- `ReferencePoolModule` 只能安装于 GlobalScope；未来 Event Center 只依赖它。
- `GameObjectPoolModule` 只能安装于 SceneScope，并只依赖 Resource Management Runtime 的 `ResourceModule`、`ResourceKey` 与 `ResourceLease<GameObject>`。
- 对象的借出与归还使用严格引用身份；外来、重复、错池和空引用不会静默成功。
- `MaxRetained` 只限制空闲缓存；借出数量不设硬上限。
- Prefab Lease 持有到 SceneScope 卸载；缩容到零不会释放模板。
- 首版不含动态注册、后台预热、Component 泛型入口、集合池、ScriptableObject 池、CSV、反射修补、事件中心或多线程借还。
