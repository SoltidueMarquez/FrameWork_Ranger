# Phase 1.8：HTY 式主从配置工作台实施计划

> 状态：已实施，自动化验收通过；真实窗口视觉检查待确认<br>
> 日期：2026-08-22<br>
> 决策：[ADR-EC-009](./ADR/ADR-EC-009_HTY_Style_Configuration_Workspace.md)

## 1. 目标

将项目配置页从纵向字段和多项内联 Inspector 堆叠改为作用域主从工作台，并让独立 Global/Scene Config Inspector 共用紧凑模块列表。用户应能始终看清当前配置作用域、模块状态和组合依赖诊断。

## 2. 已实施范围

### 2.1 主从工作台

- 左侧导航集中展示项目入口、Global、Default Scene 和全部场景覆盖，使用作用域徽章、模块数和错误数表达状态。
- 右侧固定“配置编辑 / 组合依赖图”工具栏；项目、Global、默认 Scene 和精确场景覆盖分别解析自己的编辑与图上下文。
- Center 项目配置页关闭宿主外层滚动，由左右面板各自持有滚动位置；普通扩展页继续使用原行为。
- 窄 Inspector 自动退化为顶部作用域下拉，不出现三列或水平滚动。

### 2.2 紧凑模块配置

- ModuleConfig 使用 32px 行展示启用状态、Module ObjectField、类型、优先级、依赖数、诊断、Ping、眼睛和删除。
- 搜索与“全部/启用/异常”筛选只改变显示索引；筛选期间禁用重排。
- 添加按钮打开 `ModuleBase` 资产选择器；所有结构修改支持 Undo，并通过现有 `SetModules` 保存 Odin 数据。
- 单个 Config 最多展开一个 Module 真实 Inspector；切换、删除、引用替换、停用宿主和程序集重载都会释放旧 Editor。
- GlobalConfig 的 DriverHandler 继续由真实 Odin Inspector 绘制，没有建立第二套 PropertyTree。

### 2.3 状态、诊断与缓存

- 当前作用域、右侧页签、模块搜索、筛选和展开目标只写入 `SessionState`。
- 场景覆盖优先使用 Scene GUID，未选场景时临时使用索引；GUID 补齐、条目删除和引用失效会修复当前选择。
- Module Resolver 结果按配置与 Module Dirty revision 缓存；项目聚合校验同时纳入设置、配置、场景绑定和 Build Settings revision，普通 Repaint 不重复执行完整校验。

## 3. 接口与边界

- `FrameworkCenterPage.UseHostContentScroll` 是唯一新增的 Editor 公共扩展属性，默认值为 `true`。
- Runtime 公共 API、模块生命周期、依赖规则、配置资产字段和序列化格式不变。
- HTY/LyingBottle 与 YokiFrame 保持只读；现有 Resource、场景、ProjectSettings 和 IDE 改动不属于本阶段。

## 4. 验收入口

自动化证据、人工检查和未覆盖项见 [Phase 1.8 验收与复盘](./17_Phase1_8_HTY_Style_Configuration_Workspace_Acceptance_And_Review.md)。
