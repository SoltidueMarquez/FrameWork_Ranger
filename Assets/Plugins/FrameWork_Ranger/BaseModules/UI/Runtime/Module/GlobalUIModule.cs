using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.ResourceManagement;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    /// <summary>唯一 UI 模块；内部管理两种生命周期，不安装 SceneUIModule。</summary>
    [CreateAssetMenu(fileName = "GlobalUIModule", menuName = "FrameWork_Ranger/Modules/UI")]
    [FrameworkArchitecture("UI 模块", "集中配置并管理全局/场景 UI、域绑定与输入。",
        FrameworkArchitectureLayer.PublicFacade, 120, typeof(UIContext), typeof(ResourceModule))]
    public sealed class GlobalUIModule : DirectModuleBase, ISceneScopeLifecycle, ISceneScopeReady, IModuleUpdate
    {
        public UIResourcesConfig UIResources;
        public UIConfiguration GlobalConfiguration = new UIConfiguration();
        public UIConfiguration SceneConfiguration = new UIConfiguration();
        [NonSerialized] private UIResourcesConfig m_resourcesSnapshot;
        private static readonly Type[] Dependencies = { typeof(ResourceModule) };
        [NonSerialized] private UIContext m_global;
        [NonSerialized] private UIContext m_scene;
        [NonSerialized] private GameObject m_root;
        [NonSerialized] private UIInputCoordinator m_input;
        [NonSerialized] private Func<ResourceKey, CancellationToken, UniTask<UIPrefabLease>> m_loader;
        [NonSerialized] private long m_sequence;
        public UIContext Global => m_global != null && m_global.IsAlive ? m_global :
            throw new InvalidOperationException("Global UI 尚未加载或已经卸载。");
        public UIContext Scene => TryGetScene(out var context) ? context :
            throw new InvalidOperationException("当前没有有效 Scene UI 所有者。");
        public FrameworkSceneScopeInfo SceneInfo { get; private set; }
        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
        public bool TryGetScene(out UIContext context)
        { context = m_scene; return context != null && context.IsAlive; }
        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Context.ScopeKind != ModuleScopeKind.Global)
                throw new InvalidOperationException("GlobalUIModule 只能安装在 GlobalScope。");
            var resources = Context.GetModule<ResourceModule>();
            Initialize(async (key, token) =>
            {
                var lease = await resources.AcquireAsync<GameObject>(key, token);
                return new UIPrefabLease(lease.Value, lease.Release);
            });
            return UniTask.CompletedTask;
        }
        internal void Initialize(Func<ResourceKey, CancellationToken, UniTask<UIPrefabLease>> loader)
        {
            var errors = ValidateConfiguration();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            m_loader = loader;
            m_root = new GameObject("Ranger UI");
            if (Application.isPlaying) DontDestroyOnLoad(m_root);
            m_input = new UIInputCoordinator();
            // 克隆目录与面板数据，运行状态不得写回资源 SO。
            m_resourcesSnapshot = UIResources ? Instantiate(UIResources) : null;
            if (m_resourcesSnapshot)
                foreach (var directory in new[] { m_resourcesSnapshot.Global, m_resourcesSnapshot.Scene })
                    foreach (var entry in directory.Panels)
                        if (entry?.Panel) entry.Panel = Instantiate(entry.Panel);
            m_global = new UIContext(this, UIOwner.Global,
                m_resourcesSnapshot ? m_resourcesSnapshot.Global.Snapshot(null, false) : GlobalConfiguration, m_root.transform);
        }
        /// <summary>Editor 与运行加载共用；Overlay 域使用明确且互不冲突的排序。</summary>
        public List<string> ValidateConfiguration()
        {
            if (UIResources) return UIResources.Validate();
            var errors = new List<string>();
            var overlayOrders = new Dictionary<int, string>();
            var configurations = new[] { GlobalConfiguration, SceneConfiguration };
            for (int i = 0; i < configurations.Length; i++)
            {
                string owner = i == 0 ? "Global" : "Scene";
                var config = configurations[i];
                if (config == null) { errors.Add(owner + ": 配置不能为空。"); continue; }
                foreach (var error in config.Validate()) errors.Add(owner + ": " + error);
                if (config.Domains == null) continue;
                foreach (var domain in config.Domains)
                {
                    if (domain == null || domain.RenderMode != RenderMode.ScreenSpaceOverlay) continue;
                    string name = owner + "/" + domain.Key;
                    if (overlayOrders.TryGetValue(domain.SortingOrder, out var previous))
                        errors.Add(name + " 与 " + previous + " 的 Overlay 排序冲突：" + domain.SortingOrder);
                    else overlayOrders.Add(domain.SortingOrder, name);
                }
            }
            return errors;
        }
        public UniTask OnSceneScopeStartingAsync(FrameworkSceneScopeInfo scope, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (m_scene != null) throw new InvalidOperationException("旧 Scene UI 尚未结束。");
            SceneInfo = scope;
            m_scene = new UIContext(this, UIOwner.Scene,
                m_resourcesSnapshot ? m_resourcesSnapshot.Scene.Snapshot(scope.ScenePath, true) : SceneConfiguration, m_root.transform);
            Pulse();
            return UniTask.CompletedTask;
        }
        public async UniTask OnSceneScopeReadyAsync(FrameworkSceneScopeInfo scope, CancellationToken token)
        {
            if (!ReferenceEquals(scope, SceneInfo) || !m_resourcesSnapshot) return;
            var context = m_scene;
            foreach (var entry in m_resourcesSnapshot.Scene.Panels)
            {
                token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(scope, SceneInfo) || !context.IsAlive) throw new OperationCanceledException();
                if (!entry.Enabled || !entry.Panel || !entry.AutoOpen || !entry.Allows(scope.ScenePath)) continue;
                string key = entry.Panel.Definition.Key;
                if (context.TryGet(key, out _)) continue;
                try { await context.OpenAsync(key, null, token); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    if (entry.Required) throw new InvalidOperationException("必须成功的场景 UI 打开失败：" + key, ex);
                    Debug.LogError("[Ranger UI] 自动打开失败（继续场景启动）：" + key + "\n" + ex);
                }
            }
        }
        public async UniTask OnSceneScopeEndingAsync(FrameworkSceneScopeInfo scope)
        {
            if (!ReferenceEquals(scope, SceneInfo)) return;
            var context = m_scene;
            SceneInfo = null;
            m_scene = null;
            try { Pulse(); if (context != null) await context.DisposeAsync(); }
            finally { Pulse(); }
        }
        protected override UniTask OnUnloadAsync() => ShutdownAsync();
        internal async UniTask ShutdownAsync()
        {
            var scene = m_scene; var global = m_global;
            m_scene = null; SceneInfo = null;
            var errors = new List<Exception>();
            try { if (scene != null) await scene.DisposeAsync(); } catch (Exception ex) { errors.Add(ex); }
            try { if (global != null) await global.DisposeAsync(); } catch (Exception ex) { errors.Add(ex); }
            m_global = null; DestroyOwned(m_root); m_root = null; m_loader = null;
            if (m_resourcesSnapshot)
            {
                foreach (var directory in new[] { m_resourcesSnapshot.Global, m_resourcesSnapshot.Scene })
                    foreach (var entry in directory.Panels) if (entry?.Panel) DestroyOwned(entry.Panel);
                DestroyOwned(m_resourcesSnapshot); m_resourcesSnapshot = null;
            }
            if (errors.Count > 0) throw new AggregateException("UI 模块已清理，但部分所有者释放失败。", errors);
        }
        internal UniTask<UIPrefabLease> LoadPrefabAsync(ResourceKey key, CancellationToken token) => m_loader(key, token);
        internal UniTask<UIPrefabLease> LoadPrefabAsync(UIPanelDefinition definition, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return definition.UseDirectPrefab
                ? UniTask.FromResult(new UIPrefabLease(definition.DirectPrefab, null))
                : LoadPrefabAsync(definition.ResourceKey, token);
        }
        internal long NextSequence() => ++m_sequence;
        internal void Pulse()
        {
            m_global?.Update();
            m_scene?.Update();
            m_input?.Update(m_global, m_scene);
        }
        public void OnModuleUpdate(float deltaTime) { Pulse(); }
        internal static void DestroyOwned(UnityEngine.Object target)
        {
            if (!target) return;
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
    }
}
