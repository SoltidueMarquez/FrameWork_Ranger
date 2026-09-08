using System;
using NUnit.Framework;

namespace FrameWork_Ranger.Pooling.Tests
{
    internal sealed class TrackedPoolTests
    {
        private sealed class Item
        {
            internal int RentCount;
            internal int ReturnCount;
        }

        [Test]
        public void PrewarmAndEmptyRent_UseInitialAndExpansionBatch()
        {
            var created = 0;
            var pool = CreatePool(
                new PoolCapacitySettings(2, 3, -1, 0f),
                () =>
                {
                    created++;
                    return new Item();
                });

            pool.PrewarmInitial();
            var first = pool.Rent();
            var second = pool.Rent();
            var third = pool.Rent();

            Assert.That(created, Is.EqualTo(5));
            Assert.That(pool.BorrowedCount, Is.EqualTo(3));
            Assert.That(pool.InactiveCount, Is.EqualTo(2));
            Assert.That(first.RentCount, Is.EqualTo(1));
            Assert.That(second.RentCount, Is.EqualTo(1));
            Assert.That(third.RentCount, Is.EqualTo(1));
        }

        [Test]
        public void FiniteRetainedLimit_DoesNotCreateObjectsThatWouldBeImmediatelyDiscarded()
        {
            var created = 0;
            var destroyed = 0;
            var pool = CreatePool(
                new PoolCapacitySettings(0, 8, 1, 0f),
                () =>
                {
                    created++;
                    return new Item();
                },
                _ => destroyed++);

            var item = pool.Rent();
            Assert.That(created, Is.EqualTo(2));
            Assert.That(pool.InactiveCount, Is.EqualTo(1));

            pool.Return(item);
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
            Assert.That(destroyed, Is.EqualTo(1));
        }

        [Test]
        public void Return_RejectsNullForeignAndDuplicate_WhileTryReturnReturnsFalse()
        {
            var pool = CreatePool(new PoolCapacitySettings(0, 1, -1, 0f), () => new Item());
            var rented = pool.Rent();
            var foreign = new Item();

            Assert.Throws<ArgumentNullException>(() => pool.Return(null));
            Assert.Throws<InvalidOperationException>(() => pool.Return(foreign));
            Assert.That(pool.TryReturn(foreign), Is.False);
            pool.Return(rented);
            Assert.Throws<InvalidOperationException>(() => pool.Return(rented));
            Assert.That(pool.TryReturn(rented), Is.False);
        }

        [Test]
        public void IdleShrink_WaitsForFullyIdleThenRemovesOneBatchPerInterval()
        {
            var destroyed = 0;
            var pool = CreatePool(
                new PoolCapacitySettings(1, 2, -1, 5f),
                () => new Item(),
                _ => destroyed++);
            pool.PrewarmInitial();
            var first = pool.Rent();
            var second = pool.Rent();
            var third = pool.Rent();
            pool.Return(second);
            pool.Return(third);

            pool.TickIdleShrink(100f);
            Assert.That(destroyed, Is.Zero, "仍有借出对象时不得缩容");
            pool.Return(first);
            pool.TickIdleShrink(4.9f);
            Assert.That(destroyed, Is.Zero, "重新完全空闲后应恢复完整周期");
            pool.TickIdleShrink(0.2f);
            Assert.That(destroyed, Is.EqualTo(2));
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
        }

        [Test]
        public void CallbackFailure_RemovesAndDestroysCurrentObjectThenPropagates()
        {
            var destroyed = 0;
            var pool = new TrackedPool<Item>(
                new PoolCapacitySettings(0, 1, -1, 0f),
                () => new Item(),
                _ => throw new InvalidOperationException("rent failed"),
                null,
                _ => destroyed++);

            var exception = Assert.Throws<InvalidOperationException>(() => pool.Rent());

            Assert.That(exception.Message, Does.Contain("rent failed"));
            Assert.That(destroyed, Is.EqualTo(1));
            Assert.That(pool.TotalCount, Is.Zero);
        }

        [Test]
        public void ClearInactive_CanReachZero_AndShutdownReturnsBorrowedBeforeDestroy()
        {
            var destroyed = 0;
            var pool = CreatePool(
                new PoolCapacitySettings(2, 1, -1, 1f),
                () => new Item(),
                _ => destroyed++);
            pool.PrewarmInitial();
            pool.ClearInactive();
            Assert.That(pool.InactiveCount, Is.Zero);

            var borrowed = pool.Rent();
            pool.Shutdown();

            Assert.That(borrowed.ReturnCount, Is.EqualTo(1));
            Assert.That(destroyed, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => pool.Rent());
        }

        [Test]
        public void BatchCreationFailure_RollsBackOnlyNewObjects()
        {
            var attempts = 0;
            var destroyed = 0;
            var pool = CreatePool(
                new PoolCapacitySettings(0, 3, -1, 0f),
                () =>
                {
                    attempts++;
                    if (attempts == 3)
                    {
                        throw new InvalidOperationException("factory failed");
                    }

                    return new Item();
                },
                _ => destroyed++);

            Assert.Throws<InvalidOperationException>(() => pool.Rent());
            Assert.That(destroyed, Is.EqualTo(2));
            Assert.That(pool.TotalCount, Is.Zero);
        }

        private static TrackedPool<Item> CreatePool(
            PoolCapacitySettings settings,
            Func<Item> factory,
            Action<Item> onDestroy = null)
        {
            return new TrackedPool<Item>(
                settings,
                factory,
                item => item.RentCount++,
                item => item.ReturnCount++,
                onDestroy ?? (_ => { }));
        }
    }
}
