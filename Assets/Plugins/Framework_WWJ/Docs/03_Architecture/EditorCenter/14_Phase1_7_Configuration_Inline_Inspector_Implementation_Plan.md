# Phase 1.7：配置资产内联 Inspector 实施计划

> 状态：已实施，自动化验收通过；真实窗口视觉检查待确认<br>
> 日期：2026-08-22<br>
> 决策：[ADR-EC-008](./ADR/ADR-EC-008_Editor_Owned_Inline_Inspectors.md)

## 1. 目标

在 Framework Center 项目配置页和独立 Global/Scene Config Inspector 中，为配置资产与 Module 引用增加 HTY 语义的眼睛按钮。用户可以在当前上下文直接展开目标的真实 Inspector 编辑，同时不把纯 UI 状态写入配置资产。

## 2. 已实施范围

### 2.1 共享内联宿主

- `FrameworkInlineInspectorHost` 使用宿主资产、语义槽位和目标资产稳定标识读写 `SessionState`。
- 眼睛按钮使用深浅主题自适应的 `VisibilityOn/Off` 内置图标；空引用保持禁用。
- 每个已展开槽位通过 `Editor.CreateCachedEditor` 复用目标真实 Inspector，不创建第二套 Odin PropertyTree。
- 引用替换、条目删除、收起、宿主销毁、程序集重载和 Unity 退出统一释放子 Editor。
- 子 Inspector 异常被限制在当前内联框中；`ExitGUIException` 继续传播以保持 Unity IMGUI 语义。

### 2.2 项目设置与场景绑定

- Global Config、Default Scene Config 和每个场景覆盖 Scene Config 使用“ObjectField + 眼睛”同行布局。
- 场景绑定以 Scene GUID 作为稳定槽位；未绑定 Scene 时以当前索引作为临时槽位。
- SceneAsset、缓存 GUID 和 Path 保持原有绘制，不增加无关按钮。

### 2.3 Module 配置

- `ModuleConfigEntryDrawer` 保留 Odin 列表折叠、拖动、启用开关、Required 和序列化行为。
- Module ObjectField 右侧提供眼睛按钮；展开后绘制该 Module SO 的真实 Inspector。
- 合法 Module 槽位跟随 Module 资产标识，重排列表不会把展开状态转移到其他模块。
- Runtime 字段不再声明自动 `[InlineEditor]`，避免默认内联与受控展开重复。

## 3. 接口与边界

- Runtime 公共 API、模块生命周期、依赖解析、程序集和配置序列化格式不变。
- 展开/收起不产生 Undo、不标记配置资产 Dirty；子 Inspector 的实际属性编辑继续使用其原生 Undo/Redo。
- 不支持浮动 Inspector、跨会话布局共享或项目级 UI 状态资产。
- HTY/LyingBottle 与 YokiFrame 全程只读，用户已有配置资产和第三方设置改动保持不变。

## 4. 验收入口

自动化证据与人工检查清单见 [Phase 1.7 验收与复盘](./15_Phase1_7_Configuration_Inline_Inspector_Acceptance_And_Review.md)。
