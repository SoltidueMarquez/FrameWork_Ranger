# Resource Management 需求简报

> 日期：2026-08-19<br>
> 状态：已确认并实现。

## 目标与调用方

资源管理模块为游戏业务提供统一的异步资源获得与释放入口，同时让调用方明确知道资源来自 Unity Resources 还是 Addressables。首批正式调用方是后续 GameObject Pool；当前垂直切片使用 Prefab 证明双后端行为。

开发者可观察到：相同键与精确类型的并发加载只执行一次底层请求；每个调用者获得独立 Lease；最后一份 Lease 归还后框架缓存消失。

## 必须支持

- Global 生命周期与既有 Scope 回滚。
- Unity Resources、Addressables 1.22.3 两个必需 Provider。
- 显式 `ResourceKey` 后端路由，不使用裸字符串重载。
- 异步 Acquire、独立取消、失败可重试、single-flight 和引用诊断。
- Addressables Handle 确定性释放；Resources 只解除框架引用。
- 配置诊断、Framework Center 只读页面、双后端 Sample、EditMode/PlayMode/Player 验收。

## 明确不实现

同步加载、后端自动回退、场景加载、Label/批量加载、进度、预加载、远端 Catalog/CDN、热更新、TTL/LRU、强制 `UnloadAsset` 或 `UnloadUnusedAssets`。

## 生命周期与错误

- 两个 Provider 按 Resources、Addressables 顺序初始化；任一失败都使 Global 加载失败并回滚。
- 无效 Key 抛 `ArgumentException`；缺少后端抛 `InvalidOperationException`；调用方取消保持 `OperationCanceledException`；底层失败统一为 `ResourceLoadException`。
- Shutdown 拒绝新请求、取消 Pending、报告未归还 Lease、使 Lease 失效、释放后端句柄并逆序关闭 Provider。
- Acquire、Lease 访问/释放和诊断只能在创建模块的 Unity 主线程执行。

## 验收目标

- 纯契约、缓存、single-flight、取消、失败重试、线程与配置验证通过 EditMode。
- Global 回滚、真实 Resources/Addressables、场景切换、Shutdown 和双后端 Sample 通过 PlayMode。
- 全框架回归、Addressables 本地内容、Windows Player 构建和命令行双后端冒烟全部通过。
