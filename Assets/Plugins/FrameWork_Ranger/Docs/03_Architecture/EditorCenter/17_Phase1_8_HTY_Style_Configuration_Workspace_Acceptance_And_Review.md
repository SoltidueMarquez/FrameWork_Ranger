# Phase 1.8：HTY 式主从配置工作台验收与复盘

> 状态：自动化验收通过；真实窗口视觉检查待用户确认<br>
> 日期：2026-08-22<br>
> 验证对象：当前工作树的 `Assets`、`Packages`、`ProjectSettings` 隔离副本<br>
> Unity：6000.5.9f1

## 1. 自动化结果

权威工程由 Unity GUI 占用，未自动关闭。验证副本位于忽略目录 `Logs/UnityCli/phase18-isolated-20260822-052327`，使用项目统一入口 `Tools/UnityCli.ps1`。

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| Import / C# 编译 | Passed，退出码 0 | `20260822-052654-import-35592/import.log` |
| Framework_WWJ 定向 EditMode | Passed，70/70 | `20260822-053210-testeditmode-23864/editmode-results.xml` |
| 完整 EditMode | Passed，93/93 | `20260822-053331-testall-33008/editmode-results.xml` |
| 完整 PlayMode | Passed，17/17 | `20260822-053331-testall-33008/playmode-results.xml` |

完整 EditMode 包含 Framework_WWJ 自身与包附加用例。架构目录测试通过，当前新增生产类型均有声明式架构元数据且诊断为零。

## 2. 覆盖行为

- 工作台新会话默认 Global、页签往返、Scene GUID 稳定键、临时索引升级和删除后的选择修复。
- Center 页面默认宿主滚动与项目配置页自管滚动的兼容契约。
- Module 添加、替换、启停、删除、重排、Undo、Dirty、搜索、异常筛选和源索引映射。
- Odin 模块列表通过复制资产完成保存往返，未增加或迁移序列化字段。
- 互斥 Inspector 切换会销毁旧 Editor；删除活动槽位会清理 SessionState 与缓存。
- 原有 ModuleGraph、Bootstrap、生命周期、Resource Management EditMode/PlayMode 回归全部通过。

## 3. 人工检查清单

- [ ] 深色与浅色主题下，导航选中、Hover、错误/警告/通过颜色均清晰。
- [ ] Framework Center 最小宽度和更宽窗口下，260px 导航与右侧内容不互相遮挡。
- [ ] 普通窄 Inspector 使用作用域下拉，并隐藏模块类型、优先级和依赖等次要列。
- [ ] Global、Default Scene 和多个场景覆盖切换后，右侧 Inspector 与依赖图上下文一致。
- [ ] 搜索、筛选、重排、启停、替换、Ping、眼睛、删除、Undo/Redo 和保存行为符合预期。
- [ ] Module 自定义 Inspector/OdinEditor、Global DriverHandler 和异常模块详情可以正常编辑。
- [ ] 页面停用、引用替换和程序集重载后 Console 无 Editor 泄漏或 GUI 布局异常。

## 4. 复盘

- HTY 最有价值的部分是信息分层和单项详情，不是其暗色像素值或 PropertyTree 基础设施。
- Phase 1.7 的缓存与释放能力被保留并扩展为互斥组；旧 `ModuleConfigEntryDrawer` 已由共享紧凑视图取代，不再存在两套 Module 列表交互。
- 自动化无法判断真实窗口的视觉密度、文字截断和高 DPI 手感；这些项目保留为人工验收，不将自动化通过描述为视觉确认。

## 5. 2026-09-09：场景覆盖新建入口补充

- “添加场景覆盖”移到导航的场景覆盖分组标题旁；窄布局的“＋覆盖”提供明确的用途提示。添加后自动选中空绑定并切回配置编辑。
- 覆盖项 Scene Config 未关联时可点击“新建配置”。保存对话框默认使用场景所在目录和 `<场景名>SceneConfig.asset`；未选择场景时使用 `Assets/SceneConfig.asset`。新资产为空配置，成功后自动关联并展开编辑。
- 取消保存不修改绑定；已有文件拒绝覆盖。添加、关联和移除支持 Undo/Redo，撤销关联或移除绑定不会删除保存的配置资产。
- 共用工作台同时服务 Framework Center 和中央设置 Inspector；Runtime API、序列化结构和 Build Settings 不变。
- 验证副本：`D:/tmp/FrameworkSceneOverride-20260909`，来源为当前工作树的 Assets、Packages、ProjectSettings；使用 `Tools/UnityCli.ps1`，Unity 6000.5.9f1。
- Import 退出码 0；配置工作台、Center 基础设施和项目设置解析定向 EditMode **21/21 Passed**，退出码 0。新增测试覆盖自动切换编辑页签、增删关联 Undo/Redo、空配置、取消、同名资产保留以及场景身份和配置引用的保存重载。
- 日志和结果：`D:/tmp/FrameworkSceneOverride-20260909-logs/import.log`、`editmode.log`、`editmode-results.xml`。
- 布局源码已检查：宽布局入口随左侧导航滚动，窄布局入口位于顶部。后台窗口截图仍停留在 Scene 页，未取得配置页视觉证据；正常/窄/低窗口的实际显示和原生保存对话框交互保留为人工验收。
