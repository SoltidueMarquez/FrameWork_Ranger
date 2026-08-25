# ADR-EC-008：Editor 持有的配置内联 Inspector

> 状态：Accepted<br>
> 日期：2026-08-22<br>
> 阶段：Editor Center Phase 1.7

> Phase 1.8 说明：多项原位展开的页面呈现已由 [ADR-EC-009](./ADR-EC-009_HTY_Style_Configuration_Workspace.md) 的主从工作台与单项互斥详情取代；本 ADR 的 `SessionState`、真实 Editor 缓存、异常隔离和释放生命周期决策继续有效。

## 背景与约束

`FrameworkProjectSettingsInspector` 为了提供场景绑定、联合诊断和依赖图，使用自定义 IMGUI 绘制而没有调用 Odin 默认属性树，因此项目设置字段上的 `[InlineEditor]` 并不控制当前页面。Global/Scene Config 中的 Module 引用则使用 `[InlineEditor]` 自动展开，列表较长时缺少按需查看能力。

HTY 的 `ModuleItemCfgDrawer` 和 `CfgDataDrawer` 证明了“引用旁的眼睛按钮 + 原位真实 Inspector”适合配置工作流，但其展开布尔值进入配置数据、部分路径每帧创建 PropertyTree/Editor，不符合 Framework_WWJ 的纯配置资产和生命周期约束。

## 考虑方案

1. 继续使用 Odin `[InlineEditor]`：实现最少，但无法统一项目设置自定义页面，模块列表也会持续占用空间。
2. 复制 HTY 的 Drawer 与 Editor 创建方式：交互接近参考，但会把 UI 状态写入资产或增加每帧对象创建。
3. 使用 Editor 持有的共享宿主：由眼睛按钮控制 `SessionState`，按槽位缓存真实 Unity Editor，并在明确生命周期释放。

## 决策

采用 Editor 持有的共享宿主：

- Global Config、Default Scene Config、场景覆盖 Scene Config 和每个 Module 引用均使用独立眼睛按钮。
- 展开状态只保存目标资产标识到 `SessionState`；新 Unity 会话默认收起，不产生 Undo 或资产 Dirty。
- 子 Inspector 通过 `Editor.CreateCachedEditor` 创建并复用，继续命中目标已有的自定义 Inspector/OdinEditor。
- 引用替换、列表删除、收起、宿主停用、程序集重载和 Unity 退出都会释放对应 Editor。
- 配置资产引用使用语义槽位；Module 槽位跟随唯一 Module 资产，避免列表重排改变展开对象。
- 删除 Runtime 字段上的自动 `[InlineEditor]` 展示属性，保留原有 Odin 序列化、Required 校验和列表行为。

## 后果与非目标

- 项目设置页与独立 Global/Scene Config Inspector 获得一致的按需编辑体验，并允许“配置 → Module”两级展开。
- 同一会话内重新打开页面可以恢复状态；状态不跨 Unity 会话、设备或团队同步。
- 重复引用同一个 Module 资产会共享一个展开槽位；重复 Module 本身仍由现有配置诊断报告。
- 本阶段不新增浮动 Inspector 窗口、不改变配置解析或序列化格式，也不修改 HTY/LyingBottle。

## 验证

- EditMode 验证状态往返与隔离、目标替换、资产 Dirty、Editor 缓存复用/释放、列表失效清理和真实 Config Inspector 解析。
- Unity Import、完整 EditMode/PlayMode 回归和架构目录零诊断检查必须通过。
- 真实窗口检查深浅主题、列表重排、两级展开、Undo/Redo 和异常隔离。
