using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.ResourceManagement
{
    /// <summary>
    /// ResourceModule 的运行编排器，负责双 Provider 初始化、路由表与 ResourceStore 生命周期。
    /// </summary>
    [FrameworkArchitecture(
        "资源模块 Handler",
        "串行初始化双 Provider、构建后端路由与 ResourceStore，并在卸载时完成聚合清理。",
        FrameworkArchitectureLayer.RuntimeDriving,
        180,
        typeof(ResourceModule),
        typeof(ResourceProviderBase),
        typeof(ResourceStore))]
    [Serializable]
    public sealed class ResourceHandler : ModuleHandlerBase
    {
        #region Inspector 配置

        [OdinSerialize]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        private List<ResourceProviderBase> m_providers = new List<ResourceProviderBase>();

        #endregion

        #region 运行状态

        [NonSerialized]
        private Dictionary<ResourceBackendKind, ResourceProviderBase> m_providerMap;

        [NonSerialized]
        private List<ResourceProviderBase> m_initializedProviders;

        [NonSerialized]
        private ResourceStore m_store;

        #endregion

        #region 生命周期

        protected override async UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            if (Context.ScopeKind != ModuleScopeKind.Global)
            {
                throw new InvalidOperationException("ResourceModule 只能安装在 GlobalScope。" );
            }

            m_providerMap = BuildProviderMap();
            m_initializedProviders = new List<ResourceProviderBase>(m_providerMap.Count);
            try
            {
                await InitializeProviderAsync(ResourceBackendKind.UnityResources, cancellationToken);
                await InitializeProviderAsync(ResourceBackendKind.Addressables, cancellationToken);
                m_store = new ResourceStore(m_providerMap);
            }
            catch (Exception initializationException)
            {
                var errors = new List<Exception> { initializationException };
                await ShutdownInitializedProvidersAsync(errors);
                m_providerMap = null;
                m_initializedProviders = null;
                throw errors.Count == 1
                    ? initializationException
                    : new AggregateException("ResourceModule Provider 初始化与回滚发生异常。", errors);
            }
        }

        protected override async UniTask OnUnloadAsync()
        {
            var errors = new List<Exception>();
            if (m_store != null)
            {
                try
                {
                    await m_store.ShutdownAsync();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            await ShutdownInitializedProvidersAsync(errors);
            m_store = null;
            m_providerMap = null;
            m_initializedProviders = null;
            if (errors.Count > 0)
            {
                throw new AggregateException("ResourceModule 关闭时发生异常。", errors);
            }
        }

        private Dictionary<ResourceBackendKind, ResourceProviderBase> BuildProviderMap()
        {
            var providers = new Dictionary<ResourceBackendKind, ResourceProviderBase>();
            for (var i = 0; i < m_providers.Count; i++)
            {
                var provider = m_providers[i];
                if (provider == null)
                {
                    throw new InvalidOperationException($"ResourceHandler Providers[{i}] 为空。" );
                }

                if (!Enum.IsDefined(typeof(ResourceBackendKind), provider.Backend))
                {
                    throw new InvalidOperationException(
                        $"Provider {provider.GetType().FullName} 声明了无效后端 {provider.Backend}。" );
                }

                if (providers.ContainsKey(provider.Backend))
                {
                    throw new InvalidOperationException(
                        $"ResourceHandler 重复配置了 {provider.Backend} Provider。" );
                }

                providers.Add(provider.Backend, provider);
            }

            RequireProvider(providers, ResourceBackendKind.UnityResources);
            RequireProvider(providers, ResourceBackendKind.Addressables);
            return providers;
        }

        private static void RequireProvider(
            IReadOnlyDictionary<ResourceBackendKind, ResourceProviderBase> providers,
            ResourceBackendKind backend)
        {
            if (!providers.ContainsKey(backend))
            {
                throw new InvalidOperationException($"ResourceHandler 缺少必须的 {backend} Provider。" );
            }
        }

        private async UniTask InitializeProviderAsync(
            ResourceBackendKind backend,
            CancellationToken cancellationToken)
        {
            var provider = m_providerMap[backend];
            await provider.InitializeAsync(cancellationToken);
            m_initializedProviders.Add(provider);
        }

        private async UniTask ShutdownInitializedProvidersAsync(ICollection<Exception> errors)
        {
            if (m_initializedProviders == null)
            {
                return;
            }

            for (var i = m_initializedProviders.Count - 1; i >= 0; i--)
            {
                try
                {
                    await m_initializedProviders[i].ShutdownAsync();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            m_initializedProviders.Clear();
        }

        #endregion

        #region Module 门面协作

        internal UniTask<ResourceLease<T>> AcquireAsync<T>(
            ResourceKey key,
            CancellationToken cancellationToken)
            where T : Object
        {
            if (m_store == null)
            {
                throw new InvalidOperationException("ResourceModule 尚未完成加载或已经关闭。" );
            }

            return m_store.AcquireAsync<T>(key, cancellationToken);
        }

        internal ResourceDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            return m_store?.CreateDiagnosticsSnapshot();
        }

        internal IReadOnlyList<ResourceProviderBase> ConfiguredProviders => m_providers;

        internal void SetProviders(IReadOnlyList<ResourceProviderBase> providers)
        {
            m_providers.Clear();
            if (providers == null)
            {
                return;
            }

            for (var i = 0; i < providers.Count; i++)
            {
                m_providers.Add(providers[i]);
            }
        }

        #endregion
    }
}
