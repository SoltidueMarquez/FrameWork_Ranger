# Framework_WWJ Phase 1.3：预览页签与固定快捷页签验收与复盘

> 状态：已实施并通过自动化验收<br>
> 验收日期：2026-08-18<br>
> Unity：2022.3.62f3<br>
> 范围：Framework Center 页签状态、顶部页签交互、EditMode 测试与文档。

## 1. 交付结论

Phase 1.3 已完成。左侧“最近访问”已移除，导航只保留页面分类、搜索结果和扩展诊断。右侧顶部改为“有序固定页 + 唯一预览页 + `?`”：未固定页共用一个临时槽位，点击自绘图钉后才进入跨会话保存的快捷页列表。

新增纯 Editor `FrameworkCenterTabModel`统一处理预览替换、固定、取消固定、关闭回退和顺序调整。窗口只负责绘制和页面生命周期，因此固定或拖拽当前页不会重复触发 `OnDeactivated` / `OnActivated`。

## 2. 实际实现

- 本地状态升级为 v2，只持久化 `pinnedPageIds` 的稳定顺序与最后活动固定页。
- 旧 JSON 缺少 v2 版本号时直接回到概览预览，不把旧 `openTabs` 或 `recentPageIds` 解释成用户主动固定。
- 固定页使用水平滚动区；标签主体移动超过 4px 后开始拖拽，提供插入线、边缘自动滚动和 `Esc` 取消。
- 图钉由 `FrameworkCenterStyles` 使用 IMGUI 基本图元绘制，不依赖 Unity 内置图标名。
- 帮助页遵守普通页签规则；已固定时点击 `?` 会更新文档并激活原固定页，不复制页签。
- Runtime 公开 API、中央设置格式、Packages、ProjectSettings、Resources 与场景均未由本阶段修改。

## 3. 实现快照

| 区域 | C# 文件 | 物理行 | 本期变化 |
| --- | ---: | ---: | --- |
| `Runtime` | 41 | 3,479 | 无变更 |
| `Editor` | 30 | 4,200 | 新增页签模型，重做状态与顶部页签交互 |
| `Tests/EditMode` | 9 | 1,037 | 新增完整页签状态转换与 v2 持久化测试 |
| `Tests/PlayMode` | 7 | 707 | 无用例变更，作为 Runtime 回归门禁 |

Runtime/Editor 架构元数据声明为 65 项；生产中心仍只发现 5 个显式标记的页面。

## 4. 自动化验收

验证在项目内隔离副本中使用 Unity 2022.3.62f3 官方 Test Runner 执行，避免占用用户已打开的主 Unity Editor。由于当时 GitHub TLS 连接不可用，隔离副本临时指向主项目已锁定的 UniTask 2.5.10 本地缓存；正式 `Packages/manifest.json` 未改动。

| 门禁 | 结果 | 用例 | NUnit 时长 |
| --- | --- | ---: | ---: |
| 完整脚本编译 | Passed | Runtime / Editor / Samples / Tests 全部程序集 | — |
| EditMode | Passed | 33/33 | 0.305 s |
| PlayMode | Passed | 13/13 | 0.295 s |

EditMode 已覆盖：v1 状态丢弃、v2 往返、失效/重复 PageId 清理、单预览替换、固定页激活保留预览、固定/取消固定、关闭回退、顺序调整与帮助页一致性。PlayMode 13/13 继续证明本期未改变自动 Bootstrap、Global/SceneScope、Tick、失败回滚和 Shutdown。

## 5. 人工验收步骤

1. 在 Unity 打开 `Framework_WWJ → Framework Center`，确认左侧不再显示“最近访问”。
2. 从左侧依次打开“项目配置”、“代码架构”、“Core Skeleton 示例”；确认顶部始终只有一个斜体预览页，并由新页替换。
3. 点击预览页的空心图钉；确认该页移到左侧固定区，图钉变为实心强调色。
4. 固定两个以上页面，拖动标签主体调整顺序；确认插入线、边缘自动滚动、松手提交和 `Esc` 取消正常。
5. 分别点击图钉和关闭按钮：取消固定应将该页变成活动预览；关闭固定页应同时移除快捷入口。
6. 关闭当前固定页，验证左邻居 → 右邻居 → 已有预览 → 概览预览的回退顺序；`Ctrl/Cmd+W` 应一致。
7. 在普通页点击 `?`，将帮助页固定后切换其他页面再点击 `?`；确认使用原固定帮助页且文档内容更新。
8. 关闭并重开 Framework Center，然后重启 Unity；确认固定页及顺序恢复，上次预览页不恢复。
9. 重复验证 `Ctrl/Cmd+K`、`Esc` 清搜索、固定区水平滚轮和页面内容无遮挡。

## 6. 已知边界

- 批处理验收可以证明状态转换、持久化与编译，不能代替有界面 Unity 中的视觉、命中区和拖拽手感复验。
- 固定页不支持拖出窗口、拆分视图、跨窗口拖放或页面内部状态持久化。
- 预览页签不持久化；当没有可恢复的固定页时，窗口使用未固定的概览页作为默认。

## 7. 相关资料

- [Phase 1.3 实施计划](./06_Phase1_3_Preview_And_Pinned_Tabs_Implementation_Plan.md)
- [Framework Center 架构](./01_Editor_Center_Architecture.md)
- [ADR-EC-004：预览页签与固定页面快捷入口](./ADR/ADR-EC-004_Preview_And_Pinned_Page_Tabs.md)
- [Phase 1.2 验收与复盘](./05_Phase1_2_Editor_Center_UX_Acceptance_And_Review.md)
