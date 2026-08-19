using NUnit.Framework;

namespace Framework_WWJ.ResourceManagement.Tests
{
    internal sealed class ResourceKeyTests
    {
        [Test]
        public void Factories_KeepExplicitBackendAndOrdinalLocation()
        {
            var resources = ResourceKey.FromResources("Framework/Enemy");
            var addressables = ResourceKey.FromAddressables("Framework/Enemy");

            Assert.That(resources.Backend, Is.EqualTo(ResourceBackendKind.UnityResources));
            Assert.That(addressables.Backend, Is.EqualTo(ResourceBackendKind.Addressables));
            Assert.That(resources, Is.Not.EqualTo(addressables));
            Assert.That(
                ResourceKey.FromAddressables("Framework/Enemy"),
                Is.Not.EqualTo(ResourceKey.FromAddressables("framework/enemy")));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void EmptyLocation_IsInvalid(string location)
        {
            Assert.That(ResourceKey.FromAddressables(location).IsValid, Is.False);
        }

        [TestCase("Resources/Enemy")]
        [TestCase("/Enemy")]
        [TestCase("Enemy.prefab")]
        [TestCase("Folder\\Enemy")]
        public void InvalidResourcesShape_IsRejected(string location)
        {
            Assert.That(ResourceKey.FromResources(location).IsValid, Is.False);
        }

        [Test]
        public void DefaultKey_IsInvalid()
        {
            Assert.That(default(ResourceKey).IsValid, Is.False);
        }
    }
}
