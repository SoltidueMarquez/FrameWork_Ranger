# Framework_WWJ Phase 1.2：Editor Center UX 验收与复盘

> 状态：已实现并通过自动化验收  
> 验收日期：2026-08-07  
> Unity：2022.3.62f3  
> 范围：Framework Center 视觉、页面发现边界、共享节点图视口、场景组合依赖图预览、EditMode 测试与文档。

## 1. 交付结论

Phase 1.2 已完成。Framework Center 保留多标签、最近访问、帮助和快捷键，同时重建为清晰的顶部栏、横向标签栏、扁平左侧导航、动态页面标题卡片和统一内容层级。正式页面必须显式声明 `[FrameworkCenterPageExtension]`，EditMode 测试中的 `First`、`Second` 和重复 ID 替身不再进入生产目录；旧 Library 状态中的失效测试标签会在窗口打开时清理。

代码架构图和模块依赖图现共用一个轻量 IMGUI 视口：35%–200% 鼠标中心缩放、中键或 `Alt + 左键`平移、适配全部节点和 100% 重置。节点继续使用固定自动布局，不可拖动，也没有引入 GraphView 或 UI Toolkit。

固定项目设置 Inspector（Framework Center 的“项目配置”页内嵌该 Inspector）现在可选择任意 `SceneAsset`。预览只写入本次编辑器会话的 `SessionState`，并复用 `FrameworkProjectSettingsResolver` 的“精确覆盖 → 默认配置 → 空 SceneScope”规则，不修改设置资产或场景映射。

## 2. 实际实现快照

按 2026-08-07 工作区中的 `.cs` 文件和物理行统计：

| 区域 | C# 文件 | 物理行 | 本期变化 |
| --- | ---: | ---: | --- |
| `Runtime` | 41 | 3,479 | 无生命周期与序列化格式变更 |
| `Editor` | 29 | 3,650 | 新增正式页面标记、状态清理器和共享图视口，重做窗口样式与两个图 Drawer |
| `Tests/EditMode` | 8 | 825 | 新增视口数学与生产发现边界测试 |
| `Tests/PlayMode` | 7 | 707 | 无用例变更，作为 Runtime 回归门禁 |
| `Samples/CoreSkeleton/Runtime` | 6 | 275 | 无变更 |
| `Samples/CoreSkeleton/Editor` | 2 | 263 | 示例页增加正式扩展标记 |

Runtime/Editor 架构元数据声明现为 64 项；生产中心自动发现 5 个带正式标记的页面。

## 3. 自动化验证结果

验证在项目内隔离副本中使用 Unity 2022.3.62f3 官方批处理 Test Runner 执行，避免占用用户当前打开的主 Unity Editor。

| 门禁 | 结果 | 用例 | NUnit 时长 |
| --- | --- | ---: | ---: |
| 完整脚本编译 | Passed | Runtime / Editor / Samples / Tests 全部程序集 | — |
| EditMode | Passed | 24/24 | 0.232 s |
| PlayMode | Passed | 13/13 | 0.348 s |

EditMode 已验证：

- 自动注册表只发现带正式标记的页面，不出现 `test.first` 重复诊断；
- 显式注入候选仍能稳定排序并报告真实重复 ID；
- 旧测试标签会被清理并回退到概览页；
- 鼠标锚点缩放不漂移、缩放上下限、平移、适配和 100% 重置计算正确；
- 场景解析继续覆盖精确覆盖、默认配置和合法空 SceneScope；
- 架构 Attribute 覆盖、关系、源码定位和既有模块图测试继续通过。

PlayMode 13/13 继续验证自动 Bootstrap、Global/SceneScope 生命周期、A/B 场景切换、Handler 切换、失败回滚、Tick 隔离与 Shutdown，确认本期没有改变 Runtime 行为。

## 4. 人工验收步骤

1. 在 Unity 打开 `Framework_WWJ → Framework Center`。确认顶部栏、搜索框、Edit/Play 徽标、标签栏、左侧导航与页面标题之间没有文字挤压或重叠。
2. 检查左侧导航和标签：只应存在概览、项目配置、代码架构、帮助、Core Skeleton 示例等正式页面，不应看到 `First`、`Second`，也不应有 `test.first` 重复诊断。
3. 打开多个正式标签，关闭并重开窗口。确认当前标签、正式标签和最近访问恢复，失效测试标签不会恢复。
4. 打开“代码架构”，分别验证 35%、100%、200%：
   - 滚轮围绕鼠标位置缩放；
   - 中键或 `Alt + 左键`平移；
   - “适配”显示全部节点，“100%”恢复原始比例；
   - 点击节点仍显示正确详情，双击或“打开脚本”能在 Rider 打开对应源码。
5. 打开“项目配置”并展开“场景组合依赖图”：
   - 选择 `CoreSkeleton_A.unity`，应显示“精确场景覆盖”和 A 的 SceneConfig；
   - 选择 `CoreSkeleton_B.unity`，应显示 B 的 SceneConfig；
   - 选择未登记场景，应显示默认配置或“合法空 SceneScope”；
   - 使用“使用当前活动场景”和“清除并预览默认上下文”验证两个快捷入口；
   - 场景变化后图自动适配，且设置资产与映射没有被改写。
6. 在该模块依赖图上重复验证 35%、100%、200%、平移与适配，确认 Global/Scene 分区、连线、节点文字和点击位置一致。
7. 如需复验 Runtime 示例，在“Core Skeleton 示例”页运行 Scene A/B：Global Module InstanceID 应保持不变，Scene Handler 应在 Counter/Pulse 间切换；Shutdown 后 Host 消失。
8. 在 Unity Test Runner 运行 `Framework_WWJ.Tests.EditMode` 与 `Framework_WWJ.Tests.PlayMode`，基线应为 24/24 与 13/13。

## 5. 计划偏差与已知边界

- 视觉布局、真实鼠标事件、Rider 打开行为必须在有界面的 Unity 中人工确认；批处理验收验证了编译、目录发现、状态迁移和视口坐标数学，不能替代视觉手感检查。
- 节点布局仍是确定性的分层自动布局；平移和缩放状态只存在于当前窗口对象，不持久化节点坐标。
- 项目配置预览复用现有设置 Inspector，因此 Framework Center 与独立选中 `FrameworkProjectSettings.asset` 时具有相同的场景选择和图行为。
- 本期没有修改 Runtime 公共 API、生命周期、中央设置字段、Packages、ProjectSettings、场景或示例配置资产。

## 6. 相关资料

- [Phase 1.2 实施计划](./04_Phase1_2_Editor_Center_UX_Implementation_Plan.md)
- [Framework Center 架构](./01_Editor_Center_Architecture.md)
- [声明式代码架构图](./02_Declarative_Code_Graph.md)
- [ADR-EC-001：可发现的页面模型](./ADR/ADR-EC-001_Discoverable_Framework_Center_Pages.md)
- [ADR-EC-003：共享可导航图视口](./ADR/ADR-EC-003_Shared_Navigable_Graph_Viewport.md)
- [Phase 1.1 验收与复盘](./03_Phase1_1_Acceptance_And_Review.md)
