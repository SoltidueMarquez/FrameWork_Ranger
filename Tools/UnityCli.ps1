[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet(
        'Doctor',
        'Import',
        'TestEditMode',
        'TestPlayMode',
        'TestAll',
        'BuildAddressables',
        'BuildWindows64',
        'ResourceSmoke'
    )]
    [string] $Task = 'Doctor',

    [string] $UnityEditor,

    [string] $ProjectPath,

    [string] $OutputRoot,

    [string] $PlayerPath,

    [string] $TestFilter,

    [string] $TestCategory,

    [string] $AssemblyNames,

    [switch] $AcceptApiUpdate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Path))
}

function Get-ProjectEditorVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot
    )

    $versionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "找不到 Unity 版本文件：$versionFile"
    }

    $versionLine = Get-Content -LiteralPath $versionFile |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1

    if (-not $versionLine -or $versionLine -notmatch '^m_EditorVersion:\s*(.+)$') {
        throw "无法从 ProjectVersion.txt 读取 m_EditorVersion：$versionFile"
    }

    return $Matches[1].Trim()
}

function Test-EditorVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $EditorPath,

        [Parameter(Mandatory = $true)]
        [string] $ExpectedVersion
    )

    if (-not (Test-Path -LiteralPath $EditorPath -PathType Leaf)) {
        return $false
    }

    $productVersion = (Get-Item -LiteralPath $EditorPath).VersionInfo.ProductVersion
    if ([string]::IsNullOrWhiteSpace($productVersion)) {
        return $false
    }

    return $productVersion -match ('^' + [regex]::Escape($ExpectedVersion) + '(?:_|$)')
}

function Resolve-UnityEditor {
    param(
        [string] $ExplicitEditor,

        [Parameter(Mandatory = $true)]
        [string] $ExpectedVersion
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitEditor)) {
        $explicitPath = Resolve-AbsolutePath -Path $ExplicitEditor
        if (-not (Test-Path -LiteralPath $explicitPath -PathType Leaf)) {
            throw "指定的 Unity Editor 不存在：$explicitPath"
        }

        if (-not (Test-EditorVersion -EditorPath $explicitPath -ExpectedVersion $ExpectedVersion)) {
            $actualVersion = (Get-Item -LiteralPath $explicitPath).VersionInfo.ProductVersion
            throw "指定的 Unity Editor 版本不匹配。需要 $ExpectedVersion，实际为 $actualVersion。"
        }

        return $explicitPath
    }

    $candidates = [System.Collections.Generic.List[string]]::new()

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR_PATH)) {
        $candidates.Add($env:UNITY_EDITOR_PATH)
    }

    # 当前工作站通过 Unity Hub 使用此目录布局。
    $candidates.Add((Join-Path 'D:\unityhub' (Join-Path $ExpectedVersion 'Editor\Unity.exe')))

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates.Add((Join-Path $env:ProgramFiles (Join-Path 'Unity\Hub\Editor' (Join-Path $ExpectedVersion 'Editor\Unity.exe'))))
    }

    $candidates.Add((Join-Path 'D:\UnityHub\Editors' (Join-Path $ExpectedVersion 'Editor\Unity.exe')))
    $candidates.Add((Join-Path 'D:\Program Files\Unity\Hub\Editor' (Join-Path $ExpectedVersion 'Editor\Unity.exe')))

    $pathCommand = Get-Command 'Unity.exe' -ErrorAction SilentlyContinue
    if ($pathCommand) {
        $candidates.Add($pathCommand.Source)
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        $candidatePath = Resolve-AbsolutePath -Path $candidate
        if (Test-EditorVersion -EditorPath $candidatePath -ExpectedVersion $ExpectedVersion) {
            return $candidatePath
        }
    }

    throw @"
找不到与项目版本 $ExpectedVersion 匹配的 Unity Editor。
请通过 -UnityEditor 指定 Unity.exe，或设置用户环境变量 UNITY_EDITOR_PATH。
"@
}

function Test-ProjectInUse {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot
    )

    $normalizedProjectRoot = $ProjectRoot.Replace('\', '/').TrimEnd('/')

    try {
        $unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")
        foreach ($unityProcess in $unityProcesses) {
            if ([string]::IsNullOrWhiteSpace($unityProcess.CommandLine)) {
                continue
            }

            $projectPathMatch = [regex]::Match(
                $unityProcess.CommandLine,
                '(?i)(?:^|\s)"?-projectpath"?\s+(?:"(?<quoted>[^"]+)"|(?<plain>\S+))'
            )

            if (-not $projectPathMatch.Success) {
                continue
            }

            $processProjectPath = $projectPathMatch.Groups['quoted'].Value
            if ([string]::IsNullOrWhiteSpace($processProjectPath)) {
                $processProjectPath = $projectPathMatch.Groups['plain'].Value
            }

            $normalizedProcessProjectPath = $processProjectPath.Replace('\', '/').TrimEnd('/')
            if ($normalizedProcessProjectPath.Equals(
                $normalizedProjectRoot,
                [System.StringComparison]::OrdinalIgnoreCase
            )) {
                return $true
            }
        }
    }
    catch {
        # 精简系统或受限会话可能无法访问 Win32_Process，继续使用文件锁与窗口标题回退。
    }

    $projectName = Split-Path -Leaf $ProjectRoot
    foreach ($unityProcess in @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)) {
        if (-not [string]::IsNullOrWhiteSpace($unityProcess.MainWindowTitle) -and
            $unityProcess.MainWindowTitle.StartsWith(
                "$projectName - ",
                [System.StringComparison]::OrdinalIgnoreCase
            )) {
            return $true
        }
    }

    $lockFile = Join-Path $ProjectRoot 'Library\UnityLockfile'
    if (Test-Path -LiteralPath $lockFile -PathType Leaf) {
        $stream = $null
        try {
            $stream = [System.IO.File]::Open(
                $lockFile,
                [System.IO.FileMode]::Open,
                [System.IO.FileAccess]::ReadWrite,
                [System.IO.FileShare]::None
            )
        }
        catch [System.IO.IOException] {
            return $true
        }
        finally {
            if ($null -ne $stream) {
                $stream.Dispose()
            }
        }
    }

    return $false
}

function Assert-ProjectAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot
    )

    if (Test-ProjectInUse -ProjectRoot $ProjectRoot) {
        throw "项目正由 Unity Editor 打开。请先保存并关闭同一工程，再运行 CLI：$ProjectRoot"
    }
}

function New-ValidationRoot {
    param(
        [string] $RequestedRoot,

        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot,

        [Parameter(Mandatory = $true)]
        [string] $TaskName
    )

    if ([string]::IsNullOrWhiteSpace($RequestedRoot)) {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $root = Join-Path $ProjectRoot "Logs\UnityCli\$stamp-$($TaskName.ToLowerInvariant())-$PID"
    }
    else {
        $root = Resolve-AbsolutePath -Path $RequestedRoot
    }

    New-Item -ItemType Directory -Force -Path $root | Out-Null
    return $root
}

function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [Parameter(Mandatory = $true)]
        [string] $DisplayName
    )

    function ConvertTo-CommandLineArgument {
        param(
            [Parameter(Mandatory = $true)]
            [AllowEmptyString()]
            [string] $Argument
        )

        if ($Argument.Length -gt 0 -and $Argument -notmatch '\s' -and $Argument.IndexOf('"') -lt 0) {
            return $Argument
        }

        # ProcessStartInfo.Arguments 接收单个 Windows 命令行字符串；按 CommandLineToArgvW
        # 规则处理空白、引号和引号前的反斜杠，确保包含空格的路径不会被拆开。
        $builder = [System.Text.StringBuilder]::new()
        [void] $builder.Append('"')
        $backslashCount = 0

        foreach ($character in $Argument.ToCharArray()) {
            if ($character -eq '\') {
                $backslashCount++
                continue
            }

            if ($character -eq '"') {
                [void] $builder.Append(('\' * (($backslashCount * 2) + 1)))
                [void] $builder.Append('"')
                $backslashCount = 0
                continue
            }

            if ($backslashCount -gt 0) {
                [void] $builder.Append(('\' * $backslashCount))
                $backslashCount = 0
            }

            [void] $builder.Append($character)
        }

        if ($backslashCount -gt 0) {
            [void] $builder.Append(('\' * ($backslashCount * 2)))
        }

        [void] $builder.Append('"')
        return $builder.ToString()
    }

    $argumentLine = (($Arguments | ForEach-Object { ConvertTo-CommandLineArgument -Argument $_ }) -join ' ')
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.Arguments = $argumentLine
    $startInfo.UseShellExecute = $false

    Write-Host "[Unity CLI] 开始：$DisplayName"
    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "无法启动进程：$FilePath"
    }

    try {
        $process.WaitForExit()
        $exitCode = $process.ExitCode
    }
    finally {
        $process.Dispose()
    }

    Write-Host "[Unity CLI] 退出码：$exitCode"
    return $exitCode
}

function Assert-SuccessfulExit {
    param(
        [Parameter(Mandatory = $true)]
        [int] $ExitCode,

        [Parameter(Mandatory = $true)]
        [string] $DisplayName,

        [Parameter(Mandatory = $true)]
        [string] $LogPath
    )

    if ($ExitCode -ne 0) {
        throw "$DisplayName 失败，退出码为 $ExitCode。日志：$LogPath"
    }
}

function Assert-TestResult {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ResultPath,

        [Parameter(Mandatory = $true)]
        [string] $Platform
    )

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        throw "$Platform 未生成测试结果 XML：$ResultPath"
    }

    [xml] $resultDocument = Get-Content -LiteralPath $ResultPath -Raw
    $testRun = $resultDocument.DocumentElement
    if ($null -eq $testRun -or $testRun.Name -ne 'test-run') {
        throw "$Platform 测试结果不是预期的 NUnit test-run XML：$ResultPath"
    }

    $result = $testRun.GetAttribute('result')
    $total = [int] $testRun.GetAttribute('total')
    $passed = [int] $testRun.GetAttribute('passed')
    $failed = [int] $testRun.GetAttribute('failed')

    Write-Host "[Unity CLI] $Platform 测试：result=$result, total=$total, passed=$passed, failed=$failed"

    if ($total -le 0) {
        throw "$Platform 没有执行任何测试。结果：$ResultPath"
    }

    if ($result -ne 'Passed' -or $failed -ne 0) {
        throw "$Platform 测试失败。结果：$ResultPath"
    }
}

function Get-TestFilterArguments {
    $arguments = [System.Collections.Generic.List[string]]::new()

    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $arguments.Add('-testFilter')
        $arguments.Add($TestFilter)
    }

    if (-not [string]::IsNullOrWhiteSpace($TestCategory)) {
        $arguments.Add('-testCategory')
        $arguments.Add($TestCategory)
    }

    if (-not [string]::IsNullOrWhiteSpace($AssemblyNames)) {
        $arguments.Add('-assemblyNames')
        $arguments.Add($AssemblyNames)
    }

    return $arguments.ToArray()
}

function Invoke-UnityImport {
    param(
        [Parameter(Mandatory = $true)]
        [string] $EditorPath,

        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot,

        [Parameter(Mandatory = $true)]
        [string] $ValidationRoot
    )

    $logPath = Join-Path $ValidationRoot 'import.log'
    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.Add('-batchmode')
    $arguments.Add('-quit')
    $arguments.Add('-projectPath')
    $arguments.Add($ProjectRoot)

    if ($AcceptApiUpdate) {
        $arguments.Add('-accept-apiupdate')
    }

    $arguments.Add('-logFile')
    $arguments.Add($logPath)

    $exitCode = Invoke-NativeProcess -FilePath $EditorPath -Arguments $arguments.ToArray() -DisplayName '导入与脚本编译'
    Assert-SuccessfulExit -ExitCode $exitCode -DisplayName '导入与脚本编译' -LogPath $logPath
    Write-Host "[Unity CLI] 日志：$logPath"
}

function Invoke-UnityTests {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('EditMode', 'PlayMode')]
        [string] $Platform,

        [Parameter(Mandatory = $true)]
        [string] $EditorPath,

        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot,

        [Parameter(Mandatory = $true)]
        [string] $ValidationRoot
    )

    $name = $Platform.ToLowerInvariant()
    $logPath = Join-Path $ValidationRoot "$name.log"
    $resultPath = Join-Path $ValidationRoot "$name-results.xml"

    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.Add('-batchmode')
    $arguments.Add('-projectPath')
    $arguments.Add($ProjectRoot)
    $arguments.Add('-runTests')
    $arguments.Add('-testPlatform')
    $arguments.Add($Platform)
    $arguments.Add('-testResults')
    $arguments.Add($resultPath)

    foreach ($filterArgument in (Get-TestFilterArguments)) {
        $arguments.Add($filterArgument)
    }

    $arguments.Add('-logFile')
    $arguments.Add($logPath)

    # Unity 6 中 -runTests 与 -quit 同用会在测试完成前退出，因此这里不添加 -quit。
    $exitCode = Invoke-NativeProcess -FilePath $EditorPath -Arguments $arguments.ToArray() -DisplayName "$Platform 测试"
    Assert-SuccessfulExit -ExitCode $exitCode -DisplayName "$Platform 测试" -LogPath $logPath
    Assert-TestResult -ResultPath $resultPath -Platform $Platform
    Write-Host "[Unity CLI] 日志：$logPath"
    Write-Host "[Unity CLI] 结果：$resultPath"
}

function Invoke-AddressablesBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string] $EditorPath,

        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot,

        [Parameter(Mandatory = $true)]
        [string] $ValidationRoot
    )

    $logPath = Join-Path $ValidationRoot 'addressables.log'
    $arguments = @(
        '-batchmode',
        '-quit',
        '-projectPath', $ProjectRoot,
        '-buildTarget', 'win64',
        '-executeMethod', 'UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent',
        '-logFile', $logPath
    )

    $exitCode = Invoke-NativeProcess -FilePath $EditorPath -Arguments $arguments -DisplayName 'Addressables 本地内容构建'
    Assert-SuccessfulExit -ExitCode $exitCode -DisplayName 'Addressables 本地内容构建' -LogPath $logPath
    Write-Host "[Unity CLI] 日志：$logPath"
}

function Invoke-WindowsBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string] $EditorPath,

        [Parameter(Mandatory = $true)]
        [string] $ProjectRoot,

        [Parameter(Mandatory = $true)]
        [string] $ValidationRoot,

        [Parameter(Mandatory = $true)]
        [string] $ExecutablePath
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ExecutablePath) | Out-Null
    $logPath = Join-Path $ValidationRoot 'player-build.log'
    $arguments = @(
        '-batchmode',
        '-quit',
        '-projectPath', $ProjectRoot,
        '-buildTarget', 'win64',
        '-buildWindows64Player', $ExecutablePath,
        '-logFile', $logPath
    )

    $exitCode = Invoke-NativeProcess -FilePath $EditorPath -Arguments $arguments -DisplayName 'StandaloneWindows64 Player 构建'
    Assert-SuccessfulExit -ExitCode $exitCode -DisplayName 'StandaloneWindows64 Player 构建' -LogPath $logPath

    if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        throw "Unity 返回成功，但没有生成 Player：$ExecutablePath"
    }

    Write-Host "[Unity CLI] Player：$ExecutablePath"
    Write-Host "[Unity CLI] 日志：$logPath"
}

function Invoke-ResourceSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ExecutablePath,

        [Parameter(Mandatory = $true)]
        [string] $ValidationRoot
    )

    if (-not (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        throw "找不到 Resource 冒烟所需的 Player：$ExecutablePath。请先运行 BuildWindows64。"
    }

    $logPath = Join-Path $ValidationRoot 'resource-smoke.log'
    $arguments = @(
        '-batchmode',
        '-nographics',
        '-frameworkRangerResourceSmoke',
        '-logFile', $logPath
    )

    $exitCode = Invoke-NativeProcess -FilePath $ExecutablePath -Arguments $arguments -DisplayName 'Resource 双后端 Player 冒烟'
    Assert-SuccessfulExit -ExitCode $exitCode -DisplayName 'Resource 双后端 Player 冒烟' -LogPath $logPath
    Write-Host "[Unity CLI] 日志：$logPath"
}

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}

$ProjectPath = Resolve-AbsolutePath -Path $ProjectPath
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "Unity 项目目录不存在：$ProjectPath"
}

$expectedVersion = Get-ProjectEditorVersion -ProjectRoot $ProjectPath
$resolvedEditor = Resolve-UnityEditor -ExplicitEditor $UnityEditor -ExpectedVersion $expectedVersion
$actualVersion = (Get-Item -LiteralPath $resolvedEditor).VersionInfo.ProductVersion
$projectInUse = Test-ProjectInUse -ProjectRoot $ProjectPath

if ($Task -eq 'Doctor') {
    $unityProcesses = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)

    Write-Host '[Unity CLI] 环境检查通过'
    Write-Host "  Project：$ProjectPath"
    Write-Host "  Required Editor：$expectedVersion"
    Write-Host "  Resolved Editor：$resolvedEditor"
    Write-Host "  Product Version：$actualVersion"
    Write-Host "  PowerShell：$($PSVersionTable.PSVersion)"
    Write-Host "  Unity Processes：$($unityProcesses.Count)"
    Write-Host "  Project In Use：$projectInUse"

    if ($projectInUse) {
        Write-Warning '同一工程当前由 Unity Editor 打开；CLI 配置有效，但执行导入、测试或构建前必须先关闭 GUI Editor。'
    }

    exit 0
}

$validationRoot = New-ValidationRoot -RequestedRoot $OutputRoot -ProjectRoot $ProjectPath -TaskName $Task

if ([string]::IsNullOrWhiteSpace($PlayerPath)) {
    $PlayerPath = Join-Path $ProjectPath 'Builds\UnityCli\FrameWork_Ranger.exe'
}
else {
    $PlayerPath = Resolve-AbsolutePath -Path $PlayerPath
}

switch ($Task) {
    'Import' {
        Assert-ProjectAvailable -ProjectRoot $ProjectPath
        Invoke-UnityImport -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot
    }
    'TestEditMode' {
        Assert-ProjectAvailable -ProjectRoot $ProjectPath
        Invoke-UnityTests -Platform 'EditMode' -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot
    }
    'TestPlayMode' {
        Assert-ProjectAvailable -ProjectRoot $ProjectPath
        Invoke-UnityTests -Platform 'PlayMode' -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot
    }
    'TestAll' {
        Assert-ProjectAvailable -ProjectRoot $ProjectPath
        Invoke-UnityTests -Platform 'EditMode' -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot
        Invoke-UnityTests -Platform 'PlayMode' -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot
    }
    'BuildAddressables' {
        Assert-ProjectAvailable -ProjectRoot $ProjectPath
        Invoke-AddressablesBuild -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot
    }
    'BuildWindows64' {
        Assert-ProjectAvailable -ProjectRoot $ProjectPath
        Invoke-WindowsBuild -EditorPath $resolvedEditor -ProjectRoot $ProjectPath -ValidationRoot $validationRoot -ExecutablePath $PlayerPath
    }
    'ResourceSmoke' {
        Invoke-ResourceSmoke -ExecutablePath $PlayerPath -ValidationRoot $validationRoot
    }
}

Write-Host "[Unity CLI] 完成。输出目录：$validationRoot"
