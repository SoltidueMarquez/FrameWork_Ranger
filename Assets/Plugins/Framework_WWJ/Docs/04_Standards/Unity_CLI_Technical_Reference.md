# Framework_WWJ Unity CLI 技术参考

> 文档类型：当前实现事实与维护说明<br>
> 检查日期：2026-08-22<br>
> 脚本：项目根目录 `Tools/UnityCli.ps1`<br>
> 适用版本：Unity 6000.5.9f1、Test Framework 1.7.0、Addressables 2.9.1、Windows PowerShell 5.1 / PowerShell 7

本文解释 Framework_WWJ 的 Unity CLI 包装层如何定位 Editor、避免工程争用、等待 Windows GUI 进程、组织测试与构建、判定失败以及扩展新任务。强制约束以 [Unity CLI 开发规则](./Unity_CLI_Development_Rules.md) 为准；只想运行命令时阅读 [Unity 6000 CLI 开发与验证](./Unity_6000_CLI.md)。

## 1. 设计目标与非目标

### 1.1 目标

- 用一个可版本控制的入口复现本地开发和自动化验证。
- 从工程本身读取 Unity 版本，而不是依赖操作者记忆。
- 在启动 Unity 前发现版本错误和同工程占用。
- 对 Windows 路径空格、GUI 子系统进程等待和退出码提供稳定处理。
- 将日志、NUnit XML、Player 输出与具体任务关联。
- 把“进程完成”“测试通过”“产物存在”拆成独立断言。
- 支持隔离工程验证，不要求为了自动化强行关闭用户当前工作。

### 1.2 非目标

- 不取代 Unity Hub 的 Editor 安装和许可证管理。
- 不提供实时操控 Hierarchy、Inspector 或 Scene View 的远程协议。
- 不恢复 MCP/EditorMcpAdapter。
- 不把当前 Windows Player 构建入口抽象成通用多平台 CI 系统。
- 不在包装层隐藏 Unity、测试或构建失败。

## 2. 当前环境事实

| 项目 | 当前值 | 事实来源 |
| --- | --- | --- |
| 工程版本 | `6000.5.9f1`，revision `b57deb96f08d` | `ProjectSettings/ProjectVersion.txt` |
| 本机 Editor | `D:\unityhub\6000.5.9f1\Editor\Unity.exe` | `Doctor` + 文件产品版本 |
| Test Framework | `1.7.0` | `Packages/manifest.json` / `packages-lock.json` |
| Addressables | `2.9.1` | `Packages/manifest.json` / `packages-lock.json` |
| 默认日志根 | `Logs/UnityCli` | `Tools/UnityCli.ps1` |
| 默认 Player | `Builds/UnityCli/Framework_WWJ.exe` | `Tools/UnityCli.ps1` |

版本和测试数量都是易变事实。升级后必须重新读取源文件和本次 XML，不直接复制本文旧数值作为新验收结果。

## 3. 总体执行链

```text
调用参数
   │
   ├─ 解析 ProjectPath ──> 读取 ProjectVersion.txt
   │                              │
   ├─ 定位 Unity.exe <────────────┘
   │          │
   │          └─ 校验 ProductVersion
   │
   ├─ Doctor：输出诊断并结束
   │
   └─ 其他任务
          │
          ├─ 检查同 projectPath 的 Unity 进程
          ├─ 创建 Logs/UnityCli/<run>
          ├─ 生成安全的 Windows 参数字符串
          ├─ 启动进程并 WaitForExit
          ├─ 检查 ExitCode
          └─ 测试/构建专用结果断言
```

脚本使用 `Set-StrictMode -Version Latest` 和 `$ErrorActionPreference = 'Stop'`，未定义变量、路径解析错误和 PowerShell 非终止错误不会被静默忽略。

## 4. 参数模型

### 4.1 公共参数

| 参数 | 含义 | 默认值/行为 |
| --- | --- | --- |
| `-Task` | 要执行的任务 | `Doctor` |
| `-ProjectPath` | Unity 项目根 | `Tools` 的父目录，即仓库根 |
| `-UnityEditor` | 显式 Editor 路径 | 自动定位 |
| `-OutputRoot` | 本次日志/XML 目录 | `Logs/UnityCli/<timestamp>-<task>-<pid>` |
| `-PlayerPath` | Player 输入或输出路径 | `Builds/UnityCli/Framework_WWJ.exe` |
| `-AcceptApiUpdate` | 为 Import 添加 `-accept-apiupdate` | 关闭 |

### 4.2 测试过滤参数

| 参数 | 传给 Test Framework | 典型用途 |
| --- | --- | --- |
| `-TestFilter` | `-testFilter` | 完整测试名、名称列表或正则 |
| `-TestCategory` | `-testCategory` | Smoke、Integration 等分类 |
| `-AssemblyNames` | `-assemblyNames` | 只运行指定测试程序集 |

多个值由 Test Framework 使用分号解释。PowerShell 调用时必须把包含分号的整体放在同一对引号中，避免被 Shell 当成语句分隔符。

## 5. ProjectPath 与版本解析

### 5.1 ProjectPath

脚本位于根目录 `Tools`，没有传入 `-ProjectPath` 时通过 `$PSScriptRoot` 的父目录获得项目根。这使入口与调用者当前工作目录解耦；从其他目录执行仍指向脚本所在仓库。

显式相对路径以当前 PowerShell 工作目录为基准，最终通过 `System.IO.Path.GetFullPath` 规范化。路径必须存在且包含 `ProjectSettings/ProjectVersion.txt`。

### 5.2 版本来源

`Get-ProjectEditorVersion` 读取：

```text
m_EditorVersion: 6000.5.9f1
```

脚本不从文件夹名、`.sln`、Hub 最近项目或人工配置推断版本。`m_EditorVersionWithRevision` 用于文档事实，但当前定位契约以 `m_EditorVersion` 为版本键。

### 5.3 Editor 定位顺序

`Resolve-UnityEditor` 按以下顺序寻找候选：

1. 调用参数 `-UnityEditor`；
2. 环境变量 `UNITY_EDITOR_PATH`；
3. 当前工作站布局 `D:\unityhub\<version>\Editor\Unity.exe`；
4. `%ProgramFiles%\Unity\Hub\Editor\<version>\Editor\Unity.exe`；
5. 已知的 D 盘 Hub 目录；
6. `PATH` 中的 `Unity.exe`。

显式路径不存在或版本错误时立即失败，不继续回退。自动候选只有文件存在且产品版本匹配才会被接受。

### 5.4 产品版本校验

Windows 文件属性中的 `ProductVersion` 当前为：

```text
6000.5.9f1_b57deb96f08d
```

脚本要求它以 `ProjectVersion.txt` 中的 `6000.5.9f1` 开头，且后续只能是 `_` 或字符串结束。这样允许 Unity 附加 revision，同时拒绝 `6000.5.9f2`、其他分支或仅文件名相似的 Editor。

## 6. 同工程占用检测

Unity 不允许两个 Editor 同时打开同一项目。仅检查 `Library/UnityLockfile` 在 Unity 6000.5 上不充分：文件存在时，其他进程有时仍能取得文件句柄。因此脚本使用三层检测。

### 6.1 Win32_Process 命令行

脚本查询 `Unity.exe`，只解析 `-projectPath` 参数并对规范化后的完整路径做大小写不敏感的精确比较。它不会把完整命令行写到控制台，因为 Hub 启动参数可能包含会话信息。

精确比较避免把 `Framework_WWJ` 与 `Framework_WWJ_Copy` 这类前缀相同的项目误判为同一工程。Asset Import Worker 也携带 `-projectPath`，因此主 Editor 或其活跃 Worker 都能触发占用保护。

### 6.2 窗口标题回退

当系统策略不允许查询 Win32_Process 时，脚本检查 Unity 主窗口标题是否以 `<ProjectName> - ` 开头。它只能作为回退，因为不同语言、无窗口进程和同名项目会降低精度。

### 6.3 UnityLockfile 回退

最后尝试以 `FileShare.None` 打开 `Library/UnityLockfile`。收到 `IOException` 表示文件被占用。文件存在但可独占打开时不直接判为活跃，避免崩溃后残留文件造成永久阻塞。

## 7. Windows 进程启动与等待

### 7.1 为什么不依赖 `$LASTEXITCODE`

`Unity.exe` 是 Windows GUI 子系统可执行文件。PowerShell 使用调用运算符 `&` 启动它时，某些宿主会在 Unity 真正退出前返回，并且不保证设置 `$LASTEXITCODE`。这会让自动化提前结束或把未定义退出码当成结果。

包装脚本改用 `System.Diagnostics.ProcessStartInfo`：

1. `UseShellExecute = false`；
2. `Process.Start` 启动；
3. `WaitForExit()` 等待真实进程结束；
4. 从 `Process.ExitCode` 读取结果；
5. 在 `finally` 中释放 `Process`。

这套语义同时用于 Unity Editor 和已构建 Player。

### 7.2 参数引用

Windows 的 `ProcessStartInfo.Arguments` 是单个命令行字符串，不是已经分词的数组。脚本对每个逻辑参数应用兼容 `CommandLineToArgvW` 的引用规则：

- 无空白和引号的普通参数原样写入；
- 需要时用双引号包裹整个参数；
- 引号前的连续反斜杠加倍并转义引号；
- 结束引号前的尾部反斜杠加倍。

因此包含空格的项目、Editor、日志、测试过滤器和 Player 路径不会被 Unity 拆成多个参数。新增任务必须继续传递“逻辑参数数组”，不能先手工拼成带引号的大字符串。

## 8. 输出目录

没有传入 `-OutputRoot` 时，`New-ValidationRoot` 创建：

```text
Logs/UnityCli/yyyyMMdd-HHmmss-<task>-<PowerShell PID>/
```

时间、任务名和 PowerShell PID 共同降低并发或连续运行覆盖证据的风险。当前输出包括：

| 任务 | 日志 | 额外结果 |
| --- | --- | --- |
| Import | `import.log` | 无 |
| TestEditMode | `editmode.log` | `editmode-results.xml` |
| TestPlayMode | `playmode.log` | `playmode-results.xml` |
| TestAll | 两组日志 | 两组 XML |
| BuildAddressables | `addressables.log` | Addressables 平台输出 |
| BuildWindows64 | `player-build.log` | Player 目录 |
| ResourceSmoke | `resource-smoke.log` | Player 内部断言 |

`Logs` 和 `Builds` 已被 `.gitignore` 排除。日志仍是本地诊断证据，不因为被忽略就可以包含或传播秘密。

## 9. Task 实现

### 9.1 Doctor

`Doctor` 不启动新的 Unity Editor。它报告：

- 解析后的项目路径；
- 工程要求版本；
- 实际 Editor 路径与产品版本；
- PowerShell 版本；
- 当前 Unity 进程数量；
- 目标项目是否正在使用。

项目正在使用时 `Doctor` 仍以成功结束，因为环境定位本身有效；它发出警告。其他需要打开项目的任务会在启动进程前失败。

### 9.2 Import

底层参数等价于：

```text
-batchmode -quit -projectPath <project> [-accept-apiupdate] -logFile <import.log>
```

`-batchmode` 禁止交互弹窗阻塞；`-quit` 在导入和脚本编译完成后退出。默认不加入 `-accept-apiupdate`，保持日常验证只读源码语义。

### 9.3 TestEditMode / TestPlayMode

底层参数等价于：

```text
-batchmode
-projectPath <project>
-runTests
-testPlatform EditMode|PlayMode
-testResults <result.xml>
[filters]
-logFile <test.log>
```

测试任务故意不传 `-quit`。Unity 6 文档明确说明 `-runTests` 与 `-quit` 同用时会在测试完成前退出；安装的 Test Framework 1.7.0 源码也会对该组合记录警告。

### 9.4 TestAll

`TestAll` 先启动一个 EditMode Unity 进程，完成全部退出码/XML 断言后，再启动独立的 PlayMode 进程。EditMode 失败时 PlayMode 不继续执行，以保留最早失败并避免用后续输出覆盖注意力。

### 9.5 BuildAddressables

底层入口：

```text
-batchmode -quit
-projectPath <project>
-buildTarget win64
-executeMethod UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent
-logFile <addressables.log>
```

`BuildPlayerContent` 是 Addressables 的静态 Editor API。包装层当前检查 Unity 退出码；模块验收还需要按 Resource 规则检查平台内容与双后端行为。

### 9.6 BuildWindows64

底层入口：

```text
-batchmode -quit
-projectPath <project>
-buildTarget win64
-buildWindows64Player <player.exe>
-logFile <player-build.log>
```

成功条件为 Unity 退出码 `0` 且目标 `.exe` 存在。默认目标在被忽略的 `Builds/UnityCli`，避免污染仓库。

### 9.7 ResourceSmoke

该任务不打开 Unity 项目，而是运行已构建 Player：

```text
<player.exe>
-batchmode -nographics
-frameworkWwjResourceSmoke
-logFile <resource-smoke.log>
```

脚本先验证 Player 存在，再等待 Player 退出并检查退出码。`-frameworkWwjResourceSmoke` 的业务断言由 Resource Management Player 入口负责，必须覆盖 Resources 与 Addressables 两个后端。

## 10. 测试 XML 判定

Test Framework 输出 NUnit `test-run` XML。包装层读取根节点的：

| 属性 | 要求 |
| --- | --- |
| `result` | 必须为 `Passed` |
| `total` | 必须大于 `0` |
| `passed` | 用于完成报告 |
| `failed` | 必须为 `0` |

以下情形即使 Unity 退出码为 `0` 也判失败：

- 未生成 XML；
- XML 根不是 `test-run`；
- 属性缺失或不能解析；
- 过滤器匹配零用例；
- 根结果不是 `Passed`；
- `failed` 不为零。

这一层防止测试入口未真正运行、过滤器拼错或 Test Framework 只完成启动流程时出现“假通过”。

## 11. 退出码与结果矩阵

| 进程退出码 | 专用结果 | 结论 |
| ---: | --- | --- |
| 非 `0` | 任意 | 失败，先查看任务日志 |
| `0` | 测试 XML 缺失/失败/零用例 | 失败 |
| `0` | Player 文件缺失 | 构建失败 |
| `0` | 测试 XML Passed 且用例大于零 | 测试通过 |
| `0` | Import 无编译错误并正常退出 | 导入/编译通过 |
| `0` | Resource Player 业务断言退出 `0` | 冒烟通过 |

如果自定义 `-executeMethod` 捕获异常后仍正常返回，Unity 可能产生错误的成功退出码。入口实现必须重新抛出，或调用 `EditorApplication.Exit` 返回非零值。

## 12. 隔离工程验证

隔离工程用于解决“用户必须保持主工程 GUI 打开”和“自动验证必须独占 projectPath”的冲突。推荐流程：

1. 在系统临时目录创建具有唯一名称的目录；
2. 复制当前工作树的 `Assets`、`Packages`、`ProjectSettings`；
3. 用原仓库脚本和 `-ProjectPath <temp>` 执行 `Import`/测试；
4. 将结果明确标记为隔离验证；
5. 检查没有 Unity/AssetImportWorker 仍引用临时路径；
6. 验证目标仍位于预期临时根且名称匹配；
7. 删除临时目录。

不要复制主工程的 `Library` 作为干净验证输入。冷导入时间更长，但能验证包解析、脚本导入和项目自洽性。需要复现主工程缓存问题时，应把它作为单独诊断，不与干净导入混为一个结论。

## 13. 常见问题与排障

### 13.1 `Project In Use=True`

含义：某个 Unity 主进程或 Asset Import Worker 的 `-projectPath` 精确指向目标项目，或回退检测发现窗口/锁占用。

处理：保存并关闭 GUI Editor，等待 Worker 退出，再运行 `Doctor`。不要删除活跃 `UnityLockfile`，不要结束无法确认所有者的进程。

### 13.2 找不到 Editor

检查顺序：

1. `ProjectVersion.txt` 是否是预期版本；
2. Unity Hub 是否安装了该精确版本；
3. 使用 `-UnityEditor` 显式指定；
4. 或设置用户级 `UNITY_EDITOR_PATH`；
5. 再运行 `Doctor` 检查产品版本。

不要把另一个 patch 版本临时改名放到目标目录；文件产品版本仍会拒绝它。

### 13.3 Unity 进程结束但脚本没有退出

先检查是否仍有主 Unity、崩溃处理器或任务启动的子进程。查看日志末尾是否出现正常 batchmode 退出。若是自定义异步 `-executeMethod`，确认方法最终结束并且没有非前台流程阻止 Editor 关闭。

### 13.4 退出码为 `0` 但没有 XML

常见原因：

- 手工命令错误地同时传入 `-runTests` 和 `-quit`；
- `-testResults` 路径不可写或引用被 Shell 拆分；
- Test Framework 在发现/设置阶段异常退出；
- 实际运行的不是目标项目。

使用包装任务重试，检查测试日志中的命令行测试启动、结果保存和 Editor 退出段落。

### 13.5 测试为零用例

检查 `-AssemblyNames` 是否是 asmdef 的程序集名而非文件路径；检查 `-TestFilter` 的完整命名空间、正则转义和分号引用；确认目标测试程序集的平台与 `-testPlatform` 一致。

### 13.6 导入失败

优先从 `import.log` 查找首个 `error CS`、Package Manager 错误或 Assembly Updater 错误。不要先删除整个项目或接受所有 API 更新。只有升级任务可以使用 `-AcceptApiUpdate`，使用后审查 Git 差异。

### 13.7 许可证或 Hub 会话问题

CLI 复用当前工作站的 Unity 许可证。许可证解析失败时通过 Unity Hub 重新登录或处理许可证，不把凭据加入脚本。诊断日志和进程命令行在分享前必须脱敏。

### 13.8 Addressables 或 Player 失败

先确认 `BuildAddressables` 与 `BuildWindows64` 使用相同 `-buildTarget win64`。检查 Addressables profile、Build Settings 场景、目标平台模块和构建日志的第一个错误。不要只查看最后的“build failed”汇总。

## 14. 扩展新 Task

新增可复用任务时按以下顺序修改 `Tools/UnityCli.ps1`：

1. 将稳定任务名加入 `ValidateSet`；
2. 为任务创建职责单一的 `Invoke-*` 函数；
3. 通过逻辑字符串数组传递参数；
4. 统一调用 `Invoke-NativeProcess`；
5. 检查退出码；
6. 对 XML、文件或业务日志增加专用断言；
7. 在主 `switch` 中接线；
8. 判断是否需要 `Assert-ProjectAvailable`；
9. 更新三份 CLI 文档和 Docs 索引；
10. 在隔离工程执行成功、失败和路径含空格场景。

不要为一个任务复制 Editor 定位、参数引用、进程等待或结果目录逻辑。新增通用能力时优先扩展已有公共函数，并保持错误消息包含任务名和证据路径。

## 15. 安全审查清单

- [ ] 是否只读取并比较 `-projectPath`，没有打印完整 Unity 进程命令行？
- [ ] 是否避免在参数、日志摘要和文档中暴露凭据？
- [ ] 是否在递归清理前解析并验证了精确临时路径？
- [ ] 是否没有自动关闭用户 GUI Editor？
- [ ] 是否把日志、XML 和构建物留在 Git 忽略范围？
- [ ] 是否检查了退出码与任务专用结果？
- [ ] 是否明确区分权威工作区与隔离副本结果？

## 16. 官方与项目参考

- [Unity 6 Editor 命令行参数](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)
- [Unity Test Framework 1.7 命令行参数](https://docs.unity3d.com/Packages/com.unity.test-framework@1.7/manual/reference-command-line.html)
- [Addressables 2.9 BuildPlayerContent API](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/api/UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent.html)
- [Unity 6000 迁移验收](../03_Architecture/Distribution/01_Unity6_Migration_Acceptance.md)
- [Resource Management 验收](../03_Architecture/FoundationModules/ResourceManagement/04_Acceptance_And_Review.md)
