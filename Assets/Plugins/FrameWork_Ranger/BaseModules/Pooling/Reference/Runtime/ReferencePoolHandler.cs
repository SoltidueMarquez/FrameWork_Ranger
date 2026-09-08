using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Pooling;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Reference
{
    /// <summary>
    /// ReferencePoolModule 的 GlobalScope 运行编排器。
    /// </summary>
    [Serializable]
    [FrameworkArchitecture(
        "引用池 Handler",
        "校验类型覆盖、惰性建立精确类型池并驱动主线程循环缩容与泄漏清理。",
        FrameworkArchitectureLayer.RuntimeDriving,
        124,
        typeof(ReferencePoolModule),
        typeof(ReferencePoolRuntime))]
    public sealed class ReferencePoolHandler : ModuleHandlerBase, IModuleUpdate
    {
        [OdinSerialize]
        private PoolCapacitySettings m_defaultCapacity = PoolCapacitySettings.CreateReferenceDefaults();

        [OdinSerialize]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        private List<ReferencePoolTypeOverride> m_typeOverrides = new List<ReferencePoolTypeOverride>();

        [NonSerialized]
        private Dictionary<Type, ReferencePoolRuntime> m_pools;

        [NonSerialized]
        private List<ReferencePoolRuntime> m_creationOrder;

        [NonSerialized]
        private int m_mainThreadId;

        [NonSerialized]
        private bool m_acceptingRequests;

        internal PoolCapacitySettings DefaultCapacity => m_defaultCapacity;

        internal IReadOnlyList<ReferencePoolTypeOverride> TypeOverrides => m_typeOverrides;

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            if (Context.ScopeKind != ModuleScopeKind.Global)
            {
                throw new InvalidOperationException("ReferencePoolModule 只能安装在 GlobalScope。" );
            }

            m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
            cancellationToken.ThrowIfCancellationRequested();
            var validated = ValidateConfiguration();
            m_pools = new Dictionary<Type, ReferencePoolRuntime>(validated.Count);
            m_creationOrder = new List<ReferencePoolRuntime>(validated.Count);
            try
            {
                for (var i = 0; i < validated.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var entry = validated[i];
                    var runtime = new ReferencePoolRuntime(entry.ItemType, entry.Capacity);
                    m_pools.Add(entry.ItemType, runtime);
                    m_creationOrder.Add(runtime);
                    runtime.PrewarmInitial();
                }

                cancellationToken.ThrowIfCancellationRequested();
                m_acceptingRequests = true;
                return UniTask.CompletedTask;
            }
            catch (Exception loadException)
            {
                var errors = new List<Exception> { loadException };
                ShutdownPools(errors);
                ResetRuntimeState();
                throw errors.Count == 1
                    ? loadException
                    : new AggregateException("ReferencePoolModule 加载失败，回滚时又发生异常。", errors);
            }
        }

        protected override UniTask OnUnloadAsync()
        {
            AssertMainThread();
            m_acceptingRequests = false;
            var errors = new List<Exception>();
            if (m_creationOrder != null)
            {
                for (var i = 0; i < m_creationOrder.Count; i++)
                {
                    var runtime = m_creationOrder[i];
                    if (runtime.BorrowedCount > 0)
                    {
                        Debug.LogWarning(
                            $"ReferencePoolModule 卸载时类型 {runtime.ItemType.FullName} 仍有 " +
                            $"{runtime.BorrowedCount} 个对象未归还；将执行 OnReturn 并强制清理。" );
                    }
                }
            }

            ShutdownPools(errors);
            ResetRuntimeState();
            if (errors.Count > 0)
            {
                throw new AggregateException("ReferencePoolModule 关闭时发生清理异常。", errors);
            }

            return UniTask.CompletedTask;
        }

        public void OnModuleUpdate(float deltaTime)
        {
            AssertReadyAndMainThread();
            for (var i = 0; i < m_creationOrder.Count; i++)
            {
                m_creationOrder[i].Tick(deltaTime);
            }
        }

        internal T Rent<T>() where T : class, IReferencePoolItem, new()
        {
            AssertReadyAndMainThread();
            var type = typeof(T);
            if (!m_pools.TryGetValue(type, out var runtime))
            {
                runtime = new ReferencePoolRuntime(type, m_defaultCapacity);
                try
                {
                    runtime.PrewarmInitial();
                }
                catch (Exception creationException)
                {
                    try
                    {
                        runtime.Shutdown();
                    }
                    catch (Exception cleanupException)
                    {
                        throw new AggregateException(
                            $"惰性建立引用池 {type.FullName} 失败，回滚时又发生异常。",
                            creationException,
                            cleanupException);
                    }

                    throw;
                }

                m_pools.Add(type, runtime);
                m_creationOrder.Add(runtime);
            }

            return runtime.Rent<T>();
        }

        internal void Return(IReferencePoolItem item)
        {
            AssertReadyAndMainThread();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "不能向引用池归还 null。" );
            }

            var type = item.GetType();
            if (!m_pools.TryGetValue(type, out var runtime))
            {
                throw new InvalidOperationException(
                    $"对象类型 {type.FullName} 没有已建立的引用池，不能归还外来对象。" );
            }

            runtime.Return(item);
        }

        internal bool TryReturn(IReferencePoolItem item)
        {
            AssertReadyAndMainThread();
            if (item == null || !m_pools.TryGetValue(item.GetType(), out var runtime))
            {
                return false;
            }

            return runtime.TryReturn(item);
        }

        internal void ClearInactive<T>() where T : class, IReferencePoolItem
        {
            AssertReadyAndMainThread();
            if (m_pools.TryGetValue(typeof(T), out var runtime))
            {
                runtime.ClearInactive();
            }
        }

        internal IReadOnlyList<ReferencePoolDiagnosticsSnapshot> CreateDiagnosticsSnapshots()
        {
            AssertReadyAndMainThread();
            var snapshots = new List<ReferencePoolDiagnosticsSnapshot>(m_creationOrder.Count);
            for (var i = 0; i < m_creationOrder.Count; i++)
            {
                snapshots.Add(m_creationOrder[i].CreateDiagnosticsSnapshot());
            }

            return snapshots;
        }

        internal void SetConfiguration(
            PoolCapacitySettings defaultCapacity,
            IReadOnlyList<ReferencePoolTypeOverride> typeOverrides)
        {
            m_defaultCapacity = defaultCapacity;
            m_typeOverrides.Clear();
            if (typeOverrides == null)
            {
                return;
            }

            for (var i = 0; i < typeOverrides.Count; i++)
            {
                m_typeOverrides.Add(typeOverrides[i]);
            }
        }

        private List<ReferencePoolTypeOverride> ValidateConfiguration()
        {
            if (m_defaultCapacity == null)
            {
                throw new InvalidOperationException("ReferencePoolHandler 默认容量配置为空。" );
            }

            if (!m_defaultCapacity.TryValidate(out var defaultError))
            {
                throw new InvalidOperationException($"ReferencePoolHandler 默认容量无效：{defaultError}" );
            }

            if (m_typeOverrides == null)
            {
                throw new InvalidOperationException("ReferencePoolHandler 类型覆盖列表为空引用。" );
            }

            var validated = new List<ReferencePoolTypeOverride>(m_typeOverrides.Count);
            var types = new HashSet<Type>();
            for (var i = 0; i < m_typeOverrides.Count; i++)
            {
                var entry = m_typeOverrides[i];
                if (entry == null)
                {
                    throw new InvalidOperationException($"ReferencePoolHandler TypeOverrides[{i}] 为空。" );
                }

                if (!ReferencePoolRuntime.TryValidateItemType(entry.ItemType, out var typeError))
                {
                    throw new InvalidOperationException(
                        $"ReferencePoolHandler TypeOverrides[{i}] 类型无效：{typeError}" );
                }

                if (!types.Add(entry.ItemType))
                {
                    throw new InvalidOperationException(
                        $"ReferencePoolHandler 重复配置了类型 {entry.ItemType.FullName}。" );
                }

                if (entry.Capacity == null)
                {
                    throw new InvalidOperationException(
                        $"ReferencePoolHandler 类型 {entry.ItemType.FullName} 的容量配置为空。" );
                }

                if (!entry.Capacity.TryValidate(out var capacityError))
                {
                    throw new InvalidOperationException(
                        $"ReferencePoolHandler 类型 {entry.ItemType.FullName} 的容量无效：{capacityError}" );
                }

                validated.Add(entry);
            }

            return validated;
        }

        private void ShutdownPools(ICollection<Exception> errors)
        {
            if (m_creationOrder == null)
            {
                return;
            }

            for (var i = m_creationOrder.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_creationOrder[i].Shutdown();
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }
        }

        private void ResetRuntimeState()
        {
            m_acceptingRequests = false;
            m_pools = null;
            m_creationOrder = null;
            m_mainThreadId = 0;
        }

        private void AssertReadyAndMainThread()
        {
            AssertMainThread();
            if (!m_acceptingRequests || m_pools == null)
            {
                throw new InvalidOperationException("ReferencePoolModule 尚未完成加载或已经关闭。" );
            }
        }

        private void AssertMainThread()
        {
            if (m_mainThreadId == 0 || Thread.CurrentThread.ManagedThreadId != m_mainThreadId)
            {
                throw new InvalidOperationException("ReferencePoolModule 只能在加载它的 Unity 主线程调用。" );
            }
        }
    }
}
