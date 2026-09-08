using NUnit.Framework;

namespace FrameWork_Ranger.Pooling.Tests
{
    internal sealed class PoolCapacitySettingsTests
    {
        [Test]
        public void Defaults_MatchApprovedValues()
        {
            var reference = PoolCapacitySettings.CreateReferenceDefaults();
            Assert.That(reference.InitialCount, Is.EqualTo(30));
            Assert.That(reference.ExpansionBatchSize, Is.EqualTo(20));
            Assert.That(reference.MaxRetained, Is.EqualTo(-1));
            Assert.That(reference.IdleShrinkIntervalSeconds, Is.EqualTo(15f));

            var gameObject = PoolCapacitySettings.CreateGameObjectDefaults();
            Assert.That(gameObject.InitialCount, Is.Zero);
            Assert.That(gameObject.ExpansionBatchSize, Is.EqualTo(5));
            Assert.That(gameObject.MaxRetained, Is.EqualTo(-1));
            Assert.That(gameObject.IdleShrinkIntervalSeconds, Is.EqualTo(120f));
        }

        [TestCase(-1, 1, -1, 1f, "InitialCount")]
        [TestCase(0, 0, -1, 1f, "ExpansionBatchSize")]
        [TestCase(0, 1, -2, 1f, "MaxRetained")]
        [TestCase(2, 1, 1, 1f, "MaxRetained")]
        public void InvalidCounts_AreRejected(
            int initial,
            int expansion,
            int retained,
            float idle,
            string expected)
        {
            var settings = new PoolCapacitySettings(initial, expansion, retained, idle);

            Assert.That(settings.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain(expected));
        }

        [Test]
        public void NonPositiveIdleInterval_DisablesShrinkAndRemainsValid()
        {
            Assert.That(
                new PoolCapacitySettings(0, 1, -1, -5f).TryValidate(out _),
                Is.True);
        }
    }
}
