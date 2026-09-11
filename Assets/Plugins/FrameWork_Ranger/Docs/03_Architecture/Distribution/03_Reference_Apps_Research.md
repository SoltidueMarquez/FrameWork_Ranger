# 参考 App 静态调研

> 检查日期：2026-09-11。对象为用户提供的本地发行程序；非联网产品评测。未启动参考程序、未使用其认证、未连接其服务或执行安装/同步/发布。

## 1. 方法与证据等级

- 读取 Windows 文件版本信息、Electron `app.asar` 的文件表与选定脚本，以及单文件 EXE 中可读符号。
- **事实**表示检查到的元数据、代码入口或明确分支；**推断**表示由符号推测的产品意图；**建议**是我们自己的设计。
- ASAR 是应用打包容器。选定文件仅提取到系统临时目录检查，未运行其中代码，也不把第三方实现复制到框架仓库。
- 下列 ASAR 路径均指容器内路径。行号对应本次打包文件，不保证与参考作者的源码行号一致。

## 2. HtyHub：模块分发参考

来源：`D:/unityhub/UnityProjects/FrameWork/HtyHub/resources/app.asar`。

**元数据事实：** `package.json` 中产品包名 `htyhub-app`，版本 `0.3.4`，入口 `out/main/index.js`。依赖包含 React 18、Zustand、electron-updater、fast-glob 及 ZIP 处理库；描述自称支持 Mac/Windows，但本次只检查 Windows 发行物。

| 能力 | 静态证据 | 对我们的价值 |
| --- | --- | --- |
| 多项目登记与切换 | `out/main/index.js:417` 起的 `projects:*` IPC | 以游戏项目为上下文显示模块状态 |
| 包索引与元数据 | 同文件约 171–174 行的 `index.json`、`htypackage.json` | 独立模块需要可检索的身份、版本和依赖 |
| 安装依赖 | `installPackage:1781`，包括循环依赖分支、Unity 依赖处理和安装顺序 | 安装计划必须解释自动加入的依赖 |
| 更新、卸载、上传 | `updatePackage:1876`、`uninstallPackage:2089`、`uploadNewPackage:2195`、`uploadUpdatePackage:2202` | 模块发布者与使用者可在同一 App 操作 |
| 本地变更检查 | `packages:detectChanges`，约 4006 行起 | 模块详情中区分版本落后与本地修改 |
| 文件和 `.meta` | ZIP 安装/上传路径及 `.meta` 处理分支 | Unity 资产身份需要随模块维护 |
| 操作任务和 App 更新 | `tasks:*:4229`、`update:*:5682` 起 | 长操作有进度、失败状态和记录 |

**限制：** 包下载结束与本地状态保存不能等同于 Unity 编译、场景引用或模块功能通过。本次看到安装过程中逐包落地与更新项目清单，未验证整体事务回滚。也没有证据支持宣称其覆盖所有本地修改冲突、历史版本兼容或第三方许可问题。

**建议借鉴：** 项目切换、模块列表/详情、安装依赖解释、版本与变更信息、操作任务。首版不扩展到其 Skills、Memory、仓库管理权限等外围能力。

## 3. HtyApp：开发同步与合并参考

来源：`D:/unityhub/UnityProjects/FrameWork/HtyApp/resources/app.asar`。

**元数据事实：** `package.json` 中包名 `hty-skill-manager`，版本 `0.6.3`；Electron CommonJS 主进程，React 19、Zustand 5，另含编辑器、差异与多语言相关依赖。

| 能力 | 证据 | 判断 |
| --- | --- | --- |
| 项目与仓库双向同步 | `electron/sync-service.cjs:160` 起 `sync_bulk_sync`，`RepoToProject` / `ProjectToRepo` | 已看到方向选择及复制、覆盖、删除的实现 |
| 差异计算与同步基线 | `electron/sync-utils/diff-engine.cjs:120` 的 `isConflict`、`sync-state-storage.cjs` | 已看到基线存在性和内容摘要比较 |
| 规则、项目与日志持久化 | `blacklist-storage.cjs`、`filter-scheme-storage.cjs`、`project-storage.cjs`、`sync-log-storage.cjs` | 适合参考过滤方案和操作历史 |
| 后台计算 | `diff-worker.cjs` 与相关服务 | 扫描不应阻塞界面 |
| 三方合并会话 | `electron/merge-session.cjs:5`、`:150`、`:380` | 保存 base/local/target/result，区分待解决冲突 |
| 合并会话调用方 | `electron/service.cjs` 的 `prepareAppendPublishMerge`、更新实例流程（约 737、807 行） | 本次确认用于 Skill 发布/更新；未证明用于目录同步 |

### 需要超越参考实现的两处语义

1. `diff-engine.cjs` 的 `isConflict` 定义了删除相关判断，但当前 `computeDiffs` 在仅一端存在文件时直接产生 added/deleted；调用冲突判断的可见分支是双方文件都存在的情况。因此本次不能认定其实际扫描链路正确覆盖“上游删除、本地修改”。我们的验收必须单列此场景。
2. `sync_bulk_sync` 对选定路径按方向直接复制或删除。该分支本身没有展示写前完整冲突复核和整体回滚。不能因为界面提供冲突状态，就把写操作的保护当成已验证事实。

这两点是限定版本、限定代码路径的静态观察，不是对整个产品的完整缺陷审计。

**建议借鉴：** 源/目标明确的差异工作区、批量选择、过滤方案、可继续的合并会话。我们应以“模块 + 精确来源 + 安装基线”组织同步，而不是让用户每次重新选两个任意目录。

## 4. HtyFrameworkSync：早期专用同步工具参考

来源：`D:/unityhub/UnityProjects/FrameWork/HtyFrameworkSync.exe`，文件大小 162,841,560 字节，Windows 产品版本 `1.0.0`。

**静态事实：** 可读符号包含 `HtyFrameworkSync.dll`、`HtyFrameworkSync.Views.ProjectWindow`、`MainViewModel`、WPF PresentationFramework，以及 `net8.0-windows/win-x64` 构建路径片段。

与任务直接相关的符号：

- `StartScanAsync`、`RefreshDiff_Click`、`DiffModeCombo_SelectionChanged`；
- `MenuUpdateFromRepo_Click`、`MenuApplyToRepo_Click`；
- `BulkUpdateFromRepo_Click`、`BulkApplyToRepo_Click`、`SyncFileOneWayAsync`；
- `DetectConflicts`、`SyncDirection`；
- Feishu 下载/上传窗口及版本记录相关服务。

**推断：** 这是以 WPF/.NET 构建的 Windows 项目与仓库同步工具，界面可能围绕项目、差异和两个同步方向组织。符号不证明这些功能当前能成功运行；本次没有还原业务代码，也没有验证冲突算法、回滚、凭据存储或云端行为。飞书部分不纳入我们的需求。

## 5. 综合借鉴矩阵

| 主题 | 主要参考 | 我们的建议 |
| --- | --- | --- |
| 按需选择和管理模块 | HtyHub | 模块目录、依赖解释、版本选择、安装状态 |
| 游戏内开发并回写 | HtyApp / HtyFrameworkSync | 默认按模块识别改动，区分通用与项目专属 |
| 合并冲突 | HtyApp 的合并会话 | 采用基线/项目/目标三方模型，自行验证边界场景 |
| Unity 分发正确性 | 参考程序不足以证明 | 增加 GUID、配置归属、程序集与 Unity 验证 |
| 平台与 UI 技术 | Electron 两个实例，WPF 一个实例 | 按团队语言和平台需求选型，不由参考数量决定 |

已有 [YokiFrame 调研](../../02_References/YokiFrame/00_YokiFrame_Kit_Architecture_And_Source_Map.md)可补充 Scanner/Planner/Apply/Verify 分层思路；它是 2026-08-19 的项目记录，本轮没有重新验证该项目最新源码。

## 6. 后续交互验证

正式做视觉设计时，再用临时示例目录体验项目切换、安装依赖确认、两端变更、取消操作和失败提示。当前文档没有把未观察到的页面样式或交互细节写成事实。正式产品采用自有名称和设计，本轮输出仅保留归纳与定位，不纳入参考程序源码和发行物。
