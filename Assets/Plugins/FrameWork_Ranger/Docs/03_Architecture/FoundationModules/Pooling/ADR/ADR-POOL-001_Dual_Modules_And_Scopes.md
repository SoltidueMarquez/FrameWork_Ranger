# ADR-POOL-001：双 Module 与双 Scope

- 状态：已接受
- 日期：2026-08-31
- 所属阶段：Pooling

## 背景与约束

引用对象需要跨场景复用，GameObject 实例和借出泄漏必须随场景边界确定性销毁。未来 Event Center 不应因复用事件载荷而依赖 GameObject、Transform 或 Resource。

## 决定

在一个 `BaseModules/Pooling` 垂直胶囊中建立两个 Module：Global `ReferencePoolModule` 与 Scene `GameObjectPoolModule`。二者共享 Core Runtime 算法，但没有 Module 级互相依赖。GameObject Module 精确依赖 ResourceModule；未来 Event Center 只依赖 Reference Runtime。

## 影响与非目标

同一产品能力拥有两个可独立安装的作用域门面，换来清晰生命周期和最小依赖。首版不提供把两者重新包成全局 `PoolingModule` 的聚合门面。

## 验证

配置校验、精确依赖 EditMode 测试以及场景替换 PlayMode 测试。
