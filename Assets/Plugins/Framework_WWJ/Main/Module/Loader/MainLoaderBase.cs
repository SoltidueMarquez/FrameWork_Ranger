using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.Framework_WWJ.Utils;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    // ============================================================================
    // ModuleComparer：模块初始化顺序比较器
    // ============================================================================

    /// <summary>
    /// 按配置中的初始化优先级升序排序，用于 MainLoaderBase 决定模块初始化顺序。
    /// 数值越小越先初始化（例如：-100 比 0 先，0 比 100 先）。
    /// </summary>
    public class ModuleComparer : IComparer<MainLoaderBase.ModuleRuntimeItem>
    {
        public int Compare(MainLoaderBase.ModuleRuntimeItem x, MainLoaderBase.ModuleRuntimeItem y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;
            return x.initPriority.CompareTo(y.initPriority);
        }
    }
    
    // ============================================================================
    // MainLoaderBase：框架模块加载与生命周期调度核心
    // ============================================================================
    
    /// <summary>
    /// 【在框架中的作用】
    /// MainLoaderBase 是框架的“模块管理器”：负责从配置（ModuleCfg）中收集模块、按优先级顺序初始化、
    /// 每帧派发 Update/FixedUpdate/LateUpdate，以及在场景结束时反初始化、销毁。入口脚本（FrameworkEntry）
    /// 挂到场景后，会持有一个 MainLoaderBase 子类实例和一份 ModuleCfg，在 Start 里调用 Born → Init，
    /// 在 Update/FixedUpdate/LateUpdate 里转发给本类，在 OnDestroy 里调用 UnInit → Die。
    ///
    /// 【实现的功能】
    /// 1. 配置注入：通过 SetCfg 注入 ModuleCfg，Born 时根据 cfg.modules 注册所有启用模块。
    /// 2. 生命周期：Born（注册模块 + 静态配置初始化）→ Init（协程顺序初始化）→ 运行期每帧派发 → UnInit（逆序反初始化）→ Die（清理）。
    /// 3. 模块管理：AddModule/RemoveModule/GetModule/GetModules，支持运行时动态增删；已初始化后追加的模块会单独走一遍 Init。
    /// 4. 暂停/恢复：Pause/Run 同时设置内部暂停标志并通知所有模块。
    /// 5. 进度与事件：实现 IMainLoading，对外暴露 Progress、CurrentContent 以及 onStartInitLife/onEndInitLife/onUpdateInitLife，便于对接 Loading UI。
    ///
    /// 【不负责的内容（第一阶段）】
    /// 不接入 SceneLoader、Global 单例、动态配置（HotRuntimeData）、复杂日志；协程由入口或本组件所在 GameObject 托管。
    /// </summary>
    public abstract class MainLoaderBase : MonoBehaviour, IMainLoader, IMainLoading
    {
        public sealed class ModuleRuntimeItem
        {
            public string key;
            public IModule module;
            public int initPriority;
        }

        /// <summary>
        /// 可选：指向“持有本 Loader 的宿主”（如 FrameworkEntry），用于部分扩展逻辑。
        /// </summary>
        public IModule module { get; set; }

        /// <summary>
        /// 当前使用的模块配置，由 SetCfg 注入，Born 时据此注册模块并调用 StaticCfgInit。
        /// </summary>
        protected ModuleCfg m_cfg;
        /// <summary>
        /// 按顺序存放所有已注册模块运行项（含配置优先级），用于遍历、排序和 Init/UnInit 顺序派发。
        /// </summary>
        protected List<ModuleRuntimeItem> m_modules = new List<ModuleRuntimeItem>();
        /// <summary>
        /// 模块 key → 运行项，用于按字符串快速查找（GetModule(key)）。
        /// </summary>
        protected Dictionary<string, ModuleRuntimeItem> m_moduleDict = new Dictionary<string, ModuleRuntimeItem>();
        /// <summary>
        /// 是否已完成一轮 Init 协程；为 true 时，后续 AddModule 会对新模块单独执行 Init。
        /// </summary>
        protected bool m_hasInit;
        /// <summary>
        /// 全局暂停标志；为 true 时 Update/FixedUpdate/LateUpdate 不再派发。
        /// </summary>
        protected bool m_isPaused;
        /// <summary>
        /// 协程宿主：Init 协程、以及运行时 AddModule 后的单模块 Init 协程，都由此 MonoBehaviour 启动。
        /// </summary>
        protected MonoBehaviour m_coroutineHost;
        protected static readonly ModuleComparer s_moduleComparer = new ModuleComparer();

        #region ---------- IMainLoading 接口：供 Loading UI 或外部查询进度 ----------
        private float m_Progress;
        private string m_CurrentContent;
        public event Action onStartInitLife;
        public event Action onEndInitLife;
        public event Action onUpdateInitLife;
        public float Progress => m_Progress;
        public string CurrentContent => m_CurrentContent;
        #endregion

        /// <summary>
        /// 注入模块配置。应在 Born() 之前由入口脚本调用（如 FrameworkEntry.Start 里 SetCfg(_moduleCfg)）。
        /// </summary>
        public void SetCfg(ModuleCfg cfg)
        {
            m_cfg = cfg;
        }

        /// <summary>
        /// 设置协程宿主。Init 时若传入的 host 非空会同时记在这里；也可提前调用以指定用哪个 MonoBehaviour 跑 Init 协程。
        /// </summary>
        public void SetCoroutineHost(MonoBehaviour host)
        {
            m_coroutineHost = host;
        }

        /// <summary>
        /// 【生命周期·诞生】清空当前模块列表与字典，根据 m_cfg.modules 中 on==true 的项重新注册模块，并调用配置的 StaticCfgInit。
        /// 在 Init 之前由入口脚本调用（通常 FrameworkEntry.Start 里先 SetCfg 再 Born 再 Init）。
        /// </summary>
        public virtual void Born()
        {
            m_modules.Clear();
            m_moduleDict.Clear();
            m_hasInit = false;

            if (m_cfg?.modules != null && !m_cfg.modules.IsEmpty())
            {
                foreach (var item in m_cfg.modules)
                {
                    if (!item.on || item.module == null) continue;
                    string key = !string.IsNullOrEmpty(item.moduleKey) ? item.moduleKey : item.module.GetType().Name;
                    if (string.IsNullOrEmpty(key))
                        key = System.Guid.NewGuid().ToString();
                    AddModule(key, item.module, item.initPriority);
                }
            }

            m_cfg?.StaticCfgInit();
        }

        #region 【生命周期·初始化】

        /// <summary>
        /// 【生命周期·初始化】启动模块初始化协程。由入口脚本在 Born() 之后调用（如 FrameworkEntry.Start 里 Init(this)）。
        /// 会使用传入的 host 作为协程宿主（若为 null 则用本组件所在 GameObject），并启动 InitCoroutine()，
        /// 按优先级顺序对每个模块执行 BeginInit → Init → EndInit，同时更新 Progress/CurrentContent 并触发 IMainLoading 事件。
        /// </summary>
        /// <param name="host"> 用于 StartCoroutine 的 MonoBehaviour，通常为 FrameworkEntry 自身。 </param>
        public void Init(MonoBehaviour host)
        {
            m_coroutineHost = host != null ? host : this;
            m_coroutineHost.StartCoroutine(InitCoroutine());
        }

        /// <summary>
        /// 初始化协程：触发 onStartInitLife → 按 initPriority 排序 → 依次对每个模块执行 BeginInit/Init/EndInit，
        /// 每步更新 Progress、CurrentContent 并触发 onUpdateInitLife → 最后 m_hasInit=true 并触发 onEndInitLife。
        /// 无模块时直接标记完成并触发 onEndInitLife。
        /// </summary>
        public IEnumerator InitCoroutine()
        {
            onStartInitLife?.Invoke();
            m_Progress = 0f;
            m_CurrentContent = null;

            if (m_modules.IsEmpty())
            {
                m_hasInit = true;
                m_Progress = 1f;
                onEndInitLife?.Invoke();
                yield break;
            }

            m_modules.Sort(s_moduleComparer);
            int count = m_modules.Count;
            for (int i = 0; i < count; i++)
            {
                var runtimeItem = m_modules[i];
                var m = runtimeItem?.module;
                m_Progress = (float)i / count;
                m_CurrentContent = m?.GetType().Name ?? "?";
                onUpdateInitLife?.Invoke();

                try
                {
                    m?.BeginInit();
                    m?.Init();
                    m?.EndInit();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainLoaderBase] Module init error: {m?.GetType().Name}, {ex.Message}");
                }

                yield return null;
            }

            m_hasInit = true;
            m_Progress = 1f;
            m_CurrentContent = null;
            onEndInitLife?.Invoke();
        }

        #endregion
        

        /// <summary>
        /// 【生命周期·反初始化】逆序对当前所有模块执行 BeginUnInit → UnInit → EndUnInit。
        /// 由入口脚本在 OnDestroy 中、Die() 之前调用（如 FrameworkEntry.OnDestroy 里先 UnInit 再 Die）。
        /// </summary>
        public virtual void UnInit()
        {
            if (m_modules.IsEmpty()) return;
            for (int i = m_modules.Count - 1; i >= 0; i--)
            {
                var m = m_modules[i]?.module;
                try
                {
                    m?.BeginUnInit();
                    m?.UnInit();
                    m?.EndUnInit();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainLoaderBase] Module UnInit error: {m?.GetType().Name}, {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 【生命周期·销毁】调用配置的 StaticCfgUnInit，清空模块列表与字典，重置 m_hasInit。
        /// 由入口脚本在 OnDestroy 中、UnInit() 之后调用。
        /// </summary>
        public virtual void Die()
        {
            // 先让所有模块各自完成 Die 生命周期，再卸载配置与清空列表。
            if (!m_modules.IsEmpty())
            {
                for (int i = m_modules.Count - 1; i >= 0; i--)
                {
                    var module = m_modules[i]?.module;
                    if (module == null) continue;
                    try
                    {
                        module.Die();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MainLoaderBase] Module Die error: {module.GetType().Name}, {ex.Message}");
                    }
                }
            }

            m_cfg?.StaticCfgUnInit();
            m_modules.Clear();
            m_moduleDict.Clear();
            m_hasInit = false;
        }

        /// <summary>
        /// 每帧由入口脚本调用（如 FrameworkEntry.Update）。若未暂停，则遍历所有 isRunning 的模块并调用其 UpdateHandle()。
        /// </summary>
        public void LoaderUpdate()
        {
            if (m_isPaused) return;
            for (int i = 0; i < m_modules.Count; i++)
            {
                var m = m_modules[i]?.module;
                if (m is { isRunning: true }) m.UpdateHandle();
            }
        }

        /// <summary>
        /// 每帧由入口脚本在 FixedUpdate 中调用。若未暂停，则遍历所有 isRunning 的模块并调用其 FixedUpdateHandle()。
        /// </summary>
        public void LoaderFixedUpdate()
        {
            if (m_isPaused) return;
            for (int i = 0; i < m_modules.Count; i++)
            {
                var m = m_modules[i]?.module;
                if (m is { isRunning: true }) m.FixedUpdateHandle();
            }
        }

        /// <summary>
        /// 每帧由入口脚本在 LateUpdate 中调用。若未暂停，则遍历所有 isRunning 的模块并调用其 LateUpdateHandle()。
        /// </summary>
        public void LoaderLateUpdate()
        {
            if (m_isPaused) return;
            for (int i = 0; i < m_modules.Count; i++)
            {
                var m = m_modules[i]?.module;
                if (m is { isRunning: true }) m.LateUpdateHandle();
            }
        }

        /// <summary>
        /// 暂停：设置 m_isPaused=true，并通知所有模块 Pause()。此后 Update/FixedUpdate/LateUpdate 不再派发。
        /// </summary>
        public void Pause()
        {
            m_isPaused = true;
            for (int i = 0; i < m_modules.Count; i++) m_modules[i]?.module?.Pause();
        }

        /// <summary>
        /// 恢复：设置 m_isPaused=false，并通知所有模块 Run()。
        /// </summary>
        public void Run()
        {
            m_isPaused = false;
            for (int i = 0; i < m_modules.Count; i++) m_modules[i]?.module?.Run();
        }

        /// <summary>
        /// 注册一个模块。key 为空时自动生成 Guid；若 key 已存在则先移除旧模块再添加。
        /// 若此时已完成初始化（m_hasInit），会对新模块单独启动协程执行 BeginInit → Init → EndInit。
        /// </summary>
        public void AddModule(string key, IModule module, int initPriority = 0)
        {
            if (module == null) return;
            if (string.IsNullOrEmpty(key))
                key = System.Guid.NewGuid().ToString();
            if (m_moduleDict.ContainsKey(key))
            {
                Debug.LogWarning($"[MainLoaderBase] 添加模块: key 已经存在 '{key}', replacing.");
                RemoveModule(key);
            }

            // 先执行模块的 Born 生命周期，再注册到列表中。
            // 与参考框架保持一致：实例被加入系统时立即完成一次性的构建/接线工作。
            try
            {
                module.Born();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainLoaderBase] Module Born error: {module.GetType().Name}, {ex.Message}");
                return;
            }

            var runtimeItem = new ModuleRuntimeItem
            {
                key = key,
                module = module,
                initPriority = initPriority
            };
            m_moduleDict[key] = runtimeItem;
            m_modules.Add(runtimeItem);
            if (m_hasInit && m_coroutineHost != null)
                m_coroutineHost.StartCoroutine(InitSingleModule(module));
        }

        /// <summary>
        /// 对“运行时才加入”的单个模块执行一遍初始化流程（BeginInit → Init → EndInit），由 AddModule 在 m_hasInit 为 true 时启动。
        /// </summary>
        private IEnumerator InitSingleModule(IModule m)
        {
            try
            {
                m?.BeginInit();
                m?.Init();
                m?.EndInit();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainLoaderBase] Late AddModule init error: {m?.GetType().Name}, {ex.Message}");
            }
            yield return null;
        }

        /// <summary>
        /// 按 key 移除模块（从字典和列表中移除），返回是否找到并移除。
        /// </summary>
        public bool RemoveModule(string key)
        {
            if (!m_moduleDict.TryGetValue(key, out var runtimeItem)) return false;

            // 在真正移除前，先触发该模块的 Die 生命周期。
            var module = runtimeItem?.module;
            if (module != null)
            {
                try
                {
                    module.Die();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainLoaderBase] RemoveModule Die error: {module.GetType().Name}, {ex.Message}");
                }
            }

            m_moduleDict.Remove(key);
            int idx = m_modules.IndexOf(runtimeItem);
            if (idx >= 0) m_modules.RemoveAt(idx);
            return true;
        }

        /// <summary>
        /// 按 key 获取模块；不存在则返回 null。
        /// </summary>
        public IModule GetModule(string key)
        {
            return m_moduleDict.TryGetValue(key, out var item) ? item.module : null;
        }

        /// <summary>
        /// 返回当前所有模块的只读列表（与初始化/更新顺序一致）。
        /// </summary>
        public IReadOnlyList<IModule> GetModules()
        {
            var modules = new List<IModule>(m_modules.Count);
            for (int i = 0; i < m_modules.Count; i++)
            {
                modules.Add(m_modules[i]?.module);
            }
            return modules;
        }

        // ---------- IMainLoader 继承 IModuleHandler / IModuleHandlerLateUpdateSupport ----------
        // MainLoader 自身作为“根”不执行业务逻辑，仅满足接口；这些方法由框架在需要时调用，此处空实现即可。
        void IModuleHandler.Born() { }
        void IModuleHandler.Die() { }
        void IModuleHandler.Init() { }
        void IModuleHandler.UnInit() { }
        void IModuleHandler.Update() { }
        void IModuleHandler.FixedUpdate() { }
        void IModuleHandler.Pause() { }
        void IModuleHandler.Run() { }
        void IModuleHandlerLateUpdateSupport.LateUpdate() { }
    }
}
