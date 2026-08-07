# Framework_WWJ Phase 1.2：Editor Center 视觉与节点图交互优化

> 状态：已实现并通过自动化验收  
> 日期：2026-08-07  
> 范围：Framework Center、Editor 节点图、中央设置场景组合预览、EditMode 测试与文档。

## 1. 阶段目标

Phase 1.2 在不改变 Runtime 生命周期和中央设置资产格式的前提下，解决三个已经由实际截图和代码确认的问题：

- Framework Center 视觉层级松散，测试页面被 TypeCache 误发现并造成 PageId 重复；
- 代码架构图与模块依赖图只有滚动条，没有标准图编辑器的缩放和平移；
- 项目配置页只能查看 GlobalConfig 与 DefaultSceneConfig，无法按具体场景验证真实组合。

## 2. 已确认决定

- 保留完整多标签、最近访问、帮助入口和现有快捷键。
- 使用自适应深浅主题、低饱和蓝色强调、扁平卡片和明确 Hover/Selected 状态。
- 正式页面必须继承 `FrameworkCenterPage` 并声明 `[FrameworkCenterPageExtension]`。
- 测试候选仍可通过显式注入构造器验证排序和重复 ID，但不参与生产自动发现。
- 两类图共用一个 Editor 内部视口模型；缩放范围 35%–200%，滚轮围绕鼠标缩放，中键或 Alt+左键平移。
- 节点位置继续由确定性算法生成，不支持拖动或持久化节点布局。
- 项目配置依赖图选择任意 SceneAsset，并复用 Runtime 的“精确覆盖 → 默认配置 → 空 SceneScope”解析规则。
- 场景选择只存入 SessionState，不修改项目设置、场景或其他版本控制资产。

## 3. 实现边界

### Framework Center

- 42px 顶部栏、30px 可横向滚动标签栏、208px 左侧导航和紧凑页面标题卡片。
- 标签标题宽度限制为 96–200px，当前标签使用蓝色下划线；失效 PageId 在状态恢复时自动清除。
- 样式、颜色和扁平绘制辅助集中在 `FrameworkCenterStyles`，窗口只负责结构、导航与状态。

### 共享图视口

- `FrameworkGraphViewportState` 保存 Zoom、Pan、待适配状态和纯坐标换算。
- `FrameworkGraphViewport` 负责工具栏、输入、裁剪、网格背景、Canvas/Viewport 变换和适配计算。
- 架构图保留搜索、筛选、节点选择、双击打开与详情。
- 模块图保留 Global/Scene 分区、诊断颜色、拓扑层级、优先级和配置顺序。

### 场景组合依赖图

- Inspector 提供 SceneAsset、当前活动场景、清除预览三个入口。
- 首次打开优先使用已保存的活动场景；之后按设置资产 GUID 保存本会话选择。
- 预览标题明确显示精确覆盖、默认配置或空 SceneScope，并同时展示项目设置与模块图诊断。
- 场景改变后重新解析并请求图视口适配全部节点。

## 4. 非目标

- 不引入 UI Toolkit、GraphView、新包、节点拖动、布局持久化、缩略图或运行时图编辑。
- 不修改 FrameworkProjectSettings 序列化字段、模块生命周期、Scope、排序、回滚或公开 Runtime API。
- 不修改 LyingBottle、Packages、ProjectSettings、场景或示例配置资产。

## 5. 验收

- 生产 Framework Center 不显示 First/Second 测试页面，也不产生 `test.first` 重复诊断。
- 深色主题下标题、标签、导航、卡片和诊断有清晰层级；最小窗口尺寸下不重叠。
- 两类图在 35%、100%、200% 下均可缩放、平移、适配，点击和双击位置准确。
- Scene A/B 分别解析到自己的 SceneConfig；未登记场景解析到默认或空 SceneScope。
- 全部 Unity 编译、EditMode 和 PlayMode 回归通过，文档与 ADR 回写实际证据。
