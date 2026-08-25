# Editor MCP Adapter 需求简报

> 日期：2026-08-19  
> 状态：提议，等待用户确认边界。

## 1. 输入解释与证据标签

- **事实：** 用户希望仿照“JTY 框架”加入 MCP 服务适配；已提供的参考工程是
  `D:\unityhub\UnityProjects\LyingBottle`，其中实际框架名称为 HTY/ActFramework。
- **推断：** 在用户提供另一份 JTY 参考前，本计划把“JTY”按“HTY/ActFramework”理解。
- **事实：** Framework_WWJ 当前 Runtime 核心采用 SO 模板、运行克隆、Global/Scene Scope、串行生命周期，
  Framework Center 已提供显式页面扩展机制。
- **事实：** Framework_WWJ 当前没有 MCP 代码；工作区存在其他未提交的 ResourceManagement 与文档改动，
  本设计不覆盖或重写它们。
- **候选：** 本适配器作为一个正式的 Editor-only 模块交付，但不继承 `ModuleBase`，不改变核心生命周期。

## 2. 要解决的问题

让本机 AI 编程客户端通过标准 MCP 调用 Unity Editor 与 Framework_WWJ 的受控只读能力，避免每个客户端
各写一套 Unity 自动化协议，并使工具暴露、鉴权、主线程执行、重载恢复和诊断都可观察、可测试、可卸载。

### 首批调用方

1. Codex Desktop/CLI：第一验收客户端。
2. 官方 MCP C# SDK Smoke Client：自动化端到端验收客户端。
3. Cursor/Claude：只提供配置片段与兼容性说明，不在第一版自动写用户配置。

### 可观察结果

- Framework Center 能构建、启动、停止 Host，并显示端点、协议版本、工具目录和最近错误。
- 客户端能发现 7 个工具，使用有效 Token 调用并获得结构化结果。
- 所有 Unity API 都在 Editor 主线程执行；并发调用不会并发修改 Unity 状态。
- 脚本重编译、Domain Reload、进入/退出 Play Mode、关闭 Editor 后没有孤儿 Host 或悬挂请求。
- 服务运行前后不修改 Framework SO、中央设置、场景或用户资产。

## 3. 第一版范围

### 必须实现

- Unity 2022.3.62f3、Windows x64、Editor-only。
- 独立 .NET 8 MCP Host，使用官方 `ModelContextProtocol.AspNetCore` 2.0.x。
- MCP `2026-07-28` 无状态 Streamable HTTP，并由官方 SDK提供向旧客户端降级的兼容路径。
- 单一 `http://127.0.0.1:<port>/mcp` 端点；Bearer Token 默认强制开启。
- Unity 与 Host 通过受控标准输入/输出 JSON Lines 桥接，stdout 只承载桥协议，Host 日志走 stderr。
- 显式标记的工具发现、稳定排序、重复名称诊断、固定进程内目录；启用项变化后重启 Host 生效。
- 有界单线程 Unity 主线程队列、请求超时、排队取消、Host 崩溃和 Domain Reload 清理。
- Framework Center 页面、项目本地 Library 设置、配置片段复制、Smoke Test。
- 7 个只读工具：
  `framework.get_status`、`framework.list_modules`、`framework.validate_configuration`、
  `unity.get_project_info`、`unity.read_console`、`unity.find_assets`、`unity.get_scene_hierarchy`。

### 明确不实现

- 玩家构建中的 MCP、Runtime Module SO、Handler、Global/Scene 配置项。
- 场景、资产、Prefab、脚本、Package、ProjectSettings 的写入或删除工具。
- 任意菜单执行、Shell 命令、反射调用任意方法等通用逃生口。
- 外网、局域网或 `0.0.0.0` 监听；OAuth、多人或跨机器授权。
- MCP prompts、resources、apps、tasks、subscriptions 和服务端主动请求。
- 启动时自动修改 Codex/Cursor/Claude 的用户配置或系统环境变量。
- 复制 LyingBottle 的 69 个命令或其 API 名称。

### 延后能力

- 写工具与逐工具确认策略。
- 长任务、Tasks 扩展、编译后续跑与显式 job handle。
- macOS/Linux Host 发布物。
- 客户端配置的一键安装/卸载。
- 模块工具由其他 Editor asmdef 动态扩展时的热更新通知。

## 4. 生命周期与所有权

| 对象 | 创建者 | 所有者 | 释放者 | 持久化 |
| --- | --- | --- | --- | --- |
| Editor 设置 | `McpAdapterSettings.instance` | 当前 Unity 项目 | Unity Editor | `Library/Framework_WWJ/Mcp/Settings.asset` |
| Bearer Token | `McpTokenStore` | 当前项目 + 当前 OS 用户 | 用户显式重置 | `Library/Framework_WWJ/Mcp/token`，不进版本库 |
| Host 进程 | `McpAdapterLifecycle` | 当前 Unity Editor 进程 | Reload/退出/手动 Stop | 不持久化 |
| 工具目录 | `FrameworkMcpToolRegistry` | 当前 AppDomain | Domain Reload | 不持久化 |
| 请求 | `McpBridgeChannel` | Host 请求 ID | 完成/取消/超时/断线 | 不持久化 |
| Unity 队列项 | `McpMainThreadDispatcher` | 当前 AppDomain | 完成/取消/Reload | 不持久化 |

- Scope：Editor 进程级，与 GlobalScope/SceneScope 无关。
- 实例数：每个 Unity 项目 Editor 进程最多一个 Host、一个 Bridge、一个主线程执行队列。
- Shutdown：停止接收新请求，取消排队项，给 Host 最多 2 秒优雅退出，随后仅杀死本模块启动且 PID 匹配的子进程。

## 5. 调用、取消、失败与重试

- 工具调用是异步请求/响应；Host 负责 MCP 协议，Unity 侧返回 `success/data/error` 的结构化 JSON。
- 默认超时 30 秒；队列容量 32；Unity 执行并发度固定为 1。
- 客户端取消时，未开始的队列项被移除；正在执行的不可取消 Unity API允许完成，但结果被丢弃。
- 工具业务错误使用 MCP `isError=true` 和结构化错误；未知工具、非法协议或头不一致使用协议级错误。
- Host 崩溃不自动无限重启；60 秒内最多 3 次，之后熔断并要求用户在 Center 手动重试。
- Domain Reload 后若启用 AutoStart 且 Host 发布物有效，重新建立工具目录和 Host；旧请求不重放。

## 6. 依赖与配置

- Unity asmdef 直接依赖：`Framework_WWJ.Runtime`、`Framework_WWJ.Editor`、UnityEditor。
- Host 依赖：.NET 8、`ModelContextProtocol.AspNetCore` 2.0.x；依赖只存在于 `Host~`，不进入 Unity 编译域。
- JSON：Unity 桥层使用 `JsonUtility` 处理强类型 envelope，工具 arguments/results 作为不透明 JSON 字符串传递；
  Host 使用官方 SDK/System.Text.Json 验证 schema，从而不改 `Packages/manifest.json`。
- SO/中央配置：不创建 Module SO，不修改 `FrameworkProjectSettings.asset`，不产生场景接线。

## 7. 性能、安全与资产约束

- 绑定地址必须精确为 `127.0.0.1`；拒绝非预期 Host/Origin，不启用通配 CORS。
- Token 至少 256 bit 随机值；不通过命令行参数传递，不在日志和普通 UI 中明文显示。
- 请求体上限 1 MiB，工具输出上限 4 MiB，超限返回结构化错误。
- Editor 聚焦、Host 预热后的只读调用候选指标：P95 小于 250 ms；外部 Host 空闲私有内存小于 150 MiB。
- 主线程单次队列 pump 预算 10 ms；工具自身必须分页/限量，禁止无界层级和全盘扫描。
- 原始 SO、中央配置和场景在 Play Mode 前后序列化内容不变。

## 8. 需求验收证据

- EditMode：工具发现、schema、稳定排序、重复诊断、设置、Token、桥消息、队列取消、7 个工具的边界测试。
- PlayMode：Framework 状态/模块列表在进入和退出 Play Mode 时正确，Host 不进入玩家生命周期，资产不脏写。
- Host 测试：当前协议发现/list/call、旧协议兼容、Token 401、头/Body 不一致、超限、取消、断桥、父进程退出。
- 端到端：Smoke Client 和 Codex 分别完成发现与 7 工具调用；Domain Reload 后可重新连接。
- 人工：Center 启停、复制配置、Token 隐藏/重置、Host 缺失和 dotnet 缺失诊断。

## 9. 需要用户批准的决定

1. “JTY”按 LyingBottle 中的 HTY/ActFramework 理解。
2. 采用外部 .NET 8 Host，而不是照搬 Unity 内 `HttpListener`。
3. 第一版只交付 7 个只读工具。
4. 第一验收平台为 Windows x64，第一客户端为 Codex。
5. 第一版只生成/复制客户端配置，不自动写用户目录或环境变量。
