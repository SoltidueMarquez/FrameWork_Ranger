# UI 模块使用说明

一个 `GlobalUIModule` 管理 Global 与当前 SceneScope 的 UI，支持 Overlay、Camera、World 三种 Canvas 域。HTY 是关系、组件和布局的主要参考；Ranger 使用明确父面板依附，并将显示顺序、输入限制、关闭联动分别管理。设计与验证见 [UI 架构文档](../../Docs/03_Architecture/FoundationModules/UI/README.md)。

## 安装与配置

配置关系为 **中央 GlobalConfig → 唯一 GlobalUIModule → 一份 UIResourcesConfig（资源总配置）→ 多个 UIPanelConfig（独立面板 SO）**。工程可以保存多个总配置供选择，项目只使用模块指定的一份。总配置中的“UI 域”定义 Canvas，“面板目录”收录具体窗口的 SO；域不是窗口，也不是额外的模块。

1. 在 Framework Center 的基础模块 / UI 页，下拉选择资源总配置，无需拖拽。页面记住选择并标记项目使用项；“创建配置”生成空业务目录，只有默认域。下拉只切换编辑对象，点击“设为项目使用”才会完整校验依赖并写入唯一模块，可撤销。GlobalScope 仍需 `ResourceModule`；地址模式需启用对应后端。
2. 在“全局 UI／场景 UI”下添加面板引用，可以选已有 `UIPanelConfig` 或创建新 SO。目录条目控制是否收录、适用场景及自动打开，面板 SO 配置资源和行为。域按渲染方式显示必要字段，隐藏的模式参数仍保留。无需 SceneUIModule 或每场景一份 UI 总配置。
3. 域 key 在各自目录内唯一。Overlay 域的 `SortingOrder` 在两组目录中不能冲突。Camera 填 `PlaneDistance`，World 填 `WorldSize` 和 `WorldScale`；World 的单位像素到世界单位比例由后者决定。
4. 在面板 SO 选择实际 Prefab。默认“直接引用”，可以拖入或用选择器选择任意项目 Prefab；也可明确选择 Resources / Addressables 地址模式。“从 Prefab 填写地址”只解析地址，不持有隐藏的 Prefab 引用。直接引用会由配置持有资源依赖，关闭销毁的是窗口实例。`MutexGroup` 留空可并存；同 owner、同域、同非空组互斥。面板层级只控制域内窗口，域显示顺序控制 Canvas。普通面板无模态，确认框通常选所属域，全局遮罩可选所有 UI 域。
5. Prefab 根使用 `UIView` 或其派生类，业务引用手动绑定，必需引用标记 `[UIRequired]`；在 Odin“面板逻辑”中选择对应的可序列化 Logic。页面下方的“辅助工具：Prefab 创建与校验”是独立的检查工具，不决定实际加载资源。实际入口始终在面板 SO 的“资源与逻辑”中。

直接选中 GlobalUIModule 资产与中央配置内嵌编辑使用同一中文 Inspector。“所属 UI 域”从当前目录下拉选择，“加载优先级”位于高级设置。进入运行模式后模板编辑和项目配置切换均禁用。新增、移动或删除配置资产后，下拉目录自动更新；若中央配置缺失或存在多个 UI 条目，页面说明原因，不擅自替换。

每个场景配置一个适合项目输入方案的 EventSystem。本工程使用 Input System，样例使用 `InputSystemUIInputModule`。UI 模块不会创建另一个 EventSystem，也不会接管角色输入。

## 指定哪些场景启用

在总配置的“场景 UI”展开面板条目：“适用全部场景”默认开启；关闭后选择场景资产列表。范围外的场景连手动 `OpenAsync` 也不能打开该面板。场景移动时通过 GUID 更新路径，删除场景会标记失效。空列表表示不适用任何场景。

“进入场景自动打开”独立于适用范围，默认关闭。开启后，在场景模块和 Driver 加载后处理完成、SceneScope 就绪前按目录顺序打开；不传业务数据，Logic 可读取配置和已加载模块。业务已经打开的单实例直接跳过。普通失败记录原因并继续；勾选“必须成功”则失败回滚本轮 SceneScope，保留 Global，不自动退回旧 Unity 场景。Global 窗口仍由业务显式打开。

旧内嵌目录可在模块 Inspector 或项目中心点击“提取旧目录为独立 SO”。提取先保存重载并校验，然后关联模块；保留旧字段、模块 GUID、面板 Key、地址和 Logic。旧 Scene 条目迁为全部场景适用、自动打开关闭。已有新总配置时不会混合两份目录或重复提取。当前 `Samples/Generated` 下的 Inventory、Shop 等是演示窗口，不会加入新建业务配置。

## 打开、刷新与关闭

```csharp
using FrameWork_Ranger;
using FrameWork_Ranger.UI;

await Framework.WhenReadyAsync();
var ui = Framework.GetModule<GlobalUIModule>();
var notice = await ui.Global.OpenAsync("Notice", "保存成功");

var sceneUI = ui.Scene; // 捕获当前轮次，跨 await 后仍指向这一轮
var inventory = await sceneUI.OpenAsync("Inventory", inventoryData);
var confirm = await sceneUI.OpenChildAsync(inventory, "Confirm", itemData);
sceneUI.Close(confirm);
sceneUI.Refresh(inventory, updatedInventoryData);
ui.Global.Close(notice);
```

所有操作从 Unity 主线程调用。业务 Module 应声明 `GlobalUIModule` 依赖并通过 ModuleContext 获取；普通组件可使用上面的静态入口。过渡期用 `ui.TryGetScene(out var sceneUI)` 探测，直接读取不可用的 `ui.Scene` 会抛出明确状态异常。

每个 owner/key 单实例。已打开时再次 Open 执行 OnRefresh 并在所属 Layer 内置顶，不重放 OnOpen；关闭缓存后再打开执行 OnOpen。句柄带打开轮次，旧句柄不能操作重开的面板。捕获的旧 Scene 上下文不能打开或刷新新场景 UI，结束轮次的关闭操作幂等。

父子必须同 owner、同域，子面板 Layer 不低于父面板。单实例子窗不能被其他父面板抢走；关闭父面板清理自己的依附子树。互斥面板先加载和校验新实例，准备失败保留原面板；业务 OnOpen 抛错不承诺重放旧面板回调回滚。

## Logic 与 View

```csharp
// NoticeView.cs
public sealed class NoticeView : UIView
{
    [UIRequired] public UnityEngine.UI.Text Message;
}

// NoticeLogic.cs
[System.Serializable]
public sealed class NoticeLogic : UILogic<NoticeView>
{
    public override void OnOpen(object data) => Show(data);
    public override void OnRefresh(object data) => Show(data);
    private void Show(object data) => View.Message.text = data as string ?? "";
}
```

每个实例克隆独立 Logic，配置模板不保存运行状态。OnCreate / OnDispose 对应实例生命期；OnOpen / OnClose 对应每次打开生命期。订阅按钮或业务事件放在 OnOpen，在 OnClose 对称解除。OnRefresh 处理新数据；OnFocus / OnBlur 只表示输入焦点变化。Logic 内 `View` 和 `UI` 分别指向绑定的 View 与所属上下文。

KeepAlive 隐藏并保留 View、Logic 和 Prefab 租约；DestroyOnClose 先结束逻辑，Unity 对象销毁后释放租约。SceneScope 结束会取消在途打开并销毁全部场景缓存，GlobalUIModule 卸载清理所有 UI。单个清理回调失败会记录异常，继续清理其余对象，所有者卸载最终汇总异常。

面板 SO 可组合淡入淡出、缩放和滑动；默认无动画，使用不受 `Time.timeScale` 影响的时间。`OpenAsync` 等入场结束，`Close` 发起退场，`await CloseAsync(handle)` 等退场与关闭策略完成。`OnOpen` / `OnClose` 在入场 / 退场开始各调用一次。入场立即阻挡其他 UI、自身完成后可操作；退场立即禁用自身、结束后撤销模态。关闭中重开会排队，强制卸载跳过动画。动画位于独立呈现节点，不改业务 View 的拖动和布局坐标。

## Camera / World 绑定

在供给相机的场景添加 `UIDomainBinding`，填写 Owner、DomainKey 和 Camera。World 还需 WorldAnchor；相机必须有效且启用。组件先于 UI 模块启用也可登记。只有当前 SceneScope 所属场景的候选有效，旧 Additive 场景不会抢占新绑定。同一域出现多个有效候选会报告冲突。

Global 域可借用当前场景绑定；真正常驻的绑定对象可以勾选 Persistent。Persistent 只用于 Global，调用方负责让对象常驻。WorldAnchor 仅提供位姿，UI 保留在自己拥有的根下，不成为场景目标的子对象。

首次或关闭后重新打开缺少相机 / 挂载点时，OpenAsync 失败，不触发 OnOpen。加载过程中失去绑定也会失败并释放半成品。已经打开的 Global Camera / World 面板失绑时暂停显示和输入，保留逻辑打开状态；重绑定恢复，不重放 OnOpen。`UIPanelHandle.IsOpen` 与 `IsPresented` 分别表达这两种状态。

## 输入、组件与布局

模态默认选用 Domain；Global 限制所有受管域。有效弹窗和其上层依附弹窗可以操作；暂停呈现的域不施加模态。框架通过自有 CanvasGroup 和 EventSystem 选择协调，不改写业务按钮的 interactable，也不修改 Time.timeScale。Prefab 内禁止 `ignoreParentGroups` 和嵌套 `Canvas.overrideSorting` 绕过限制。全局模态只定义输入范围，跨 Canvas 模式的实际遮挡仍由相机和域排序决定。

| 类型 | 使用方式 |
| --- | --- |
| UIImage / UIRawImage | 原生 Image / RawImage 的换图、透明度、射线与尺寸适配封装 |
| UIButton / UIDragEvents | 左右点击与按下松开、悬停、焦点、中断；双击/连发默认关闭。双击额外通知，不延迟普通点击。长按/连发/实际拖动后不补发点击；拖拽回调组件按需添加，普通按钮不抢 ScrollRect 拖动 |
| UIToggle / UIControlVisual | 逻辑开关与导航焦点独立；多目标颜色、换图、显隐、Animator。SetValue(value, false) 静默改值仍更新外观；使用该外观组件时关闭原生 Transition，避免争写 |
| UISelectionGroup | Inspector 配置 Toggle Items，AllowNone 控制能否无选择；SetSelected(index, false) 静默刷新 |
| UIItemView / UIListController | 按 key Sync 复用和重排；Sync(data, scroll) 保持邻近存活项的可见位置，Clear 只清理控制器拥有的条目 |
| UIScrollTools | ScrollToItem、AlignItem、ScrollToTop/Bottom、ScrollByDistance（UI 坐标单位）、ScrollToAsync；新定位、用户拖动或关闭取消平滑滚动 |
| UIRectTransformTools / UIElementTools | 锚点、边距、宽高、坐标转换、保持位置换 pivot；显隐、交互、图片/文字、只解除自身监听的 Subscribe 返回 IDisposable |
| UIWindowDrag | 放在标题/拖动区，指定 Target 和可选 Bounds；三种 Canvas 通用，失去输入即中断 |
| UIFoldout / UIAccordionGroup | 标题、内容、默认展开、箭头、单展开或多展开；即时切换与布局刷新，保留内容实例和状态 |
| UIGradient / UIOutlineGroup | 两色线性渐变；Image/UIImage 组透明轮廓合成描边，支持颜色/粗细与合成分辨率上限，不要求纹理 Read/Write |
| UIHorizontalLayout / UIVerticalLayout | 间距、padding、对齐、最小 / 偏好 / 可伸展尺寸分配 |
| UIGridLayout | 固定列数、CellSize 与 Spacing；运行时改参数后调用 RefreshLayout |
| UILayoutItem | 每个子项独立对齐、位置偏移及布局是否控制宽/高；偏移沿布局坐标，X 向右、Y 向下 |
| UIContentFitter / UIIgnoreLayout | 内容适配与宽高上下限；显式排除自定义布局中的子项 |
| UILayoutScheduler | MarkDirty 合并刷新；BeginBatch/Dispose 批量修改；FlushNow 立即结算相关根 |

自定义布局沿用 uGUI 的先横后纵测量流程，TMP 获得宽度后再计算换行高度。同轴不能同时由父布局和自身 ContentSizeFitter 控制尺寸，Editor 校验会提示。列表不做虚拟化，也不强制依赖 Scene 对象池。原生 Image、Text 和 TMP 可用于 View；业务使用 TMP 时需按 Unity 常规流程导入 TMP Essential Resources 并配置字体。本工程样例用普通 Text，TMP 聚焦测试只验证换行几何。

## 运行样例

打开 [UI_A](./Samples/Generated/UI_A.unity) 后进入 Play Mode。屏幕右上保留 Global 提示；菜单可切换背包 / 商店、Camera / World 面板、全局模态以及 UI_A / UI_B 场景。面板的 Add item 验证动态列表和滚动，Confirm 打开同域依附弹窗，Close 关闭当前面板。

样例已追加到既有 GlobalConfig 和 Build Settings。新工程可先保存当前场景，再执行 `Tools / FrameWork_Ranger / UI / Create Sample Assets`；工具只创建缺失资产，保留既有业务配置。已有其他 UI 配置时需将样例目录合入该配置，工具会明确报告冲突。

扩展演示入口为 `UI_Components.unity`，面板自动打开；`UI_OptionalFailure.unity` 故意缺少相机绑定，记录错误后仍打开组件面板；`UI_RequiredFailure.unity` 演示必须成功失败导致 SceneScope 回滚。后两者在 Console 产生错误是演示的预期结果。可执行 `Tools / FrameWork_Ranger / UI / 创建扩展组件演示` 只创建缺失资产。

多图描边仅面向同 Canvas 的普通 Image/UIImage，不合并文字、RawImage 或复杂自定义材质，输出不拦截射线；建议把需要描边的图片放在紧凑组内，避免超大合成纹理。暂不包含多实例、绑定代码生成、复杂流式布局和虚拟列表。本轮契约与实际验收见 [资源配置与组件扩展](../../Docs/03_Architecture/FoundationModules/UI/07_Resources_And_Components_Evolution.md)，首版历史检查见 [原实施与验收](../../Docs/03_Architecture/FoundationModules/UI/05_Implementation_And_Acceptance.md)。
