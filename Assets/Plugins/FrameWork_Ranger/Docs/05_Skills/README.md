# FrameWork_Ranger Skill 路由

> 更新：2026-09-05。自然语言启动与 JSON 任务记忆已接入。
> 用户在本项目新建对话后直接描述需求即可，无需输入 Skill 名称或粘贴工作流。

## 项目入口

根目录 [AGENTS.md](../../../../../AGENTS.md)指向本页和[任务记忆协议](./01_Task_Memory_And_Recovery.md)。个人 Skills 提供简短路由，项目文档保存实际工作流程与稳定设计；不在两处维护模块进度副本。

当前主机的个人 Skills 位于 `C:\Users\Maugham\.codex\skills`，本会话可发现这些技能。工程路径由当前工作目录确定，外层名字可能仍为 `FrameWork_WWJ`，不跳转到旧的绝对路径。

## 自然语言路由

| 用户意图 | 技能 | 项目文档 |
| --- | --- | --- |
| 项目需求、状态、解释、任务继续 | `$work-with-framework-ranger` | [Docs 入口](../README.md)、[任务记忆](./01_Task_Memory_And_Recovery.md) |
| 新模块、模块功能、设计、实现、修复 | `$build-framework-ranger-module` | [流水线](../03_Architecture/FoundationModules/01_AI_Module_Development_Pipeline.md)、[交付契约](../03_Architecture/FoundationModules/02_Module_Delivery_Contract_And_Templates.md) |
| 核心生命周期、Module/Handler、Scope 或共享 Editor 基础设施 | 再加 `$framework-ranger-lightweight-refactor` | [Core 入口](../03_Architecture/Core/README.md)、[Editor 入口](../03_Architecture/EditorCenter/README.md)及相关现行 ADR |
| 仓库、安装、分发、跨项目同步或管理工具 | `$plan-framework-ranger-distribution` | [分发入口](../03_Architecture/Distribution/README.md)，按研究/设计/实施请求范围执行 |

“事件中心”“想做一个音频模块”“继续昨天的功能”等表达均可匹配，不要求包含 `FrameWork_Ranger` 字样。若个人 Skill 未加载或另一台机器未安装，直接读取表中项目文档继续工作；项目入口不依赖用户先修复个人技能安装。

## 每次任务加载多少

先看任务目标、任务 JSON 与相关模块入口，再按需打开设计、源码、ADR 或参考资料。不要每次都加载全量 Core 历史、全部模块、HTY/YokiFrame 资料和旧测试基线。

开始长任务前保存初始要求；恢复时读取所有有效要求，包括已实现的要求。关键回答和阶段结果及时落盘，具体规则只维护在任务记忆协议中。

## 授权与设计调整

用户只要求研究/设计时按该范围交付。用户已要求实现且边界清楚时继续执行；已有授权持续有效。只确认改变目标、公共行为、所有权或依赖的关键未决选择；内部调整与符合约定的修复直接推进。

流程不需要审批哈希、固定批准口令、逐脚本重新批准或每项澄清单独立 REQ。需要小实验时先验证具体未知点，设计随事实演进。

## 工具与验证

Unity 操作使用 `Tools/UnityCli.ps1`，遵守[按改动选择验证的规则](../04_Standards/Unity_CLI_Development_Rules.md)。不默认添加指纹、完整环境快照、复杂失效图或全量测试；参考包中的飞书和监督命令不自动执行。

JSON 模板位于项目根 `.workflow/_templates/`，当前没有额外 CLI 结果摘要或自动监督器。是否扩展工具依据实际使用成本决定。

## 当前模块与事件试点

模块进度看[基础模块入口](../03_Architecture/FoundationModules/README.md)、对应验收记录和实际代码。个人 Skill 不保存“哪些模块已经完成”的清单。

用户重新开始事件中心时可立即进入需求与设计；保留现有 Pooling 未验收事实，仅在影响 Reference Pool 的实际依赖时处理相关检查。不要把当前流程更新任务自动转成 Event 实现任务。

## 同步与验证范围

2026-09-05 已同步根 AGENTS、入口/模块/核心/分发个人 Skills、任务 JSON 模板、模块流程、计划粒度和 CLI 验证选择。技能 metadata 与文件校验用于确认可发现的配置、链接和语法，不证明模型每次都会正确路由；事件中心的新对话是实际使用试点。

Codex 会在任务启动时读取项目 AGENTS，Skill 可按描述隐式匹配，隐式调用默认开启。依据：[官方 AGENTS 说明](https://learn.chatgpt.com/docs/agent-configuration/agents-md)、[官方 Skill 说明](https://learn.chatgpt.com/docs/build-skills)。本机沿用已被当前应用发现的个人技能目录，不为匹配文档中的其他目录再次复制同名 Skills。
