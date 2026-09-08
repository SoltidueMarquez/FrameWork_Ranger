using System;
using FrameWork_Ranger.Pooling;

namespace FrameWork_Ranger.Pooling.Reference
{
    /// <summary>
    /// 一个精确 System.Type 对应的引用池运行实例。
    /// </summary>
    [FrameworkArchitecture(
        "精确类型引用池",
        "缓存类型工厂并把引用生命周期、Dispose 和统计委托给严格跟踪池。",
        FrameworkArchitectureLayer.RuntimeDriving,
        123,
        typeof(TrackedPool<>))]
    internal sealed class ReferencePoolRuntime
    {
        private readonly Type m_itemType;
        private readonly TrackedPool<IReferencePoolItem> m_pool;

        internal Type ItemType => m_itemType;

        internal int BorrowedCount => m_pool.BorrowedCount;

        internal ReferencePoolRuntime(Type itemType, PoolCapacitySettings settings)
        {
            ValidateItemType(itemType);
            m_itemType = itemType;
            var factory = CreateFactory(itemType);
            m_pool = new TrackedPool<IReferencePoolItem>(
                settings,
                factory,
                item => item.OnRent(),
                item => item.OnReturn(),
                DisposeIfNeeded);
        }

        internal void PrewarmInitial()
        {
            m_pool.PrewarmInitial();
        }

        internal T Rent<T>() where T : class, IReferencePoolItem
        {
            return (T)m_pool.Rent();
        }

        internal void Return(IReferencePoolItem item)
        {
            m_pool.Return(item);
        }

        internal bool TryReturn(IReferencePoolItem item)
        {
            return m_pool.TryReturn(item);
        }

        internal void ClearInactive()
        {
            m_pool.ClearInactive();
        }

        internal void Tick(float deltaTime)
        {
            m_pool.TickIdleShrink(deltaTime);
        }

        internal void Shutdown()
        {
            m_pool.Shutdown();
        }

        internal ReferencePoolDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            return new ReferencePoolDiagnosticsSnapshot(
                m_itemType,
                m_pool.InactiveCount,
                m_pool.BorrowedCount,
                m_pool.TotalCount,
                m_pool.IdleSecondsRemaining);
        }

        internal static bool TryValidateItemType(Type itemType, out string error)
        {
            if (itemType == null)
            {
                error = "引用池覆盖类型不能为空。";
                return false;
            }

            if (!typeof(IReferencePoolItem).IsAssignableFrom(itemType))
            {
                error = $"类型 {itemType.FullName} 未实现 {typeof(IReferencePoolItem).FullName}。";
                return false;
            }

            if (!itemType.IsClass || itemType.IsAbstract || itemType.ContainsGenericParameters)
            {
                error = $"类型 {itemType.FullName} 必须是封闭的非抽象 class。";
                return false;
            }

            if (itemType.GetConstructor(Type.EmptyTypes) == null)
            {
                error = $"类型 {itemType.FullName} 缺少公共无参构造函数。";
                return false;
            }

            error = null;
            return true;
        }

        private static void ValidateItemType(Type itemType)
        {
            if (!TryValidateItemType(itemType, out var error))
            {
                throw new InvalidOperationException(error);
            }
        }

        private static Func<IReferencePoolItem> CreateFactory(Type itemType)
        {
            return () => (IReferencePoolItem)Activator.CreateInstance(itemType);
        }

        private static void DisposeIfNeeded(IReferencePoolItem item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
