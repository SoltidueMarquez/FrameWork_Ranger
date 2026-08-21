# ADR-DIST-001：迁移到 Unity 6000 与 FrameWork_Ranger 仓库

- 状态：已接受
- 日期：2026-08-22
- 范围：工程版本、仓库历史、第三方依赖与迁移清理边界

## 背景与约束

Framework_WWJ 原工作区位于 `D:\unityhub\UnityProjects\Framework_Test`，使用 Unity 2022.3.62f3，旧远端为 `SoltidueMarquez/Framework_WWJ`。新的 Unity Hub 工程位于 `D:\unityhub\UnityProjects\FrameWork\FrameWork_WWJ`，使用 Unity 6000.5.9f1、URP 17.5.0，并连接新远端 `SoltidueMarquez/FrameWork_Ranger`。

新工程已经由用户导入 Odin 4.0.2.3 与 DOTween 1.2.825。它们的资产、`.meta`、DOTweenSettings 和编译宏属于目标工程基线，迁移不得用旧仓库中的第三方版本覆盖。

## 决定

1. 以 Unity 6000 工程为目标树，保留其 URP、Input System、根 SampleScene、`Assets/Settings` 和主要 ProjectSettings。
2. 通过一个双父提交接入旧 `main` 的完整 Git 历史；目标基线 `c029fbf` 与旧框架基线 `6f84ae8` 都必须是最终提交的祖先。
3. 从旧树迁入 `Assets/Plugins/Framework_WWJ` 与 `Assets/AddressableAssetsData`，保留原 GUID；不迁入旧 `.idea`、旧第三方副本和带失效 Prefab 引用的旧根 SampleScene。
4. 以目标 Packages 为基础，加入 Addressables 2.9.1 与 UniTask 2.5.11；不改变框架公共 API、程序集名称或序列化契约。
5. Build Settings 保留 Unity 6 根 SampleScene 与 Input 配置，并追加三个框架示例场景和 Addressables 配置对象。
6. Unity 自动升级产生的差异必须逐项审查；`Library`、`Temp`、`Logs`、构建产物和无关设置漂移不进入 Git。
7. 直接记录 Unity 官方 batchmode 命令，不增加仓库内 CLI 包装脚本或 GitHub Actions。
8. 旧仓库删除前建立完整 bundle 并验证可恢复。新远端通过分支、测试和重新克隆验证后才能快进 `main`；禁止 force-push。
9. Odin 是付费资产；包含 Odin 的 GitHub 仓库必须保持私有，未经明确排除不得推送到公开远端。
10. Unity Cloud 项目和组织不在本次清理范围内。

## 兼容性调整

Unity 6000 将 `Scene.handle` 暴露为 `SceneHandle`，其 `GetRawData()` 返回 `ulong`。框架内部场景描述、协调器和 Runtime 所有权令牌统一提升为 `ulong`，两处 Unity 场景桥接使用 `scene.handle.GetRawData()`；测试辅助代码中已知为正数的合成 `int` 令牌在边界显式提升。这个变化只影响内部所有权链，不改变 `Module`、`Handler`、Resource 公共 API 或持久化数据。

## 验收门禁

- Unity 6000.5.9f1 完成依赖解析和无编译错误导入。
- EditMode 67/67、PlayMode 18/18 通过。
- 架构目录仍为 104 个正式节点、25 个 Resource 节点、零诊断。
- URP SampleScene、Input、FrameworkProjectSettings、三个框架示例和全部序列化引用无 Missing。
- Addressables 本地内容、StandaloneWindows64 Player 和 `-frameworkWwjResourceSmoke` 双后端冒烟通过，进程退出码为 0。
- 新远端重克隆后工作树干净，两个基线都可由 `main` 到达，且 `origin` 只指向 `FrameWork_Ranger`。

本地实现与 Unity 6000 隔离验证结果见 [迁移验收](./01_Unity6_Migration_Acceptance.md)；远端与清理门禁仍按该文档的未关闭项执行。

## 清理门禁

永久删除旧 GitHub 仓库由用户在网页执行。只有确认旧 URL 不可访问、备份 bundle 可恢复、新远端重克隆与验证全部通过后，才删除旧本地目录 `D:\unityhub\UnityProjects\Framework_Test`。备份 bundle 永久保留。
