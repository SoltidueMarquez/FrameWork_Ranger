# Unity 6000 与 FrameWork_Ranger 迁移验收

> 验收日期：2026-08-22  
> Unity：6000.5.9f1  
> 状态：本地迁移与隔离工程验证通过；远端隐私、推送、重克隆和旧仓库删除门禁待完成

## 1. Git 与恢复基线

- 目标 Unity 6 基线：`c029fbf4902475cdeb576bcb8f7c74909c0cbd0b`。
- 用户 Odin/DOTween 依赖基线提交：`2ef4b0f99d023004c41fe1ef155a1c0e95c102e9`。
- 旧框架基线：`6f84ae8a51a6f633c539d938708f386e67aeeec9`。
- 双父历史合并提交：`e18dec5a335e5ec4d1b44a3a528a59e26ebc2e90`。
- `git merge-base --is-ancestor` 已确认目标与旧框架两个基线都是迁移分支祖先。
- 备份：`D:\unityhub\UnityProjects\FrameWork\_MigrationBackups\Framework_WWJ-main-6f84ae8.bundle`。
- bundle 大小：5,082,063 bytes；SHA-256：`8E0E3A42F5218D3554E8E570A14D24910F2D83C76375A4AF45599EBAEF5E6CF6`。
- `git bundle verify`、临时 bare clone 与 `git fsck --full` 均通过，恢复 clone 的 `main` 指向 `6f84ae8`。

## 2. 迁入边界

- 保留 Unity 6 的 URP SampleScene、Input System、`Assets/Settings`、ProjectSettings 与用户导入的 Odin 4.0.2.3、DOTween 1.2.825。
- 迁入完整 `Assets/Plugins/Framework_WWJ`、`Assets/AddressableAssetsData` 及原 `.meta`。
- 未迁入旧 `.idea`、旧 Odin/DOTween、副本 ProjectSettings 和失效的旧根 SampleScene。
- Build Settings 包含 Unity 6 根 SampleScene、CoreSkeleton A/B、ResourceManagementSample，并同时保留 Input 与 Addressables 配置对象。
- 全部 Assets `.meta` GUID 扫描为零重复。

## 3. Unity 6 兼容调整

- Packages 锁定 UniTask 2.5.11、Addressables 2.9.1、URP 17.5.0、Input System 1.20.0、Test Framework 1.7.0。
- Scene Handle 内部所有权链提升为 `ulong`，Unity 边界使用 `scene.handle.GetRawData()`。
- 示例克隆身份诊断由已废弃的 `GetInstanceID()` 改为 `GetEntityId().GetHashCode()`，保持原 `int RuntimeInstanceId` 属性签名。
- 公共 Module、Handler、Resource API、程序集名称和序列化契约未改变。

## 4. 自动验证结果

验证在只含目标 `Assets`、`Packages`、`ProjectSettings` 的隔离副本中执行。首次导入后逐文件 SHA-256 比对，除 Unity 生成的 `packages-lock.json` 外与目标输入一致；该锁文件已回写目标工程。临时序列化扫描器只存在于隔离副本，不进入仓库。

| 门禁 | 结果 |
| --- | --- |
| Unity 6000 导入与脚本编译 | 通过，退出码 0 |
| EditMode | 框架 67/67；Addressables 包附加 1/1；XML 总计 68/68 |
| PlayMode | 18/18 |
| 架构目录 | 类型清单未增删；104 个正式节点、25 个 Resource 节点；目录诊断测试通过且为零 |
| Build Settings GUID | 4 个场景与 Input/Addressables 两个配置对象全部解析到现有 `.meta` |
| 序列化引用 | 4 个场景、366 个项目自有对象，Missing 0 |
| Addressables 本地内容 | 通过；catalog、content state 与 2 个 Windows bundle 已生成 |
| StandaloneWindows64 | 通过，退出码 0；C# warning 0、build error 0 |
| `-frameworkWwjResourceSmoke` | PASS；Resources/Addressables 双后端 Acquire/Instantiate/Destroy/Release；退出码 0 |

测试命令见 [Unity 6000 CLI 验证命令](../../04_Standards/Unity_6000_CLI.md)。隔离工程、Library、Logs、测试 XML、Addressables 生成物和 Player 构建均不提交。

## 5. 未关闭门禁

1. 保存并关闭当前 GUI Editor，退出并重新登录 Unity Hub 轮换曾出现在 Editor 日志中的会话凭证；不得提交任何 Logs。
2. `FrameWork_Ranger` 在迁移实施时是公开仓库。由于分支包含付费 Odin 资产，必须先将远端改为私有，或另行批准从 Git 中排除 Odin，才能推送。
3. 推送 `codex/unity6-migration`，全部远端检查通过后以 fast-forward 更新 `main`，禁止 force-push。
4. 从新远端重克隆，复验两个历史基线、干净工作树、测试和唯一 `origin`。
5. 用户在 GitHub 网页永久删除旧 `SoltidueMarquez/Framework_WWJ`；确认旧 URL 不可访问后再删除旧本地目录。bundle 永久保留，Unity Cloud 不删除。

