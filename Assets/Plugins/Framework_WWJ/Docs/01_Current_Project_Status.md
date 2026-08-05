# Framework_WWJ 当前项目状态

> 盘点时间：2026-08-06  
> Unity 项目：`D:\unityhub\UnityProjects\Framework_Test`  
> 框架目录：`Assets/Plugins/Framework_WWJ`

## 1. 环境基线

| 项目 | 当前值 |
| --- | --- |
| Unity | 2022.3.62f3 |
| Git 分支 | `main`，跟踪 `origin/main` |
| 最新提交 | `b24566c`（2026-03-25，技术文档支持） |
| 主要命名空间 | `Plugins.Framework_WWJ` |
| 程序集状态 | Framework_WWJ 下没有独立 `.asmdef`，旧代码进入 Unity 默认程序集 |
| 显式 Unity Packages | Collaboration、Development、TMP、Timeline、UGUI、Visual Scripting 与 Unity 内置模块 |
| Asset 依赖 | 旧代码直接依赖 Sirenix Odin；项目中还存在 DOTween 设置资产 |

`Packages/manifest.json`、`packages-lock.json`、Unity 版本与部分 ProjectSettings 在本次整理前已经有用户修改。本次清理不触碰这些文件。

## 2. 本次整理前的工作区状态

工作区不是干净的，且这些变化早于本次文档整理：

- 已提交目录 `Main/Module` 被删除，同时出现未跟踪目录 `Main/ModuleBase`。
- 对两个目录逐文件进行换行归一化比较后，`ModuleBase` 中的 34 个文件与 Git `HEAD` 下原 `Module` 文件完全一致，目录 `.meta` GUID 也被保留。
- 因此这只是**旧核心目录搬迁**，不是轻量化核心重构。
- `Main/Audio` 和其中仅用于打印生命周期的 `AudioModule` 已被删除。
- 原 `Module SO` 下的 `MainRuntimeCfg.asset` 与“默认通用配置”资产已被删除。
- 原 `Assets/Resources/Cube.prefab` 已被删除；它曾是对象池配置的测试模板。
- `研究日志/LoaderDesign.md` 已原样移动到 `Docs/LoaderDesign.md`，文件自身 `.meta` 保持一致。
- `Assets/Scenes/SampleScene.unity` 仍引用旧 `Main.prefab`。若旧 Prefab 被移除，场景中的该实例需要在新骨架阶段重建。

## 3. 旧代码规模

整理前 `Main` 与 `Utils` 共 31 个 C# 文件、约 4,783 行：

| 区域 | 主要内容 | 状态判断 |
| --- | --- | --- |
| `Main/Base/SO` | `GeneralSO` 与 SO Creator 隐藏特性 | 很薄，但核心绑定 Odin |
| `Main/ModuleBase` | 入口、模块契约、Handler、配置、Loader、Prefab | 旧核心完整搬迁，未按新方案重构 |
| `Main/Pool` | 通用池、GameObject 池、Handler、Module、配置和技术文档 | 最完整的业务模块，但提交信息仍是“等待测试” |
| `Main/Resource` | 多加载渠道的资源路径数据结构 | 只有信息类型，没有资源管理模块 |
| `Main/EditorUtils` | SO Creator 与 Pool Manager 编辑器窗口 | 功能较重，且编辑器代码未放进 `Editor` 目录/独立编辑器程序集 |
| `Utils` | List/Dictionary 扩展和 `FastDictionary` | API 面很大，混入随机分配、反射式 List 操作等不同职责 |

## 4. 旧 Unity 资产接线

旧 `Main.prefab` 包含：

- `MainLoader` 组件；
- `FrameworkEntry` 组件；
- `FrameworkEntry.m_loader` 指向同对象上的 `MainLoader`；
- `FrameworkEntry.m_moduleCfg` 指向已经从工作区删除的 `MainRuntimeCfg.asset`。

被删除的“默认通用配置”曾内嵌两条模块配置：

| moduleKey | 模块 | autoRun | initPriority |
| --- | --- | --- | --- |
| `Audio` | `AudioModule` | `true` | `0` |
| `Pool` | `PoolModule` + `PoolHandler` | `true` | `0` |

现存 `ObjectPoolCfg.asset` 曾配置一个名为 `Cube` 的池：预热 5、单次扩容 1、缩减时间 30 秒，但它引用的 Cube Prefab 已被删除。

## 5. 重构状态结论

截至本次清理前，计划中的轻量核心**尚未开始实现**。仓库中不存在以下目标类型：

- `ModuleSO`
- `ModuleHandlerBase`
- `ModuleConfigSO`
- `FrameworkRuntime`
- 同时持有 `globalConfig` 与 `sceneConfig` 的新版 `FrameworkEntry`

现有代码仍使用 `Born/BeginInit/Init/EndInit/BeginUnInit/UnInit/EndUnInit/Die`、运行时增删模块、Loading 进度事件、配置包递归合并、静态/动态配置占位和 Pause/Run 状态。

因此，准确结论是：**做过目录整理和局部功能迭代，但没有进入计划中的轻量化重构。**

## 6. 验证状态

- 没有发现 Framework_WWJ 专属的 EditMode/PlayMode 测试或独立测试程序集。
- 对象池提交说明为“实现，等待测试”；不能把技术文档中的性能结论当成已验证指标。
- 本轮目标是归档与清理，不把旧实现重新修到可编译状态。

