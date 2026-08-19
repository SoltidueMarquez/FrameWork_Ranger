# Resource Management 模块入口

> 状态：Resource 阶段已实现并通过自动化、构建与 Standalone 双后端冒烟；Pooling 与 Event Center 尚未开始。

本目录是 Framework_WWJ 资源管理模块的权威事实源。Runtime 实现位于 `BaseModules/ResourceManagement`，模块采用显式后端键、独立 Lease、同键同类型 single-flight，并同时提供 Unity Resources 与 Addressables 1.22.3。

## 文档

1. [需求简报](./00_Requirement_Brief.md)
2. [参考研究](./01_Reference_Research.md)
3. [架构与公共契约](./02_Architecture_And_Public_Contracts.md)
4. [实施计划](./03_Implementation_Plan.md)
5. [验收与复盘](./04_Acceptance_And_Review.md)
6. [ADR-RM-001：垂直模块胶囊与集成边界](./ADR/ADR-RM-001_Vertical_Capsule_And_Integration_Boundaries.md)
7. [ADR-RM-002：Lease、single-flight 与即时缓存移除](./ADR/ADR-RM-002_Lease_Single_Flight_And_Immediate_Eviction.md)
8. [ADR-RM-003：显式后端路由且禁止回退](./ADR/ADR-RM-003_Explicit_Backend_Routing_Without_Fallback.md)
9. [ADR-RM-004：双后端初始化、取消与释放](./ADR/ADR-RM-004_Dual_Backend_Lifecycle_Cancellation_And_Release.md)

## 当前边界

- Global 唯一模块；SceneConfig 不得安装 ResourceModule。
- 公开调用只经由 `ResourceModule.AcquireAsync<T>(ResourceKey)`。
- 缓存身份为 `(Backend, Location, exact T)`。
- Resources 的 Release 只解除框架持有；Addressables 的 Release 归还原生 Handle。
- 不提供同步加载、自动回退、场景加载、批量/Label、进度、预加载、远端内容、热更新、TTL/LRU 或全局内存整理。

下一阶段如进入 Pooling，应只引用 Resource Runtime 公共契约，不引用两个具体后端程序集。
