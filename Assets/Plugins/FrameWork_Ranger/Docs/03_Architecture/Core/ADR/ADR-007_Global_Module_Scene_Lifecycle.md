# ADR-007：Global 模块的可选 SceneScope 生命周期

- 状态：已接受并实现，验证见 [UI 验收](../../FoundationModules/UI/05_Implementation_And_Acceptance.md)
- 日期：2026-09-09
- 补充：ADR-005、ADR-006

## 决定

用户确认 UI 只安装一个 GlobalUIModule；场景 UI 仍随实际 SceneScope 结束清理。增加 ISceneScopeLifecycle，由已加载 Global 模块可选实现，Runtime 调用，Core 不引用 UI。

新配置及模块图预检通过、旧 Scope 结束后，创建新 Scope 身份。Starting 按已加载 Global 顺序，在 Scene Driver/模块加载前调用；进入前登记参与者。Ending 在 Scene Driver/模块卸载前逆序调用所有已进入参与者，包含 Starting 中途失败者。失败回滚与正常卸载共用配对清理；单个 Ending 异常收集后继续。空 SceneScope 同样通知。

FrameworkSceneScopeInfo 保存场景 Handle、路径与轮次，对象身份区分不同 Runtime。此身份不开放 Global 查询 Scene 模块。Starting 令牌只控制建立过程，不能代替长期服务的 Scope 存活令牌；新配置预检失败时旧 Scope 及其 UI 保留。

不替换业务 Driver、不依赖 EventModule 广播、不添加 Scene UI 代理模块。参与者不得在回调内发起并等待同一 Runtime 队列的场景切换或 Shutdown。

## 验证

2026-09-10 补充：新增可选 `ISceneScopeReady`，不增加旧 ISceneScopeLifecycle 实现者负担。只通知已经进入 Starting 的参与者，通知位于 Scene 模块与 Driver.AfterScopeLoad 之后、Scope 宣布就绪之前。UI 在此执行按目录配置的自动打开；必须成功失败直接沿已有 SceneScope 回滚路径清理，Global 保留。空 Scope 同样通知，Ready 回调也不能重入并等待同一 Runtime 切换队列。新增测试检查 Driver/Ready 顺序与失败配对清理，实际结果见 UI v2 验收。

聚焦测试覆盖空 Scope、失败 Starting 配对、逆序清理异常、实际 Runtime 切换与配置预检失败保留。结果以实际 CLI 输出与 UI 任务记录为准。
