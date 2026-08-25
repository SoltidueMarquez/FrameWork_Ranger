### Framework_WWJ Loader 复刻与演进计划

#### 目标

- **核心目标**：基于参考框架 `ActFramework_ByHZR`，在 `Framework_WWJ` 中实现一个**轻量级、可运行的模块加载/管理骨架**，优先保证简单、稳定和易维护。
- **演进方向**：预留扩展点，后续逐步加入 SceneLoader/Global、动态配置、Loading UI 等高级特性，但不在第一阶段一次性全部实现。

#### 现状简要

- 当时的 `MainLoaderBase` 已基本按参考框架移植至 `Assets/Plugins/Framework_WWJ/Main/Module/Loader/MainLoaderBase.cs`（该旧代码已在重建前清理），但仍依赖：
  - `SceneLoader` 相关事件（场景卸载/覆盖）、
  - `Global.Instance.StartCoroutine`、
  - 一些调试/工具类（`ConsoleLogger`、`TerminalLogEventHandler`、`CommonConstant`、`GuidSpawner`、动态配置 `HotRuntimeData` 等）。
- 这些依赖增加了复杂度，使得“先跑通一个最小可行版框架”变得困难。

#### 功能分级（面向独立游戏/个人项目）

- **第一优先：必须有（先做好这些，再考虑别的）**
  - **模块基础契约与管理**：
    - `IModule` / `IModuleHandler` 的生命周期（Born/Init/UnInit/Die、Update/FixedUpdate/LateUpdate、Pause/Run）。
    - `MainLoaderBase` 的核心职责：
      - 持有 `List<IModule>` + 字典（`FastDictionary<string, IModule>` 或普通 `Dictionary`）。
      - 从 `ModuleCfg.modules` 与可选 `GetExtraModules()` 构建模块列表。
      - 使用 `ModuleComparer`（或简单排序规则）按优先级排序。
      - 协程驱动初始化流程（顺序 Init，支持进度和当前模块信息）。
      - 每帧派发 Update/FixedUpdate/LateUpdate。
      - 模块的 Pause/Run、增删查（`AddModule/RemoveModule/GetModule/GetModules`）。
  - **最小配置层**：
    - 一个可以提供 `List<ModuleItemCfg>` 的 `ModuleCfg` 子类（或最小版 `ModuleCfg`）。
    - `ModuleItemCfg` 至少包含：`moduleKey`、`IModule` 引用、开关 `on`。
  - **最小入口 MonoBehaviour**：
    - 一个简单入口脚本（例如 `FrameworkEntry`），挂在场景中：
      - 以字段持有 `MainLoaderBase` 实例和 `ModuleCfg` 引用。
      - 在 `Awake/Start` 中：设置 cfg、`Born()` + `Init()`（用自身的 `StartCoroutine`）。
      - 在 `Update/FixedUpdate/LateUpdate` 中调用 Loader 的对应方法。
      - 在 `OnDestroy` 时调用 `UnInit()` + `Die()`。
- **第二优先：很有用，但可以第二阶段再做**
  - **类型索引与泛型查询**：
    - `m_typeIndex` + `GetModule<T>`/`GetModules<T>`/`TryGetModule<T>`/`RemoveModule<T>` 等，便于按类型快速访问模块。
    - 对独立开发非常实用，但可以在最小骨架跑通后再加，或先只实现最常用的 `GetModule<T>()`。
  - **动态添加模块**：
    - 在运行时通过 `AddModule` 增加新模块，并在 `m_hasInit == true` 时自动执行 Init。
    - 适合需要 DLC/热插拔系统时使用，第一阶段可以保留接口但减少依赖。
  - **Loading 进度与 UI 对接接口**：
    - 保留 `IMainLoading` 事件与 `Progress`/`CurrentContent`，便于后续直接挂接 Loading UI。
    - UI 本身（如 `MainLoadingUI`）可以延后实现。
- **第三优先：可选高级特性（复杂度较高，可视项目需求慢慢加）**
  - **SceneLoader 集成（多场景生命周期管理）**：
    - 在 `Born()` 中根据当前 `module` 的场景名订阅 `SceneLoader` 的场景卸载/覆盖事件。
    - 在场景卸载开始/结束时自动触发 Loader/模块的 UnInit/Die。
    - 这是高级用法，对简单单场景/少切场景的独立项目可以后置，先用入口 Mono 的 OnDestroy 来做收尾即可。
  - **Global 单例与跨场景 Loader**：
    - `Global.Instance.StartCoroutine` 作为统一协程宿主，以及跨场景保留的模块体系。
    - 适合大型项目或需要全局模块（网络、全局音频、账号等）时再考虑。
  - **动态配置系统（HotRuntimeData、DynamicCfgInit/UnInit）**：
    - 完整的热更 `SO`/文本配置体系对工具链要求较高，初期可用普通 ScriptableObject/JSON/表格手动加载替代。
  - **调试工具与 GUI 支持**：
    - `IModuleGUISupport`、`TerminalLogEventHandler`、彩色日志、运行时 GUI 调试面板等。
    - 属于锦上添花型功能，可在框架稳定后按需迁移。

#### 第一阶段目标架构（轻量可运行骨架）

- **入口层**：
  - 一个简洁的 `FrameworkEntry`（或类似命名）脚本：
    - `[SerializeField] MainLoaderBase loader;`
    - `[SerializeField] ModuleCfg cfg;`
    - `Start` 里：`loader.module = this; loader.SetCfg(cfg); loader.Born(); loader.Init();`
    - `Update/FixedUpdate/LateUpdate` 里分别调用 `loader.Update()` 等。
    - `OnDestroy` 里：安全地调用 `loader.UnInit()` + `loader.Die()`。
- **Loader 层（MainLoaderBase 精简版）**：
  - 切分出一个“**纯管理核心**”子集，内部只依赖：
    - `IModule` / `IModuleHandler`、
    - `ModuleCfg`、`ModuleItemCfg`、
    - `FastDictionary`（或 `Dictionary`）、
    - Unity 自身类型（`MonoBehaviour`、`Coroutine`、`ScriptableObject`）。
  - 将以下内容标为“第二/第三阶段”：
    - `SceneLoader` 相关字段与事件绑定。
    - `Global.Instance.StartCoroutine`（改为由入口脚本托管协程）。
    - 动态配置访问（`GetDynamicSoCfg/GetDynamicTextCfg`）。
    - 高级调试类（`ConsoleLogger`、`TerminalLogEventHandler` 等），暂时用 `Debug.LogError` 代替或直接去掉日志。
- **配置层（最小实现）**：
  - 在当时的 `Assets/Plugins/Framework_WWJ/Main/Module/Config/ModuleCfg.cs`（该旧代码已清理）上，确保：
    - 有一个可以在 Inspector 中维护 `List<ModuleItemCfg>` 的实现。
    - `ModuleItemCfg` 至少具备：`string moduleKey; IModule module; bool on; int initPriority`（或类似）字段。
  - 静态/动态配置（`StaticCfgInit/DynamicCfgInit`）暂时可做空实现或仅保留最简单的版本。

#### 第二阶段及以后：有计划地引入高级特性

- **第二阶段：类型索引 + Loading UI + 简单调试**

  - 在不改动外部调用方式的前提下：
    - 完整实现 `m_typeIndex` 相关方法（Get/Remove/TryGet 等）。
    - 复用 `IMainLoading` 事件，设计自己的 Loading UI（不必完全照抄参考框架）。
    - 为关键路径加上简单日志（基于 `Debug.Log`），以后再替换为更完整的终端日志系统。

- **第三阶段：SceneLoader / Global / 动态配置体系**

  - 设计自己的 SceneLoader 或基于 Unity `SceneManager` 的适配层，然后：
    - 把当前 `MainLoaderBase` 中关于场景的逻辑封装到一个接口/辅助类中，减少耦合。
  - 需要跨场景保留的模块时，再引入 `Global` 风格的单例 Loader。
  - 当你有稳定的数据管线时，再考虑把 `DynamicCfgInit/HotRuntimeData` 整体迁移过来。

  


## Framework_WWJ Loader 复刻与演进设计

本文件用于约束 **Loader 层复刻的范围与节奏**，避免一次性搬运参考框架 `ActFramework_ByHZR` 的全部复杂度。

---

### 一、第一阶段范围（轻量可运行骨架）

第一阶段只追求“**在一个场景里，模块能按配置被加载、初始化、更新、卸载**”，不追求高级功能：

- **只包含：**
  - **入口 MonoBehaviour**：例如 `FrameworkEntry`，挂在场景中。
  - **精简版 `MainLoaderBase`**：
    - 持有 `List<IModule>` + 字典（可以先用 `FastDictionary<string, IModule>`，也可以用 `Dictionary<string, IModule>`）。
    - 从 `ModuleCfg.modules`（以及可选 `GetExtraModules()`）填充模块列表。
    - 按模块优先级排序（`ModuleComparer` 或等价逻辑）。
    - 通过协程顺序执行模块 `Init`，并提供简单的进度信息。
    - 每帧派发 `Update/FixedUpdate/LateUpdate`，支持 `Pause/Run`、`AddModule/RemoveModule/GetModule`。
  - **最小配置层**：
    - 一个 `ModuleCfg` 子类（或最简实现），能在 Inspector 中维护 `List<ModuleItemCfg>`。
    - `ModuleItemCfg` 至少有：`string moduleKey; IModule module; bool on; int initPriority`（或等价字段）。

- **明确第一阶段不做（全部延后）：**
  - 不接入 **`SceneLoader`**：
    - 不订阅场景卸载/覆盖事件。
    - 场景结束时只依赖入口 Mono 的 `OnDestroy` 做收尾。
  - 不依赖 **`Global.Instance.StartCoroutine`**：
    - 所有协程均由入口 Mono 自己的 `StartCoroutine` 驱动。
  - 不实现 **动态配置系统**：
    - `DynamicCfgInit/UnInit`、`HotRuntimeData`、`GetDynamicSoCfg`、`GetDynamicTextCfg` 等全部视为“未来高级特性”，第一阶段可以留空或不暴露。
  - 不引入 **Global 单例 Loader** 与跨场景模块。
  - 不引入复杂的日志/终端系统：
    - 例如 `ConsoleLogger`、`TerminalLogEventHandler`、`CommonConstant` 等，在需要时用 `Debug.Log*` 临时代替。
  - 不强制接入 **Loading UI**：
    - 保留 `IMainLoading` 的事件和进度接口即可，UI 可在第二阶段再实现。

> 核心约束：**只要一个场景 + 一个入口脚本 + 一个 Loader + 一个最小配置，就必须能正常跑通模块生命周期。其他全部可以等以后再加。**

---

### 二、功能分级（必须 / 可选 / 高级）

- **第一优先：必须实现（第一阶段内完成）**
  - **模块基础契约与管理**
    - `IModule` / `IModuleHandler` 生命周期：`Born/Init/UnInit/Die`，以及 `Update/FixedUpdate/LateUpdate`、`Pause/Run`。
    - `MainLoaderBase` 核心职责：
      - `List<IModule>` + `Dictionary`/`FastDictionary` 存储。
      - 从 `ModuleCfg.modules`（+ 可选 `GetExtraModules`）构建模块集合。
      - 使用 `ModuleComparer`（或内部排序逻辑）按优先级排序。
      - 协程顺序执行模块初始化流程（支持 `isLoading`、`Progress`、`CurrentContent`）。
      - 派发 `Update/FixedUpdate/LateUpdate`。
      - 提供 `AddModule/RemoveModule/GetModule`、`Pause/Run` 等基础管理 API。
  - **最小配置层**
    - 一个能在 Inspector 下配置 `List<ModuleItemCfg>` 的 `ModuleCfg` 实现。
    - `ModuleItemCfg`：`moduleKey`（字符串标识）、`module`（`IModule` 实例或 `ScriptableObject` 模板）、`on`（是否启用）、`initPriority`（初始化顺序）。
  - **最小入口 MonoBehaviour**
    - 场景中挂一个入口脚本（`FrameworkEntry` 或同类），负责：
      - 拥有 `MainLoaderBase` 实例与一个 `ModuleCfg` 引用。
      - 在 `Start` 里设置 `cfg`、调用 `Born()` + `Init()`。
      - 在 `Update/FixedUpdate/LateUpdate` 里转发调用 Loader。
      - 在 `OnDestroy` 里调用 `UnInit()` + `Die()`。

- **第二优先：很有用，但可在第二阶段逐步增加**
  - **类型索引与泛型查询**
    - `m_typeIndex`、`GetModule<T>()`、`GetModules<T>()`、`TryGetModule<T>()`、`RemoveModule<T>()`。
    - 让模块访问写法从“字符串 key”演进到“按类型直接拿”，更安全好用。
  - **运行时动态添加模块**
    - 基于 `AddModule` 在运行中新增模块，并在 `m_hasInit == true` 时自动完成 Init。
  - **Loading 进度与 UI 对接**
    - 保留 `IMainLoading` 三个事件 + `Progress`、`CurrentContent`。
    - 第二阶段中可设计自己的 Loading UI（不一定照搬参考框架的 `MainLoadingUI`）。

- **第三优先：高级特性（复杂度较高、视项目规模与需求渐进式引入）**
  - **SceneLoader 集成（多场景生命周期管理）**
    - 场景卸载/覆盖开始时自动触发 Loader/模块的 `UnInit`。
    - 场景结束后自动清理模块与配置，避免残留状态。
  - **Global 单例与跨场景 Loader**
    - 提供一个 `DontDestroyOnLoad` 的单例（如 `Global`），跨场景托管 Loader 与关键模块（如网络、音频、账号）。
  - **动态配置系统**
    - 基于 `DynamicCfgInit/UnInit` 和 `HotRuntimeData` 的热更配置体系（`SO` / 文本 / AssetBundle 等）。
  - **调试工具与运行时 GUI**
    - `IModuleGUISupport`、带颜色的终端日志、运行时调试窗口等。

---

### 三、入口脚本（FrameworkEntry）设计

**目标**：用一个尽量简单的入口 Mono，把场景生命周期与 `MainLoaderBase` 串起来，并在第一阶段完全替代 `Global.Instance.StartCoroutine` 和多场景逻辑。

- **建议字段设计（示意）**

```csharp
// 仅示意，不要求完全按此实现
public class FrameworkEntry : MonoBehaviour
{
    [SerializeField] private MainLoaderBase _loader;
    [SerializeField] private ModuleCfg _moduleCfg;

    private void Start()
    {
        // 1. 将自己作为 loader 的 module.owner（如果需要）
        _loader.module = _loader.module ?? _loader.module;

        // 2. 注入配置
        _loader.SetCfg(_moduleCfg);

        // 3. 生命周期：Born + Init（使用本脚本的 StartCoroutine，而不是 Global）
        _loader.Born();
        _loader.Init(); // 第一阶段可改成：内部接受一个协程宿主 MonoBehaviour
    }

    private void Update()        => _loader.Update();
    private void FixedUpdate()   => _loader.FixedUpdate();
    private void LateUpdate()    => _loader.LateUpdate();

    private void OnDestroy()
    {
        // 场景结束时的收尾逻辑
        _loader.UnInit();
        _loader.Die();
    }
}
```

- **设计要点**
  - **协程宿主**：所有初始化/卸载协程都由此入口脚本启动，避免对 `Global.Instance.StartCoroutine` 的依赖。
  - **职责单一**：入口脚本不做任何业务逻辑，仅负责：
    - 拿到 Loader 和配置。
    - 按固定顺序调用生命周期（`Born → Init → Update → UnInit → Die`）。
  - **未来扩展**：
    - 第二阶段开始，可额外支持：
      - 通过 Inspector 启用 Loading UI。
      - 暴露调试开关，打印模块加载耗时等。

---

### 四、MainLoaderBase 外部依赖与第一阶段处理策略

本节只做“**标注 + 策略**”，真正的代码重构会在实现阶段按需进行。

- **场景相关依赖（SceneLoader）**
  - 位置示例：`Born()` 中对 `SceneLoader.Instance` 的事件订阅，以及 `Die()` 中的事件注销。
  - 作用：感知场景卸载/覆盖，自动在场景结束时触发 Loader 的收尾逻辑。
  - **第一阶段策略：**
    - 不订阅 `SceneLoader` 事件。
    - 仅依赖入口 Mono 的 `OnDestroy` 来触发 `UnInit/Die`。
    - 保留虚方法 `OnSceneFinishStart/End` 等签名，但内部可以保持空实现，等引入 SceneLoader 再填充。

- **全局协程宿主（Global.Instance.StartCoroutine）**
  - 位置示例：`Init()` 中使用 `Global.Instance.StartCoroutine(InitLife())`；`UnInit()` 中用 `Global.Instance.StartCoroutine(UnInitCoroutine())`。
  - 作用：提供一个跨场景依旧存在的协程宿主。
  - **第一阶段策略：**
    - 将协程启动职责迁移到入口脚本（例如 `FrameworkEntry`），在入口中直接 `StartCoroutine(loader.InitLife())`。
    - `MainLoaderBase` 内部不再硬编码 `Global.Instance`，可以改造为接收一个 `MonoBehaviour` 协程宿主或外部注入的委托（在后续实现里处理）。

- **动态配置与热更数据（HotRuntimeData 等）**
  - 位置示例：`GetDynamicSoCfg/GetDynamicTextCfg` 等方法。
  - 作用：根据 key 获取热更 SO 或文本，常用于外部配置驱动游戏逻辑。
  - **第一阶段策略：**
    - 保留方法签名或直接注释整段实现，仅使用 `ModuleCfg.modules` 做静态配置。
    - 如需要简单配置，可通过普通 `ScriptableObject`、JSON 或表格手写加载。

- **日志与终端工具（ConsoleLogger、TerminalLogEventHandler、CommonConstant 等）**
  - 作用：更漂亮的日志输出与终端集成。
  - **第一阶段策略：**
    - 不必迁移整套 Terminal 系统。
    - 若必须打印错误/警告，用 `Debug.LogError/Debug.LogWarning` 简单替代。

- **其它工具依赖（GuidSpawner 等）**
  - 位置示例：`AddModule` 中给 key 为空的模块自动生成唯一 key。
  - **第一阶段策略：**
    - 用 `System.Guid.NewGuid().ToString()` 等最简替代方案即可。

> 总体原则：**第一阶段允许 MainLoaderBase 保留这些“挂钩点”的接口或虚方法，但实现要么是空的，要么是极简替代，不再强依赖参考框架的整套工具库。**

---

### 五、高级特性演进路线（SceneLoader / Global / 动态配置 / Loading UI）

当第一阶段的“轻量骨架”稳定并在项目中实际使用后，可以按以下顺序逐步引入高级特性：

- **阶段 2：易用性提升**
  - 完整启用 `m_typeIndex`，特别是：
    - `GetModule<T>` / `GetModules<T>` / `TryGetModule<T>` / `RemoveModule<T>`。
  - 结合 `IMainLoading` 设计自己的 Loading UI（可以是很简单的一张图 + 进度条）。
  - 在 Loader 关键流程中增加调试日志（初始化开始/结束、每个模块耗时等）。

- **阶段 3：场景与全局管理**
  - 设计一个轻量版 `SceneLoader`，或基于 `SceneManager` 写一个事件封装：
    - 提供“场景加载开始/结束”、“场景卸载开始/结束”等事件。
    - 在 `MainLoaderBase` 中重新接入这些事件，自动完成模块的 UnInit/Die。
  - 当项目需要跨场景共享模块时，引入 `Global` 风格单例：
    - 承载某些“全局 Loader” 与模块（例如网络、音乐、账号等）。

- **阶段 4：数据与工具链**
  - 若项目对数据驱动有强需求，再引入完整的动态配置体系：
    - 统一的 `DynamicCfgInit/UnInit` 生命周期。
    - `HotRuntimeData` 抽象和查询接口。
  - 视需要迁移参考框架中的终端系统和运行时 GUI 调试能力。

> 重要提示：**所有高级特性都建立在“第一阶段骨架稳定可用”的前提上。先让简单的东西为你服务，再逐步给它加“肌肉”。**

