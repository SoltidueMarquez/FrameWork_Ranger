# Framework_WWJ Phase 1.3：预览页签与固定快捷页签实施计划

> 状态：已实施并通过自动化验收<br>
> 日期：2026-08-18<br>
> 范围：Framework Center 页签状态、顶部页签交互、EditMode 测试与文档。

## 1. 阶段目标

移除与分类导航重复的“最近访问”，将顶部页签改为 Visual Studio 风格的“多个固定页签 + 单个预览页签”。普通页面先进入临时预览槽位；用户点击图钉后才成为跨编辑器会话保存的快捷入口。

本阶段不改变 Runtime 生命周期、中央设置格式、页面扩展 API、Packages、ProjectSettings、Resources、配置资产或场景。

## 2. 已确认交互

- 固定页签从左向右排列；预览页签固定在最右端并紧邻 `?`。
- 打开未固定页面会替换唯一预览页签；打开固定页面只激活固定页，不替换已有预览。
- 固定预览页时追加到固定列表末尾；取消固定时将该页变成活动预览并替换旧预览。
- 固定页顺序允许拖拽并跨会话保存；预览页不可拖拽。
- 关闭固定页同时取消固定；关闭活动页时按左侧固定页、右侧固定页、已有预览、概览预览的顺序回退。
- 帮助页使用同一模型，可以预览和固定；页面内部阅读状态不持久化。
- 旧 `openTabs` 与 `recentPageIds` 不迁移为固定页；升级后从概览预览开始。

## 3. 实现结构

- `FrameworkCenterStateData` 升级为 v2，只保存固定页顺序和最后活动固定页。
- `FrameworkCenterStateStore` 明确识别 v2；缺失版本或损坏 JSON 时返回干净状态。
- `FrameworkCenterStateSanitizer` 只清理固定页和最后活动固定页，不再强制把概览写入持久列表。
- 新增纯 Editor 内部 `FrameworkCenterTabModel`，集中处理打开、固定、取消固定、关闭、回退和重排。
- `FrameworkCenterWindow` 通过统一转换入口驱动页面 `OnActivated` / `OnDeactivated`，并绘制固定区、预览区、图钉、关闭按钮和拖拽指示线。
- `FrameworkCenterStyles` 提供预览斜体样式和不依赖 Unity 内置资源名的图钉绘制。

## 4. 验收门禁

- EditMode 覆盖 v1 清空、v2 往返、状态清理和完整页签模型状态转换。
- 固定页拖拽提交、取消和顺序持久化可验证。
- Unity Runtime、Editor、Samples 与 Tests 程序集编译通过。
- 既有 EditMode 与 PlayMode 测试全部通过。
- 人工确认左侧无最近访问，固定/预览/帮助/关闭/拖拽/快捷键行为符合本文。

实际结果、自动化证据与人工复验步骤见 [Phase 1.3 验收与复盘](./07_Phase1_3_Preview_And_Pinned_Tabs_Acceptance_And_Review.md)。
