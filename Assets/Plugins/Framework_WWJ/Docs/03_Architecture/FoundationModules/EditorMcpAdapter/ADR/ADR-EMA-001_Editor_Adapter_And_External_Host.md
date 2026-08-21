# ADR-EMA-001：Editor-only 适配层与外部 MCP Host

- 状态：提议
- 日期：2026-08-19
- 所属模块：Editor MCP Adapter

## 背景与约束

Framework_WWJ 需要向 Codex 等客户端暴露 Unity Editor 工具。LyingBottle/HTY 的已验证做法是在 Unity Editor 进程内
运行 `HttpListener`，并用后台线程、主线程队列、Domain Reload 自动启动、Token、客户端配置与 Center 页面组成完整服务。
其真实代码也显示了 importer/update/delayCall 互锁、失焦调度、并发串行化和长任务超时需要专门治理。

同时，MCP 最新稳定规范已经从参考实现声明的 `2024-11-05` 演进到 `2026-07-28`。Framework_WWJ Runtime
应继续只承担游戏框架生命周期，不因编辑器自动化协议而引入 HTTP、鉴权、JSON-RPC 或外部 SDK。

## 候选方案

### A. Unity Editor 进程内自实现 MCP + HttpListener

- 优点：单进程、部署直观、最接近 HTY。
- 缺点：需要自行追赶 MCP 规范；网络线程、Unity 主线程、Domain Reload、Importer 与长任务互锁风险集中在 Editor；
  协议代码和业务工具难分发、难独立测试。

### B. 独立 .NET 8 官方 SDK Host + Unity JSONL Adapter

- 优点：协议、HTTP、旧版本兼容与安全更新由官方 SDK承担；Host 可独立测试；Unity 侧只有工具和主线程桥；
  Core Runtime 不受影响。
- 缺点：多一个子进程和 .NET 8 发布物；需要治理桥、父进程监视和安装体验。

### C. 直接依赖第三方 Unity MCP Package

- 优点：最快获得大量工具。
- 缺点：公共契约、分发、版本与安全边界由第三方决定；难与 Framework Center、架构元数据和模块交付契约统一。

## 建议决定

采用方案 B，并把模块定义为 Editor-only adapter：

1. MCP Host 使用官方 C# SDK 2.0.x 和 .NET 8，只监听 loopback。
2. Unity Adapter 使用 JSON Lines 与 Host 通讯，通过有界队列串行执行 Unity API。
3. Host 与 Adapter 的工具目录在每次 Host 启动时一次性协商，进程内保持不可变。
4. 第一版仅开放 7 个显式标记的只读工具。
5. 模块不继承 `ModuleBase`，不使用 Global/Scene Scope，不创建 SO/中央配置。

## 影响

- 新增一个 Editor asmdef、两个测试 asmdef、一个 Unity 忽略的 Host~ 源码区和 Library 发布/配置目录。
- Runtime/Editor `AssemblyInfo.cs` 仅增加受控 friend assembly，Core 类型和生命周期签名不变。
- 模块可独立移除；Core Runtime 不引用 Adapter。
- Host 协议版本可独立升级，但桥 schema 的破坏性变化必须增加 bridge version 并提供清晰不兼容诊断。
- 第一版要求 Windows x64 与 .NET 8；跨平台是后续发布决策。

## 明确非目标

- 不把 HTY API、命令名或 69 个工具复制到 Framework_WWJ。
- 不让外部 Host直接读取/修改 Unity 项目文件以绕过 Editor 工具权限。
- 不支持远程网络、OAuth、运行时游戏控制、写工具和自动改客户端配置。

## 验证方式

- G1 Spike 证明官方 SDK Host、动态代理工具和 JSONL Echo。
- Host xUnit 验证协议、鉴权、限制、取消、断桥和父进程退出。
- Unity EditMode/PlayMode 验证注册、队列、7 工具、生命周期和资产不脏写。
- Smoke Client 与 Codex 端到端验证；Reload/关闭后确认无孤儿进程。

## 批准记录

用户批准日期、批准范围与任何修改在此补录；当前“提议”不得解释为 Runtime 实施授权。
