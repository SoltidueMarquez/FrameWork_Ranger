# Event Center 使用与验收

2026-09-09 从已确认设计恢复实施。生产代码位于 `BaseModules/EventCenter/Runtime`，命名空间为 `FrameWork_Ranger.Events`。

## 配置与依赖

`Configuration/DefaultEventModule.asset` 已加入当前 `Resources/FrameworkGlobalConfig.asset`，沿用原有 Reference Pool 配置。Event 只能安装于 GlobalScope，声明 `ReferencePoolModule` 为必需依赖；Core 按依赖先加载引用池，关闭时先等待 Event 再卸载引用池。Event 不配置 Handler 或重复的池容量字段。

Runtime 程序集 `FrameWork_Ranger.BaseModules.EventCenter.Runtime` 直接引用 Framework Runtime、Pooling Reference Runtime 与 UniTask。Samples、EditMode Tests 和 PlayMode Tests 单向依赖 Runtime。Core 只新增两个测试程序集的友元声明，生命周期实现未修改。

## 业务事件

每个业务事件独立一个文件，继承 `EventBase`，提供公共无参构造函数。推荐 sealed，通过只读属性暴露数据，在 `OnReturn` 中清空数据和对象引用。示例实现见：

- [LevelChangedEvent.cs](../../../../BaseModules/EventCenter/Samples/Runtime/LevelChangedEvent.cs)：带实体 ID、前后等级的通知。
- [ResourcesReloadedEvent.cs](../../../../BaseModules/EventCenter/Samples/Runtime/ResourcesReloadedEvent.cs)：无载荷通知，不调用资源后端。
- [EventCenterSampleView.cs](../../../../BaseModules/EventCenter/Samples/Runtime/EventCenterSampleView.cs)：启用时等待框架就绪并订阅，停用时取消等待并注销。

```csharp
using FrameWork_Ranger.Events.Samples;

// 在框架就绪后打开面板或启用组件。
LevelChangedEvent.Subscribe(OnLevelChanged);
LevelChangedEvent.Throw("hero", 2, 3);
// 关闭面板或停用组件时成对注销。
LevelChangedEvent.Unsubscribe(OnLevelChanged);

void OnLevelChanged(LevelChangedEvent payload)
{
    int level = payload.CurrentLevel;
    // 将所需值复制给界面或异步工作，不缓存 payload。
}
```

以上是调用片段。完整可挂载组件见示例源码。带捕获的 lambda 需要保存原委托用于注销；另写一个外观相同的 lambda 不代表同一订阅。

使用 Event 的业务 Module 应在已有 `RequiredModuleTypes` 列表中包含 `typeof(EventModule)`，加载时经 `Context.GetModule<EventModule>()` 获取依赖。普通组件使用 `Framework.WhenReadyAsync(cancellationToken)`；取消 token 属于本次启用周期，不能只绑定销毁而忽略停用。

## 最小体验流程

1. 在当前正常由 FrameworkBootstrap 启动的场景中新建 GameObject，添加 `EventCenterSampleView`。
2. 进入 Play Mode，组件就绪并完成订阅后，在组件上下文菜单选择“发送等级变化”或“发送重载完成”，Console 显示接收结果。
3. 停用该对象，从其他发送方调用静态 Throw，组件不再收到；重新启用后可重新订阅。

样例未另建场景或修改 Build Settings。它复用当前项目启动配置，也没有 Addressables 或 GameObject Pool 依赖。

## 行为边界

- 精确类型路由，相同委托去重，按注册顺序执行；注销幂等，重新订阅排在末尾。
- 一次派发固定监听列表进入边界；注销即时生效，新增只参加后续新发起的发送。嵌套发送可看到新增监听，外层边界保持不变。
- 每次 Publish 独立借出载荷，包括无人监听的发送；初始化和回调结束后归还。初始化错误传播，监听错误带原异常记录后继续，归还失败不重试，初始化与归还同时失败以 AggregateException 保留两项错误。
- 未加载的 Subscribe/Throw 报错，不缓存早期请求；卸载后 Unsubscribe 无操作，不创建中心。
- 关闭时清空后续监听并拒绝新请求；活动计数覆盖 Rent、初始化、派发和 Return。回调内发起 ShutdownAsync 后必须让同步回调返回，不能阻塞主线程等待自己的发送结束。
- 不提供 Token、自动生命周期绑定、优先级、Once、跨线程投递或延迟队列；初始化 lambda 可能分配闭包，不承诺零 GC。

## 验证

本次通过项目 `Tools/UnityCli.ps1` 在当前工作区执行验证，Unity 版本为 6000.5.9f1，未使用隔离副本：

| 检查 | 实际结果 | 本机证据（相对工程根） |
| --- | --- | --- |
| Doctor | 通过，版本匹配、工程未占用 | 本轮 CLI 输出 |
| Import | 退出码 0，编译通过 | `Logs/UnityCli/20260909-145115-import-12172/import.log` |
| Event EditMode | 7/7，通过，退出码 0 | `Logs/UnityCli/20260909-145210-testeditmode-33916/editmode-results.xml` |
| Event PlayMode | 11/11，通过，退出码 0 | `Logs/UnityCli/20260909-145331-testplaymode-4832/playmode-results.xml` |
| 既有架构目录检查 | 1/1，通过，新程序集加入后目录诊断为空 | `Logs/UnityCli/20260909-145442-testeditmode-33980/editmode-results.xml` |

首轮 Import 因新测试程序集缺少 `Sirenix.Serialization.dll` 显式引用而失败，按既有测试 asmdef 配置修正后重新编译通过；Runtime 行为测试首轮均通过。测试 XML 已确认非零用例和 `failed=0`。完整进度与日志位置以 [progress.json](../../../../../../../.workflow/event-center/progress.json) 为准。

测试覆盖通道去重、派发中增删与嵌套、异常隔离、实际资产接线、真实引用池异常借还、主线程限制、Global 场景保持、Scene 误装拒绝、模板隔离、回调内关闭和样例启停。未扩展为历史 Pooling 全验收或 Player/Addressables 构建。

## 需求核对

| 有效需求 | 实现与证据 |
| --- | --- |
| REQ-001/002/005 | 保留前期 HTY/YokiFrame 研究及用户确认；带载荷等级变化和无载荷重载通知示例承接 HTY 场景 |
| REQ-003/004 | SO 与 Runtime 分离，仅依赖 Reference Pool；配置、模板隔离、真实借还与关闭顺序测试通过 |
| REQ-006/008/009 | 每事件独立静态入口、Global 中心和手动注销；静态调用、场景保持、组件启停测试通过 |
| REQ-010/011/012/013 | 主线程同步借还、去重、有序订阅、异常隔离；EditMode 通道及 PlayMode 借还/线程测试通过 |
| REQ-014/015/016 | 即时注销与新增边界、独立载荷嵌套、未加载报错和卸载后幂等清理；相关测试通过 |

WI-001 至 WI-003 的研究设计结果保持有效；本轮交付 WI-004 实施与 WI-005 验证。未额外创建 Event Editor 页面或独立场景，复用现有 Framework Center 元数据展示与启动路径。
