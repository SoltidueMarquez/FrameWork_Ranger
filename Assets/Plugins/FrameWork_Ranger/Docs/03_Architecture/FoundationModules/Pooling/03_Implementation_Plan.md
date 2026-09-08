# Pooling 逐脚本实施计划

> 状态：已按用户批准方案实施

## Runtime

- `Core/Runtime/PoolCapacitySettings.cs`：默认值、复制与完整配置校验。
- `Core/Runtime/PoolReferenceEqualityComparer.cs`：严格引用身份。
- `Core/Runtime/TrackedPool.cs`：空闲栈、空闲/借出集合、事务扩容、回调失败清理、保留上限、循环缩容、外部丢失剔除与 Shutdown。
- `Reference/Runtime/*`：引用契约、类型覆盖、精确类型运行池、Global Handler、公开 Module 与诊断快照。
- `GameObject/Runtime/*`：回调契约、池定义、Prefab Lease 运行池、Scene Handler、公开 Module 与诊断快照。

## Editor

- `PoolingConfigurationValidator` 与 `PoolingBuildValidator` 共享作用域、依赖、类型、池名、ResourceKey 和容量诊断。
- `PoolingCenterPage` 展示双 Module 配置、Resource 依赖、运行数量、未归还量与缩容倒计时。
- `MaxRetained=-1` 合法但输出潜在无限空闲缓存警告。

## Tests 与 Sample

- EditMode 覆盖容量、预热、扩容、严格归还、有限保留、循环缩容、回调失败、Dispose、Clear 与 Shutdown。
- PlayMode 覆盖双 Scope 生命周期、姿态/激活/回调顺序、错池/重复归还、外部销毁、场景替换、主线程和每帧预热预算。
- Sample Builder 追加 Global Resource + Reference，并只把 GameObject Module 绑定到独立 Pooling 场景；创建 Resources/Addressables Prefab、独立 Group、Build Settings 与 Player Smoke。

## 验证顺序

1. `Import`
2. 聚焦 Pooling EditMode/PlayMode
3. 全框架 EditMode/PlayMode
4. `BuildPoolingSample`
5. Addressables 本地内容
6. StandaloneWindows64 Player
7. ResourceSmoke 与 PoolingSmoke

具体命令见 [Unity 6000 CLI](../../../04_Standards/Unity_6000_CLI.md)。
