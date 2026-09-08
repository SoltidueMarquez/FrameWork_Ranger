# 事件中心参考研究与设计起点

研究日期：2026-09-05。范围是本机参考源码静态检查；未运行两个参考工程的测试。以下分开记录源码事实、由代码推导的风险与 FrameWork_Ranger 候选设计，候选不代表用户确认。

第二轮用户已明确一事件一类、事件类静态订阅、业务按需订阅。当前接口草案见[事件类接口与 HTY 使用场景](./02_Event_Class_API_And_HTY_Usage.md)；本页第 5 节保留为首轮候选记录，其中 Token 主入口及使用场景待用户另行提供的建议已调整。

## 1. 本轮起点

用户要求重新开始事件中心，并参考 HTY 与 YokiFrame。当前工程未发现 Event Center Runtime，也没有此前的事件中心任务 JSON。新记录位于 `.workflow/event-center/`。

历史资料记录的“Event Center 依赖池化系统”仍保留，证据见[重建设计待办](../../../00_Project/09_Rebuild_Decision_Backlog.md)和 [ADR-POOL-001](../Pooling/ADR/ADR-POOL-001_Dual_Modules_And_Scopes.md)。重启不自动撤销该方向，也不自动批准旧候选接口。

## 2. 两个参考实现

HTY 根目录为 `D:/unityhub/UnityProjects/LyingBottle`，下表的 HTY 路径相对于 `Assets/Plugins/ActFramework_ByHZR/Packages/Main/`。YokiFrame 根目录为 `D:/unityhub/UnityProjects/YokiFrame`，其路径相对于项目根。

| 主题 | HTY 源码事实 | YokiFrame 源码事实 |
| --- | --- | --- |
| 业务入口 | `Event/EventManager.cs` 是模块门面，通过 `IEventHelper` 派发；业务通常经 Global 取得实例 | `Core/Runtime/EventKit/Facade/EventKit.cs` 提供静态 Type、Enum、兼容 String 总线；总线类也可以独立实例化 |
| 类型路由 | 载荷继承 `EventHandlerBase`；实例发送按 `handler.GetType()` 精确查找，强类型订阅包装为基类回调 | `Buses/TypeEvent.cs` 按泛型 `T` 对应的 `EasyEvent<T>` 查找，支持 struct/class；没有继承链广播 |
| 枚举与字符串 | 本次核对的 EventManager 以事件类型为入口 | `Buses/EnumEvent.cs` 按枚举类型、值及载荷类型区分容器；String 门面标记为过时兼容路径 |
| 池化对象 | 业务事件载荷来自 `ReferencePoolManager`；`DefaultEventHelper.Throw(instance)` 在 finally 中归还 | EventKit 不要求载荷实现 PoolKit 接口；`EasyEvent<T>` 使用 ToolClass 的池化链表保存监听器 |
| 订阅身份 | 强类型 Subscribe 对相同委托去重，Unsubscribe 按原委托查包装映射；业务门面未返回 Token | 每次 Register 新增节点并返回 `LinkUnRegister<T>`；相同委托可重复订阅，Token 定位一次订阅 |
| 监听顺序 | `Event/Handler/ActionData.cs` 按 priority 升序插入，同级按添加顺序 | `Events/EasyEventT.cs` 按注册顺序触发；当前接口没有 priority |
| 派发中增删 | ActionData 直接遍历可修改 List，没有固定尾边界 | 每次 Trigger 固定当次尾节点；新增监听不进入该轮；注销先置空，最外层派发结束后移除节点 |
| 异常 | ActionData 捕获单个监听异常并记日志，继续执行 | EasyEvent 捕获单个监听异常并交给 `EventKitErrorHandler` |
| 生命周期 | 业务依赖 Subscribe/Unsubscribe 配对，另有目标扫描与场景结束清理入口 | Token 由业务 owner 注销；Clear 清空总线，不能替代一个调用方的注销 |

YokiFrame 文档有一处需以源码为准：当前 `Documentation~/Api/02-Core/EventKit.md` 将 Type 总线描述为“payload 运行时类型为 key”，实际 `Send<T>` 查询的是 `EasyEvent<T>`。因此基类变量持有派生实例时，不应推断它自动命中派生事件监听器。

## 3. 应借鉴的机制与已发现边界

### HTY：载荷借还与业务接线

真实用例 `Assets/Scripts/Game/Modules/Window/EventHandler/WindowScreenSizeSyncedEventHandler.cs` 按“Spawn → 填 size/source → Throw”发送；`Disable()` 重置载荷。这个流程适合参考本项目的 `OnRent/OnReturn`。

`EventManager.Throw<T>(Action<T>)` 把借出与初始化集中到发送入口，但初始化委托在 helper 的 finally 之前执行。由此可推导：初始化抛异常时，该路径未执行到 helper 的归还逻辑。新实现若采用这种 API，必须把初始化也包含在借还的 try/finally 内。

`ActionData.Invoke` 的注释声称新增监听不参与本轮，但实际循环使用变化中的 `m_actions.Count`。例如 A 在回调中向尾部添加 B，B 可在同轮被访问。新设计应明确这种行为，并通过用例验证，不能沿用注释作为契约。

`ThrowSignal(instance)` 会保留实例；相同类型覆盖和 `ClearSignal()` 的当前代码没有归还旧实例。缓存信号涉及另一套持有时间和消费规则，是否需要应由实际需求决定。

`EventHub` 的参考约定是用于启动期，和业务 EventManager 分开。它虽有 IDisposable Token 与 OnOnce，但不能据此认定业务门面已有同样 API，也不能仅因存在锁就认定整体跨线程安全：多个 Throw/Clear 入口没有在同一锁内运行。其 OnOnce 在回调后注销；若新设计提供一次订阅，应明确递归派发时是否仍只能触发一次。

### YokiFrame：订阅身份与派发边界

`Lifetime/IUnRegister.cs` 的 Link Token 绑定具体 owner 和节点。`ToolClass/Collections/PooledLinkedListNode.cs` 与 `PooledLinkedList.cs` 使用 owner、节点代次验证租约，避免旧 Token 副本操作已复用的新节点。这是借鉴“池化订阅节点”时要处理的真实所有权问题。

`EasyEvent<T>` 的尾边界和触发深度使“新增、注销、嵌套发送”可以分别定义：外层已固定边界；嵌套发送重新取得自己的边界，因而可看到刚注册的监听。是否采用这一语义仍是本项目候选。

载荷池化和监听节点复用解决不同分配来源，不能把 YokiFrame 的节点复用误认为事件载荷已经借自引用池。

## 4. 本项目已有边界

- 现行 Core 使用 SO 模板与 Scope 运行克隆；由 `ModuleContext` 获取依赖。Global 模块不能访问 Scene 模块。一个 GlobalScope、最多一个活动 SceneScope，启动与场景所有权以 [ADR-006](../../Core/ADR/ADR-006_Central_Project_Settings_And_Scene_Ownership.md) 为准。
- 实际 `ReferencePoolModule.Rent<T>()` 约束为 `class, IReferencePoolItem, new()`；`Return` 拒绝外来或重复归还对象。契约为 `OnRent/OnReturn`，永久移除时另行调用可选的 IDisposable。
- `ReferencePoolHandler` 只接受 GlobalScope，借还要求主线程和就绪状态。事件模块使用它时必须声明依赖，并在池卸载前完成自己的清理。
- Event 依赖边界限定到 `Pooling.Reference.Runtime`；无需引入 GameObject、Resource 或 Addressables。引用池现有代码不等于本轮已完成依赖行为验证。

## 5. 首轮候选方案

推荐组合为：本项目 Module/Scope 接线 + HTY 的载荷借还 + YokiFrame 的强类型监听与独立注销令牌。

| 选择 | 当前建议 | 需要解决的公共行为 |
| --- | --- | --- |
| 事件标识 | 先以强类型事件为主 | 是否需要枚举/字符串入口、无载荷事件，及路由采用泛型类型还是实例类型 |
| 池化与发送 | 由事件中心统一借出、填充、同步派发、finally 归还载荷 | 监听器只借读；保存或异步处理须复制所需数据；初始化/监听/清理失败语义明确 |
| 订阅清理 | 每次订阅返回独立 Token；需要成组清理时由调用方拥有订阅集合 | 重复订阅是否分别计数；释放是否幂等；组件 Disable、Destroy 与场景卸载谁负责 |
| 作用域 | 先以 Global 模块服务跨模块通信 | 是否还需要 Scene 隔离总线；Global 总线存在不等于 Scene 订阅自动清理 |
| 派发 | 主线程同步，按注册顺序，采用明确的当轮边界 | 派发中增删、嵌套发送、异常后是否继续；是否真有 priority/Once 需求 |
| 扩展 | 按首批场景再决定缓存信号、队列与异步能力 | 不因参考实现具备某 API 就自动承诺实现 |

以上是待讨论的建议，尚未成为正式 API。尤其“池化载荷 class”和“允许任意 struct/class 值发送”影响业务写法，需要结合首批调用场景选择；不能把它们塞入同一个含糊的所有权约定。

若采用当前建议，正式代码放在 `BaseModules/EventCenter`，一个 Runtime 程序集直接引用 Framework Runtime 与 Pooling Reference Runtime 即可。EventModule 管理配置和作用域接入，运行对象管理事件槽与派发，Token 代表一次订阅。是否需要 Handler、Editor 页面或额外程序集，留待真实职责明确后决定。

## 6. 下一步与验证

先向用户确认第一版必须打通的具体使用流程，再整理事件标识、作用域、载荷所有权和订阅生命周期。后续接口设计可以同时给出订阅、发送、注销三个小例子，不先冻结私有实现。

实施后应针对确认的行为验证：发送命中与顺序、派发中新增/注销/嵌套、初始化或监听异常时借还平衡、Token 释放与旧 Token、Scope 结束清理。只有实际采用的契约进入验证范围；不因此扩展为整个 Pooling 的历史验收。

本轮仅进行源码检查及文档、JSON、链接和 .meta 检查，未运行 Unity 编译或测试。
