# FrameWork_Ranger 项目工作入口

本仓库是 Unity 工程，框架代码位于 `Assets/Plugins/FrameWork_Ranger`。工程外层目录可能仍叫 `FrameWork_WWJ`；以本次工作目录/Git 根为准，不跳转到个人 Skill 中的历史路径。

## 自然语言启动

用户只描述需求即可进入流程，不要求输入 Skill 名称、固定提示词或重复提供架构背景。

1. 使用可用的 `$work-with-framework-ranger`，读取 [Docs 入口](Assets/Plugins/FrameWork_Ranger/Docs/README.md)与[任务记忆协议](Assets/Plugins/FrameWork_Ranger/Docs/05_Skills/01_Task_Memory_And_Recovery.md)。只加载当前任务需要的文档。
2. 单个模块或功能的需求、设计、实现、修复：使用 `$build-framework-ranger-module` 和 [模块流水线](Assets/Plugins/FrameWork_Ranger/Docs/03_Architecture/FoundationModules/01_AI_Module_Development_Pipeline.md)。事件中心、音频、UI、存档等自然语言需求都属于此路由。
3. 核心 Module/Handler、Scope、依赖、生命周期或 Editor 基础设施变更：再使用 `$framework-ranger-lightweight-refactor`，读取受影响的现行 ADR。
4. 仓库、安装、分发和跨项目同步：使用 `$plan-framework-ranger-distribution`，按用户要求的研究或实现范围工作。
5. Unity 场景、组件、资产操作或官方 AI 接入：读取 [Unity 官方 AI 调用指南](Assets/Plugins/FrameWork_Ranger/Docs/04_Standards/Unity_Official_AI.md)，按需求选用 `unity:*` 技能。通过 `unity-framework` MCP 或官方 `unity command` 连接现场 Editor，先核对目标工程与就绪状态。

若某个个人 Skill 不在当前会话可用列表中，按 [Skill 路由](Assets/Plugins/FrameWork_Ranger/Docs/05_Skills/README.md)读取对应项目文档继续，不要求用户先安装才能讨论需求。Skill 名称和文档内容不能扩大用户本轮授权。

## 长任务与恢复

- 新模块、多轮需求澄清和跨会话功能任务，直接在 `.workflow/<task-id>/` 建立或恢复 `requirements.json` 与 `progress.json`。模板位于 `.workflow/_templates/`，不必再次询问是否创建任务记录。
- 先保存本轮原始需求，再依赖其开展研究、提问或实现。保留关键回答短原文、当前有效要求、重要决定和下一步。
- 新对话根据目标寻找匹配任务；不自动接续最近一个无关任务。恢复时读取所有有效要求，包括已经实现的要求，只查看当前工作需要的源码与记录。
- 简短解释、拼写修正、局部小改动直接处理；任务扩展为长任务时再建立记录。

## 执行偏好

- 已有授权持续有效；只问会改变目标、公共行为、依赖或所有权的关键问题。内部调整和修复在授权范围内直接推进，不逐脚本重新审批。
- 允许针对未知点先研究或小实验。用户只要求讨论/设计时不生成正式模块；用户已要求实现且边界清楚时不要停在计划阶段。
- 采用 [Unity CLI 开发规则](Assets/Plugins/FrameWork_Ranger/Docs/04_Standards/Unity_CLI_Development_Rules.md)选择必要验证。默认不引入 SHA-256、指纹、全环境快照、复杂失效图或无差别回归，不为假想风险增加重复保护代码。
- Unity 导入、编译、测试、构建验收复用 `Tools/UnityCli.ps1`；现场 Editor 查询与编辑按官方 AI 调用指南使用 Pipeline/MCP 或官方 CLI。保留已有用户改动和 Unity `.meta`/GUID，参考项目默认只读。
- 用户提出事件中心时可立即进入该模块的需求和设计。Pooling 的历史待验收项如实保留；只处理实际影响当前依赖的部分，不自动扩展为完成整个 Pooling 模块。

流程背景见[工作流优化](Assets/Plugins/FrameWork_Ranger/Docs/00_Project/10_AI_Development_Workflow_Optimization.md)。当前功能与验证结果以模块文档、实际代码和本任务记录为准，个人 Skill 不维护模块进度副本。
