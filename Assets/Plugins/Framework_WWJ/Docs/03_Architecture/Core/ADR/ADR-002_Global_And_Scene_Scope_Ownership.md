# ADR-002：GlobalScope 与单 SceneScope 所有权

- 状态：已替代（场景作用域模型仍保留；Entry 挂载方式由 [ADR-006](./ADR-006_Central_Project_Settings_And_Scene_Ownership.md) 取代）
- 日期：2026-08-07
- 所属阶段：第一阶段——模块模型与驱动骨架

## 背景与约束

框架需要同时表达跨场景常驻模块和随当前场景替换的模块，并处理 Unity 场景切换中旧 Entry 销毁与新 Entry 唤醒的先后不确定性。

## 候选方案

1. 所有模块都常驻，由业务手动重置场景状态。
2. 一个 GlobalScope 与一个当前 SceneScope。
3. 任意数量并存的 Additive SceneScope。

## 决定

采用方案 2。每个可运行场景显式放置 `FrameworkEntry` 并引用相同的 `FrameworkGlobalConfig` 与自己的 `FrameworkSceneConfig`。Runtime 持有一个常驻 GlobalScope 和最多一个 SceneScope。

Entry 的 Unity Instance ID 是 SceneScope 所有者令牌。Attach、Detach 与 Shutdown 进入同一串行操作队列；迟到的旧 Entry Detach 只有在令牌匹配时才能卸载 SceneScope。

## 影响与明确非目标

- 场景替换不重建 Global 模块。
- 新 Entry 使用不同 GlobalConfig 时会被拒绝。
- Scene 加载失败只回滚新 SceneScope，Global 保持可用于后续重试。
- 第一阶段不支持多个 Additive SceneScope 并存，也不自动监听所有 SceneManager 事件推断配置。

## 验证方式

- PlayMode Entry 替换、旧 Detach、不同 GlobalConfig 与失败重试测试。
- Build Settings 中的 A/B 示例场景真实切换测试，断言 Global 克隆 ID 保持不变。
