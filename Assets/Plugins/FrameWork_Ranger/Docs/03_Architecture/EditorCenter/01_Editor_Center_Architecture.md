# Framework Center 架构

> 状态：Phase 1.1 已确认决定；Phase 1.2 已补充发现边界与视觉层级；Phase 1.3 已确认预览/固定页签模型<br>
> 日期：2026-08-18

## 页面模型

Framework Center 是一个轻量页面宿主，而不是把全部工具写进单个窗口。Editor 程序集通过 `TypeCache` 查找 `FrameworkCenterPage` 派生类，但只接纳同时声明 `[FrameworkCenterPageExtension]` 的正式页面；未来模块的 Editor 程序集需要引用 `Framework_WWJ.Editor`、增加页面类并显式添加该标记。

页面提供稳定 ID、中文名称、职责描述、分类、排序、关键词和可选帮助文档路径。宿主负责搜索、预览/固定页签、页面生命周期和异常隔离，页面只绘制自身内容。

显式标记把“可继承的页面模型”和“进入生产中心的扩展”分开：EditMode 测试替身、原型和临时派生类不会被自动发现。显式候选构造入口仍保留给注册表测试，以验证稳定排序和真实重复 ID 诊断。

## 窗口视觉层级

- 42px 顶部栏承载 WWJ 标识、全局搜索和 Edit/Play 状态徽标。
- 30px 横向标签栏的左侧是可水平滚动、可拖拽排序的固定页；右侧是唯一临时预览页和最右的 `?`。
- 固定页使用实心强调图钉；预览页使用空心弱化图钉与斜体标题；当前页保留蓝色底线。
- 208px 左侧导航只保留搜索结果、页面分类与扩展诊断，使用扁平导航行、Hover 和左侧选中强调。
- 页面标题使用独立紧凑卡片并按标题、描述实际高度计算，避免固定高度造成文字挤压。
- 颜色、卡片、边框、导航、标签和节点图样式集中在 `FrameworkCenterStyles`，同时适配深色与浅色主题。

## 本期页面

- **概览**：运行状态、固定设置和主要入口。
- **项目配置**：ProjectSettings、Global/SceneConfig、映射诊断与模块依赖图。
- **代码架构**：Runtime/Editor 声明式分层类图。
- **帮助**：轻量 Markdown 阅读和外部打开。
- **Core Skeleton 示例**：由 Samples Editor 程序集贡献，证明页面扩展不需要修改中心窗口。

## 页签状态与本地持久化

`FrameworkCenterTabModel` 是不绘制 GUI 的纯状态模型，集中管理单预览页、固定页顺序、活动页、取消固定和关闭回退。窗口使用页面 ID 变化前后的统一转换入口，保证只在活动页真正变化时调用一次 `OnDeactivated` 和 `OnActivated`。

v2 状态保存在 `Library/Framework_WWJ/FrameworkCenterState.json`，仅包含用户显式固定的 PageId 顺序和最后活动固定页。预览页、帮助阅读位置和页面临时内容不持久化。该文件不进入 Assets 或版本控制。

旧状态没有 v2 版本号，窗口会弃用旧 `openTabs` / `recentPageIds` 并从概览预览开始。v2 状态载入时仍会移除当前注册表中不存在或重复的固定 PageId。

本期不实现最近访问、收藏、前进/后退、双 Shift、使用统计、页面内部状态持久化或第三方页面热卸载。
