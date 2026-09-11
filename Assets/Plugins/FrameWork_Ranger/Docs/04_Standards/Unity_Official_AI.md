# Unity 官方 AI 接入与调用指南

验证日期：2026-09-11。工程：Unity 6000.5.9f1。本文是本项目官方 Unity 技能、CLI 与 MCP 的统一调用入口；版本和数量为该次安装的实测记录，后续以本机配置及实时工具发现为准。

## 先选择调用入口

| 当前任务 | 使用入口 | 前置条件 |
| --- | --- | --- |
| UI、音频、URP、资源等开发指导 | 匹配 `unity:*` 技能，读取对应 `SKILL.md` | 技能已加载；涉及实际场景时再连接 Editor |
| 读取或操作当前 Editor 的场景、组件和资产 | `unity-framework` MCP，或官方 `unity command` | 目标工程 Editor 已打开且 Pipeline 就绪 |
| 导入、编译、测试、构建验收 | 项目 `Tools/UnityCli.ps1` | 遵守现行 CLI 开发规则；同工程 GUI Editor 已保存关闭 |

两种 CLI 名称容易混淆：`unity.exe` 是 Unity 官方独立 CLI，`Tools/UnityCli.ps1` 是本仓库的 Editor 批处理包装脚本。需要现场编辑器操作时使用前者或 MCP；验收任务使用后者。

## 给后续 AI 任务的调用顺序

1. 根据当前工作目录/Git 根定位包含 `Assets`、`Packages`、`ProjectSettings` 的 Unity 工程。外层 `FrameWork` 文件夹不是 Unity 工程；不要把它直接作为 `projectPath`。
2. 按任务选读 Unity 技能及本项目文档。框架模块流程、现行架构和验证规则仍适用；官方技能提供具体 Unity 操作方法。
3. 需要 Editor 时，先发现当前会话的 `unity-framework` 工具并调用 `editor_status`，核对返回的 `projectPath`、`unityVersion`、`compiling`。MCP 配置名不等于函数名，具体名称和参数以当前工具 schema 为准。
4. 会话尚未加载原生 MCP 工具时，可以用同一官方 CLI 通道完成已授权工作。始终显式传 `--project-path`，避免在多工程环境误选 Editor。
5. 先按需查询，再执行所需修改。对象操作复用查询结果的句柄；是否保存场景/资产由当前任务决定。不要在检查任务中顺带执行 `save_all`、Play Mode 或包升级。
6. 记录实际调用结果。只有触及需要验收的改动时，按 [Unity CLI 开发规则](./Unity_CLI_Development_Rules.md)选择 Import、聚焦测试或构建。

已有授权直接用于对应操作，不为使用工具重复询问。任务范围不明、用户现场未保存状态或外部操作等实际未决问题，按项目原有规则处理。

## 已安装内容

| 部分 | 版本 | 位置与用途 |
| --- | --- | --- |
| Unity 官方 Codex 插件 `unity@unity-agent-plugin` | 0.1.4-beta | 本机 Codex 插件缓存；31 个技能，覆盖 UI、音频、URP、资源、CLI 等。 |
| Unity CLI | 1.0.0-beta.5 | 本机原有安装；启动 MCP 服务并连接 Editor。 |
| `com.unity.pipeline` | 0.6.0-exp.1 | 工程 `Packages/manifest.json`；从 Unity 官方 UPM 源解析，最低 Unity 6000.0。 |
| Codex MCP `unity-framework` | 已启用 | 本机用户级 Codex 配置；使用 CLI 的绝对路径，`--project-path` 固定指向本工程。 |

官方 Codex 插件本身提供 skills，没有内置 MCP server。Editor 工具由 CLI 与 Pipeline 包提供。Pipeline 当前是实验包；本次结果只证明下述接入和冒烟范围。

本机 MCP 指向 `D:\unityhub\UnityProjects\FrameWork\FrameWork_WWJ`，CLI 位于 `C:\Users\Maugham\AppData\Local\Unity\bin\unity.exe`。换机器或移动工程后须重新配置，工程的包清单不会自动安装用户级 Codex 插件和 MCP 配置。

## 使用

1. 用 Unity 6000.5.9f1 打开本工程，等待编译结束。Pipeline 随 Editor 自动启动。
2. 当前对话已能发现 31 个官方 `unity:*` 技能。原生 MCP 工具是否可用要单独检查；如果新增工具未加载，重新打开任务或 Codex 后再检查。技能出现、配置启用和工具实际调用成功是不同的验证结果。
3. 可直接要求读取场景层级、检查组件、操作对象或查询日志。需要 Editor 实际运行；本次测试用后台 Editor 已正常退出。

终端检查（在包含 `Assets`、`Packages`、`ProjectSettings` 的工程根目录执行）：

```powershell
codex plugin list
codex mcp get unity-framework
unity --version
unity command --project-path . --format json
unity command --project-path . editor_status --format json
unity command --project-path . list_open_scenes --format json
unity command --project-path . get_scene_hierarchy --format json
unity command --project-path . eval 'return UnityEngine.Application.unityVersion;' --format json
```

参数查询用 `unity command --help` 和 `unity command --project-path . <命令名> --help`；MCP 调用以实际 `inputSchema` 为准，不把 CLI 的 `--flag` 形式写入 JSON 参数。包内较详细的说明位于已解析包的 `Documentation~/` 和 `.claude/skills/unity-pipeline/SKILL.md`；PackageCache 目录名会变化，按 `com.unity.pipeline*` 定位。

`unity mcp` 是供客户端启动的 stdio 服务，手动运行后等待输入并不表示卡住。日常无需另开终端常驻运行它。

## 常用工具导航

下表名称来自本次实测目录。这里列出的工具可被发现，不代表已逐一验收；具体参数和可用性以连接后的 schema 为准。

| 目的 | 工具名 |
| --- | --- |
| 连接状态与日志 | `editor_status`、`get_console_logs`、`get_selection` |
| 场景与对象查询 | `list_open_scenes`、`get_scene_hierarchy`、`find_gameobjects` |
| 组件数据 | `get_component_properties`、`get_serialized_fields` |
| 对象与组件编辑 | `create_gameobject`、`delete_gameobject`、`set_transform`、`set_parent`、`add_component`、`set_component_properties` |
| 资产与 Prefab | `find_assets`、`get_import_settings`、`instantiate_prefab`、`create_prefab`、`save_prefab_contents` |
| 场景操作 | `open_scene`、`set_active_scene`、`save_scene` |
| 图像观察 | `capture_scene_view`、`capture_game_view`、`screenshot` |
| Play Mode | `editor_play`、`editor_pause`、`editor_stop` |
| 临时 C# 与批量操作 | `eval`、`run_script`、`batch` |
| 编译/测试诊断 | `recompile`、`recompile_status`、`list_tests`、`run_tests`、`test_status` |

需要搜索具体资产或场景对象时，先读取 `unity:generate-editor-search-query` 技能，按其 Unity Search 工作流执行。复杂 C# 操作优先放在工程 `Assets` 外的脚本中，经 `run_script` 执行；`eval` 用于临时短表达式。异步操作须读后续状态，不能把“已提交”当成“已完成”。

## 官方技能选择

无需用户记住技能名，以下映射供后续 AI 根据自然语言需求选择。只读取当前任务所需的 `SKILL.md`。

| 需求 | 技能（前缀均为 `unity:`） |
| --- | --- |
| CLI、连接或包管理 | `unity-cli`、`unity-package-management` |
| 查找具体资产/场景对象 | `generate-editor-search-query` |
| 未指定技术的游戏 UI | `ui`，再按项目实际路由 |
| UI Toolkit / Canvas / 既有 IMGUI | `ui-uitk` / `ui-ugui` / `ui-imgui` |
| 音频路由与优化 | `audio-setup-mixers`、`optimize-audio` |
| TMP 字体与多语言 | `optimize-text-mesh-pro`、`localization` |
| URP 与 Shader Graph | `migrate-birp-to-urp`、`urp-postprocessing`、`validate-urp-render-graph-renderer-feature`、`shader-graph-create-custom-node` |
| Sprite、像素画与图集 | `sprite-editor`、`sprite-segment-3x3grid`、`2d-pixel-perfect`、`manage-sprite-atlas` |
| Tilemap / RuleTile | `tilemap-palette-create`、`tilemap-ruletile-createempty`、`tilemap-ruletile-createfromsegment` |
| 3D 碰撞或寻路 | `physics-3d-collision`、`initialize-ai-navigation` |
| Web 构建优化 | `optimize-web` |
| 联机、语音与在线后端 | `setup-multiplayer-services`、`setup-vivox-voice-chat`、`build-live-game` |
| 内购和广告 | `implement-in-app-purchases`、`levelplay-unity-integration` |
| 用户要求从零创建新 Unity 项目 | `new-unity-project` |

当前框架已有 UI 模块；调用官方 UI 技能时先读取项目模块设计。技能不自动授权增加新的 UI 系统、联网服务或商业化依赖。官方插件已包含 `unity-cli` 技能，无需重复安装同名技能。

## 排障与现场恢复

| 现象 | 下一步 |
| --- | --- |
| 找不到 `unity` | 用 `Get-Command unity` 检查 PATH；本机安装路径见上表说明。重开 shell 或使用实际 CLI 绝对路径。 |
| 没有 Editor / 无法连接 | 确认打开的是正确工程，包已解析且编译完成；检查 `--project-path`。CLI/MCP 连接命令本身不会启动 Editor。 |
| 编译错误 / Safe Mode | 检查 Editor 编译日志并修复真实错误；Safe Mode 下 Pipeline 可能未加载。需要 CLI 编译时按现行规则处理工程占用。 |
| Domain Reload 时短暂断连 | 等待此次 reload 结束，再查询状态；不重复提交写操作。 |
| `blocked_by_dialog` | 根据状态中的对话框信息处理实际阻塞；不要反复重试或自动关闭用户 Editor。 |
| 后台 Editor 更新停滞 | 按包内 skill 检查 `set_autotick --enable true`；执行后读取状态。 |
| 工具缺失或参数报错 | 重新发现工具，核对当前包版本和 schema；退出码 2 通常是参数错误，应修正后再调用。 |
| CLI 成功、桌面原生 MCP 不可用 | 检查 `codex mcp get unity-framework` 的路径与启用状态，重新加载任务；可继续使用 CLI，并如实区分两条调用结果。 |

本次未安装旧的 `EditorMcpAdapter`。后续排障针对官方 Pipeline/CLI 通道，不沿用历史 MCP 的端口与配置。端口描述文件含认证信息，不将其内容提交或贴入文档。

## 迁移到其他机器或工程路径

工程包依赖随 `Packages` 文件同步；用户级插件和 MCP 配置须单独设置。已有安装无需重复执行：

```powershell
# 安装官方 Codex 技能插件
codex plugin marketplace add Unity-Technologies/unity-agent-plugin
codex plugin add unity@unity-agent-plugin

# 在实际 Unity 工程根目录，确认命令指向的是已安装的官方 CLI
$unityCommandPath = (Get-Command unity -ErrorAction Stop).Source
$unityProjectPath = (Get-Location).Path
codex mcp add unity-framework -- $unityCommandPath mcp --project-path $unityProjectPath
codex mcp get unity-framework
```

更新已有 MCP 条目前先检查目标路径；多个工程需要不同 MCP 配置名。只有在新工程明确需要导入该包时，才执行 `unity pipeline install --project-path <工程绝对路径> --package-version 0.6.0-exp.1`；版本更新应作为单独依赖变更验证。

现行编译、测试和构建验收继续使用 `Tools/UnityCli.ps1`，见 [Unity CLI 开发规则](./Unity_CLI_Development_Rules.md)。本次试用没有替换现行验证入口。

## 实际验证

- `Tools/UnityCli.ps1 -Task Doctor`：目标版本一致，启动前无 Editor 占用。
- `Tools/UnityCli.ps1 -Task Import`：退出码 0，未发现 C# 编译错误；日志 `Logs/UnityCli/20260911-234021-import-15720/import.log`。此前一次导入随任务中断，不计为通过。
- 启动一次临时后台 Editor，参数与 PID 保存在 `Logs/UnityOfficialAi/launch.json`，日志 `editor.log`，正常退出码 0 保存在 `exit-code.txt`。
- 直接启动已配置的同一 `unity.exe mcp --project-path ...`，完成 stdio MCP `initialize`、`tools/list`、`tools/call`；发现 149 个工具且无后续分页。
- MCP 调用 `editor_status` 返回 `ready`、`compiling=false` 和 `6000.5.9f1`；`list_open_scenes`、`get_scene_hierarchy` 成功读取临时空场景；`eval` 返回真实 Editor 版本，诊断为空。
- 通过同一官方 CLI/Pipeline 的 `create_gameobject` 创建临时立方体 `__UnityOfficialAiSmoke_20260911`；查询数量为 1，删除后为 0。场景未保存，无测试对象资产残留。
- 没有运行框架全量 EditMode/PlayMode 测试、Player 构建或对全部 149 个工具逐一验收。

MCP 响应、工具目录、临时诊断脚本与对象操作结果保存在 `Logs/UnityOfficialAi/`（Git 忽略）。包解析增加 Newtonsoft JSON 3.2.2，并调整原有 Mono Cecil 的依赖深度；没有升级原有 Test Framework 1.7.0。

## 撤销本次接入

需要撤销时，移除 manifest 中 `com.unity.pipeline`，再让 Unity 重新解析依赖；不要手工删除共享的传递依赖。用户级接入可分别移除：

```powershell
codex mcp remove unity-framework
codex plugin remove unity@unity-agent-plugin
```

## 官方依据

- [Unity 官方 Codex 插件与 Unity 6+ 支持说明](https://github.com/Unity-Technologies/unity-agent-plugin)
- [Unity 官方 CLI skill 与 Pipeline 安装](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/SKILL.md)
- [Unity MCP、客户端配置与后台 Editor 说明](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-cli/references/integration-advanced.md)
- [OpenAI MCP 配置说明](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)
