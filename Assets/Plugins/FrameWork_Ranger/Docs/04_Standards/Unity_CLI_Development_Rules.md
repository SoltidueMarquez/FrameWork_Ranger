# FrameWork_Ranger Unity CLI 开发规则

> 状态：已确认决策，立即生效<br>
> 确认日期：2026-08-22<br>
> 适用范围：FrameWork_Ranger 自有 Runtime、Editor、Tests、Samples、配置资产、项目设置与自动化验证<br>
> 权威入口：项目根目录 `Tools/UnityCli.ps1`

## 1. 决策

FrameWork_Ranger 的 Unity 导入、编译、测试、内容构建、Player 构建与 Player 冒烟统一通过 Unity Editor CLI 完成。仓库包装入口为 `Tools/UnityCli.ps1`，它是项目开发与验收的默认可执行契约。

MCP/EditorMcpAdapter 不再是当前项目开发链路的依赖：不要求安装、启动或连接 MCP 服务，不以 MCP 调用结果作为 Unity 编译或测试证据。历史 MCP 研究资料仍按历史证据保留，但不自动形成现行实现约束。

日常命令见 [Unity 6000 CLI 开发与验证](./Unity_6000_CLI.md)，脚本内部设计与排障见 [Unity CLI 技术参考](./Unity_CLI_Technical_Reference.md)。

## 2. 规范用语

- **必须**：不满足时不能宣称任务通过或完成。
- **应当**：默认执行；只有存在明确、可报告的原因时才能省略。
- **可以**：按任务风险和验证成本选择。
- **禁止**：不得作为项目开发流程的一部分执行。

## 3. 统一入口

1. 必须优先调用 `Tools/UnityCli.ps1`，不得在每次任务中临时拼接一套新的 Unity 命令。
2. 必须由脚本读取 `ProjectSettings/ProjectVersion.txt` 并校验 Editor 产品版本，不得仅因某个 `Unity.exe` 可以启动就认为版本正确。
3. 首次配置、Editor 升级、工作站迁移或定位失败后，必须先运行 `Doctor`。
4. 包装脚本暂不支持的单次诊断，可以直接调用 Unity Editor CLI，但必须记录完整参数、退出码和日志路径；同一能力第二次需要复用时，应当扩展包装脚本及本文档。
5. 禁止用 MCP 成功、IDE 无红线、生成 `.csproj` 或 GUI Console 看似干净替代 CLI 编译与测试。

## 4. 执行前条件

运行导入、测试或构建前必须确认：

- 当前目录属于权威工作区或明确命名的隔离验证副本；
- `ProjectVersion.txt`、`Packages/manifest.json` 和真实目标文件均来自本次验证对象；
- 已检查 Git 状态并识别用户已有改动；
- 同一 `projectPath` 的 GUI Editor 已保存并关闭；
- `Doctor` 能解析到与项目完全匹配的 Editor；
- 日志、XML 与 Player 输出位于被 Git 忽略的目录或明确的临时目录。

不得自动关闭用户正在使用的 GUI Editor。检测到工程占用时，应当停止 CLI 任务，说明阻塞条件，或在不改变验收语义的前提下使用隔离副本。

## 5. 任务与最低验证门禁

| 变更类型 | 最低 CLI 门禁 | 追加门禁 |
| --- | --- | --- |
| 纯 Markdown 文档，未改变 Unity 资产路径 | 不要求启动 Unity | 检查链接、UTF-8、事实标签和 `.meta` |
| PowerShell CLI 脚本 | `Doctor`、PowerShell 语法解析 | 在隔离副本执行受影响任务 |
| Runtime 或 Editor C# | `Import` + 相关 EditMode | 生命周期或运行时行为受影响时追加 PlayMode |
| Tests 或测试程序集配置 | 对应 `TestEditMode` / `TestPlayMode` | 修改共享测试设施时运行 `TestAll` |
| Module SO、Framework 设置、场景或序列化引用 | `Import` + `TestAll` | 按模块验收检查 GUID、引用和 Play Mode 污染 |
| Addressables 配置或资源后端 | `TestAll` + `BuildAddressables` | Resource 变更追加 Player 构建与双后端冒烟 |
| Build Settings、Player Settings 或构建代码 | `TestAll` + `BuildWindows64` | 按受影响功能执行 Player 冒烟 |
| Unity、UPM 包或 API 迁移 | 经批准的 `Import -AcceptApiUpdate` + `TestAll` | Addressables、Player 与目标平台门禁 |

“相关测试”必须覆盖本次变更的程序集、公共契约和失败路径。过滤测试可以用于开发迭代，但正式阶段验收不得只运行一个过窄过滤器。

## 6. API Updater 与可变更边界

1. `Import` 默认不得传入 `-accept-apiupdate`，避免日常编译静默改写源码。
2. 只有明确的 Unity/包升级或兼容性迁移任务可以使用 `-AcceptApiUpdate`。
3. 使用后必须立即检查 Git 状态与差异，区分预期 API 更新、Unity 自动生成内容和无关设置漂移。
4. 禁止提交 `Library`、`Temp`、`Logs`、自动生成解决方案或测试输出。
5. Unity 自动修改了用户未授权的项目设置、场景或第三方资产时，必须停止并报告，不得顺手接受。

## 7. 测试规则

1. EditMode 与 PlayMode 必须分为独立 Unity 进程顺序运行；`TestAll` 不表示在同一 Editor 会话内连续切换模式。
2. `-runTests` 禁止与 `-quit` 同用。Unity Test Framework 会在测试结束后关闭 Editor；额外的 `-quit` 可能在 XML 生成前终止测试。
3. 成功必须同时满足：进程退出码为 `0`、XML 存在、根节点为 `test-run`、`total > 0`、`result="Passed"`、`failed = 0`。
4. 测试过滤后为零用例必须判定失败，不能作为“没有失败”的通过结果。
5. 测试数量是易变事实。报告时读取本次 XML，不把历史基线硬编码成永久规则。
6. 测试失败后优先保存首个失败栈、测试名、日志路径与 XML 路径，不通过重复运行掩盖不稳定测试。

## 8. 构建与冒烟规则

- Addressables 内容构建必须检查 Unity 退出码和 `addressables.log`；仅看到输出目录不足以证明构建成功。
- Windows Player 构建必须同时检查退出码和目标可执行文件存在。
- Resource 双后端冒烟必须由已构建 Player 执行 `-frameworkRangerResourceSmoke`，以退出码 `0` 和日志中的 Resources/Addressables 双后端断言为准。
- Player 能启动、进程未崩溃或日志只出现单一后端成功，都不能替代双后端冒烟通过。
- 新增构建入口应当通过 Editor 目录下的静态 `-executeMethod` 方法实现，并让异常或 `EditorApplication.Exit(nonZero)` 传播为失败。

## 9. 隔离验证

当权威工程必须保持在 GUI Editor 中打开时，可以在临时目录建立隔离验证副本，但必须遵守：

1. 输入至少包含当前目标的 `Assets`、`Packages` 和 `ProjectSettings`，不得用陈旧 clone 代替当前工作树。
2. 必须通过 `-ProjectPath` 明确指向隔离副本，并在报告中标记“隔离验证”，不能伪称在权威工作区运行。
3. 不复制或提交原工程的 `Library`、`Temp`、`Logs`。
4. 验证完成后确认没有 Unity 子进程继续占用，再删除本次创建的临时副本。
5. 涉及真实本地缓存、平台 SDK、签名、远端服务或用户配置时，隔离验证不自动等价于目标工作区/目标机器验收。

## 10. 日志、隐私与安全

- 默认输出目录为 `Logs/UnityCli/<时间>-<任务>-<进程号>`；自定义目录也必须保持在可识别、可清理的范围内。
- Unity 日志可能包含本机路径、许可证状态、Hub 会话信息和包源信息。禁止提交日志或在公开渠道粘贴未审查的完整日志。
- 诊断 Unity 进程占用时，不输出完整进程命令行；Hub 启动参数可能包含会话令牌。
- 禁止把密码、令牌、许可证序列号或签名密钥作为仓库脚本默认值或命令示例。
- 自动化只能删除本次明确创建且已验证边界的临时目录；不得对工作区根、用户目录或未解析变量执行递归删除。

## 11. 失败与阻塞

出现下列任一情况时必须判定失败或阻塞：

- 找不到目标 Editor，或产品版本与 `ProjectVersion.txt` 不一致；
- 同一项目仍由 Unity 进程占用；
- Unity/Player 返回非零退出码；
- 日志记录编译错误、构建错误或入口异常；
- 测试 XML 缺失、不可解析、零用例或结果失败；
- 预期 Player/Addressables 输出不存在；
- 无法确认测试针对的是本次工作树；
- 缺少许可证、目标平台模块、SDK 或外部服务权限。

不得因任务耗时、输出较多或历史上曾通过而跳过失败检查。

## 12. 完成报告

Unity 相关开发任务的完成报告至少包含：

- Unity 版本与验证对象（权威工作区或隔离副本）；
- 实际执行的包装任务及重要过滤条件；
- 每个进程的退出码；
- EditMode/PlayMode 的 `total/passed/failed`；
- 构建或冒烟的目标与结果；
- 未运行门禁及原因；
- 日志/XML 所在目录，或已清理临时证据的明确说明；
- Git 中保留的预期改动和发现的无关改动。

只有文档或静态检查时，必须明确写“未运行 Unity 编译/测试”，不能表述为“项目编译通过”。

## 13. 规则维护

- 新增 `Tools/UnityCli.ps1` 任务时，同步更新命令速查、技术参考、本文任务矩阵和 Docs 索引。
- 更改失败语义、版本解析、进程等待或测试 XML 判定时，必须在隔离工程执行端到端回归。
- Unity 或 Test Framework 升级后，重新核对官方命令行参数和包内实现，不沿用未经验证的旧版本假设。
- 本文是 Unity CLI 开发规则的唯一权威来源；其他文档只摘要并链接本文。
