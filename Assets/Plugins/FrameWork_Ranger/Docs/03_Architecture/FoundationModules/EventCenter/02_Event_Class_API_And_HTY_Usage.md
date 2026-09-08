# 事件类接口与 HTY 使用场景

2026-09-05 第二轮设计。当前用户已明确：主要参考 LyingBottle 中 HTY 的真实用法，每个事件单独一个类，事件类提供静态订阅方法，业务按需在代码中订阅。有效要求见 `.workflow/event-center/requirements.json` 的 REQ-005、REQ-006。

本页保留 HTY 使用场景与事件类示例。第四至第六轮已完成主要公共行为问答，完整设计以[架构与公共契约](./03_Architecture_And_Public_Contracts.md)为准，实施范围见[最小实施计划](./04_Implementation_Plan.md)。当前尚未生成运行时代码。

## 1. 参考源码中的真实流程

以下路径相对于 `D:/unityhub/UnityProjects/LyingBottle/Assets/Scripts/Game/`。本轮只读参考源码，没有修改参考工程。

| 场景 | 事件定义与发送方 | 接收方与生命周期 |
| --- | --- | --- |
| 数值升级后刷新界面 | `Modules/NumericalResources/EventHandler/ViceBottleDataLevelUpEventHandler.cs` 保存副瓶键、数据项和前后等级；`Handler/DefaultNumericalResourcesHandler.cs` 更新数值后调用静态 Throw | `Modules/UI/BottleUI/UpgradeUI/UpgradeUI.cs` 在 OnOpen 订阅，OnClose 注销；回调筛选当前副瓶并 RefreshList |
| 跨 UI 模块切换显隐 | `Modules/UI/BottleUI/EventHandler/BottleUIVisibleChangedEventHandler.cs` 携带 visible；`Modules/UI/LevitatedModeUI/LevitatedModeUI.cs` 调用 Throw(visible) | `Modules/UI/BottleUI/BottleUIBase.cs` 成对订阅和注销，收到后处理显隐 |
| 数据整体重载后的无载荷通知 | `Modules/NumericalResources/EventHandler/NumericalResourcesReloadedEventHandler.cs` 为独立空载荷类；数值 Handler 重载后 Throw() | `Modules/UI/Common/NumericalResourceTip/NumericalResourceUI.cs` 在 RegisterEvents/UnregisterEvents 中配对订阅，收到后重建内容列表 |
| 窗口尺寸稳定后刷新画质 | `Modules/Window/EventHandler/WindowScreenSizeSyncedEventHandler.cs` 携带 size/source；`Modules/Window/WindowScreenSync.cs` 调用 Throw | `Modules/RenderQualityControl/Handler/DefaultRenderQualityControlHandler.cs` 在 Init/UnInit 配对订阅；回调复制尺寸和来源，随后在 Update 里延迟刷新 |

已核对的事件类以静态 **Throw** 封装借出、填充、发送；订阅通过 **EventManager.Instance.Subscribe<T>** 完成。用户本轮要求事件类静态订阅，因此本项目拟在独立事件类上提供 Subscribe，并配套 Unsubscribe；不把此新增接口误报为上述参考类已有的方法。

窗口用例还说明：接收方可以在回调中复制需要的数据，稍后处理；这不要求事件中心把池化载荷一直保留到下一帧。

## 2. 推荐的业务用法

事件类定义在拥有该业务协议的模块或共享业务契约目录，每个类单独一个文件。框架提供通用事件基类和事件中心；不把游戏具体事件集中写进框架 Runtime。

以 `LevelChangedEvent.cs` 为示意，调用方只需要知道这个事件类：

```csharp
// 下面是拟定 API 用法，尚未实现。
// 监听方：在面板打开且框架模块已就绪时调用。
LevelChangedEvent.Subscribe(OnLevelChanged);

// 发布方：业务数值修改成功后发送。
LevelChangedEvent.Throw(entityId, previousLevel, currentLevel);

// 监听方：面板关闭时移除同一个回调。
LevelChangedEvent.Unsubscribe(OnLevelChanged);
```

回调建议使用 `Action<LevelChangedEvent>`，可以是监听对象的实例方法。静态的是事件类上的订阅入口，不要求业务回调也声明为 static。

同一事件允许多个需要它的模块订阅。每个订阅者按载荷中的实体 ID、业务键等筛选自己关心的数据。业务条件何时满足、是否要刷新界面由接收方决定。

## 3. 单个事件类的职责与接口草案

建议每个事件类集中提供事件数据、Subscribe、Unsubscribe、Throw 和池归还时的数据清理。以下展示一个完整事件类的设计形状；`EventBase`、`EventModule` 是待实现类型，不能作为已存在 API 调用。

```csharp
using System;
using FrameWork_Ranger;
using FrameWork_Ranger.Events;

/// <summary>某个实体等级变化的通知载荷。</summary>
public sealed class LevelChangedEvent : EventBase
{
    /// <summary>发生变化的实体。</summary>
    public int EntityId { get; private set; }

    /// <summary>变化前的等级。</summary>
    public int PreviousLevel { get; private set; }

    /// <summary>变化后的等级。</summary>
    public int CurrentLevel { get; private set; }

    /// <summary>按需注册接收本事件的回调。</summary>
    public static void Subscribe(Action<LevelChangedEvent> callback)
    {
        Framework.GetModule<EventModule>().Subscribe(callback);
    }

    /// <summary>结束监听；模块已卸载时无需再次清理。</summary>
    public static void Unsubscribe(Action<LevelChangedEvent> callback)
    {
        if (Framework.TryGetModule<EventModule>(out var events))
        {
            events.Unsubscribe(callback);
        }
    }

    /// <summary>发送一次等级变化，载荷由中心负责借还。</summary>
    public static void Throw(int entityId, int previousLevel, int currentLevel)
    {
        Framework.GetModule<EventModule>().Publish<LevelChangedEvent>(e =>
        {
            e.EntityId = entityId;
            e.PreviousLevel = previousLevel;
            e.CurrentLevel = currentLevel;
        });
    }

    /// <summary>归还引用池时清空本次发送的数据。</summary>
    public override void OnReturn()
    {
        EntityId = default;
        PreviousLevel = default;
        CurrentLevel = default;
    }
}
```

`EventBase` 拟实现现有 `IReferencePoolItem`，提供默认空 OnRent，要求具体事件明确 OnReturn。无载荷通知仍使用单独的事件类，例如 `ResourcesReloadedEvent.Throw()`；不需要为无数据事件传入 null 载荷。

示例采用初始化委托表达数据填充，目的在于清楚展示统一借还责任；捕获业务参数可能分配闭包，本稿不承诺零 GC，也不据此增加复杂的无分配重载。正式实现可根据真实调用频率调整内部填充方式。

## 4. 中心与静态入口的协作

已确认首版 EventModule 仅安装在 GlobalScope。事件类静态方法仅查询当前已加载模块并转发；监听表和池依赖保存在模块运行实例中。静态事件类不另建监听表、不缓存模块克隆，也不隐式启动框架。

理由是现有 `Framework.GetModule<T>()` 按 Scene 优先、Global 后备查询（`Runtime/Core/FrameworkRuntime.cs`）。若同一个 EventModule 同时安装于两种 Scope，静态入口会随场景变化解析到不同中心。采用 Global 唯一安装可让订阅与注销始终落到同一个中心，且无须修改 Core 查询规则。此 Scope 选择已在第四轮确认。

依据已确认的同步派发和载荷借还责任，拟采用以下发送过程：

1. 从声明依赖的 Global ReferencePoolModule 借出当前事件类型。
2. 在 try 内填充数据并调用该事件类型的监听器。
3. 在 finally 中归还事件；初始化失败、无人监听也必须归还。

所有监听器只在本次回调内借读载荷。接收方需要延迟处理时复制数据；引用字段若指向可变业务对象，复制引用本身并不构成状态快照。

## 5. 已确认行为与使用约定

生命周期配对、主线程同步、载荷借还、重复订阅去重、注册顺序、监听异常隔离、派发中增删、同步嵌套及未加载调用均已确认。下表摘要业务使用方式，精确边界以完整契约为准：

| 主题 | 行为与用法 |
| --- | --- |
| 生命周期 | 业务在真实需要监听时调用 Subscribe，并在对应结束点调用 Unsubscribe。UI 可按 Open/Close，组件按启用/停用，Module/Handler 按加载/卸载配对 |
| 就绪时机 | Subscribe/Throw 在中心未加载时明确报错，不缓存订阅或事件；普通组件的 OnEnable 可能早于框架就绪，需要结合 Framework.WhenReadyAsync 与该组件取消/停用处理 |
| 重复订阅 | 同一事件类型下的相同委托去重；重复 Unsubscribe 无副作用，与 HTY 的普通业务用法接近 |
| 委托保存 | 方法组方便成对注销；使用捕获 lambda 时保留原委托，不能重新构造一个看似相同的 lambda 作为注销依据 |
| 派发规则 | 主线程同步、按注册顺序；新增监听不参与已开始的派发，已注销但未执行的监听跳过；允许同步嵌套，各次发送分别确定监听边界并独立借出载荷 |
| 监听异常 | 记录单个回调异常并继续剩余监听，不向发送方重新抛出该监听异常；初始化与清理失败单独处理并保留原因，具体设计见完整契约。所有成功借出路径均须收尾归还 |
| 卸载 | 业务结束先注销自己的监听；中心卸载清空全部监听；Global 中心不会因为某面板或某场景结束就自动知道该释放哪些业务订阅 |

“相同回调”按委托等价判断：同一对象的同一方法重复订阅只保留一份；不同对象上的同名方法各自保留。无监听变更时，A、B、C 依次注册就依次执行；B 抛异常只终止 B 本次回调，记录错误后仍执行 C。事件通知完成不等于每个监听器的业务都执行成功。

YokiFrame 的监听增删处理继续作为内部机制参考。第一版采用已确认的全局中心、普通手动注销和同步发送；用户已选择普通订阅，不增加 priority 或 SubscribeOnce。

## 6. 后续落实范围

当前公共行为问答已完成，后续按完整契约和实施计划落实 EventBase、Global EventModule、订阅容器和最小使用示例。EventModule 拟采用 DirectModuleBase，内部存储可以在保持约定行为的前提下调整。

实际实施后用同一条“打开 → 订阅 → 发送 → 回调 → 关闭 → 注销 → 再发送不再收到”流程证明按需订阅，再检查初始化异常借还、派发中增删与中心卸载。当前只核对参考源码、设计文本、JSON 和链接；未运行 Unity 编译或测试。
