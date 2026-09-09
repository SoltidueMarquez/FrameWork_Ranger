# UI 首版实施与验收

日期：2026-09-09。按已确认的 REQ-UI-001～019 实施，只有 `GlobalUIModule`；单模块内部拥有 Global 和当前 SceneScope 的 UIContext。三大基础模块的历史待验收项保持原状。本记录区分实现、聚焦测试与图形预览，不以设计说明代替实测。

## 1. 交付与需求映射

| 要求 | 实现与证据 |
| --- | --- |
| 001、002：模块流程与参考 | 01 源码研究、03 公共契约、04 实施计划、实际 Runtime/Editor/Tests/Samples 与本验收；HTY 主参考、YokiFrame 辅助，参考工程只读 |
| 003、011、019：生命周期与三种域 | UIDomain / UIDomainBinding；初次无绑定拒绝打开，加载期间失绑清理租约；既有 Global 失绑暂停、重绑定恢复。UIRuntimeTests 的绑定用例和 UISampleSceneTests 实际场景覆盖 |
| 004：Logic/View | UILogic / UILogic<TView> / UIView；每个实例独立克隆逻辑，样例手动绑定 UISampleView |
| 005：基础布局与调度 | 横纵布局、固定列网格、内容适配、显式忽略、根刷新合并；UILayoutAndListTests 检查忽略与几何，UITmpLayoutTests 检查宽度先分配、换行后高度收敛 |
| 006、013、014：关系与输入 | MutexGroup、明确父面板、按子树关闭、Domain/Global 模态、EventSystem 选择恢复。UIRuntimeTests 关系用例、UIInteractionTests 跨 owner 模态与嵌套恢复 |
| 007：关闭缓存与释放 | KeepAlive / DestroyOnClose，实例销毁后释放租约，Scope 结束等待待释放集合；UIRuntimeTests 覆盖缓存清理与延迟销毁顺序 |
| 008、012：单实例和重复打开 | owner/key 实例表、按请求处理数据、独立等待者取消；重复 Open 调 OnRefresh，同层置顶，旧句柄不作用于重开实例 |
| 009：基础组件工具 | UIButton、UISelectionGroup、UIItemView、UIListController、UIScrollTools、UIRectTransformTools；测试点击/长按、静默选择、key 复用重排、实际样例列表滚至末项 |
| 010：Editor | UIFrameworkCenterPage 创建模块和 Prefab、共享配置校验、必需引用与布局冲突检查；UIEditorCreationTests 运行创建工具并检查产物 |
| 015、016：单模块与集中目录 | GlobalUIModule 保存两组配置，ResourceModule 依赖提供按需租约；未新增 SceneUIModule 或逐场景目录映射 |
| 017、018：Core 接入与入口 | ISceneScopeLifecycle / FrameworkSceneScopeInfo；FrameworkRuntime 和 ModuleScopeRuntime 固定配对通知；ui.Global / ui.Scene / TryGetScene。Core 新增测试与既有 Scope/失败回滚回归 |

生产入口和主要职责类型已加入既有架构元数据。Runtime 不依赖 UnityEditor。Core 不依赖 UI，只增加可选生命周期能力；正式决定见 [ADR-007](../../Core/ADR/ADR-007_Global_Module_Scene_Lifecycle.md)。未修改业务 Driver 签名、模块图重复类型约束或依赖方向。

## 2. 实际检查

工程：Unity 6000.5.9f1、URP、Input System。通过 `Tools/UnityCli.ps1` 执行，参考工程未执行。

- EditMode：8/8 通过。过滤器 `SceneScopeLifecycleTests;FrameWork_Ranger.UI.Tests`，结果 `Logs/UnityCli/20260909-222919-testeditmode-12548/editmode-results.xml`。
- PlayMode：20/20 通过。过滤器 `FrameWork_Ranger.UI.Tests;FrameworkSceneParticipantTests;FrameworkSceneScopeTests;FrameworkFailureAndShutdownTests`，结果 `Logs/UnityCli/20260909-223203-testplaymode-15088/playmode-results.xml`。
- Import：退出码 0。最终生产类型架构元数据编译日志 `Logs/UnityCli/20260909-223747-import-35584/import.log`；此前功能批次已随以上测试编译通过。

EditMode 覆盖空 Scope 与中途 Starting 失败配对、逆序清理异常继续，配置重复 key / 缺域 / 跨 owner Overlay 排序冲突，按 key 复用与忽略项布局，样例生成校验，以及 TMP 换行几何。PlayMode 的过滤范围包含 UI 测试、新增 FrameworkSceneParticipantTests，以及既有 FrameworkSceneScopeTests / FrameworkFailureAndShutdownTests。

本轮实际修复了自定义忽略项被原生 LayoutElement 的 ignoreLayout=false 抵消、测试程序集 Odin 引用、样例列表拉伸宽度和同域弹窗配置等问题。TMP 几何测试使用包内 Roboto 字体与临时编辑器 SDF 材质，结束恢复设置；它不要求向项目自动导入整套 TMP Essential Resources，也不作为 TMP 成品字体渲染验收。

## 3. 样例与图形检查

已生成 `BaseModules/UI/Samples/Generated/UI_A.unity` 和 `UI_B.unity`、面板 Prefab、GlobalUIModule 配置及使用脚本。GlobalConfig 追加一个 UI 模块；Build Settings 追加两个 UI 场景，保留原模块与场景。创建工具不覆盖既有 Prefab 或场景，当前工程的样例资产随交付提供。

真实场景测试经过 ResourceModule 加载样例，验证 Overlay / Camera / World 呈现，以及旧 Additive 场景仍存在时 SceneScope 已清理、Global World 同一实例重绑到新场景、旧场景迟到卸载不误清理新 owner。样例按钮覆盖动态条目、互斥窗口、明确依附弹窗和全局模态。

`Logs/UIVisuals/overlay-preview.png`、`camera-preview.png`、`world-preview.png` 为 1280×720 离屏预览。已检查面板、文字、按钮、列表与 Global 提示的位置；World 预览包含左右两个独立世界面板。批处理环境不提供原生 GameView 截图：预览时临时把 Overlay Canvas 借给当前相机渲染，随后还原；Camera/World 使用原模式。这些图片不宣称是原生 Overlay GameView 截图。

## 4. 已知边界

- 首期范围按确认保持单实例、固定列 Grid、无动画滚动、无虚拟列表、无拖拽/折叠/绑定代码生成。
- SceneContext 固定到轮次；旧异步业务必须使用捕获的上下文，不应在 await 后重新猜测当前场景。首次无 Camera / World 绑定会直接失败，调用方应处理异常。
- 全局模态限制受管 UI 的输入，不暂停游戏；不同 Canvas 模式的可见遮挡仍由相机与排序决定。
- 样例使用 UnityResources 后端。UI 通过已有 ResourceModule 资源契约支持配置的后端；本轮没有另做 Addressables 构建、完整 Player 或全部模块回归，也不替代这些范围的历史验收。
- TMP 测试验证几何；项目实际使用 TMP 字体时需完成常规 Essential Resources / 字体设置。最终设备分辨率、输入设备与美术效果仍按业务场景验收。

完整使用步骤见 [UI README](../../../../BaseModules/UI/README.md)。
