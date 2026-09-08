using System;
using System.Collections.Generic;

namespace FrameWork_Ranger.Pooling
{
    /// <summary>
    /// 维护空闲与借出集合、批量扩容、严格归还、循环缩容和可回滚清理的通用池。
    /// </summary>
    [FrameworkArchitecture(
        "严格跟踪池",
        "集中实现池对象的显式所有权、事务式扩容、回调失败清理和循环缩容。",
        FrameworkArchitectureLayer.RuntimeDriving,
        112,
        typeof(PoolCapacitySettings))]
    internal sealed class TrackedPool<T> where T : class
    {
        private readonly PoolCapacitySettings m_settings;
        private readonly Func<T> m_factory;
        private readonly Action<T> m_onRent;
        private readonly Action<T> m_onReturn;
        private readonly Action<T> m_onDestroy;
        private readonly Func<T, bool> m_isUsable;
        private readonly Action<T> m_onExternallyLost;
        private readonly bool m_supportsExternalInvalidation;
        private readonly Stack<T> m_inactive;
        private readonly HashSet<T> m_inactiveSet;
        private readonly HashSet<T> m_borrowed;
        private readonly List<T> m_validScratch;
        private readonly List<T> m_lostScratch;

        private float m_idleSecondsRemaining;
        private bool m_acceptingRequests = true;

        internal int InactiveCount => m_inactiveSet.Count;

        internal int BorrowedCount => m_borrowed.Count;

        internal int TotalCount => InactiveCount + BorrowedCount;

        internal float IdleSecondsRemaining => m_idleSecondsRemaining;

        internal PoolCapacitySettings Settings => m_settings;

        internal IEnumerable<T> BorrowedItems => m_borrowed;

        internal TrackedPool(
            PoolCapacitySettings settings,
            Func<T> factory,
            Action<T> onRent,
            Action<T> onReturn,
            Action<T> onDestroy,
            Func<T, bool> isUsable = null,
            Action<T> onExternallyLost = null)
        {
            m_settings = settings?.Copy() ?? throw new ArgumentNullException(nameof(settings));
            if (!m_settings.TryValidate(out var error))
            {
                throw new ArgumentException(error, nameof(settings));
            }

            m_factory = factory ?? throw new ArgumentNullException(nameof(factory));
            m_onRent = onRent;
            m_onReturn = onReturn;
            m_onDestroy = onDestroy ?? throw new ArgumentNullException(nameof(onDestroy));
            m_supportsExternalInvalidation = isUsable != null;
            m_isUsable = isUsable ?? (item => !ReferenceEquals(item, null));
            m_onExternallyLost = onExternallyLost;
            m_inactive = new Stack<T>(Math.Max(1, m_settings.InitialCount));
            m_inactiveSet = new HashSet<T>(PoolReferenceEqualityComparer<T>.Instance);
            m_borrowed = new HashSet<T>(PoolReferenceEqualityComparer<T>.Instance);
            m_validScratch = new List<T>();
            m_lostScratch = new List<T>();
            ResetIdleTimer();
        }

        internal void PrewarmInitial()
        {
            while (InactiveCount < m_settings.InitialCount)
            {
                PrewarmOne();
            }
        }

        internal bool PrewarmOne()
        {
            EnsureAccepting();
            if (InactiveCount >= m_settings.InitialCount)
            {
                return false;
            }

            var item = CreateOne();
            EnsureFactoryReturnedUnowned(item);
            AddInactive(item);
            return true;
        }

        internal T Rent(Action<T> beforeRentCallback = null)
        {
            EnsureAccepting();
            PruneInvalid();
            if (m_inactiveSet.Count == 0)
            {
                ExpandForRent();
            }

            var item = PopInactive();
            if (!m_borrowed.Add(item))
            {
                DestroyWithContext(item, "借用时发现重复借出对象");
                throw new InvalidOperationException("池内部状态损坏：对象已经位于借出集合。" );
            }

            ResetIdleTimer();
            try
            {
                beforeRentCallback?.Invoke(item);
                m_onRent?.Invoke(item);
                return item;
            }
            catch (Exception callbackException)
            {
                m_borrowed.Remove(item);
                ThrowWithCleanup(callbackException, item, "池对象 Rent 回调失败，清理对象时又发生异常。" );
                throw;
            }
        }

        internal void Return(T item)
        {
            EnsureAccepting();
            ValidateReturn(item);

            try
            {
                m_onReturn?.Invoke(item);
            }
            catch (Exception callbackException)
            {
                m_borrowed.Remove(item);
                ResetIdleTimerIfFullyIdle();
                ThrowWithCleanup(callbackException, item, "池对象 Return 回调失败，清理对象时又发生异常。" );
                throw;
            }

            m_borrowed.Remove(item);
            ResetIdleTimerIfFullyIdle();
            if (m_settings.HasRetainedLimit && InactiveCount >= m_settings.MaxRetained)
            {
                m_onDestroy(item);
                return;
            }

            AddInactive(item);
        }

        internal bool TryReturn(T item)
        {
            EnsureAccepting();
            if (!CanReturn(item))
            {
                return false;
            }

            Return(item);
            return true;
        }

        internal bool OwnsBorrowed(T item)
        {
            return !ReferenceEquals(item, null) && m_borrowed.Contains(item);
        }

        internal bool OwnsInactive(T item)
        {
            return !ReferenceEquals(item, null) && m_inactiveSet.Contains(item);
        }

        internal void TickIdleShrink(float deltaTime)
        {
            EnsureAccepting();
            PruneInvalid();
            if (m_settings.IdleShrinkIntervalSeconds <= 0f ||
                BorrowedCount > 0 ||
                InactiveCount <= m_settings.InitialCount)
            {
                ResetIdleTimer();
                return;
            }

            m_idleSecondsRemaining -= Math.Max(0f, deltaTime);
            if (m_idleSecondsRemaining > 0f)
            {
                return;
            }

            var removeCount = Math.Min(
                m_settings.ExpansionBatchSize,
                InactiveCount - m_settings.InitialCount);
            var errors = new List<Exception>();
            for (var i = 0; i < removeCount; i++)
            {
                var item = PopInactive();
                TryDestroy(item, errors);
            }

            ResetIdleTimer();
            ThrowIfAny(errors, "池循环缩容时发生异常。" );
        }

        internal void ClearInactive()
        {
            EnsureAccepting();
            PruneInvalid();
            var errors = new List<Exception>();
            while (m_inactiveSet.Count > 0)
            {
                TryDestroy(PopInactive(), errors);
            }

            ResetIdleTimer();
            ThrowIfAny(errors, "清空池内空闲对象时发生异常。" );
        }

        internal int PruneInvalid()
        {
            if (!m_supportsExternalInvalidation)
            {
                return 0;
            }

            var removed = 0;
            if (m_inactiveSet.Count > 0)
            {
                m_validScratch.Clear();
                while (m_inactive.Count > 0)
                {
                    var item = m_inactive.Pop();
                    if (m_isUsable(item))
                    {
                        m_validScratch.Add(item);
                    }
                    else
                    {
                        m_inactiveSet.Remove(item);
                        removed++;
                        m_onExternallyLost?.Invoke(item);
                    }
                }

                for (var i = m_validScratch.Count - 1; i >= 0; i--)
                {
                    m_inactive.Push(m_validScratch[i]);
                }

                m_validScratch.Clear();
            }

            if (m_borrowed.Count > 0)
            {
                m_lostScratch.Clear();
                foreach (var item in m_borrowed)
                {
                    if (!m_isUsable(item))
                    {
                        m_lostScratch.Add(item);
                    }
                }

                for (var i = 0; i < m_lostScratch.Count; i++)
                {
                    m_borrowed.Remove(m_lostScratch[i]);
                    removed++;
                    m_onExternallyLost?.Invoke(m_lostScratch[i]);
                }

                m_lostScratch.Clear();
            }

            if (removed > 0)
            {
                ResetIdleTimerIfFullyIdle();
            }

            return removed;
        }

        internal void Shutdown()
        {
            if (!m_acceptingRequests)
            {
                return;
            }

            m_acceptingRequests = false;
            PruneInvalid();
            var errors = new List<Exception>();
            var borrowed = new List<T>(m_borrowed);
            for (var i = 0; i < borrowed.Count; i++)
            {
                var item = borrowed[i];
                try
                {
                    m_onReturn?.Invoke(item);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }

                m_borrowed.Remove(item);
                TryDestroy(item, errors);
            }

            while (m_inactiveSet.Count > 0)
            {
                TryDestroy(PopInactive(), errors);
            }

            ThrowIfAny(errors, "关闭池并清理对象时发生异常。" );
        }

        private void ExpandForRent()
        {
            var createCount = m_settings.ExpansionBatchSize;
            if (m_settings.HasRetainedLimit)
            {
                var usefulCount = m_settings.MaxRetained - InactiveCount + 1;
                createCount = Math.Min(createCount, Math.Max(1, usefulCount));
            }

            var created = new List<T>(createCount);
            var createdSet = new HashSet<T>(PoolReferenceEqualityComparer<T>.Instance);
            try
            {
                for (var i = 0; i < createCount; i++)
                {
                    var item = CreateOne();
                    EnsureFactoryReturnedUnowned(item);
                    if (!createdSet.Add(item))
                    {
                        throw new InvalidOperationException("池工厂在同一扩容批次返回了重复对象引用。" );
                    }

                    created.Add(item);
                }
            }
            catch (Exception creationException)
            {
                var errors = new List<Exception> { creationException };
                for (var i = created.Count - 1; i >= 0; i--)
                {
                    TryDestroy(created[i], errors);
                }

                if (errors.Count == 1)
                {
                    throw;
                }

                throw new AggregateException("池批量扩容失败，回滚新对象时又发生异常。", errors);
            }

            for (var i = 0; i < created.Count; i++)
            {
                AddInactive(created[i]);
            }
        }

        private T CreateOne()
        {
            var item = m_factory();
            if (ReferenceEquals(item, null))
            {
                throw new InvalidOperationException("池工厂返回了 null。" );
            }

            if (!m_isUsable(item))
            {
                throw new InvalidOperationException("池工厂返回了不可用对象。" );
            }

            return item;
        }

        private void AddInactive(T item)
        {
            if (!m_inactiveSet.Add(item))
            {
                throw new InvalidOperationException("池内部状态损坏：对象重复进入空闲集合。" );
            }

            m_inactive.Push(item);
        }

        private T PopInactive()
        {
            var item = m_inactive.Pop();
            if (!m_inactiveSet.Remove(item))
            {
                throw new InvalidOperationException("池内部状态损坏：空闲栈与空闲集合不一致。" );
            }

            return item;
        }

        private bool CanReturn(T item)
        {
            return !ReferenceEquals(item, null) && m_borrowed.Contains(item);
        }

        private void EnsureFactoryReturnedUnowned(T item)
        {
            if (m_inactiveSet.Contains(item) || m_borrowed.Contains(item))
            {
                throw new InvalidOperationException("池工厂返回了已经由当前池拥有的对象引用。" );
            }
        }

        private void ValidateReturn(T item)
        {
            if (ReferenceEquals(item, null))
            {
                throw new ArgumentNullException(nameof(item), "不能向池归还 null。" );
            }

            if (m_borrowed.Contains(item))
            {
                return;
            }

            if (m_inactiveSet.Contains(item))
            {
                throw new InvalidOperationException("对象已经归还，不能重复归还。" );
            }

            throw new InvalidOperationException("对象不属于此池或并非由此池借出。" );
        }

        private void EnsureAccepting()
        {
            if (!m_acceptingRequests)
            {
                throw new InvalidOperationException("池已经关闭，不再接受请求。" );
            }
        }

        private void ResetIdleTimer()
        {
            m_idleSecondsRemaining = Math.Max(0f, m_settings.IdleShrinkIntervalSeconds);
        }

        private void ResetIdleTimerIfFullyIdle()
        {
            if (m_borrowed.Count == 0)
            {
                ResetIdleTimer();
            }
        }

        private void DestroyWithContext(T item, string message)
        {
            try
            {
                m_onDestroy(item);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(message, exception);
            }
        }

        private void ThrowWithCleanup(Exception original, T item, string aggregateMessage)
        {
            try
            {
                m_onDestroy(item);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(aggregateMessage, original, cleanupException);
            }
        }

        private void TryDestroy(T item, ICollection<Exception> errors)
        {
            try
            {
                m_onDestroy(item);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        private static void ThrowIfAny(ICollection<Exception> errors, string message)
        {
            if (errors.Count > 0)
            {
                throw new AggregateException(message, errors);
            }
        }
    }
}
