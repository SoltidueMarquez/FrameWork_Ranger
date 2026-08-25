# Editor MCP Adapter 验收与复盘

> 状态：尚未实施；本文件只定义关闭阶段需要填写的证据结构，不能作为已完成证明。

## 1. 实际交付

实施后列出 Editor、Host、Tests、Samples、Library 配置和直接集成点的实际文件，不复制计划假装完成。

## 2. 需求到证据映射

| 需求 | 自动测试 | 人工/端到端证据 | 结果 |
| --- | --- | --- | --- |
| MCP 发现与 7 个只读工具 | Host protocol + tool tests | Codex/Smoke Client | 待实施 |
| Token 与 loopback 安全 | AuthenticationTests | Center 隐藏/重置检查 | 待实施 |
| Unity 主线程与有界队列 | DispatcherTests | 并发调用观察 | 待实施 |
| Reload/退出无孤儿 | BridgeFailureTests | 重编译、关闭 Editor | 待实施 |
| 资产不脏写 | EditMode/PlayMode dirtiness | Git/序列化 diff | 待实施 |
| 可卸载边界 | asmdef/依赖检查 | 移除模块临时副本编译 | 待实施 |

## 3. 必填结果

- Host `dotnet test`：待实施。
- 模块 EditMode：待实施。
- 模块 PlayMode：待实施。
- Framework_WWJ 全量回归：待实施。
- Codex 端到端：待实施。
- Domain Reload、PlayMode、Editor 退出、Host 崩溃：待实施。
- P95、内存、请求/响应限制：待实施。
- SO/中央设置/场景 dirtiness：待实施。

## 4. 偏差、限制与后续候选

实施后只记录真实偏差与已知限制。写工具、Tasks、跨平台 Host、自动客户端配置和远程访问只能作为候选，
不因本验收文件出现而自动进入实现。
