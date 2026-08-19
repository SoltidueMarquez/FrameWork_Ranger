using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement.Tests
{
    [Serializable]
    internal sealed class ResourcePlayModeProvider : ResourceProviderBase
    {
        internal static int ResourcesInitializeCount { get; private set; }

        internal static int ResourcesShutdownCount { get; private set; }

        internal static int AddressablesInitializeCount { get; private set; }

        internal static int AddressablesShutdownCount { get; private set; }

        [OdinSerialize]
        private ResourceBackendKind m_backend;

        [OdinSerialize]
        private Object m_asset;

        [OdinSerialize]
        private bool m_failInitialization;

        [NonSerialized]
        internal int InitializeCount;

        [NonSerialized]
        internal int ShutdownCount;

        [NonSerialized]
        internal int ReleaseCount;

        public override ResourceBackendKind Backend => m_backend;

        public override string ProviderName => $"PlayMode {m_backend}";

        internal ResourcePlayModeProvider()
        {
        }

        internal ResourcePlayModeProvider(ResourceBackendKind backend, Object asset)
        {
            m_backend = backend;
            m_asset = asset;
        }

        internal bool FailInitialization
        {
            set => m_failInitialization = value;
        }

        protected override UniTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCount++;
            if (m_backend == ResourceBackendKind.UnityResources)
            {
                ResourcesInitializeCount++;
            }
            else
            {
                AddressablesInitializeCount++;
            }

            if (m_failInitialization)
            {
                throw new InvalidOperationException("PlayMode provider initialization failed");
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask<IResourceBackendHandle<T>> OnLoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult<IResourceBackendHandle<T>>(
                new ResourcePlayModeHandle<T>(m_asset as T, () => ReleaseCount++));
        }

        protected override UniTask OnShutdownAsync()
        {
            ShutdownCount++;
            if (m_backend == ResourceBackendKind.UnityResources)
            {
                ResourcesShutdownCount++;
            }
            else
            {
                AddressablesShutdownCount++;
            }

            return UniTask.CompletedTask;
        }

        internal static void ResetLifecycleCounts()
        {
            ResourcesInitializeCount = 0;
            ResourcesShutdownCount = 0;
            AddressablesInitializeCount = 0;
            AddressablesShutdownCount = 0;
        }
    }

    internal sealed class ResourcePlayModeHandle<T> : IResourceBackendHandle<T> where T : Object
    {
        private T m_value;
        private Action m_release;

        public T Value => m_value;

        public bool IsValid => m_value != null;

        internal ResourcePlayModeHandle(T value, Action release)
        {
            m_value = value;
            m_release = release;
        }

        public void Dispose()
        {
            if (m_release == null)
            {
                return;
            }

            m_value = null;
            var release = m_release;
            m_release = null;
            release();
        }
    }
}
