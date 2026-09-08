# Event Center 最小实施计划

本计划承接[完整公共契约](./03_Architecture_And_Public_Contracts.md)。用户目前完成了需求与设计问答，本轮产物为契约和实施说明；以下 Runtime、资产与测试是后续实施工作，尚未创建或运行。

## 1. 实现文件与职责

正式代码根目录为 `Assets/Plugins/FrameWork_Ranger/BaseModules/EventCenter`。下列文件拆分对应实际职责，不要求先冻结每个私有字段或方法。

| 路径（相对于代码根） | 主要职责 |
| --- | --- |
| `Runtime/EventBase.cs` | 池化事件载荷基类，实现 IReferencePoolItem，定义 OnRent / OnReturn |
| `Runtime/EventModule.cs` | DirectModuleBase 门面，声明 ReferencePoolModule 依赖，验证 GlobalScope，衔接加载/卸载和公开订阅、发送方法 |
| `Runtime/EventRuntime.cs` | 每次发送的借还、线程/状态边界、事件类型表、活动发送计数和卸载收尾 |
| `Runtime/EventChannel.cs` | 单个 T 的监听顺序、委托去重、当轮边界、注销标记及嵌套派发；需要非泛型清理接口时在同一职责附近定义 |
| `Runtime/AssemblyInfo.cs` | 程序集架构说明和必要的测试可见性，遵守现有 FrameworkArchitecture 元数据惯例 |
| `Runtime/FrameWork_Ranger.BaseModules.EventCenter.Runtime.asmdef` | 引用 Framework Runtime、Pooling Reference Runtime、UniTask |
| `Configuration/DefaultEventModule.asset` | 可加入 GlobalConfig 的模块 SO 模板，不保存监听列表，不配置空 Handler |
| `Samples/Runtime/LevelChangedEvent.cs` | 一个带载荷、具有独立静态 Subscribe / Unsubscribe / Throw 的事件示例 |
| `Samples/Runtime/ResourcesReloadedEvent.cs` | 一个无载荷通知的同形示例 |
| `Samples/Runtime/EventCenterSampleView.cs` | 展示开始/停止监听、触发事件和观测回调的最小场景组件，正确处理等待就绪期间的停用 |
| `Samples/Runtime/FrameWork_Ranger.BaseModules.EventCenter.Samples.asmdef` | 示例单向依赖 Event Runtime 与必要的 Framework/UniTask 公共接口 |
| `Tests/EditMode/` | 派发、去重、增删、嵌套及异常借还的聚焦测试 |
| `Tests/PlayMode/` | 模块加载/卸载、Global 保持、模板状态隔离和组件订阅生命周期的聚焦检查 |

测试程序集只引用测试实际需要的 Runtime。采用工程既有测试方式，必要的 InternalsVisibleTo 仅用于测试夹具构造和观察状态，不扩展业务 API；具体测试文件数由可验收行为决定。

## 2. 配置和依赖接线

1. 创建 Event 模块模板及匹配 `.meta`，按现有命名和中文注释规范补充架构元数据。
2. 在当前 `Assets/Plugins/FrameWork_Ranger/Resources/FrameworkGlobalConfig.asset` 的模块条目中启用该模板；保留现有 Resource、Reference Pool 和其他用户配置。
3. EventModule 的 RequiredModuleTypes 声明 ReferencePoolModule，不依赖模块条目手工排序。Scene 配置若误装 EventModule，在其加载校验中明确拒绝。
4. 事件池容量沿用 Reference Pool 的默认和类型覆盖配置；Event 不另设一份重复容量配置。事件类型可惰性建池，不需要提前扫描和注册所有业务事件类。
5. 使用现有 Framework Center 的模块配置和架构展示，不为首版另建专用 Editor 工作台。

示例保持最小依赖：业务事件放在 Samples Runtime，使用现有框架启动方式。独立示例场景如有必要由样例工作项追加，不修改 Addressables Group 或引入资源后端；首版不需要新增 CLI 子命令。

## 3. 实施片段

### 片段一：事件通道与同步派发

实现 EventBase 和 EventChannel 的顺序监听、委托去重、增删生效规则及嵌套边界。以 A/B/C/D 的真实可观察调用顺序作为验证目标，不用仅镜像内部集合结构的测试。

通道可以在派发期间保留空项，在该类型最外层派发结束后稳定压缩。没有 Token，就不为监听节点增加租约代次系统，也不另建通用链表池。引用池首先用于用户已确认的事件载荷复用。

### 片段二：模块与引用池整合

接入 EventRuntime、Global EventModule 和既有 ReferencePoolModule。完成初始化失败后的归还、逐监听异常记录、无监听发送、主线程限制以及正常卸载。

核对回调内发起关闭的场景：停止新请求，等待正在执行的同步发送完成归还，然后允许 Core 继续卸载 Reference Pool。此等待局限于 EventModule 的既有异步卸载钩子，不修改 Core 操作队列或公共生命周期契约。

### 片段三：业务用法、模板与文档

补齐独立事件类示例和全局配置接线。用“打开 → Subscribe → Throw → 收到回调 → 关闭 → Unsubscribe → 再 Throw 不再收到”的完整流程展示使用方式。

带载荷示例对应 HTY 数值升级通知，无载荷示例对应重载完成通知。文档给出 Module 依赖声明、MonoBehaviour 等待就绪与取消、销毁前清理订阅，以及载荷只能在回调期间借读的约定。

## 4. 聚焦验证

使用 `Tools/UnityCli.ps1`，遵守[Unity CLI 规则](../../../04_Standards/Unity_CLI_Development_Rules.md)。当前仅为计划，以下检查均未运行。

| 验证范围 | 必须证明的结果 |
| --- | --- |
| 普通订阅与注销 | 同回调重复订阅仅执行一次；不同对象监听都有效；注销幂等，注销后重新订阅排到末尾 |
| 派发中增删 | A 移除 C、添加 D，本轮只执行 A/B；移除后重加不得让旧位置恢复参与本轮 |
| 同步嵌套 | 内层先完成再返回外层；同类嵌套载荷引用不同且外层值保持；新增监听只对后续新发起的发送可见 |
| 异常与引用池 | 初始化异常仍归还；某监听异常不阻止下一监听；无监听也完成借还；OnReturn 失败保留错误且不二次归还 |
| 就绪与线程 | 未加载的 Subscribe/Throw 明确报错，不产生早期缓存；就绪后可调用；错误线程请求被拒绝 |
| 生命周期 | Event 仅允许 Global；场景切换保持中心；模板不出现监听状态；卸载清除引用，卸载后注销无操作 |
| 回调中关闭 | 当前载荷在回调完成前不被池重置；所有活动发送归还后 Event 才结束卸载，随后 Reference Pool 关闭 |
| 首批使用流程 | 组件或面板结束监听后不再收到；等待框架就绪期间停用，不在停用后追加订阅 |

存在实际 C# 和配置改动后，先进行 Import，再运行 Event Center 的目标 EditMode / PlayMode 集合。例如在目标测试类存在后使用：

```powershell
./Tools/UnityCli.ps1 -Task Import
./Tools/UnityCli.ps1 -Task TestEditMode -TestFilter 'FrameWork_Ranger.Events.Tests'
./Tools/UnityCli.ps1 -Task TestPlayMode -TestFilter 'FrameWork_Ranger.Events.Tests'
```

最终过滤器以实际测试命名为准，零用例不算通过。依赖行为如失败，只处理与当前 Event 借还或卸载相关的问题。没有共享 Core 改动或资源后端需求时，不扩展为 Pooling 全验收、Addressables 内容构建或 Player 构建。

运行 Unity 前检查当前工程占用情况；不自动关闭用户 GUI Editor。需要隔离验证时遵守现有 CLI 规则并报告实际验证对象。

## 5. 交付与当前状态

实现完成后更新模块入口、实际 API 使用说明与验收结果，并在任务 JSON 中逐项关联需求和真实结果。事件监听、载荷归还和作用域清理三条链都应有实际证据。

本轮只完成设计整理和文件检查。Runtime、SO 模板、配置接线、示例与 Unity 验证仍属于待实施工作，不能依据本文宣称模块已经可用。
