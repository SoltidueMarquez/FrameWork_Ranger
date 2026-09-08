using System;
using System.Collections.Generic;
using FrameWork_Ranger.Pooling;
using FrameWork_Ranger.ResourceManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.Pooling.GameObjects
{
    /// <summary>
    /// 一个配置池的 Prefab Lease、隐藏根、实例回调缓存与严格所有权运行容器。
    /// </summary>
    [FrameworkArchitecture(
        "GameObject 池运行实例",
        "持有 Prefab Lease，并把实例姿态、激活、回调缓存、外部销毁诊断交给严格跟踪池。",
        FrameworkArchitectureLayer.RuntimeDriving,
        133,
        typeof(TrackedPool<>),
        typeof(ResourceLease<>))]
    internal sealed class GameObjectPoolRuntime
    {
        private sealed class InstanceState
        {
            internal Vector3 InitialLocalScale { get; }

            internal IGameObjectPoolCallbacks[] Callbacks { get; }

            internal InstanceState(Vector3 initialLocalScale, IGameObjectPoolCallbacks[] callbacks)
            {
                InitialLocalScale = initialLocalScale;
                Callbacks = callbacks;
            }
        }

        private readonly GameObjectPoolDefinition m_definition;
        private readonly ResourceLease<GameObject> m_prefabLease;
        private readonly GameObject m_prefab;
        private readonly Transform m_inactiveRoot;
        private readonly Dictionary<GameObject, InstanceState> m_instances;
        private readonly TrackedPool<GameObject> m_pool;

        internal string PoolName => m_definition.PoolName;

        internal ResourceKey ResourceKey => m_definition.ResourceKey;

        internal int BorrowedCount
        {
            get
            {
                m_pool.PruneInvalid();
                return m_pool.BorrowedCount;
            }
        }

        internal int InitialCount => m_definition.Capacity.InitialCount;

        internal int InactiveCount => m_pool.InactiveCount;

        internal GameObjectPoolRuntime(
            GameObjectPoolDefinition definition,
            ResourceLease<GameObject> prefabLease,
            Transform moduleRoot)
        {
            m_definition = definition ?? throw new ArgumentNullException(nameof(definition));
            m_prefabLease = prefabLease ?? throw new ArgumentNullException(nameof(prefabLease));
            m_prefab = prefabLease.Value;
            if (m_prefab == null)
            {
                throw new InvalidOperationException(
                    $"池 {definition.PoolName} 的资源 {definition.ResourceKey} 不是可用的 GameObject Prefab。" );
            }

            var root = new GameObject($"[{definition.PoolName}]");
            root.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            root.transform.SetParent(moduleRoot, false);
            root.SetActive(false);
            m_inactiveRoot = root.transform;
            m_instances = new Dictionary<GameObject, InstanceState>(
                PoolReferenceEqualityComparer<GameObject>.Instance);
            m_pool = new TrackedPool<GameObject>(
                definition.Capacity,
                CreateInstance,
                InvokeSpawned,
                ReturnToInactiveRoot,
                DestroyInstance,
                instance => instance != null,
                DiagnoseExternallyDestroyed);
        }

        internal bool PrewarmOne()
        {
            return m_pool.PrewarmOne();
        }

        internal GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent)
        {
            return m_pool.Rent(instance => PrepareForSpawn(instance, position, rotation, parent));
        }

        internal void Despawn(GameObject instance)
        {
            ValidateUsableInstance(instance);
            m_pool.Return(instance);
        }

        internal bool TryDespawn(GameObject instance)
        {
            if (!IsUsableBorrowedInstance(instance))
            {
                return false;
            }

            m_pool.Return(instance);
            return true;
        }

        internal void ClearInactive()
        {
            m_pool.ClearInactive();
        }

        internal void Tick(float deltaTime)
        {
            m_pool.TickIdleShrink(deltaTime);
        }

        internal void PruneExternallyDestroyed()
        {
            m_pool.PruneInvalid();
        }

        internal GameObjectPoolDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            m_pool.PruneInvalid();
            return new GameObjectPoolDiagnosticsSnapshot(
                PoolName,
                ResourceKey,
                m_pool.InactiveCount,
                m_pool.BorrowedCount,
                m_pool.TotalCount,
                m_pool.IdleSecondsRemaining);
        }

        internal void Shutdown()
        {
            var errors = new List<Exception>();
            try
            {
                m_pool.Shutdown();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            try
            {
                DestroyUnityObject(m_inactiveRoot == null ? null : m_inactiveRoot.gameObject);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            try
            {
                m_prefabLease.Release();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            m_instances.Clear();
            if (errors.Count > 0)
            {
                throw new AggregateException($"关闭 GameObject 池 {PoolName} 时发生异常。", errors);
            }
        }

        private GameObject CreateInstance()
        {
            var instance = Object.Instantiate(m_prefab, m_inactiveRoot, false);
            if (instance == null)
            {
                throw new InvalidOperationException($"池 {PoolName} 实例化 Prefab 返回 null。" );
            }

            instance.name = m_prefab.name;
            instance.SetActive(false);
            try
            {
                var callbacks = CollectCallbacks(instance);
                m_instances.Add(instance, new InstanceState(instance.transform.localScale, callbacks));
                return instance;
            }
            catch
            {
                DestroyUnityObject(instance);
                throw;
            }
        }

        private void PrepareForSpawn(
            GameObject instance,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
        {
            var state = GetState(instance);
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = state.InitialLocalScale;
            instance.SetActive(true);
        }

        private void InvokeSpawned(GameObject instance)
        {
            var callbacks = GetState(instance).Callbacks;
            for (var i = 0; i < callbacks.Length; i++)
            {
                callbacks[i].OnSpawned();
            }
        }

        private void ReturnToInactiveRoot(GameObject instance)
        {
            var state = GetState(instance);
            var callbacks = state.Callbacks;
            for (var i = callbacks.Length - 1; i >= 0; i--)
            {
                callbacks[i].OnDespawned();
            }

            instance.SetActive(false);
            instance.transform.SetParent(m_inactiveRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = state.InitialLocalScale;
        }

        private void DestroyInstance(GameObject instance)
        {
            m_instances.Remove(instance);
            if (instance != null)
            {
                instance.SetActive(false);
                DestroyUnityObject(instance);
            }
        }

        private void DiagnoseExternallyDestroyed(GameObject instance)
        {
            m_instances.Remove(instance);
            Debug.LogWarning(
                $"GameObject 池 {PoolName} 跟踪的实例被外部提前销毁；已从所有权状态剔除，不会重新缓存。" );
        }

        private void ValidateUsableInstance(GameObject instance)
        {
            if (ReferenceEquals(instance, null))
            {
                throw new ArgumentNullException(nameof(instance), "不能向 GameObject 池归还 null。" );
            }

            if (instance == null)
            {
                m_pool.PruneInvalid();
                throw new InvalidOperationException(
                    $"池 {PoolName} 的实例已被外部销毁，不能归还。" );
            }

            if (!m_pool.OwnsBorrowed(instance))
            {
                if (m_pool.OwnsInactive(instance))
                {
                    throw new InvalidOperationException(
                        $"实例已经归还池 {PoolName}，不能重复归还。" );
                }

                throw new InvalidOperationException(
                    $"实例不属于池 {PoolName} 或当前并非由该池借出。" );
            }
        }

        private bool IsUsableBorrowedInstance(GameObject instance)
        {
            if (ReferenceEquals(instance, null))
            {
                return false;
            }

            if (instance == null)
            {
                m_pool.PruneInvalid();
                return false;
            }

            return m_pool.OwnsBorrowed(instance);
        }

        private InstanceState GetState(GameObject instance)
        {
            if (!m_instances.TryGetValue(instance, out var state))
            {
                throw new InvalidOperationException(
                    $"池 {PoolName} 缺少实例回调缓存，所有权状态已损坏。" );
            }

            return state;
        }

        private static IGameObjectPoolCallbacks[] CollectCallbacks(GameObject instance)
        {
            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            var callbacks = new List<IGameObjectPoolCallbacks>(behaviours.Length);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IGameObjectPoolCallbacks callback)
                {
                    callbacks.Add(callback);
                }
            }

            return callbacks.ToArray();
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
