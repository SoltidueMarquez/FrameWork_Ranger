# ADR-RM-004：双后端初始化、取消与释放

- 状态：已接受
- 日期：2026-08-19
- 所属阶段：Resource Management

## 背景

Resources 与 Addressables 的取消和内存释放能力不同，但模块对调用者需要一致的所有权与失败入口。

## 决定

当前模板要求两个 Provider 都可初始化；顺序为 Resources、Addressables，失败则逆序回滚。单个等待者取消只结束自己的等待；全部取消时废弃 Pending。Addressables 释放未移交或最终归还的原生 Handle；Resources 无法中止 ResourceRequest，迟到结果不缓存，Handle Dispose 只清空框架引用。

模块不会调用 `Resources.UnloadAsset` 或 `Resources.UnloadUnusedAssets`，因此缓存为零不等于物理内存立即下降。

## 验证

PlayMode 验证真实双后端加载、Addressables 初始化失败回滚、Shutdown；Player 冒烟验证构建后的本地 Addressables 内容与 Resources 同时可用。
