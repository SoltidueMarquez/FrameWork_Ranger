# 历史计划与设计演进

本文把 Git 历史、原始文档和 2026-08 的轻量化计划放到同一条时间线上。不同阶段的方案存在冲突，不能混为一个“最终设计”。

## 第一代：参考框架裁剪清单（2026-03-02）

Git 提交 `f63590f` 增加过《必要的脚本.md》，随后在 `d3e0798` 中删除。其核心目标是从 `ActFramework_ByHZR` 中抽取最小模块骨架：

- 保留 `IModule`、`IModuleHandler`、Loader、模块配置、排序器、场景入口和一个测试模块；
- 用普通 `Dictionary`、`System.Guid`、`Debug.Log` 替换参考框架工具依赖；
- 去掉 Agent、事件总线、SceneLoader、Global、Loading UI、版本、终端和大部分 Editor/Util；
- 配置层只需要提供 `List<ModuleItemCfg>`；
- 已经提出普通 MonoBehaviour 模块或 SO 模块两种候选，但没有作最终选择。

这是一次“先评估什么必须存在”的计划，方向正确，但仍以复刻参考框架 API 为起点。

## 第二代：Loader 复刻与演进（2026-03-04）

提交 `d3e0798` 新增当前的 `LoaderDesign.md`、`MainLoaderBase`、`IMainLoading` 和 `FastDictionary`。计划分三层：

1. 单场景最小 Loader：配置注册、排序、协程初始化、三种 Tick、增删查、Pause/Run；
2. 类型索引、Loading UI、简单调试；
3. SceneLoader、Global、动态配置和完整工具链。

这份计划明确反对一次性搬入参考框架的所有复杂度，但它把运行时动态增删、Loading 进度和多阶段生命周期继续定义成“第一阶段核心”，因此实际骨架仍然偏重。

原文保存在 [LoaderDesign.md](./LoaderDesign.md)。

## 第三代：跑通旧骨架并加入 Handler（2026-03-05 至 2026-03-25）

Git 历史显示：

| 日期 | 提交 | 结果 |
| --- | --- | --- |
| 2026-03-05 | `1919f1e` 部分重构 | 加入 `FrameworkEntry`、具体 `MainLoader`，扩充 Loader |
| 2026-03-11 | `2df9f03` 跑通了 | 修正配置/Loader，旧单场景生命周期闭环能够运行 |
| 2026-03-25 | `b6d270f` 对象池系统实现，等待测试 | 加入 Handler 基类、Pool 模块、SO 配置与编辑器工具 |
| 2026-03-25 | `b24566c` 技术文档支持 | 增加对象池技术文档 |

这一代验证了几个有价值的想法：

- 场景入口只负责接线和生命周期转发；
- 初始化按优先级、销毁逆序；
- 模块作为框架门面，Handler 承载可复用业务逻辑；
- 对象池低层可以独立于框架核心。

它也暴露了问题：核心和扩展边界继续膨胀、配置职责过多、模块序列化依赖 Odin、Editor 工具过早绑定具体模块，而且对象池没有形成可靠测试闭环。

## 第四代：SO 轻量化重构计划（2026-08）

之后形成的 `$framework-wwj-lightweight-refactor` 计划把目标改成小型 SO 模块框架：

- `ModuleSO : GeneralSO`；
- `ModuleHandlerBase` 提供默认空生命周期；
- `ModuleConfigSO` 只保存模块引用列表；
- 每个场景的 `FrameworkEntry` 同时引用 global config 与 scene config；
- `FrameworkRuntime` 合并模块，global 的重复 key 优先；
- 小优先级先初始化，逆序 Shutdown；
- 生命周期缩减为 `Init/Shutdown/Tick/FixedTick/LateTick`。

计划明确把以下内容排除出 V1：

- Born/Begin/End 多阶段生命周期；
- 运行时 Add/Remove；
- Loading UI 事件；
- 配置包递归；
- 静态配置生命周期和热配置；
- 异步模块初始化；
- 大范围 Pause/Run 状态。

原阶段设想是：

1. 替换核心骨架并做最小样例；
2. 迁移 Pool；
3. 迁移 Audio 与 Resource；
4. 精简 Editor 工具。

盘点证明该计划尚未落地：`Main/Module` 到 `Main/ModuleBase` 只是内容完全相同的目录移动。

## 第五代：本次全量重建（2026-08-06 起）

当前决定比第四代更彻底：先把旧代码与设计归档，清空 Framework_WWJ 自有的 `Main`、`Utils`，等参考项目到位后重新做需求分析和边界设计。

这意味着第四代 SO 方案也只是**重要候选**，而不是未经讨论就直接实现的最终答案。新设计需要回答：

- 模块是否一定是 ScriptableObject，还是 SO 只描述配置？
- 全局模块和场景模块的所有权、复用与切场景语义是什么？
- Handler 是所有模块的强制结构，还是复杂模块的可选组合？
- 异步初始化是否属于核心；若属于，取消、失败和进度如何建模？
- 游戏代码如何获取模块：显式上下文、类型注册表、接口查询，还是受控静态入口？
- 哪些模块真正服务于第一个游戏目标？

## 跨代保留与放弃

| 概念 | 建议 |
| --- | --- |
| 模块化作为核心 | 保留 |
| 场景入口转发 Unity 生命周期 | 保留为候选 |
| 小优先级先初始化、逆序退出 | 保留 |
| Module 门面 + Handler 逻辑 | 保留为可选模式 |
| Pool 的纯 C# 低层 | 只保留思想，重写前先补测试 |
| Born/Begin/End 生命周期 | 默认放弃 |
| Loader 运行时动态增删 | V1 放弃，真实需求出现后再设计 |
| 配置包递归、热配置占位 | 放弃 |
| 巨型 Utils | 放弃，按需求重建 |
| 中心化 Editor 工具 App | 放弃，模块需要时提供小工具 |
| 对参考框架 API 兼容 | 不作为目标 |

