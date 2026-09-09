# UI v2：独立资源配置、场景规则与组件扩展

日期：2026-09-10。用户已批准整体设计；本记录对应 `.workflow/ui-evolution` 的 22 项有效要求。首版 01–06 文档保留为演进记录；涉及本次扩展的契约以本文和模块 README 为准。

## 配置职责

| 对象 | 负责什么 | 不负责什么 |
| --- | --- | --- |
| GlobalConfig | 安装唯一 GlobalUIModule 和 ResourceModule 等模块依赖 | 不逐窗口管理 UI |
| GlobalUIModule.UIResources | 指定当前项目使用的一份资源总配置 | 不重复安装 SceneUIModule |
| UIResourcesConfig | Global/Scene 域与面板 SO 收录；条目启用、适用场景、自动打开及必须成功 | 不保存窗口运行状态 |
| UIPanelConfig.Definition | Key、所属域、Prefab 来源、Logic、层级、互斥、模态、关闭与动画 | 不决定项目是否安装、在哪个场景启用 |
| UIView Prefab | 局部结构与手动绑定引用 | 不代替总配置或独立面板 SO |

项目中心下拉发现总配置，选择只编辑，显式使用按钮才安装。预检使用临时模块和临时 GlobalConfig，检查所有场景模块图后才记录 Undo 和修改资产。已有 UI 模块保留原资产和条目顺序；重复条目和缺失依赖直接提示原因。新增总配置不自动安装、不携带演示窗口。

面板资源默认直接引用 Prefab。Resources / Addressables 模式只持有地址；编辑器从 Prefab 解析地址后不保留对象引用。直接引用并不意味着懒加载该 Prefab 资源，只是实例到打开时创建；关闭销毁实例也不代表释放配置对 Prefab 的依赖。

新配置优先于旧内嵌目录，不合并。提取工具复制独立 SO，保留旧字段、GUID、资源后端和地址、Logic 及域参数；保存重载验证后才关联模块。旧 Scene 条目默认全部适用、自动打开关闭，保证原业务打开行为。原样例仍明确标记为演示。

## 场景就绪与动画

Core 新增可选 `ISceneScopeReady`。已经参与 Starting 的 Global 模块可另外实现该接口；Scene 模块及 Driver.AfterScopeLoad 全部完成后，按参与顺序通知，随后才建立 Tick/就绪状态。回调失败走既有逆序 Ending 和模块清理，不影响 Global 存活。Core 不引用 UI，未实现新接口的旧模块无需改动。

UI 创建运行快照，按当前路径过滤 Scene 面板；范围外手动打开也失败。自动打开按目录顺序传 null，已经打开则跳过，不刷新。普通失败记录错误继续，必须成功失败抛出到 Scope 回滚。场景身份仍固定到当前轮次，旧 async 持有的上下文不能操作新场景。

入场和退场只修改独立 Presentation 节点。OpenAsync 等入场完成，Close 发起退场，CloseAsync 等退场和资源退休完成。关闭中重复关闭共享完成源，重开在关闭后开始新轮次；旧异步取消和退场 finally 通过打开轮次/关闭版本检查，不能关闭重开的窗口。退出期间保留模态，关闭开始即禁用自身输入；父子正常关闭等待整棵依附子树，强制 Scope 清理取消动画。

关闭策略仍为保留 / 销毁。UIContext 使用可被多个调用者等待的 UniTaskCompletionSource 公布退休结果；底层资源释放只执行和等待一次。先销毁 Unity 对象，再归还租约。OnOpen/OnClose 在入场/退场开始各触发一次，OnDispose 对应实例释放。

## 与 HTY 的对应与边界

| HTY 参考方向 | Ranger 本轮选择 |
| --- | --- |
| 项目统一资源 SO、窗口配置 SO、Prefab 直接配置 | 增加资源总配置和独立面板 SO，并保留地址后端选择 |
| UI 域及配置式关系 | 保留三种 Canvas；显式父面板依附，显示、输入和关闭联动分别管理 |
| UIImage / 按钮 / 状态表现 | 继承原生图片和 Selectable，增加手势与多目标表现，不引入 HTY 的全局交互系统 |
| CustomVerticalLayout 等 | 扩展现有 UIVerticalLayout/UIHorizontalLayout/UIGridLayout，复用原生先横后纵测量、TMP 换行顺序和统一刷新 |
| Util 扩展 | 提供 Rect、显隐、文字/图片、订阅释放、普通列表及滚动辅助；不另建大型工具服务 |
| 动画、拖动、折叠、渐变 | 纳入独立组件；动画用 Unity/UniTask，不新增通用 Tween 依赖 |
| 多图整体描边 | 合并普通 Image 的透明轮廓，生成一次组外轮廓，不逐图描边 |

不纳入多实例、虚拟列表、流式/环形布局、绑定代码生成、复杂材质与文字动画。保持 ResourceModule 依赖；未新增资源后端枚举或更改旧字段名称/枚举数值。YokiFrame 的单体 UIPanel 组织方式不覆盖已确认的 Logic/View 分离。

## 图形和布局实现

UIImage、UIRawImage 保留原生兼容；按钮双击和连发按需开启，双击额外通知不延迟单击。UIDragEvents 独立添加，默认按钮不会成为拖动处理器。UIControlVisual 根据 Selectable 状态和 Toggle 值选调色/换图/显隐/Animator 表现，静默数据刷新仍更新外观。UIFoldout 保留实例，UIAccordionGroup 协调单项或多项展开。

UILayoutItem 分轴决定父布局是否控制尺寸，可覆盖对齐及偏移。UIContentFitter 的范围在测量后约束，避免与自身父布局同时控制同轴；校验理解 Ranger 子项控制开关。UIListController 的位置保持选择邻近存活 key，对齐刷新前后的视口位置再约束滚动范围。UIScrollMotion 只在调用平滑滚动时附加，用户拖动、新定位和禁用取消旧操作。

UIOutlineGroup 收集同组 Image 网格，GPU 合并透明度、按粗细扩张并减去原遮罩，输出不拦射线的 RawImage，跟随所属 Canvas 与遮罩。组件拥有合成 RT 和材质并在禁用/销毁释放；源纹理无需 Read/Write。粗细单位为组的 UI 单位，上限 16；最大合成边长默认 1024，避免高分辨率无限增长。效果范围为普通 Image/UIImage，不支持合并 RawImage、文字和自定义材质效果。

## 示例和验证记录

基础 UI_A/UI_B 继续验证原关系与三域切换；新增 UI_Components 自动打开组件演示。UI_OptionalFailure 与 UI_RequiredFailure 故意缺少相机绑定，分别演示继续启动与本轮 SceneScope 回滚，Console 中的错误是对应场景的预期行为。

验证使用 `Tools/UnityCli.ps1` 与当前 Unity 6000.5.9f1。用户主工程 GUI 存在未保存场景，因此本轮 Import/测试运行于 `Logs/UIEditorValidation/Project` 隔离副本。已按明确清单回写 22 个 UI 资产及其 .meta，保留原 SamplePanel/Notice Prefab、模块 GUID 和用户未保存场景；Build Settings 仅追加三个扩展演示场景。当前模块指定 `Samples/Generated/Demo_UIResources.asset`；提取的面板文件以 `Demo_Global_` / `Demo_Scene_` 区分用途。

| 检查 | 实际结果 | 日志目录（相对隔离项目） |
| --- | --- | --- |
| UI + SceneScope 生命周期 EditMode | 21/21 通过 | `Logs/UnityCli/20260910-014310-testeditmode-32760` |
| UI + SceneScope 参与者 PlayMode | 26/26 通过 | `Logs/UnityCli/20260910-014759-testplaymode-32604` |
| 最终生产代码 Import | 退出码 0 | `Logs/UnityCli/20260910-014547-import-41516` |
| 组件和原三域样例离屏渲染 | 已生成并核对组件预览；三域有像素断言 | `Logs/UIVisuals`、`Logs/UIVisualChecks` |
| 扩展资产生成及幂等整理 | 退出码 0 | `Logs/UIEvolutionBuild2.log`、`Logs/UIEvolutionBuild3.log` |

EditMode 包含资源发现/默认选择/移动后恢复、场景移动删除、提取重载保留 Logic、Undo、依赖失败不写入、宽窄 Inspector 实际绘制、子项控制/对齐、尺寸限制、列表位置保持、Core Ready 顺序及失败配对。PlayMode 包含真实场景的自动打开和两种失败策略、运行快照隔离、取消入场立即重开、OnOpen 自关闭、正常/强制动画关闭、手势和静默 Toggle、折叠/滚动中断、既有关系/绑定/资源释放，以及三种 Canvas 下的窗口拖动坐标/边界/输入中断和描边/嵌套遮罩像素验证。

排查中发现并修复：Unity 6.5 API 差异；UniTask 底层异步任务被并发等待；旧内嵌字段 HideInInspector 导致 Odin 属性树缺失；删除 GUID 的旧路径缓存；CanvasRenderer 网格直接用于 CommandBuffer 时空绘制。描边最终复制源几何到组件自有 Mesh，并在禁用/销毁释放，不更改原网格。测试失败日志属于修复过程，以上列出的是对应最终通过结果。

样例生成包装器尚无对应任务，因此使用经本机版本核对的一次性 `-executeMethod UIEvolutionSampleBuilder.BuildForValidation` 入口；该入口仅允许 batchmode，在隔离副本打开已保存的 UI_A 再创建附加场景。初次直接 Build 因空白未保存宿主场景失败，未触碰主工程 GUI；随后修正批处理宿主并成功。普通 Import 和所有测试仍使用项目包装脚本。

验证边界：未执行全框架回归、Player 构建、移动端或其他图形后端测试；本次针对 Unity 6000.5.9f1 / URP / D3D12 的编辑器与 PlayMode。Overlay 样例的离屏预览暂借相机生成并还原，原生 Overlay 路径另有运行状态与描边像素测试。未把 Pooling 等模块的历史待验收项算作本轮通过。
