# UI 模块参考研究

研究日期：2026-09-09。依据本机源码静态检查；未运行 HTY 或 YokiFrame，也不将源码中的注释当作运行验证。

## 1. 参考范围

HTY 主参考根：`D:/unityhub/UnityProjects/LyingBottle/Assets/Plugins/ActFramework_ByHZR/Packages/CustomUI/`。下表 HTY 路径均相对于此目录。

YokiFrame 辅助参考根：`D:/unityhub/UnityProjects/YokiFrame/Tools/UIKit/`。下表 YokiFrame 路径均相对于此目录。

读取了 LyingBottle AGENTS、act-framework-dev、lyingbottle-dev 与 lyingbottle-ui 的有关资料。这里只借助它们定位框架与用例；不执行参考项目的修改、提交、技能同步或外部通知。源码中的 Global、GeneralBehaviour、SmartEnum 等都是参考实现依赖，不是 Ranger 新增依赖。

## 2. UI 域、面板与生命周期

| 考察点 | 源码事实 | 对 Ranger 的意义 |
| --- | --- | --- |
| HTY UI 域 | `Manager/Domain/UIDomain.cs` 的域拥有根节点、UI 配置字典、打开记录、常驻/临时容器及组缓存；`UIDomainType` 包含 Overlay、Camera、World | 域同时管理呈现容器和面板关系，不能仅视为 sortingOrder |
| 域策略 | `Manager/SO/UIResourcesCfg.cs:201` 的 UIDomainCfg 保存 awakeOpen、autoOpenAndClose、existType、logic、autoPlaceTop；`UIDomainLogic.cs` 提供 Enter/Exit | 渲染模式、启闭策略、焦点与业务钩子是不同维度 |
| 所有者 | `Manager/Scope/UIManagerScope.cs` 将 Root 绑定到 Manager；`Manager/Base/UILogicBase.cs` 注入 owner，OpenSelf/CloseSelf 优先走 owner | 保留显式归属；Ranger 不应按“当前场景”猜测旧面板属于谁 |
| 面板逻辑/显示对象 | `UILogicBase` 驱动 Create/OpenLogic/CloseLogic/Destroy，并调用 UIEntity 上的 IUIPanel 回调；`Manager/Unit/MainUIPanel.cs` 提供组件侧生命周期入口 | HTY 提供分离机制，但实际业务也会放在面板组件中；Ranger 的职责分离是本轮确认的约定 |
| 常驻/临时 | `UIResourcesItemCfg.cs` 同时保存 UIType、PreState、UIExistType；`UILogicTemporary.cs` 本身没有关闭销毁实现；基类 CloseEffect 只是 SetActive(false) | 不能凭 Temporary 命名推导“关闭即销毁”，应单独定义关闭策略 |
| 面板关系 | `UIDomain.cs` 的 OpenRelativeHandle_PreClose/PostRegister 与 CloseRelativeHandle 管理 MutexSelf、Depend、Self；另有命名互斥队列 | 互斥替换、依赖弹窗、普通并存与返回导航须分别定义 |
| 域间关系 | `Manager/Handler/BasicUIHandler.cs` 的 OpenDomain/CloseDomain 也维护互斥域与依赖域 | 不需要为了实现面板关系而立即照搬第二套域间状态机 |
| 临时 UI 区域 | `Manager/Region/UIRegion.cs` 有 Host 与 Controller，但多个生命周期方法为空 | 不能仅按类型存在就认定有完整子区域系统可复用 |
| YokiFrame 分层 | `Adapters/Unity/Runtime/Root/UIRoot.Levels.cs` 按 UILevel.Order 创建并排序层级容器 | 排序层可参考；UILevel 不是 HTY UIDomain 的等价物 |
| YokiFrame 状态 | `Adapters/Unity/Runtime/Contracts/PanelState.cs` 区分 Preloaded、Opening、Open、Hiding、Hide、Closing、Cached、Close | 隐藏、关闭、销毁应明确区分，不只用一个 bool |
| YokiFrame 缓存 | `PanelCachePolicy.cs` 区分 Reusable、Transient、Persistent | 复用与所有权可借鉴；是否引入有界缓存待需求决定 |

HTY 的 MutexSelf 在启动新面板打开协程之前先关闭旧互斥项与依赖链。这意味着新面板加载失败时旧项可能已经关闭。Ranger 候选改为先准备新实例，成功后再执行“旧 OnClose → 新 OnOpen”；新资源加载失败时保留原界面。业务回调抛异常是否回滚是另一个问题，不能把资源准备成功等同于完整切换必然成功。

HTY Depend 使用当前依赖列表以及 Enter/Exit 维护焦点恢复；命名互斥队列仍与域内共享依赖列表协作。不能据此声称其所有队列都拥有完全隔离的弹窗栈。Ranger 若支持多个组同时交互，应显式划定依赖链归属。

面板关系问答补充：HUD、背包、商店仅为解释例子，不是用户已提出的游戏功能。“弹窗显式依附某个父面板”是 Ranger 候选；HTY 此处实际维护域内当前 Depend 链，并非逐面板 parent/child 所有权树。Enter/Exit 主要改变 IsFocus 并调用钩子，不能仅凭它承诺已自动拦截所有输入。

YokiFrame 将相关机制拆开：`Adapters/Unity/Runtime/Root/UIRoot.Levels.cs` 管显示层级；`Navigation/UIKitController.Stacks.cs` 的命名栈 Push 可选隐藏旧栈顶，Pop 可选恢复前项与关闭弹出项；`Layout/UIKitController.Modal.cs` 给模态面板添加拦截图像；`Root/UIKitController.Dialog.cs` 将 Dialog 请求排队，活动项结束后再处理下一项。命名栈不等于父子生命周期依附，普通 Open 也没有按 HTY UIExistType 自动执行域内互斥替换。

## 3. 自定义组件与便捷工具

| 能力 | HTY 事实与定位 | 首期候选 |
| --- | --- | --- |
| 通用 UI 行为 | `GeneralUIBehaviour/GeneralUIBehaviour.cs` 提供 Owner、RefreshUI、ClearUI，并与子节点更新监听协作 | View/Item 的绑定、刷新、解绑保持清楚，不引入全树业务广播 |
| 按钮 | `Element/UIButton/UIButton.cs` 组合 Graphic，提供左右键按下/持续/抬起、进入/离开、选择、拖拽；UIButtonEffect 目录拆分颜色、精灵、Animator 等反馈 | UIButton 的标准点击、交互禁用、长按和可选反馈；不要把按下、抬起和点击混成同一事件 |
| 图像与选择 | `Element/UIImage/UIImage.cs` 继承 Image 并接入自定义交互接口；CustomToggle 基于 UIButtonExtension | 在 uGUI/TMP 基础上提供有实际行为的封装，普通组件可兼容，不机械复制所有图元 |
| 滚动 | `Element/CustomScrollRect.cs` 继承 ScrollRect，提供比例/长度/步长滚动、动画、FocusTarget 与布局刷新 | 滚动至项、刷新后定位、保持或重置位置；虚拟列表是独立能力 |
| 其他现成组件 | Element 中含折叠组、输入辅助、Markdown、曲线、网格图像等 | 先列候选，不能因参考库存在就视为首期需求 |
| 编辑器创建 | `Utility/CustomUIUtility.cs` 用模板创建 Panel/Text/Input/ScrollView；Editor 下有 UIResourceGeneratorApp、UIAutoUtil、UIFixedUtil | 创建面板配置/Prefab 骨架、绑定检查、布局冲突诊断可集中进 Framework Center |
| 绑定代码生成 | YokiFrame `Adapters/Unity/Editor/PanelCreation/UIKitPanelCodeLayout.cs` 为面板与 Designer 分文件；Bindings/UIElement、UIComponent 是生成器节点基类 | 借鉴业务文件与生成文件分离；这些节点本身不是一套 Button/Image 控件实现 |
| 动态子树 | YokiFrame `Adapters/Unity/Runtime/Layout/UIDynamicElement.cs` 创建嵌套 Canvas | 可作为后续局部重建优化；其 ForceRebuild 实际调用 Canvas.ForceUpdateCanvases，不能描述为只重建自己 |

HTY 列表工具另在 `Packages/Util/Extended/ExpandUtil/AlwaysLogic/UICreate/UISmartCreator.cs`：

- `UIContentSmartCreator.Handle<TKey,TData,TMono>` 以稳定 key 复用存量项，创建新增、刷新保留项、调整 sibling 顺序并移除缺失项。
- `FullHandle` 是全量路径；`PoolHandle` 和 `FullPoolHandle` 是显式的对象池路径，不能把所有 Creator 都称为池化。
- 每次同步收束布局刷新。Ranger 候选提供 Sync/Clear/Dispose，列表 owner 持有全部条目，并约束重复 key、解绑与关闭后的事件引用。

真实用例：`D:/unityhub/UnityProjects/LyingBottle/Assets/Scripts/Game/Modules/UI/BottleUI/UpgradeUI/UpgradeUI.cs:29` 在打开时初始化内容、刷新并订阅；`RefreshList` 同步两个列表后，从面板根调用 `UpdateLayoutUpdateImmediatelyAllDepthFirst`。这个场景证明“数据→子项→滚动容器→祖先尺寸”的整体结算有实际用途，并非只有批量 Instantiate 的语法封装。

## 4. Layout 逻辑

HTY 包含三层配合：

1. **几何组件**：`Layout/BasicLayout/CustomHorizontalLayout.cs`、`CustomVerticalLayout.cs`、`CustomGridLayout.cs`、内容适配和子项偏好。以 Vertical 为例，先模拟计算内容尺寸，再应用容器尺寸和子项位置；会调整子项锚点。
2. **控制与队列**：`Layout/Core/UIControlBase.cs` 提供优先级与尺寸约束；`UILayoutController.cs` 收集同物体组件并排序，leader 可触发同队列其他控制器；`GlobalUILayoutController.cs` 以字符串 key 管理队列。
3. **刷新调度**：`Layout/Utility/UILayoutUtility.cs` 是公共入口；多项刷新实际转发到 `UILayoutUtilityService.cs`。后者提供批处理作用域、请求去重、祖先请求吸收、深度遍历与可选 LateUpdate 自动批处理。

`Layout/Core/UpdateControlUIBehaviour.cs` 对启用、子节点和尺寸变化标脏，并使用本组件更新标记与静态尺寸更新标记抑制递归。这说明父子尺寸相互影响是实际需要处理的边界。但“有重入保护”不证明所有布局都会收敛；Ranger 仍需专门验证动态文字和嵌套容器。

需要保留的设计价值：

- 业务只提出“内容变了”，统一调度把一个批次的重复刷新合并。
- 需要立即读取尺寸时有明确 Flush 接口，避免每个调用点自行延迟一帧。
- 宽度确定后才能正确测量换行文字高度；不能把所有布局简化成固定一次自底向上遍历。
- 刷新期间产生的新脏请求不可因清空队列被悄悄丢弃；也不能无限同帧递归。
- 编辑器预览与运行调度分开；编辑器重绘不应不断把场景标脏。

候选实现是以布局根为单位收集请求，按水平测量/分配、垂直测量/分配处理基础树，在明确 Flush 时结算；祖先可覆盖的重复请求合并，更新中再产生的请求进入后续批次。具体算法与 uGUI 接口适配需通过最小场景验证后确定。HTY 的字符串队列、全局单例与整套祖先缓存不直接照搬。

## 5. YokiFrame 补充：异步生命周期

`Adapters/Unity/Runtime/Root/UIKitController.Loading.cs` 以面板类型合并在途请求，加载 Prefab lease 后物化唯一实例；调用者取消与共享加载分开；控制器代次防止卸载后旧结果重新登记。`UIKitController.Lifetime.cs` 处理外部销毁的面板，`Navigation/UIKitController.Stacks.cs` 提供命名栈与 Push/Pop。

这些机制值得转化为 Ranger 的行为测试：同 key 并发打开只创建一次、一个等待者取消不影响其他人、模块卸载后迟到资源释放、旧 handle 不操作新实例。Ranger 已有 ResourceModule.AcquireAsync 和 ResourceLease，无需再移植 ResKit 或另建 Resources/Addressables 选择器。

## 6. Ranger 接入事实

- `Runtime/Graph/ModuleGraphResolver.cs:114` 在合并 Global/Scene 图后按具体类型去重，同一个 UIModule 不能同时安装两处。这不要求将 UI 生命周期拆成两个 Module；此前双模块候选已被用户审阅后的单个 GlobalUIModule 方向替代。
- `BaseModules/ResourceManagement/Runtime/Module/ResourceModule.cs` 仅公开异步 AcquireAsync；实例活着或关闭缓存时仍须持有 Prefab lease，销毁后才释放。
- `BaseModules/Pooling/GameObject/Runtime/GameObjectPoolHandler.cs:59` 限制 SceneScope，Global UI 不能依赖它。面板缓存可以由 UI 所有者保留原实例；真正列表池化若接入现有池，需要 Scene 适配边界。
- SO 保存模板，Scope 运行克隆持有运行对象。场景 Camera/挂载点不能写入公共模板资产，必须在运行时绑定并处理解绑。
- Event 已有独立事件类与主线程同步派发。若 UI 提供跨模块打开/关闭通知，应复用它并声明依赖；本地按钮回调无需全部转为全局广播。

本次研究没有新增第三方库，没有修改 Core/参考工程，也没有验证 UI Runtime。当前设计见 [公共契约](./03_Architecture_And_Public_Contracts.md)。重设计补核发现：FrameworkRuntime 在新配置预检通过后才结束旧 Scope；ModuleScopeRuntime 已统一正常卸载与加载失败回滚；现有 Driver 钩子不能直接作为普通模块订阅接口。UI 拟新增的生命周期能力属于 Ranger 工程提案，不是 HTY/YokiFrame 的现成契约。
