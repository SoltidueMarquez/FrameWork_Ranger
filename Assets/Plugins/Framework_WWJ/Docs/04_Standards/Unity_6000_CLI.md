# Unity 6000 CLI 验证命令

> 工程：`D:\unityhub\UnityProjects\FrameWork\FrameWork_WWJ`  
> Editor：Unity 6000.5.9f1  
> Shell：Windows PowerShell

这些命令直接调用 Unity Editor，不依赖仓库内包装脚本或 GitHub Actions。运行前保存并关闭同一工程的 GUI Editor；同一 `projectPath` 不能同时被两个 Editor 进程打开。日志和测试结果写到 `Logs`，该目录已被 Git 忽略。

## 1. 公共变量

```powershell
$UnityEditor = 'D:\unityhub\6000.5.9f1\Editor\Unity.exe'
$ProjectPath = 'D:\unityhub\UnityProjects\FrameWork\FrameWork_WWJ'
$ValidationRoot = Join-Path $ProjectPath 'Logs\MigrationValidation'
New-Item -ItemType Directory -Force -Path $ValidationRoot | Out-Null
```

Unity 进程完成后始终检查 `$LASTEXITCODE`。非零退出码、日志中的编译错误或失败测试都视为失败。

## 2. 导入、依赖解析与 API 更新

```powershell
& $UnityEditor -batchmode -quit `
  -projectPath $ProjectPath `
  -accept-apiupdate `
  -logFile (Join-Path $ValidationRoot 'import.log')
if ($LASTEXITCODE -ne 0) { throw "Unity import failed: $LASTEXITCODE" }
```

`-accept-apiupdate` 只允许 Unity 对本次迁移所需的 API 兼容更新；运行后必须审查 Git 差异，不提交 `Library`、`Temp`、`Logs` 或无关设置漂移。

## 3. EditMode 与 PlayMode

```powershell
& $UnityEditor -batchmode `
  -projectPath $ProjectPath `
  -runTests -testPlatform EditMode `
  -testResults (Join-Path $ValidationRoot 'editmode-results.xml') `
  -logFile (Join-Path $ValidationRoot 'editmode.log')
if ($LASTEXITCODE -ne 0) { throw "EditMode failed: $LASTEXITCODE" }

& $UnityEditor -batchmode `
  -projectPath $ProjectPath `
  -runTests -testPlatform PlayMode `
  -testResults (Join-Path $ValidationRoot 'playmode-results.xml') `
  -logFile (Join-Path $ValidationRoot 'playmode.log')
if ($LASTEXITCODE -ne 0) { throw "PlayMode failed: $LASTEXITCODE" }
```

Test Framework 会在执行结束后关闭 Editor，因此测试命令不额外传入 `-quit`；Unity 6000.5 同时收到两者时可能在测试启动前退出且不生成 XML。测试结果 XML 是用例计数与失败详情的事实来源；目标基线为 EditMode 67/67、PlayMode 18/18。

## 4. Addressables 本地内容

Addressables 提供公共静态入口 `AddressableAssetSettings.BuildPlayerContent()`，可直接由 `-executeMethod` 调用：

```powershell
& $UnityEditor -batchmode -quit `
  -projectPath $ProjectPath `
  -buildTarget win64 `
  -executeMethod UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent `
  -logFile (Join-Path $ValidationRoot 'addressables.log')
if ($LASTEXITCODE -ne 0) { throw "Addressables build failed: $LASTEXITCODE" }
```

## 5. StandaloneWindows64 Player

```powershell
$PlayerPath = Join-Path $ProjectPath 'Builds\MigrationValidation\Framework_WWJ.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $PlayerPath) | Out-Null

& $UnityEditor -batchmode -quit `
  -projectPath $ProjectPath `
  -buildTarget win64 `
  -buildWindows64Player $PlayerPath `
  -logFile (Join-Path $ValidationRoot 'player-build.log')
if ($LASTEXITCODE -ne 0) { throw "Player build failed: $LASTEXITCODE" }
```

## 6. Resource 双后端 Player 冒烟

```powershell
& $PlayerPath -batchmode -nographics `
  -frameworkWwjResourceSmoke `
  -logFile (Join-Path $ValidationRoot 'resource-smoke.log')
if ($LASTEXITCODE -ne 0) { throw "Resource smoke failed: $LASTEXITCODE" }
```

成功标准是进程退出码 `0`，并且日志同时记录 Resources 与 Addressables 后端成功；不能只以“Player 能启动”替代双后端断言。

## 7. 参考

- [Unity 6 Editor 命令行参数](https://docs.unity3d.com/cn/current/Manual/EditorCommandLineArguments.html)
- [Unity Test Framework 命令行参数](https://docs.unity3d.com/Packages/com.unity.test-framework@1.7/manual/reference-command-line.html)
- [Addressables BuildPlayerContent API](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/api/UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent.html)
