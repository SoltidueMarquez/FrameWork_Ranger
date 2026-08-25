# ADR-006：中央项目设置与活动场景所有权

- 状态：已接受
- 日期：2026-08-07
- 所属阶段：Phase 1.1——中央启动、统一编辑器中心与架构类图
- 替代：[ADR-002：GlobalScope 与单 SceneScope 所有权](./ADR-002_Global_And_Scene_Scope_Ownership.md)中的 Entry 挂载方式

## 背景与约束

Phase 1 通过每场景 `FrameworkEntry` 显式引用 GlobalConfig 与 SceneConfig。它能清楚表达所有权，但需要每个场景重复放置组件并容易漏配。项目需要像 HTY Framework Center 一样集中管理 GlobalConfig，同时仍保持一个 GlobalScope、一个活动 SceneScope、串行切换和迟到卸载保护。

## 候选方案

1. 保留 Entry，并提供统一预制体。
2. 使用固定 Resources 项目设置和自动 Bootstrap。
3. 使用 ProjectSettings Preloaded Assets 注入配置。

## 决定

采用方案 2。固定 `Resources/FrameworkProjectSettings.asset` 保存唯一 GlobalConfig、可空默认 SceneConfig 和以场景 GUID/路径标识的覆盖表。首个场景加载后自动创建 Host，活动 Scene 的 Handle 作为唯一 SceneScope 所有者令牌。

活动场景变化、场景卸载与 Shutdown 继续进入同一异步队列。Coordinator 为每个已提交场景维护取消源；旧场景迟到卸载只有在 Handle 与当前 SceneScope 所有者一致时才会生效。未登记场景使用默认配置；默认为空时创建零模块 SceneScope。

## 影响与明确非目标

- 删除 `FrameworkEntry` 公共类型、Inspector 和所有示例引用。
- 场景不需要 Framework 预制体或组件。
- 固定设置缺失或无 GlobalConfig 时，在任何克隆前进入 Failed。
- Additive 场景只有成为活动场景后才替换唯一 SceneScope。
- 不支持多个并存 SceneScope、运行时热改配置或自动创建缺失资产。

## 验证方式

- EditMode 验证配置解析、重复映射和默认/空 Scope。
- PlayMode 验证无 Entry 自动启动、A/B 切换、Global 保持、取消、迟到卸载和 Shutdown。
- 人工确认示例场景 Hierarchy 不含 Entry，Framework Center 可以定位全部配置。
