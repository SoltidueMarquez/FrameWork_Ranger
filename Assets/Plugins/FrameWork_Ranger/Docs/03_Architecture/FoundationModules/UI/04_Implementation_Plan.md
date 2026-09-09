# UI 实施计划

2026-09-09 已执行的单模块实施计划。对应 [公共契约](./03_Architecture_And_Public_Contracts.md)，原有行为问答与单个 GlobalUIModule 方向已经确认。最终两项选择已确认，实施结果见 [验收记录](./05_Implementation_And_Acceptance.md)。工作进度只在 `.workflow/ui-module/progress.json` 维护。

## 0. 本轮审阅重点

已确认行为保持有效。本计划按以下已审阅方式执行：

- 已确认只安装一个 GlobalUIModule，并在模块内集中配置 Global/Scene 两组域与面板目录，各场景按需打开，不预加载全部资源（REQ-UI-016）。首期不增加场景配置映射。
- 已确认新增可选 ISceneScopeLifecycle 接口（REQ-UI-017），由已加载 Global 模块实现，Runtime 固定顺序调用 Starting/Ending。UI 在 Scene 模块加载前建立上下文，在卸载/回滚前统一清理。保留配置预检失败时旧 Scope/UI 不变的现有语义。接口不替换 Driver、不依赖 EventModule、不放开 Global 反查 Scene。完整顺序与失败处理见公共契约 1.1；正式决定与实现已落 ADR-007。
- 单模块通过 ui.Global / ui.Scene 轻量上下文对外操作，保留同 owner/key 单实例语义；Scene 上下文绑定具体轮次。TryGetScene 用于探测过渡期，旧上下文不会切换为新场景。
- UIView 保存组件引用，UILogic<TView> 处理业务生命周期；配置保存 Logic 模板，运行时创建独立 Logic。不额外引入没有替换用途的 Handler 层。
- 公开操作以 panel key 定位配置，以带 owner/轮次的 handle 定位一次打开；Open/OpenChild 异步，Refresh/Close 等已有实例操作同步。data 暂用 object 配合 Logic 内类型校验，后续若需要强类型入口应说明修改后的签名。
- Prefab 通过已有 ResourceModule 获取资源租约；保留实例时保留租约，销毁后释放。列表第一版按稳定 key 复用存量条目、新建/删除差异项，不将所有 UI 强制接到仅属于 Scene 的 GameObjectPoolModule。
- 已确认重新绑定恢复；首次缺少 Camera/World 必需绑定时直接打开失败，加载前和提交前均检查；既有 Global 失绑仍暂停/恢复（REQ-UI-019）。
- 自定义 Layout 接入 uGUI 的测量/分配流程，提供独立可用的基础组件和统一批处理刷新；不复制 HTY 的整套全局队列与缓存体系。

所有公共设计选择已确认，执行下述切片。已确认的面板行为、组件范围不重新逐题询问。每次仅呈现一个影响结果的选择，无回答不自动推进；全部确定后按用户本轮指示开始实施，不再逐脚本请求批准。

实现分为下面九个片段，每段有明确结果。必要的编译和行为检查跟随变更批次进行，不把文档完成当作模块完成。

## 1. 目标目录

```text
BaseModules/UI/
  Runtime/
    Module/        GlobalUIModule、内部运行容器、所有者上下文
    Configuration/ 域、面板、策略配置与校验
    Panels/        Logic、View、Handle、实例与异步操作
    Domains/       三种渲染域、运行绑定组件
    Interaction/   模态协调、焦点与输入限制
    Components/    按钮、切换组、滚动工具
    Collections/   稳定 key 列表同步
    Layout/        基础布局、内容适配、刷新调度
  Editor/          Framework Center 页面、创建和校验
  Tests/EditMode/  配置、状态、关系与几何等聚焦检查
  Tests/PlayMode/  真实生命周期、交互、绑定及资源检查
  Samples/         独立配置、Prefab、双场景和使用脚本
```

这是职责划分，不要求每个私有辅助类独立成目录；公开类型保持一文件一主要类型。最小程序集先是 Runtime/Editor 与必要 Tests/Samples，不机械复制 HTY 或 YokiFrame 的程序集数量。全部 Unity 资产维护 `.meta`，生产程序集加入既有架构元数据。

Core 受影响处限于 Runtime/Abstractions 与 Scope 的可选生命周期接口与只读身份、FrameworkRuntime/ModuleScopeRuntime 的固定调用位置，以及对应 Core 测试。ModuleGraphResolver 的重复类型和依赖方向、FrameworkSceneConfig 模块表、现有 Driver 签名保持既有契约。实现时如发现需要扩大边界，先在设计中说明实际原因。

## 2. 可验收切片

| 工作项 | 关联要求 | 实施与可观察结果 |
| --- | --- | --- |
| UI-0 场景生命周期能力 | 003、007、011、015、017 | 正式 Core ADR；可选接口、轮次身份、固定 Starting/Ending 顺序；配置失败保留旧 Scope、空 Scope、加载失败、取消和 Shutdown 聚焦测试 |
| UI-A 配置与模块装配 | 003、004、015、016、018 | 单个 GlobalUIModule、Global/Scene 集中目录、按需加载、安装范围校验、模板/运行状态分离、资源依赖、所有者与配置诊断；两类上下文和无有效 Scene 时的调用结果 |
| UI-B 面板生命周期 | 007、008、012 | Logic/View、唯一实例、异步物化、轮次 handle、关闭保留/销毁、重复刷新同层置顶；失败与卸载清理 |
| UI-C 域与绑定 | 003、011、019 | 三种模式、显式场景绑定、借用目标清理、Global 丢失绑定暂停、重新绑定恢复；不重放 OnOpen |
| UI-D 关系与输入 | 006、013、014 | 分组互斥、明确父面板依附、关闭联动、域内/全局模态，嵌套焦点恢复与选择清理 |
| UI-E Layout | 005 | 横纵网格、内容适配、忽略项、批处理与显式 Flush；动态文字和嵌套尺寸收敛 |
| UI-F 组件与列表 | 009 | 点击/长按、切换/选择组、滚动至项、按 key 同步和清理；原生 Image/TMP 兼容 |
| UI-G Editor 与样例 | 010及上述要求 | Center 创建/校验、手动引用、独立示例和双场景重绑定，代码架构目录正常 |
| UI-H 验收与文档 | 001、002及上述要求 | 真实编译、聚焦行为检查、三种模式视觉核对、使用说明与实际限制 |

三大基础模块无关历史待办不进入 UI 切片。已明确排除的动画、拖拽、折叠、多实例和代码生成不创建占位 API。

## 3. 关键验证

1. 配置：重复 key、缺失域/Prefab、Scope 装错、资源后端配置错误可定位，模板运行后不被写入 View 或运行字典。
2. 生命周期：同 key 并发只物化一次；单等待者取消不影响他人；所有等待者取消、Close/Unload 后迟到结果不复活。旧轮次 handle 不关闭重新打开的面板。
3. 保留与销毁：KeepAlive 保持实例/lease，但解除本轮业务订阅；DestroyOnClose 和模块卸载释放资源。OnCreate/OnOpen/OnClose 异常仍能清理。
4. 重复打开：新数据进入 OnRefresh，OnOpen 次数不增加，置顶不越配置层级。
5. 场景：覆盖预检失败保留旧 Scope/UI、空 Scope、Starting 自身失败仍配对 Ending、Scene 装配失败回滚、正常卸载、活动场景替换但旧 Additive 场景仍存活、框架关闭。旧上下文/解绑不作用于新轮次，清理异常不跳过其他参与者或 Scene 模块。旧所有者缓存面板和迟到打开均被清理。Global 面板不挂在即将销毁的 Scene 父节点下；Camera/World 丢失绑定暂停输入和显示，重新绑定恢复且不再次 OnOpen；首次未绑定明确失败且不触发 OnOpen、无半成品残留。
6. 关系：资源准备失败时旧互斥面板保留；同组互斥和不同组并存；依附关闭只影响其实际所有者；焦点恢复与关闭分离。
7. 输入：域内模态不冻结其他域；全局模态跨 Global/Scene 所有者生效；嵌套弹窗可操作；解除模态不会把业务禁用的控件启用。验证指针与 EventSystem 选择，不能只检查一个 bool；跨模式输入限制不假装等于可见遮挡。
8. Layout：固定宽度动态换行、嵌套自适应、零子项、忽略项、禁用/销毁/重新挂父节点，批次去重且重入新增请求不丢失。检查静止时无持续重排。
9. 列表/组件：重复数据 key 不造成半更新；删除条目解绑；长按不补发点击，禁用终止按压；目标条目进入可见滚动区域。
10. Editor：创建不覆盖用户资产或手写代码，缺失引用有实际路径；运行只读检查不修改模板；Unity Console 不出现 UI 代码编译错误。

测试围绕上述非平凡行为组织，复用已有测试设施，不按数量或镜像实现凑用例。三种渲染模式还需要可见场景检查；逻辑测试通过不等同于相机/遮挡/世界位置正确。

## 4. 验证执行和交付

按项目规则使用 `Tools/UnityCli.ps1`。先核对当前 Git 改动和工程占用，必要时 Doctor；C# 批次完成运行 Import，再运行 UI 聚焦 EditMode 与 PlayMode。涉及共享装配时加对应 Core 图解析检查，涉及真实后端时复用 Resource 场景/用例证明受影响行为。

不默认 TestAll、完整 Player 或重复全后端构建。不自动关闭用户 GUI Unity；被占用时可按现有规则验证隔离副本，并明确记录对象和限制。样例生成只追加自身所需配置，不重置其他模块。

交付产物：完整 UI 模块、可运行样例、README 使用步骤、重要架构决策、真实验收记录与任务 JSON。各项实测结果与保留边界见验收记录；不将计划条目当作全部已覆盖的测试断言。

