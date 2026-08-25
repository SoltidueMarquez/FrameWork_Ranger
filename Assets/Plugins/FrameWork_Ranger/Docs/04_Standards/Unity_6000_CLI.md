# Unity 6000 CLI 开发与验证

> 配置日期：2026-08-22<br>
> 工程：`D:\unityhub\UnityProjects\FrameWork\FrameWork_Ranger`<br>
> Editor：Unity 6000.5.9f1<br>
> Shell：PowerShell 7 或 Windows PowerShell 5.1

项目直接使用 Unity Editor CLI，不依赖 MCP 服务。仓库入口是根目录的 `Tools/UnityCli.ps1`；它读取 `ProjectSettings/ProjectVersion.txt`、定位完全匹配的 Editor、检查同工程占用、检查退出码与 NUnit XML，并把日志和结果写入已被 Git 忽略的 `Logs/UnityCli`。

本文只提供命令速查。所有开发与验收必须遵守 [Unity CLI 开发规则](./Unity_CLI_Development_Rules.md)；版本解析、进程等待、测试判定、隔离验证和排障原理见 [Unity CLI 技术参考](./Unity_CLI_Technical_Reference.md)。

## 1. 首次检查

从项目根目录运行：

```powershell
pwsh -File .\Tools\UnityCli.ps1 -Task Doctor
```

当前工作站会自动定位 `D:\unityhub\6000.5.9f1\Editor\Unity.exe`。其他工作站可通过参数或用户环境变量覆盖：

```powershell
pwsh -File .\Tools\UnityCli.ps1 -Task Doctor `
  -UnityEditor 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe'

[Environment]::SetEnvironmentVariable(
  'UNITY_EDITOR_PATH',
  'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe',
  'User'
)
```

脚本拒绝版本不匹配的 Editor。运行实际任务前保存并关闭同一工程的 GUI Editor；Unity 不允许两个 Editor 进程同时打开同一 `projectPath`。

## 2. 日常开发命令

```powershell
# 导入、依赖解析与脚本编译
pwsh -File .\Tools\UnityCli.ps1 -Task Import

# 单独运行测试
pwsh -File .\Tools\UnityCli.ps1 -Task TestEditMode
pwsh -File .\Tools\UnityCli.ps1 -Task TestPlayMode

# 顺序运行 EditMode + PlayMode
pwsh -File .\Tools\UnityCli.ps1 -Task TestAll
```

默认导入不会接受 API Updater 修改。只有升级 Unity 或依赖并准备审查兼容性改动时才显式启用：

```powershell
pwsh -File .\Tools\UnityCli.ps1 -Task Import -AcceptApiUpdate
git status --short
git diff
```

不要提交 `Library`、`Temp`、`Logs` 或无关设置漂移。

## 3. 聚焦测试

Test Framework 1.7.0 支持按完整测试名/正则、分类和程序集过滤：

```powershell
pwsh -File .\Tools\UnityCli.ps1 -Task TestEditMode `
  -AssemblyNames 'FrameWork_Ranger.Tests.EditMode'

pwsh -File .\Tools\UnityCli.ps1 -Task TestPlayMode `
  -TestFilter 'FrameWork_Ranger.*Resource*'

pwsh -File .\Tools\UnityCli.ps1 -Task TestEditMode `
  -TestCategory 'Smoke'
```

分号分隔多个值时，把整个值保留在同一对引号内。过滤后零用例会被脚本判为失败，避免错误过滤造成“假通过”。

Test Framework 会在执行结束后关闭 Editor，因此测试命令不传入 `-quit`。Unity 6 官方文档明确指出 `-runTests` 与 `-quit` 同用会在测试完成前退出。结果 XML 是用例计数与失败详情的事实来源；当前项目基线为 EditMode 67/67、PlayMode 18/18，完整迁移运行还会包含 Addressables 包的附加 EditMode 用例。

## 4. 构建与 Resource 冒烟

```powershell
# Addressables 本地内容
pwsh -File .\Tools\UnityCli.ps1 -Task BuildAddressables

# 默认输出 Builds\UnityCli\FrameWork_Ranger.exe
pwsh -File .\Tools\UnityCli.ps1 -Task BuildWindows64

# 运行已构建 Player 的 Resources + Addressables 双后端冒烟
pwsh -File .\Tools\UnityCli.ps1 -Task ResourceSmoke
```

可以用 `-PlayerPath` 覆盖 Player 路径，用 `-OutputRoot` 覆盖本次日志与 XML 目录。Resource 冒烟的成功标准是退出码 `0`，并且日志同时记录 Resources 与 Addressables 后端成功；不能只以“Player 能启动”替代双后端断言。

## 5. 失败判定

- Unity 或 Player 进程退出码非 `0`。
- 测试未生成 NUnit XML、执行用例数为 `0`、根结果不是 `Passed` 或失败数非 `0`。
- `ProjectVersion.txt` 与实际 Editor 产品版本不一致。
- 进程命令行、Unity 窗口或活跃 `Library/UnityLockfile` 表明同一工程已被占用。

## 6. 官方参考

- [Unity 6 Editor 命令行参数](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)
- [Unity Test Framework 命令行参数](https://docs.unity3d.com/Packages/com.unity.test-framework@1.7/manual/reference-command-line.html)
- [Addressables BuildPlayerContent API](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/api/UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent.html)
