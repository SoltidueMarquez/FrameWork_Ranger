# Phase 1.7：配置资产内联 Inspector 验收与复盘

> 自动化状态：通过<br>
> 真实窗口视觉状态：待用户确认<br>
> 验证日期：2026-08-22<br>
> Unity：6000.5.9f1

## 1. 交付结论

项目配置页的 Global Config、Default Scene Config、场景覆盖 Scene Config，以及 Global/Scene Config 中的 Module 引用均已接入按需眼睛按钮。展开区域调用目标真实 Inspector，支持配置到 Module 的两级编辑；UI 状态只进入 `SessionState`，未改变 Runtime 公共契约或资产序列化格式。

HTY/LyingBottle 和 YokiFrame 未修改。权威工程由 Unity GUI 打开，因此本轮按照 CLI 规则，以当前 `Assets`、`Packages`、`ProjectSettings` 建立隔离副本验证。

## 2. 自动化证据

| 验证项 | 结果 | 证据 |
| --- | --- | --- |
| Import / C# 编译 | Passed | `20260822-043415-import-3216/import.log`，退出码 0 |
| Phase 1.7 定向 EditMode | Passed，10/10 | `20260822-043458-testeditmode-31696/editmode-results.xml`，失败 0 |
| 完整 EditMode | Passed，83/83 | `20260822-043540-testeditmode-33324/editmode-results.xml`，失败 0 |
| 完整 PlayMode | Passed，17/17 | `20260822-043623-testplaymode-23368/playmode-results.xml`，失败 0 |
| 架构目录 | Passed | `FrameworkArchitectureCatalogTests` 通过，生产目录诊断为零 |

隔离验证根目录：`D:\unityhub\UnityProjects\FrameWork\Framework_WWJ_Phase17_20260822_01\Logs\UnityCli`。

## 3. 新增 EditMode 覆盖

- 新会话键默认收起，以及展开/收起的 SessionState 往返。
- 不同引用槽位可同时展开且互不影响。
- 引用替换会清理旧目标状态，稍后换回旧资产也不会意外展开。
- 展开状态变化不会标记目标资产 Dirty。
- 相同目标 Editor 复用缓存，收起和宿主 Dispose 会销毁缓存。
- 列表删除或场景绑定身份变化会释放失效槽位。
- GlobalConfig 引用通过 Unity 注册表命中现有 `ModuleConfigInspector`。
- 场景绑定优先使用 Scene GUID，空 Scene 使用索引槽位。

## 4. 真实窗口检查清单

请在 `Framework_WWJ/Framework Center > 项目配置` 和独立 Global/Scene Config Inspector 中确认：

- [ ] 深色与浅色主题下，开启/关闭眼睛图标及 Tooltip 清晰。
- [ ] Global、Default Scene 和场景覆盖 Config 均能独立展开、编辑和收起。
- [ ] Global/Scene Config 的多个 Module 可以同时展开，列表重排后仍对应原 Module。
- [ ] 打开 Config 后继续打开 Module，二级内联布局和滚动可正常使用。
- [ ] 空引用按钮禁用；替换引用或删除条目后旧 Inspector 立即消失。
- [ ] 属性修改可保存并支持 Undo/Redo；只切换眼睛不会让资产变脏。
- [ ] 页面切换后当前 Unity 会话恢复展开状态；重启 Unity 后默认收起。
- [ ] 子 Inspector 报错时只显示局部错误框，仍能使用眼睛按钮收起。

在这些真实窗口项确认前，本记录只声明代码与自动化验收通过，不声明人工视觉验收关闭。

## 5. 复盘

- 直接复用目标注册的 Unity Editor，使 Odin、自定义诊断和未来模块 Inspector 自动进入同一内联入口。
- SessionState 保存目标标识而非简单布尔值，引用变化时可以识别并清理陈旧展开状态。
- 宿主按有效槽位收口缓存，解决列表删除后不可见 Editor 持续存活的问题。
- HTY 的眼睛交互被保留，但资产内 UI 字段和每帧 PropertyTree/Editor 创建没有进入 Framework_WWJ。
