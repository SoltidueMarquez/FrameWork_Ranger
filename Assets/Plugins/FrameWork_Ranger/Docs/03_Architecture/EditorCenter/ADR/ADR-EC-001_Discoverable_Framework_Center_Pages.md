# ADR-EC-001：可发现的 Framework Center 页面模型

- 状态：已接受
- 日期：2026-08-07
- 所属阶段：Phase 1.1；Phase 1.2 补充正式发现边界

## 背景与约束

框架需要一个类似 HTY LyingBottle Center 的统一入口，但不能把后续所有业务模块的编辑器 UI 都堆进单个窗口脚本。页面扩展还需要保持依赖方向清晰，让 Samples Editor 和未来模块 Editor 可以接入，而 Runtime 不依赖 Editor。

## 候选方案

1. 在窗口中硬编码所有页面和导航。
2. 使用公共页面基类与 `TypeCache` 自动发现 Editor 页面。
3. 每个模块注册菜单并维护独立窗口。

## 决定

采用方案 2。`FrameworkCenterPage` 声明稳定 PageId、名称、描述、分类、排序、关键词、帮助文档和页面生命周期；`FrameworkCenterPageRegistry` 发现具备公共无参构造函数、非抽象且显式声明 `[FrameworkCenterPageExtension]` 的正式页面并稳定排序。

Phase 1.2 根据实际问题补充显式标记：TypeCache 会扫描已经加载的 EditMode 测试程序集，单纯依靠继承会把测试替身当作生产页面。正式发现必须检查 Attribute；测试使用的显式候选构造器仍可绕过自动发现过滤，以独立验证重复 ID 和排序算法。

窗口只负责导航、搜索、标签、最近访问、状态恢复和异常隔离。页面通过 `FrameworkCenterPageContext` 请求打开页面、打开帮助、选择对象或重绘，不直接操作窗口的内部集合。重复 PageId 保留稳定排序后的第一个页面并显示诊断。

## 影响与明确非目标

- Framework Center 成为配置、架构图、帮助和示例工具的统一入口。
- 后续 Editor 模块可以通过新增页面类并添加 `[FrameworkCenterPageExtension]` 扩展，无需修改窗口宿主。
- 测试替身、抽象基类和没有正式标记的临时派生类不会污染窗口或本地标签状态。
- 标签和最近访问写入 `Library`，不进入版本控制。
- 本阶段不实现收藏、完整前进/后退历史、使用统计或远程页面。
- 页面绘制异常只影响当前页，不应使整个中心不可用。

## 验证方式

- EditMode 验证页面发现、稳定排序、重复 PageId 和损坏/过期 JSON 回退。
- 示例 Editor 程序集提供外部 `CoreSkeletonSamplePage`，证明跨程序集自动发现。
- 人工验证搜索、标签关闭/恢复、快捷键和页面错误恢复。
