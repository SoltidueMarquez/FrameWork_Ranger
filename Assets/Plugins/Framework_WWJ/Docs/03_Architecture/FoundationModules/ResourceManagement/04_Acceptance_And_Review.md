# Resource Management 验收与复盘

> 日期：2026-08-19<br>
> 结论：Resource Management 双后端阶段通过；Pooling 与 Event Center 仍处于等待状态。

## 实际交付

- Global ResourceModule、ResourceHandler、ResourceStore、显式 ResourceKey 与独立 ResourceLease。
- Unity Resources 与 Addressables 1.22.3 两个隔离 Provider。
- 配置/构建诊断、Framework Center 只读页、Sample 构建页与双后端场景。
- Module SO、中央 Global 接线、第三个场景绑定、Build Settings、Addressables Settings/Group/Entry 和稳定 `.meta`。
- 命令行参数触发的 Sample Runtime Standalone 冒烟入口；正常游戏不触发。

## 自动测试结果

| 门禁 | 结果 |
| --- | --- |
| Resource EditMode | 22/22 Passed |
| Resource PlayMode | 5/5 Passed |
| 全框架 EditMode | 55/55 Passed |
| 全框架 PlayMode | 18/18 Passed |
| Unity C# 编译 | Passed，无 C# 诊断 |
| Addressables 本地内容 | Passed，20.81 s |
| StandaloneWindows64 Player | Passed，75,299,205 bytes，11.56 s，0 warning（最终重建） |
| Player 双后端冒烟 | Passed，退出码 0 |

Player 日志包含：`[Framework_WWJ][ResourceStandaloneSmoke] PASS 双后端 Acquire/Instantiate/Destroy/Release。`

## 配置与所有权检查

- `ResourceModule.asset` 持久化一个 ResourceHandler 和两个 Provider。
- GlobalConfig 保留 SampleGlobalClock 并追加 ResourceModule；SceneConfig 无 ResourceModule。
- FrameworkProjectSettings 保留 A/B 绑定并追加 Resource Sample；CoreSkeleton 实际重建后两项 Resource 接线仍存在。
- PlayMode 覆盖模板克隆、初始化失败回滚、重复 Provider 零副作用、场景切换保持同一 Global ResourceModule、Shutdown 使泄漏 Lease 失效。
- Resources 最后一份 Lease 只清理框架引用；Addressables 最后一份 Lease 只释放一次原生 Handle。

## 与计划的偏差

- 为了在已打开的 Unity 实例中完成门禁，测试和构建使用过一次性 Editor 启动器；所有启动器均在交付前删除。
- Addressables 首次内容构建会打开 Build Report 窗口；关闭该报告后构建结果正常返回。
- 增加了仅受专用命令行参数控制的 Standalone Smoke Runner，用于证明 Player 中真实双后端链路；它属于 Sample，不进入生产 Runtime。

## 已知限制

首版只正式验收 Prefab；不承诺 Resources 物理内存立即回收；不包含同步、回退、场景、批量、远端、热更新或驻留缓存策略。Addressables 与 Odin 自动生成的 Addressables 支持资产属于本次包接入的一部分。

## 后续

Resource 阶段关闭后，下一次对话可开始 Pooling 的需求确认；不得直接实现 Pooling，也不得让 Event Center 依赖 Resource 或具体池实现。

## Phase 1.4 后续集成记录

2026-08-20，Resource Management 的 25 个生产顶层类型已接入 Framework Center 分层代码架构目录，覆盖 Runtime、Editor、Unity Resources 与 Addressables；目录诊断与源码定位失败均为零。该改动只补充架构元数据和编辑器导航，不改变本页已验收的资源 Runtime 行为。完整证据见 [Phase 1.4 验收与复盘](../../EditorCenter/09_Phase1_4_Hierarchical_Architecture_Navigator_Acceptance_And_Review.md)。

## 2026-08-22 默认场景验收入口收敛

为便于用户直接运行并逐段理解 Resource 代码，当前验收入口已从三个框架示例场景收敛到 Unity 默认 `Assets/Scenes/SampleScene.unity`。CoreSkeleton A/B 场景及其专用 Module、Handler、SO、Editor 页面、构建器和测试已删除；Resource 专用场景与空 SceneConfig 也已删除。

`FrameworkGlobalConfig.asset` 保留原 GUID 并迁入 `Assets/Plugins/Framework_WWJ/Resources`，其唯一 Global 模块为 `ResourceModule.asset`。`FrameworkProjectSettings.asset` 的默认 SceneConfig 和场景覆盖表均为空，Build Settings 只包含默认场景；默认场景挂载 `ResourceManagementSampleView`。Resource 双后端 Prefab、Addressables Entry、交互验收代码、生产代码与正式测试均保留。

权威工程当时由 GUI Editor 打开，因此验证在由当前 `Assets`、`Packages`、`ProjectSettings` 创建的隔离副本执行：Import/C# 编译退出码 0，EditMode 68/68、PlayMode 17/17，Addressables 本地内容与 StandaloneWindows64 Player 构建退出码 0，Player 双后端冒烟退出码 0，并记录 `PASS 双后端 Acquire/Instantiate/Destroy/Release`。该清理只改变示例与验收入口，不改变 Resource Runtime 公共契约。
