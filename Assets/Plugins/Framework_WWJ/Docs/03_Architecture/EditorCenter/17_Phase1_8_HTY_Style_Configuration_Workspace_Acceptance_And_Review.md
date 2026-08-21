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
