# Framework Center 架构

> 状态：Phase 1.1 已确认决定；Phase 1.2 已补充发现边界与视觉层级  
> 日期：2026-08-07

## 页面模型

Framework Center 是一个轻量页面宿主，而不是把全部工具写进单个窗口。Editor 程序集通过 `TypeCache` 查找 `FrameworkCenterPage` 派生类，但只接纳同时声明 `[FrameworkCenterPageExtension]` 的正式页面；未来模块的 Editor 程序集需要引用 `Framework_WWJ.Editor`、增加页面类并显式添加该标记。

页面提供稳定 ID、中文名称、职责描述、分类、排序、关键词和可选帮助文档路径。宿主负责搜索、标签、最近访问和异常隔离，页面只绘制自身内容。

显式标记把“可继承的页面模型”和“进入生产中心的扩展”分开：EditMode 测试替身、原型和临时派生类不会被自动发现。显式候选构造入口仍保留给注册表测试，以验证稳定排序和真实重复 ID 诊断。

## 窗口视觉层级

- 42px 顶部栏承载 WWJ 标识、全局搜索和 Edit/Play 状态徽标。
- 30px 横向标签栏保留完整多标签工作流；标签宽度限制为 96–200px，当前项使用蓝色强调线。
- 208px 左侧导航保留最近访问与分类，改用扁平导航行、Hover 和左侧选中强调。
- 页面标题使用独立紧凑卡片并按标题、描述实际高度计算，避免固定高度造成文字挤压。
- 颜色、卡片、边框、导航、标签和节点图样式集中在 `FrameworkCenterStyles`，同时适配深色与浅色主题。

## 本期页面

- **概览**：运行状态、固定设置和主要入口。
- **项目配置**：ProjectSettings、Global/SceneConfig、映射诊断与模块依赖图。
- **代码架构**：Runtime/Editor 声明式分层类图。
- **帮助**：轻量 Markdown 阅读和外部打开。
- **Core Skeleton 示例**：由 Samples Editor 程序集贡献，证明页面扩展不需要修改中心窗口。

## 本地状态

标签、当前页和最近 8 个页面保存在 `Library/Framework_WWJ/FrameworkCenterState.json`。该文件是当前项目的本地编辑器状态，不进入 Assets 或版本控制。

窗口重新载入状态时会移除当前注册表中不存在的标签和最近访问项；因此 Phase 1.1 遗留的 `First`、`Second` 等测试 PageId 不会再次显示。

本期不实现收藏、前进/后退、双 Shift、使用统计或第三方页面热卸载。
