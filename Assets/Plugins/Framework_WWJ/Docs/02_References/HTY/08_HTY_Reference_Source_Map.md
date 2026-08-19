# HTY 参考源码索引

> 参考项目根目录：`D:\unityhub\UnityProjects\LyingBottle`<br>
> 参考定位：LyingBottle 是 HTY / ActFramework 的实际使用项目，不是另一个平级框架。<br>
> 默认规则：只读。除非用户明确改变范围，不修改 LyingBottle 的代码、资产、文档或 Skills。

## 1. 进入参考项目

在阅读 LyingBottle 代码前，先按顺序加载：

1. `AGENTS.md`
2. `.agents/skills/act-framework-dev/SKILL.md`
3. `.agents/skills/lyingbottle-dev/SKILL.md`
4. `.agents/skills/actframework-singleton/SKILL.md`

需要生命周期细节时，再读取：

- `.agents/skills/act-framework-dev/references-necessary/framework-lifecycle.md`
- `.agents/skills/lyingbottle-dev/references/architecture/overview.md`
- `.agents/skills/lyingbottle-dev/references/architecture/modules.md`
- `.agents/skills/lyingbottle-dev/references/cross-cutting/framework-runtime.md`

这些 Skill 和文档描述参考项目的约束，不自动成为 Framework_WWJ 的规则。

## 2. 核心入口与契约

路径均相对于 LyingBottle 根目录。

| 主题 | 源码 |
| --- | --- |
| 跨场景宿主 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/Global.cs` |
| 场景宿主 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/Main.cs` |
| 通用 Loader | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/MainLoaderBase.cs` |
| 全局 / 场景 Loader | `.../MainLoop/GlobalLoader.cs`、`.../MainLoop/MainLoader.cs` |
| 模块组合接口 | `.../MainLoop/Core/IModule.cs` |
| Handler 接口 | `.../MainLoop/Core/IModuleHandler.cs` |
| 宿主接口 | `.../MainLoop/Core/IModuleHost.cs` |
| 生命周期状态推进 | `.../MainLoop/Unit/ModuleLifecycleHelper.cs` |
| External SO 模块 | `.../MainLoop/Unit/ExternalModuleBase.cs` |
| Internal 场景模块 | `.../MainLoop/Unit/InternalModuleBase.cs` |
| Agent | `.../MainLoop/Agent/Agent.cs` |

`...` 在本表中代表 `Assets/Plugins/ActFramework_ByHZR/Packages/Main`。

## 3. 配置与装配

| 主题 | 文件或资产 |
| --- | --- |
| 模块配置基类 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/Core/ModuleCfg.cs` |
| 运行时配置组合 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/MainRuntimeCfg.cs` |
| 框架全局配置类型 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/GlobalCfg.cs` |
| 框架全局配置资产 | `Assets/Plugins/ActFramework_ByHZR/Resources/FrameworkModuleCfg/全局框架配置.asset` |
| 内嵌默认模块 | `Assets/Plugins/ActFramework_ByHZR/Resources/FrameworkModuleCfg/内嵌默认模块.asset` |
| LyingBottle 全局模块表 | `Assets/Cfg/GlobalCfg.asset` |
| 项目模块 SO | `Assets/Resources/SO/Modules/` |

分析配置问题时，应同时检查类型、资产引用和真实场景使用，不能只读 C# 声明。

## 4. 场景与运行时流程

| 主题 | 文件 |
| --- | --- |
| 场景加载器 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/HtyScene/SceneLoader.cs` |
| MainLoop 总览 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/README.md` |
| Main 使用方式 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/HowToUse.md` |
| 完整使用说明 | `Assets/Plugins/ActFramework_ByHZR/Packages/Main/MainLoop/Document/ActFramework使用说明书.md` |
| 框架体检 | `Assets/Plugins/ActFramework_ByHZR/FRAMEWORK_REVIEW.md` |

文档可能落后于代码。架构结论应优先以实际源码和 Unity 资产为准，再用文档解释设计意图。

## 5. 基础模块参考路线

### 事件

1. `Assets/Plugins/ActFramework_ByHZR/Packages/Main/Event/README.md`
2. `.../Packages/Main/Event/EventManager.cs`
3. `.../Packages/Main/Event/Handler/DefaultEventHelper.cs`
4. `.../Packages/Main/Event/Base/EventHandlerBase.cs`
5. `.../Packages/Main/Event/StaticEvent/EventHub.cs`

重点区分：`EventHub` 只服务框架启动阶段和模块系统尚未就绪时；业务事件使用 `EventManager` 模块。HTY 的事件对象继承 `EventHandlerBase`，可以配合 `ReferencePoolManager` 复用；这证明池化可行，但不自动证明 Framework_WWJ 的事件公共契约必须依赖整个池模块。

### 对象池

1. `Assets/Plugins/ActFramework_ByHZR/Packages/Main/Pool/README.md`
2. `.../Packages/Main/Pool/Base/Core/GeneralPool.cs`
3. `.../Packages/Main/Pool/Base/Core/ObjectPool.cs`
4. `.../Packages/Main/Pool/Base/Core/ReferencePool.cs`
5. `.../Packages/Main/Pool/ObjectPool/ObjectPoolManager.cs`
6. `.../Packages/Main/Pool/ReferencePool/ReferencePoolManager.cs`
7. `.../Packages/Main/Pool/ObjectPool/Handler/ObjectPoolHandler.cs`
8. `.../Packages/Main/Pool/ReferencePool/Handler/ReferencePoolHandler.cs`

重点观察通用池、GameObject 池和引用池的职责分层，以及 Manager + Handler + Config 的接线；同时核对 `FRAMEWORK_REVIEW.md` 记录的缓存释放风险。HTY 的对象池与引用池是不同 Manager，不应仅凭“Pool”目录就合并成一个公共接口。

### 音频

1. `Assets/Plugins/ActFramework_ByHZR/Packages/RuntimeModule/Audio/README.md`
2. `.../RuntimeModule/Audio/AudioManager.cs`
3. `.../RuntimeModule/Audio/AudioEventHandler.cs`
4. `.../RuntimeModule/Audio/SO_HtyAudioCfg.cs`

重点区分框架生命周期接入、配置资产、事件桥接和底层音频实现，不把 Wwise 或具体音频中间件视为核心依赖。

### 资源

1. `Assets/Plugins/ActFramework_ByHZR/Packages/Main/Resource/README.md`
2. `.../Packages/Main/Resource/ResourceManager.cs`
3. `.../Packages/Main/Resource/Handler/DefaultResourceHelper.cs`
4. `Assets/Plugins/ActFramework_ByHZR/Packages/RuntimeModule/Resources/README.md`

重点观察 Resources、AssetBundle、Addressables 和业务资源描述的边界。`IResourceHandler` 当前组合 LoaderConfig、AssetLoader、Unloader 与 BundleManager 四类接口，但实现仍很宽；参考项目并不存在一个天然适合轻量复制的单一资源 Handler。

## 6. 程序集与依赖

优先检查：

- `Assets/Plugins/ActFramework_ByHZR/Packages/Main/ActFramework.Main.asmdef`
- `Assets/Plugins/ActFramework_ByHZR/Packages/MainCore/ActFramework.MainCore.asmdef`
- `Assets/Plugins/ActFramework_ByHZR/Packages/CoreServices/ActFramework.CoreServices.asmdef`
- `Assets/Plugins/ActFramework_ByHZR/Packages/ActFramework.Packages.asmdef`

它们分别体现核心层、扩展核心层、服务层和总聚合层的依赖扩张。Framework_WWJ 后续设计程序集时，应从目标模块的最小依赖反推边界。

## 7. 建议阅读路线

| 任务 | 最小阅读集合 |
| --- | --- |
| 研究模块骨架 | `IModule`、`IModuleHost`、`MainLoaderBase`、`ModuleLifecycleHelper` |
| 研究全局/场景作用域 | `Global`、`Main`、两个 Loader、三个配置类型 |
| 研究配置复杂度 | `ModuleCfg`、`MainRuntimeCfg`、三个配置资产 |
| 研究对象池 | Pool README、三个 Pool Core 文件、框架体检对应章节 |
| 研究事件 | Event README、EventManager、DefaultEventHelper、EventHandlerBase、EventHub、ReferencePoolManager |
| 研究音频或资源 | 对应 README、Manager/配置、asmdef 和 LyingBottle 实际调用点 |

形成结论后，把摘要写入 Framework_WWJ Docs；不要依赖未来会话重新扫描整个 LyingBottle。
