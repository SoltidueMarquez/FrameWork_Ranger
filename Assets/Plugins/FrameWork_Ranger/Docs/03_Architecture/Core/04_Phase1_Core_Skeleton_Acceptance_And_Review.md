# Framework_WWJ 第一阶段验收与复盘

> 历史说明：本文是 Phase 1 的验收基线。文中的 `FrameworkEntry`、Entry 所有者和 9/9、11/11 测试计数已被 Phase 1.1 取代；当前结果见 [Phase 1.1 验收与复盘](../EditorCenter/03_Phase1_1_Acceptance_And_Review.md)。

> 状态：已完成验收  
> 验收日期：2026-08-07  
> Unity：2022.3.62f3  
> 范围：模块模型、驱动骨架、编辑器诊断、测试与最小 A/B 场景示例。

## 1. 交付结论

第一阶段骨架已经实现并通过测试。当前项目包含 32 个 Runtime 脚本、3 个核心 Editor 脚本、11 个测试脚本、7 个示例 Runtime/Editor 脚本和 6 个独立程序集。旧 `Main`/`Utils` 未恢复，资源、对象池、音频等正式业务模块未进入本阶段。

已交付能力：

- Odin SO 模块模板、GlobalConfig/SceneConfig 与运行时克隆隔离。
- Direct Module 与 Module + 多态 Handler 两种实现方式。
- 精确类型依赖、共享图校验、稳定拓扑排序和中文诊断。
- 一个 GlobalScope、一个可替换 SceneScope、Entry 所有者令牌和串行结构操作。
- UniTask 串行 Load、反向 Unload、失败回滚、聚合清理异常与确定性 Shutdown。
- Update/FixedUpdate/LateUpdate 显式能力接口和单目标异常隔离。
- 静态只读门面、受限 ModuleContext 与 Driver 前后置钩子。
- Odin/IMGUI Inspector 诊断与只读依赖图。
- 两个使用相同 GlobalConfig、不同 Handler 的可切换示例场景。

## 2. 验证证据

为避免关闭用户当前打开的 Unity，最终验证在只包含 `Assets`、`Packages`、`ProjectSettings` 的临时项目副本中，使用 Unity 官方 `-runTests -batchmode` 执行。临时副本不写回正式项目。

| 测试集 | 结果 | 用例 | 失败/跳过 | NUnit 时长 |
| --- | --- | ---: | ---: | ---: |
| EditMode | Passed | 9 | 0 / 0 | 0.165 s |
| PlayMode | Passed | 11 | 0 / 0 | 0.377 s |

最终批处理日志没有 `warning CS`、`error CS` 或 `Scripts have compiler errors`。覆盖内容包括：

- 依赖方向、稳定排序、缺失/禁用/重复/自依赖/循环诊断。
- SO 克隆与内嵌 Handler 克隆、模板状态隔离、销毁后模板仍有效。
- Direct/Handler 生命周期状态、Context 可见性、Handler 绑定释放和卸载异常继续清理。
- Awake 自动启动、Global→Scene 顺序、查询与就绪等待。
- SceneScope 替换、迟到 Detach、GlobalConfig 冲突、配置错误零克隆、失败回滚和重试。
- 原始异常经 `LastException` 与 `WhenReadyAsync` 传递。
- Shutdown 重复调用、克隆/Host 销毁，以及 Shutdown 状态拒绝就绪等待。
- Update/FixedUpdate/LateUpdate、Global→Module→Handler 次序、场景过渡 Tick 和异常隔离。
- 从 Build Settings 真实加载 `CoreSkeleton_A`，切换到 `CoreSkeleton_B`，Global 克隆 ID 不变且 Handler 从 `SampleCounterHandler` 切换为 `SamplePulseHandler`。

## 3. 实现中发现并修正的问题

### 3.1 未观察的就绪失败

失败 Scope 已正确回滚，但在没有外部调用者等待 `WhenReadyAsync` 时，UniTask 失败完成源会在稍后 GC 时再次发布未观察异常，可能污染后续场景或测试。

修正方式：Runtime 为失败的就绪完成源增加内部静默观察者，只消费 UniTask 的观察标记；原始异常仍保留在 `LastException`，未来的 `WhenReadyAsync` 仍会抛出同一个异常。新增测试同时验证异常对象一致性。

### 3.2 示例资产落盘

示例配置和场景由一次性 Editor 构建流程创建，以确保 Odin 序列化与 Unity GUID 正确。一次性自动执行入口已移除，正式代码只保留 `Tools/Framework WWJ/Rebuild Core Skeleton Sample` 显式菜单。

## 4. 与计划的偏差

- 新增一项真实示例场景集成测试；原计划的测试工厂 Entry 替换测试不足以单独证明 Build Settings 场景接线。
- 示例使用 IMGUI `OnGUI`，不需要 Camera 或 EventSystem，因此场景只保留 FrameworkEntry、SampleView 和说明对象。
- Build Settings 实施前没有已有场景条目；最终只追加并启用 A/B 两项，没有删除或重排用户条目。
- 没有引入日志抽象、DI、GraphView 或运行时动态增删模块。

## 5. 已知边界与遗留问题

- 只支持一个当前 SceneScope；Additive 多场景并存尚未定义所有权。
- 模块类型在活动框架内唯一，不支持接口绑定或同类型多实例。
- 加载严格串行，尚未提供并行依赖批次。
- 应用退出只能触发尽力而为的异步 Shutdown；业务关键持久化不能依赖退出时异步完成。
- Inspector 图是只读 IMGUI 布局，没有缩放、拖拽或自动修复。
- 尚未实现任何正式业务模块，也没有选择下一阶段的游戏垂直切片。

## 6. 下一阶段输入

下一阶段仍需用户确认：第一个可验证游戏目标、最少业务模块、场景模型、资源类型和验收标准。候选模块不因第一阶段完成而自动进入实现。

相关依据：

- [第一阶段实现计划](./03_Phase1_Core_Skeleton_Implementation_Plan.md)
- [ADR 目录](./ADR/README.md)
- [重建设计待办](../../00_Project/09_Rebuild_Decision_Backlog.md)
