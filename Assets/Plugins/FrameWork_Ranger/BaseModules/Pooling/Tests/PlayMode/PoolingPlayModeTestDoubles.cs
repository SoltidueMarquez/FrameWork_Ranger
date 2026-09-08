using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using FrameWork_Ranger.ResourceManagement;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.Pooling.Tests
{
    [Serializable]
    internal sealed class PoolingTestResourceProvider : ResourceProviderBase
    {
        [OdinSerialize]
        private ResourceBackendKind m_backend;

        [OdinSerialize]
        private GameObject m_prefab;

        [OdinSerialize]
        private bool m_failLoad;

        internal static int ReleaseCount { get; private set; }

        public override ResourceBackendKind Backend => m_backend;

        public override string ProviderName => $"Pooling Test {m_backend}";

        internal PoolingTestResourceProvider()
        {
        }

        internal PoolingTestResourceProvider(
            ResourceBackendKind backend,
            GameObject prefab,
            bool failLoad = false)
        {
            m_backend = backend;
            m_prefab = prefab;
            m_failLoad = failLoad;
        }

        protected override UniTask<IResourceBackendHandle<T>> OnLoadAsync<T>(
            string location,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (m_failLoad)
            {
                throw new InvalidOperationException("Pooling test resource load failed");
            }

            return UniTask.FromResult<IResourceBackendHandle<T>>(
                new PoolingTestResourceHandle<T>(m_prefab as T));
        }

        internal static void Reset()
        {
            ReleaseCount = 0;
        }

        internal static void RecordRelease()
        {
            ReleaseCount++;
        }
    }

    internal sealed class PoolingTestResourceHandle<T> : IResourceBackendHandle<T>
        where T : Object
    {
        private T m_value;

        internal PoolingTestResourceHandle(T value)
        {
            m_value = value;
        }

        public T Value => m_value;

        public bool IsValid => m_value != null;

        public void Dispose()
        {
            if (ReferenceEquals(m_value, null))
            {
                return;
            }

            m_value = null;
            PoolingTestResourceProvider.RecordRelease();
        }
    }

    internal sealed class PoolingCallbackProbe : MonoBehaviour, IGameObjectPoolCallbacks
    {
        private static readonly List<string> Events = new List<string>();

        [SerializeField]
        private string m_label;

        internal static IReadOnlyList<string> RecordedEvents => Events;

        internal string Label
        {
            set => m_label = value;
        }

        public void OnSpawned()
        {
            Events.Add($"{m_label}.spawn");
        }

        public void OnDespawned()
        {
            Events.Add($"{m_label}.despawn");
        }

        internal static void ResetEvents()
        {
            Events.Clear();
        }
    }

    internal sealed class PoolingReferencePayload : IReferencePoolItem
    {
        internal int RentCount { get; private set; }

        internal int ReturnCount { get; private set; }

        public PoolingReferencePayload()
        {
        }

        public void OnRent()
        {
            RentCount++;
        }

        public void OnReturn()
        {
            ReturnCount++;
        }
    }
}
