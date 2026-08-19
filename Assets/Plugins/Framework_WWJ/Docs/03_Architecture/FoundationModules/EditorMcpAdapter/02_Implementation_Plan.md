# Editor MCP Adapter 具体实施计划

> 状态：等待用户批准。批准前不得创建本计划中的 Editor、Host、Tests、Samples 或 Library 配置。

## 1. 目标与实施原则

完成一个可卸载的 Editor-only MCP 适配胶囊，以官方 SDK Host 隔离协议变化，以显式只读工具验证 Unity 主线程、
Domain Reload、鉴权和诊断闭环。实施不修改 Core Module/Handler/Scope 契约，不触碰 ResourceManagement 工作区改动。

## 2. 目标目录与程序集

```text
Assets/Plugins/Framework_WWJ/BaseModules/EditorMcpAdapter/
├─ Editor/
│  ├─ Contracts/
│  ├─ Core/
│  ├─ Host/
│  ├─ Diagnostics/
│  ├─ FrameworkCenter/
│  ├─ Tools/ReadOnly/
│  ├─ AssemblyInfo.cs
│  └─ Framework_WWJ.BaseModules.EditorMcpAdapter.Editor.asmdef
├─ Tests/
│  ├─ EditMode/
│  └─ PlayMode/
├─ Samples~/
│  ├─ README.md
│  └─ client-config.example.json
└─ Host~/
   ├─ src/Framework_WWJ.Mcp.Host/
   ├─ tests/Framework_WWJ.Mcp.Host.Tests/
   └─ BuildHost.ps1
```

- Editor asmdef：只包含 Editor，引用 `Framework_WWJ.Runtime` 与 `Framework_WWJ.Editor`。
- EditMode/PlayMode 测试 asmdef：Editor-only、`UNITY_INCLUDE_TESTS`、单向引用 Adapter Editor。
- Host~ 不进入 Unity AssetDatabase/asmdef；目标 `net8.0`，引用 `ModelContextProtocol.AspNetCore` 2.0.x。
- 直接集成点：在 Runtime/Editor 的 `AssemblyInfo.cs` 各增加本 Adapter Editor 的 friend assembly；
  当前两文件已有未提交改动，实现前必须重新合并，禁止覆盖现有 ResourceManagement 条目。

## 3. Unity 侧逐脚本设计

### 3.1 Contracts

| 脚本与路径 | 类型与成员 | 实现与协作 |
| --- | --- | --- |
| `Editor/Contracts/FrameworkMcpToolRisk.cs` | `public enum FrameworkMcpToolRisk { ReadOnly }` | 第一版故意只允许 ReadOnly；新增写级别必须走新 ADR。 |
| `Editor/Contracts/FrameworkMcpToolAttribute.cs` | `public sealed` Attribute；无字段 | 显式生产发现标记；`Inherited=false`。 |
| `Editor/Contracts/FrameworkMcpToolDescriptor.cs` | `internal sealed`；Name、Title、Description、InputSchemaJson、OutputSchemaJson、Risk、TimeoutMs | 不可变构造；`Validate()` 检查名称、schema 非空、超时范围；序列化给 Host。 |
| `Editor/Contracts/FrameworkMcpToolRequest.cs` | `internal sealed`；RequestId、ToolName、ArgumentsJson、DeadlineUtc | 桥请求 DTO；只保存数据，不引用 Unity 对象。 |
| `Editor/Contracts/FrameworkMcpToolResponse.cs` | `internal sealed`；RequestId、IsError、ContentJson、ErrorCode、Message；`Success/Error` 工厂 | 统一业务结果；消息限长，禁止异常对象跨桥。 |
| `Editor/Contracts/FrameworkMcpToolContext.cs` | `public sealed`；ProjectRoot、IsPlaying、IsCompiling、Repaint 回调 | 每次调用在主线程创建；只暴露受控上下文。 |
| `Editor/Contracts/IFrameworkMcpTool.cs` | Descriptor；`UniTask<FrameworkMcpToolResponse> ExecuteAsync(string, FrameworkMcpToolContext, CancellationToken)` | 工具契约；不得持有请求间 Unity 对象或静态运行状态。 |

### 3.2 Core 与主线程

| 脚本与路径 | 字段/属性/方法 | 实现与协作 |
| --- | --- | --- |
| `Editor/Core/FrameworkMcpToolRegistry.cs` | `m_toolsByName`、Diagnostics；`Discover()`、`TryGet()`、`CreateDescriptors()` | 用 `TypeCache` 查带 Attribute 的实现；公共无参构造；稳定排序；任一重复/非法 schema 阻止 Host 启动。 |
| `Editor/Core/FrameworkMcpToolExecutor.cs` | Registry；`ExecuteAsync(request, token)` | 校验 deadline/工具/参数长度，建立 Context，捕获异常并生成结构化错误；不实现重试。 |
| `Editor/Core/McpMainThreadDispatcher.cs` | `ConcurrentQueue<WorkItem>`、capacity=32、pumpBudgetMs=10；`Enqueue()`、`Cancel()`、`Pump()`、`Shutdown()` | `[InitializeOnLoad]` 注册 `EditorApplication.update`；每次只执行一个工具，预算用于出队与轻量调度；Reload 完成全部等待项。 |
| `Editor/Core/McpBridgeJsonCodec.cs` | `Serialize<T>()`、`TryDeserializeEnvelope()` | 用 `JsonUtility` 只处理强类型 envelope；arguments/content 保持转义后的原始 JSON 字符串；检查 4 MiB 上限。 |
| `Editor/Core/McpRequestTracker.cs` | `Dictionary<string, CancellationTokenSource>`；`Begin()`、`Cancel()`、`Complete()`、`CancelAll()` | RequestId 去重、deadline CTS、断桥清理；所有访问在锁内，完成后 Dispose。 |

### 3.3 Host 生命周期、设置与桥

| 脚本与路径 | 字段/属性/方法 | 实现与协作 |
| --- | --- | --- |
| `Editor/Host/McpAdapterSettings.cs` | ScriptableSingleton；AutoStart、BasePort=6100、PortScanRange=20、TimeoutMs=30000、EnabledTools、HostPath；`SaveSettings()` | `[FilePath]` 保存到 Library；端口/超时钳制；不保存 PID、连接或 Token。 |
| `Editor/Host/McpTokenStore.cs` | `TokenPath`、`GetOrCreate()`、`Reset()`、`Mask()` | 生成 32 随机字节 Base64Url；原子写 Library；不打明文日志；重置后强制重启 Host。 |
| `Editor/Host/McpBridgeChannel.cs` | Process stdin/stdout、reader CTS、writer lock、事件 `MessageReceived/Faulted`；`StartReadLoop()`、`Send()`、`Dispose()` | 后台逐行读取 Host stdout；解析后把 invoke/cancel 交给 Dispatcher；stderr 单独限长收集诊断。 |
| `Editor/Host/McpHostProcess.cs` | `Process m_process`、Bridge、State、Endpoint、LastError；`StartAsync()`、`StopAsync()`、`ForceTerminateOwnedChild()` | 先验证 Host 版本/哈希，再以隐藏窗口启动；通过 stdin 发送 Token/工具，不把 secret 放参数；只终止自己记录的 PID。 |
| `Editor/Host/McpAdapterLifecycle.cs` | `[InitializeOnLoad]`；单例 HostProcess、重启窗口计数；`StartAsync()`、`StopAsync()`、`OnBeforeReload()`、`OnQuitting()` | 统一 AutoStart、手动启停、Reload/退出；60 秒 3 次熔断；部分启动逆序清理。 |
| `Editor/Host/McpClientConfigBuilder.cs` | `BuildCodexInstructions()`、`BuildCursorJson()`、`BuildClaudeJson()` | 只返回文本；不访问用户配置目录、不运行 CLI、不设环境变量；Token 复制由 UI 显式触发。 |

### 3.4 诊断与 Framework Center

| 脚本与路径 | 字段/属性/方法 | 实现与协作 |
| --- | --- | --- |
| `Editor/Diagnostics/McpAdapterDiagnostic.cs` | Severity、Code、Message、FixActionId | 结构化诊断；Code 覆盖 dotnet/Host 缺失、版本、端口、Token、工具、桥、熔断。 |
| `Editor/Diagnostics/McpAdapterDiagnostics.cs` | `Collect(settings, lifecycle, registry)` | 纯快照聚合；不在收集时构建 Host、扫描用户目录或改变状态。 |
| `Editor/FrameworkCenter/McpAdapterCenterPage.cs` | PageId、状态滚动、工具滚动、Smoke 状态；`OnGUI()`、`DrawStatus/Controls/Tools/ClientConfig/Diagnostics()` | `[FrameworkCenterPageExtension]`；按钮触发生命周期/构建任务；显示掩码 Token、端点、协议、PID、队列、最近错误；帮助链接本目录 README。 |
| `Editor/FrameworkCenter/McpHostBuildService.cs` | BuildState、日志摘要；`BuildAsync()`、`Cancel()` | 仅在按钮点击后调用 `BuildHost.ps1`；输出到 Library；隐藏窗口；禁止 Editor 启动时自动下载/构建。 |
| `Editor/FrameworkCenter/McpSmokeTestService.cs` | LastResult/Duration；`RunAsync()` | 启动 Host~ smoke client，依次 server/discover、tools/list、get_status；结果回显 Center，不绕过 Token。 |

### 3.5 第一版 7 个工具

每个工具一个文件，位于 `Editor/Tools/ReadOnly/`，均为 `public sealed`、带 `[FrameworkMcpTool]`、无运行时字段。

| 脚本 | 参数 DTO / 关键方法 | 算法与协作 |
| --- | --- | --- |
| `FrameworkGetStatusTool.cs` | 空参数；`ExecuteAsync` | 读取 `Framework.State/IsReady/LastException` 与 Editor 状态；异常只返回类型和摘要。 |
| `FrameworkListModulesTool.cs` | Scope、IncludeDisabled；`ExecuteAsync` | 复用中央设置、运行 Scope 记录和模块图内部只读 API；稳定按 scope/order/type 排序，最多 256 项。 |
| `FrameworkValidateConfigurationTool.cs` | ScenePath；`ExecuteAsync` | 调现有 `FrameworkProjectSettingsResolver` 与 `ModuleGraphResolver`；按稳定 code 输出，不复制校验算法。 |
| `UnityGetProjectInfoTool.cs` | 空参数；`ExecuteAsync` | 读取 Unity/产品/目标平台/活动场景/编译播放状态；默认隐藏绝对项目根。 |
| `UnityReadConsoleTool.cs` | Severity、Contains、Limit；`ExecuteAsync` | 将现有 Console 读取反射封装为独立内部 helper；只读、截断堆栈和单条长度；反射签名变化返回诊断。 |
| `UnityFindAssetsTool.cs` | Filter、Roots、Limit；`ExecuteAsync` | 校验根目录后调用 `AssetDatabase.FindAssets`/GUIDToAssetPath/GetMainAssetTypeAtPath；不 Load 全部资产。 |
| `UnityGetSceneHierarchyTool.cs` | ScenePath、RootPath、MaxDepth、Limit、IncludeComponents；`ExecuteAsync` | 只遍历已加载目标 Scene；显式栈深度优先、稳定 sibling 顺序；只输出组件类型名，不序列化字段。 |

辅助实现若只服务一个工具，保持为同目录的独立 `internal` 类型文件；实施中不得把多个工具塞进一个脚本。

## 4. .NET Host 逐文件设计

| 文件 | 类型/成员 | 实现与协作 |
| --- | --- | --- |
| `Host~/src/.../Framework_WWJ.Mcp.Host.csproj` | net8.0、SDK 2.0.x、锁定依赖 | 启用 deterministic/nullable；生成运行时版本清单；不引用 Unity DLL。 |
| `Host~/src/.../Program.cs` | `Main(args)` | 配置 Kestrel loopback、Host 过滤、请求上限、Bearer middleware、官方 MCP server；控制台日志重定向 stderr。 |
| `Host~/src/.../McpHostOptions.cs` | ParentPid、BasePort、ScanRange、BridgeLimit | 只接收非敏感参数；Token/工具由 `bridge.init` 读取；严格校验。 |
| `Host~/src/.../BridgeEnvelope.cs` | Type、RequestId、Payload 字段 | System.Text.Json 多态 envelope；限制深度/长度；未知类型 fail closed。 |
| `Host~/src/.../UnityBridgeConnection.cs` | pending TCS、stdin read、stdout write lock；`InitializeAsync()`、`InvokeAsync()`、`CancelAsync()`、`DisposeAsync()` | 与 Unity JSONL 对接；断开时完成全部 TCS；每个请求 timeout/cancel 后发送 cancel。 |
| `Host~/src/.../UnityProxyTool.cs` | 继承/包装官方 `McpServerTool`；Descriptor、Bridge；`InvokeAsync()` | 从 Unity descriptor 动态构造工具；转发 arguments；把 Unity response 映射为 text + structuredContent + isError。 |
| `Host~/src/.../UnityToolCatalog.cs` | `Register(descriptors)`、稳定列表 | 启动监听前注册；重复、非法 schema、非 ReadOnly 直接失败；目录进程内不可变。 |
| `Host~/src/.../BearerTokenMiddleware.cs` | Token hash；`InvokeAsync(HttpContext)` | 固定时间比较；保护 `/mcp`；不记录 Authorization；错误返回 401。 |
| `Host~/src/.../ParentProcessMonitor.cs` | ParentPid、poll interval；`RunAsync()` | 父 PID 消失或 stdin EOF 时触发 Host 停止，防孤儿。 |
| `Host~/src/.../EndpointSelector.cs` | `BindAsync(basePort, range)` | 仅扫描 loopback；返回实际端点；端口耗尽给可读错误。 |
| `Host~/BuildHost.ps1` | `-Runtime win-x64`、`-Output` | 校验目标在项目 Library 内，执行 restore/test/publish，输出版本清单和 SHA-256；不删除工作区其他目录。 |

## 5. 测试与样例文件

### EditMode

- `FrameworkMcpToolRegistryTests.cs`：显式发现、未标记排除、排序、重复、非法名称/schema。
- `McpBridgeJsonCodecTests.cs`：转义、Unicode、4 MiB 边界、未知类型、损坏行。
- `McpMainThreadDispatcherTests.cs`：容量、FIFO、单并发、排队取消、Shutdown、超时。
- `McpAdapterSettingsTests.cs`：默认值、钳制、Library 路径、不脏写 Assets。
- `McpTokenStoreTests.cs`：长度、稳定读取、重置、掩码、日志不泄露。
- `ReadOnlyToolsTests.cs`：7 工具正常、空结果、截断、非法路径/范围和稳定排序。

### PlayMode

- `McpFrameworkStatePlayModeTests.cs`：进入 Play Mode 后 status/list_modules 反映 Ready 与 Global/Scene，退出后无 Runtime 持有。
- `McpAdapterAssetDirtinessPlayModeTests.cs`：调用 7 工具前后模板 SO、中央设置与场景不被标脏。

### Host xUnit 与端到端

- `ProtocolCompatibilityTests.cs`：2026-07-28 discover/list/call 和 SDK 旧客户端兼容。
- `AuthenticationTests.cs`：缺失/错误/正确 Token，Origin/Host，敏感日志。
- `BridgeFailureTests.cs`：Unity 断桥、超时、取消、重复 ID、父进程退出。
- `LimitsTests.cs`：请求/响应上限、队列 busy、schema 错误。
- `SmokeClient`：官方 SDK 连接真实 Host，调用 7 个工具并验证 output schema。

`Samples~/README.md` 记录 Codex/Cursor/Claude 配置示例、Center 操作路径和人工验收；
`client-config.example.json` 只含占位符，不含真实 Token/端口。

## 6. 实施门禁与顺序

1. **G0 批准：** 用户确认需求简报 5 项决定与 ADR；未批准不写代码。
2. **G1 Host Spike：** 在临时/模块 Host~ 中证明官方 SDK 2.0.x、net8、loopback、动态工具代理与 Unity JSONL Echo；失败则回到 ADR，不继续堆命令。
3. **G2 边界编译：** 创建目录、asmdef、Host csproj、meta；Unity 与 `dotnet build/test` 均通过，依赖箭头正确。
4. **G3 纯契约：** Registry、schema、codec、Token、Host auth/协议测试通过。
5. **G4 生命周期：** 进程、桥、主线程队列、取消、超时、Reload/退出清理通过；无孤儿进程。
6. **G5 垂直切片：** 先只接 `framework.get_status`，完成 SDK Smoke Client 与 Codex 真实调用。
7. **G6 工具集：** 按 status → config/modules → project/console → asset/scene 顺序加入其余 6 工具；每个先过测试再加下一个。
8. **G7 Center/样例：** 启停、诊断、Host 构建、配置复制、Smoke Test 和帮助完成。
9. **G8 回归/收尾：** 模块 EditMode/PlayMode、Host tests、全 Framework_WWJ 回归、人工 Reload/PlayMode/关闭检查；回写验收、索引、状态和架构元数据。

任何门禁失败都停在当前层，不通过增加更多工具掩盖基础问题。

## 7. 量化验收

- Host：启动后 5 秒内 ready；Stop/Reload 后 2 秒内退出，0 个孤儿子进程。
- 协议：`server/discover`、`tools/list`、7 个 `tools/call` 全通过；非法 Token 100% 返回 401。
- 线程：100 个并发只读请求保持 Unity 执行并发度 1，队列顺序稳定，超过容量明确 busy。
- 性能：预热后 `framework.get_status` 本机 100 次 P95 < 250 ms；Host 空闲私有内存 < 150 MiB。
- 限制：1 MiB 请求、4 MiB 响应、30 秒默认超时、日志/资产/层级结果均按声明截断。
- 资产：调用、PlayMode 和 Reload 前后 Framework 模板、中央设置、场景序列化 diff 为 0。
- 回归：以执行时仓库最新用例数为准，Framework_WWJ 全 EditMode/PlayMode 通过；不硬编码历史 33/13。
- 文档：README、需求、ADR、计划、验收、Docs 索引、当前状态、决策待办、架构 Attribute 全部一致。

## 8. 明确迁移与清理

- 不修改 LyingBottle、其 `.mcp.json`、`MCP/`、ActFramework 或 HtyHub。
- 不复用 `Unity*` 工具名称，不要求用户迁移现有 HTY MCP 配置。
- 删除模块时，移除 Adapter 文件夹和 Library 下本模块数据即可；Core Runtime 仍可编译运行。
- Host 进程、Token、endpoint 和 publish 目录都在本模块明确路径下；卸载说明提供手动可恢复清理步骤。
- 若未来分发 App 接管 Host 发布物，只更新分发清单与安装器，不把协议依赖引入 Core Runtime。
