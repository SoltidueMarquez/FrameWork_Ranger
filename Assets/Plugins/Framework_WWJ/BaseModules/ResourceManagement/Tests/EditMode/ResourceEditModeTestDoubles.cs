using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework_WWJ.ResourceManagement.Tests
{
    internal sealed class ResourceFakeProvider : ResourceProviderBase
    {
        private readonly ResourceBackendKind m_backend;

        internal Object Asset { get; set; }

        internal bool FailLoads { get; set; }

        internal bool IgnoreCancellation { get; set; }

        internal UniTaskCompletionSource<Object> LoadGate { get; set; }

        internal int LoadCount { get; private set; }

        internal int ReleaseCount { get; private set; }

        internal int InitializeCount { get; private set; }

        internal int ShutdownCount { get; private set; }

        internal ResourceFakeProvider(ResourceBackendKind backend)
        {
            m_backend = backend;
        }

        public override ResourceBackendKind Backend => m_backend;

        public override string ProviderName => $"Fake {m_backend}";

        protected override UniTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCount++;
            return UniTask.CompletedTask;
        }

        protected override async UniTask<IResourceBackendHandle<T>> OnLoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
        {
            LoadCount++;
            if (FailLoads)
            {
                throw new InvalidOperationException("Fake load failure");
            }

            var asset = Asset;
            if (LoadGate != null)
            {
                asset = IgnoreCancellation
                    ? await LoadGate.Task
                    : await LoadGate.Task.AttachExternalCancellation(cancellationToken);
            }

            return new ResourceFakeBackendHandle<T>(asset as T, () => ReleaseCount++);
        }

        protected override UniTask OnShutdownAsync()
        {
            ShutdownCount++;
            return UniTask.CompletedTask;
        }
    }

    internal sealed class ResourceFakeBackendHandle<T> : IResourceBackendHandle<T> where T : Object
    {
        private T m_value;
        private Action m_onRelease;

        public T Value => m_value;

        public bool IsValid => m_value != null;

        internal ResourceFakeBackendHandle(T value, Action onRelease)
        {
            m_value = value;
            m_onRelease = onRelease;
        }

        public void Dispose()
        {
            if (m_onRelease == null)
            {
                return;
            }

            m_value = null;
            var callback = m_onRelease;
            m_onRelease = null;
            callback();
        }
    }
}
