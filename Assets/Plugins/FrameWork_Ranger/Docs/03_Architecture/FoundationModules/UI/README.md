# UI 模块

2026-09-09 首版已实现；2026-09-10 按批准设计扩展独立资源总配置/面板 SO、场景适用范围和自动打开、动画及组件工具。最新契约及本轮验收见 [资源配置与组件扩展](./07_Resources_And_Components_Evolution.md)，下文中的“首期”描述保留为首版背景。

用户已确认单个 GlobalUIModule，模块内部管理 Global/Scene 两种 UI 生命周期；两者均支持 Overlay / Camera / World 三种渲染域。面板采用 Logic + View，关闭逐面板配置保留或销毁；SceneScope 结束清理对应 UI，GlobalUIModule 卸载清理全部 UI。首期同 owner/key 只支持单实例。

面板关系已确认以 HTY 配置式关系为主，并区分显示顺序、焦点与输入、关闭联动；依附弹窗显式指定同 owner/域父面板，沿自身依附链关闭，禁止抢占其他父面板的单实例弹窗。配置已确认由 GlobalUIModule 集中管理 Global/Scene 两组目录，各场景按需打开。生命周期已确认采用 ISceneScopeLifecycle 由框架自动通知 UI；调用入口确定为 ui.Global/ui.Scene；首次缺少 Camera/World 必需绑定直接打开失败，既有 Global 面板失绑仍暂停恢复。

基础运行时组件和工具范围已确认，包含按钮（点击/长按/交互开关）、切换/选择组、滚动至项、View/Item、按 key 同步列表、RectTransform 与布局刷新工具，兼容 Image/TMP。Editor 接入 Framework Center，提供创建和校验，引用手动绑定；拖拽、折叠组、动画和绑定代码生成不纳入首期。

- [源码参考研究](./01_Reference_Research.md)：HTY 为主，YokiFrame 为辅，区分事实、推断和建议。
- [需求与设计起点](./02_Design_Starting_Point.md)：问答形成过程与设计演进。
- [架构与公共契约](./03_Architecture_And_Public_Contracts.md)：单模块契约；包含已实现的场景生命周期接口、配置、调用示例与异常清理。
- [实施计划](./04_Implementation_Plan.md)：职责目录、需求关联切片与聚焦验收方式。
- [实施与验收](./05_Implementation_And_Acceptance.md)：实现映射、真实检查结果和验证边界。
- [配置编辑器改版](./06_Configuration_Editor.md)：配置下拉、独立项目使用按钮与共用中文 Inspector。
- [资源配置与组件扩展](./07_Resources_And_Components_Evolution.md)：本轮 SO 工作流、就绪通知、动画和组件扩展。
- [模块使用说明](../../../../BaseModules/UI/README.md)：安装、Logic/View、绑定、组件与样例入口。
- [有效需求与问答](../../../../../../../.workflow/ui-module/requirements.json)
- [任务进度与恢复](../../../../../../../.workflow/ui-module/progress.json)

沿用既有模块流水线；UI 是 Resource、Pooling、Event 之后的新模块，资料沿用本目录组织，不改变三大基础模块的定位。参考工程保持只读，参考工程的开发规则不自动成为 Ranger 的产品要求。

Global Camera/World 失去场景绑定时暂停呈现与输入，保留打开状态，重绑定后恢复而不重放 OnOpen。重复打开面板执行独立刷新并同层置顶。模态可配置域内/全局输入限制，默认域内，不暂停游戏。这些行为均已由逐题回答确认。

