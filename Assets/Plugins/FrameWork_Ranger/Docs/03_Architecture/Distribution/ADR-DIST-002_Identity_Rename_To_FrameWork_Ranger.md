# ADR-DIST-002：产品身份统一为 FrameWork_Ranger

- 状态：已接受
- 日期：2026-08-25
- 范围：现行产品名、本地工程目录、插件路径、C# 命名空间、程序集、序列化类型名、菜单、资源键、CLI、当前文档与 Codex Skills

## 背景与约束

GitHub 远端已经是 `SoltidueMarquez/FrameWork_Ranger`。本地 Unity 工程、插件目录、C# 身份和文档仍使用 `FrameWork_WWJ` / `Framework_WWJ`。两套名字同时出现会让仓库、Hub、代码和 Skills 看起来像不同项目。

本次只统一现行身份。不改 Git 远端、不重写提交历史、不 force-push。`Docs/01_History` 与已关闭阶段证据继续保留当时的 `Framework_WWJ` 字样。

## 决定

1. 现行产品名、Unity `productName`、本地工程目录、插件根目录一律为 `FrameWork_Ranger`。大小写与 GitHub 仓库名一致。
2. C# 根命名空间与 asmdef 名称使用 `FrameWork_Ranger`。Editor 为 `FrameWork_Ranger.Editor`，Resource 模块为 `FrameWork_Ranger.ResourceManagement*`。
3. 编辑器菜单为 `FrameWork_Ranger/Framework Center`。运行时宿主对象与日志前缀为 `[FrameWork_Ranger]`。
4. 示例 Resources 路径改为 `FrameWork_Ranger/ResourceManagement/ResourcesSamplePrefab`。Addressables Group 名称改为 `FrameWork_Ranger ResourceManagement Samples`，地址改为 `framework-ranger/samples/resource-management/addressables-prefab`。
5. CLI 冒烟标志写成 `-frameworkRangerResourceSmoke`。这是命令行惯例例外：标志继续使用小写 `framework` 前缀，产品名仍是 `FrameWork_Ranger`。默认 Player 为 `Builds/UnityCli/FrameWork_Ranger.exe`。
6. Codex Skills 目录与 `$` 名称改为 `framework-ranger` 形式；旧 `$...-wwj` 名称不再保留别名。
7. 目录与 asmdef 文件用 `git mv` 改名，配套 `.meta` 一起改名且 GUID 不变。
8. 历史文档不改写成“从来就叫 Ranger”。`ADR-DIST-001` 仍记录迁移当时的旧目录和旧仓库名。

## 身份映射

| 旧现行名 | 新现行名 |
| --- | --- |
| `FrameWork_WWJ` / `Framework_WWJ` | `FrameWork_Ranger` |
| `Assets/Plugins/Framework_WWJ/` | `Assets/Plugins/FrameWork_Ranger/` |
| `Framework_WWJ.Runtime` | `FrameWork_Ranger.Runtime` |
| `namespace Framework_WWJ` | `namespace FrameWork_Ranger` |
| `$work-with-framework-wwj` | `$work-with-framework-ranger` |

## 非目标

- 不改变 Module/Handler/Scope 生命周期、依赖排序或回滚语义。
- 不实现分发 App，不拆 UPM 包，不开始 Pooling。
- 不修改 Odin、DOTween 或其他第三方插件身份。

## 后果

Odin 资产中的程序集限定名必须与新 asmdef 同时替换，否则配置会 Missing。Editor Center 的 Library 状态目录从 `Library/Framework_WWJ` 变为 `Library/FrameWork_Ranger`，旧会话页签状态不迁移。

## 验收

2026-08-25 在隔离工程 `D:\unityhub\UnityProjects\FrameWork\_Verify_RangerIdentity_20260825` 上用 Unity 6000.5.9f1 验证（权威工程当时被 GUI Editor 占用）：

| 项 | 结果 |
| --- | --- |
| Import / C# 编译 | 退出码 0；12 个 `FrameWork_Ranger.*` 程序集生成 |
| EditMode | Passed 93/93 |
| PlayMode | Passed 17/17 |
| Addressables | `Addressable content successfully built` |
| StandaloneWindows64 Player | 退出码 0，`FrameWork_Ranger.exe` 存在 |
| Resource 冒烟 | 退出码 0；日志含 `[FrameWork_Ranger][ResourceStandaloneSmoke] PASS 双后端 Acquire/Instantiate/Destroy/Release` |

插件根 `.meta` GUID 仍为 `64a8c79de1abcfa42a12ccacf15fb40b`。本地工程目录重命名需在关闭占用 `FrameWork_WWJ` 的 Unity GUI 后执行。
