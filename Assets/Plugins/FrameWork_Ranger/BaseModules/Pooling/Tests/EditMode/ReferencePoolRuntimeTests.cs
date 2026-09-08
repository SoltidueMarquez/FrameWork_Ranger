using System;
using FrameWork_Ranger.Pooling.Reference;
using NUnit.Framework;

namespace FrameWork_Ranger.Pooling.Tests
{
    internal sealed class ReferencePoolRuntimeTests
    {
        private sealed class DisposableItem : IReferencePoolItem, IDisposable
        {
            internal int RentCount;
            internal int ReturnCount;
            internal int DisposeCount;

            public DisposableItem()
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

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        private abstract class AbstractItem : IReferencePoolItem
        {
            public void OnRent()
            {
            }

            public void OnReturn()
            {
            }
        }

        private sealed class PrivateConstructorItem : IReferencePoolItem
        {
            private PrivateConstructorItem()
            {
            }

            public void OnRent()
            {
            }

            public void OnReturn()
            {
            }
        }

        [Test]
        public void PermanentRemoval_DisposesReferenceItem()
        {
            var runtime = new ReferencePoolRuntime(
                typeof(DisposableItem),
                new PoolCapacitySettings(0, 1, 0, 0f));

            var item = runtime.Rent<DisposableItem>();
            runtime.Return(item);

            Assert.That(item.RentCount, Is.EqualTo(1));
            Assert.That(item.ReturnCount, Is.EqualTo(1));
            Assert.That(item.DisposeCount, Is.EqualTo(1));
            Assert.That(runtime.CreateDiagnosticsSnapshot().TotalCount, Is.Zero);
        }

        [Test]
        public void PrewarmedRentReturn_HotPathHasNoManagedAllocation()
        {
            var runtime = new ReferencePoolRuntime(
                typeof(DisposableItem),
                new PoolCapacitySettings(1, 1, -1, 0f));
            runtime.PrewarmInitial();

            var warmup = runtime.Rent<DisposableItem>();
            runtime.Return(warmup);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                var item = runtime.Rent<DisposableItem>();
                runtime.Return(item);
            }

            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocatedBytes, Is.Zero);
            runtime.Shutdown();
        }

        [TestCase(typeof(AbstractItem))]
        [TestCase(typeof(PrivateConstructorItem))]
        [TestCase(typeof(string))]
        public void InvalidConfiguredTypes_AreRejected(Type type)
        {
            Assert.That(ReferencePoolRuntime.TryValidateItemType(type, out var error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }
    }
}
