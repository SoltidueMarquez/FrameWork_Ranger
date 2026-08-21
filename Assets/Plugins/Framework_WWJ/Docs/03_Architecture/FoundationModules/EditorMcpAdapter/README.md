# Editor MCP Adapter 设计入口

> 日期：2026-08-19  
> 状态：提议，等待用户批准；尚未创建 Runtime、Editor、Host、测试或配置代码。

本目录描述 Framework_WWJ 的首个 MCP 服务适配模块。它面向 Unity Editor 与 AI 编程客户端，
不进入玩家构建，也不成为 `ModuleBase`、GlobalScope 或 SceneScope 的运行时模块。

## 文档

1. [需求简报](./00_Requirement_Brief.md)
2. [架构与公开契约](./01_Architecture_And_Public_Contracts.md)
3. [具体实施计划](./02_Implementation_Plan.md)
4. [验收与复盘占位](./03_Acceptance_And_Review.md)
5. [ADR-EMA-001：Editor-only 适配层与外部 MCP Host](./ADR/ADR-EMA-001_Editor_Adapter_And_External_Host.md)

## 当前建议

- **事实：** LyingBottle/HTY 的 `EditorMcpKit` 在 Unity Editor 进程内使用 `HttpListener`、反射命令注册、
  主线程队列、Bearer Token、客户端配置同步和 FrameworkCenter 页面；当前源码中可识别 69 个命令类，
  旧发布清单 `MCP/HtyUnityMCPServer/.../tools.json` 列出 28 个工具。
- **事实：** 参考实现固定声明 MCP `2024-11-05`；2026-08-19 的官方最新稳定规范是 `2026-07-28`，
  已采用无握手的无状态核心、`server/discover`、标准 HTTP 路由头和缓存提示。
- **建议决定：** MCP 协议与 HTTP 服务由独立 .NET 8 Host 承担；Unity Editor 侧只负责工具目录、
  主线程执行、Host 进程生命周期和 Framework Center 诊断。
- **建议决定：** 第一版只交付 7 个只读工具，先证明协议、线程、Domain Reload、鉴权和清理闭环；
  场景/资产/脚本写入工具进入后续独立审批，不随基础适配器自动扩张。

## 审批边界

用户批准本计划前，只允许维护本目录设计资料。批准后仍按实施门禁逐层推进；任何写工具、
自动修改 Codex/Cursor/Claude 用户配置、远程网络监听或 Runtime 能力都需要新的明确决定。
