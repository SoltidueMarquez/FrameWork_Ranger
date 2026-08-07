# 声明式代码架构图

> 状态：Phase 1.1 已确认决定；Phase 1.2 已补充共享视口交互  
> 日期：2026-08-07

## 元数据边界

Runtime 与 Editor 程序集的顶层类和关键接口通过 `FrameworkArchitectureAttribute` 声明：

- 显示名称；
- 中文职责；
- 固定架构层；
- 同层排序值；
- 少量关键协作类型。

继承和直接接口实现由反射推导；Attribute 不承担源码调用分析。Attribute 带有 `Conditional("UNITY_EDITOR")`，玩家构建不保留职责字符串和关系数组。

## 图形语义

- 类为实心节点，接口为描边节点。
- 继承为实线，接口实现为虚线，显式协作为点线。
- 节点按固定层分列，并按 Order、显示名称和类型全名稳定排列。
- 点击显示名称、职责、程序集、基类、接口、协作类型与源码路径；双击或按钮使用 Unity 外部脚本编辑器打开源码。
- 搜索只高亮结果，不删除节点和改变布局。

## 共享视口交互

代码架构图与模块依赖图使用同一个 `FrameworkGraphViewportState` / `FrameworkGraphViewport`：

- 缩放范围为 35%–200%，滚轮以鼠标所在位置为中心缩放；
- 中键或 `Alt + 左键`拖动画布，节点本身不能拖动；
- 工具栏提供“适配”和“100%”，并显示当前缩放百分比；
- 初次打开、程序集筛选变化或场景预览变化会适配全部节点，搜索高亮不会重置视角；
- 网格、连线、箭头、节点与点击区域使用同一 Canvas/Viewport 坐标变换，缩放后选择和双击仍命中实际节点。

视口只负责导航和裁剪，架构节点的固定分层布局、关系语义和源码定位规则保持不变。详细决定见 [ADR-EC-003](./ADR/ADR-EC-003_Shared_Navigable_Graph_Viewport.md)。

## 范围

包含 `Framework_WWJ.Runtime` 与 `Framework_WWJ.Editor`；排除 Samples、Tests、第三方程序集、枚举、委托和私有嵌套辅助类型。EditMode 测试负责检查目标范围内的 Attribute 覆盖率。
