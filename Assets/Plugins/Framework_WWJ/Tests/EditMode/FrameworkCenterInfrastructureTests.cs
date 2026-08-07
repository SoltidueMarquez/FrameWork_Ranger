using System;
using System.IO;
using System.Linq;
using Framework_WWJ.Editor;
using NUnit.Framework;

namespace Framework_WWJ.Tests
{
    internal sealed class FrameworkCenterInfrastructureTests
    {
        private string m_tempDirectory;

        [SetUp]
        public void SetUp()
        {
            m_tempDirectory = Path.Combine(Path.GetTempPath(), $"FrameworkCenterTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(m_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_tempDirectory))
            {
                Directory.Delete(m_tempDirectory, true);
            }
        }

        [Test]
        public void Registry_UsesStableOrder_AndRejectsDuplicatePageId()
        {
            var registry = new FrameworkCenterPageRegistry(new[]
            {
                typeof(SecondTestPage),
                typeof(FirstTestPage),
                typeof(DuplicateTestPage),
            });

            Assert.That(registry.Pages.Count, Is.EqualTo(2));
            Assert.That(registry.Pages[0].PageId, Is.EqualTo("test.first"));
            Assert.That(registry.Diagnostics.Count, Is.EqualTo(1));
            StringAssert.Contains("重复", registry.Diagnostics[0]);
        }

        [Test]
        public void Registry_AutomaticDiscovery_OnlyContainsMarkedProductionPages()
        {
            var registry = new FrameworkCenterPageRegistry();

            Assert.That(registry.Pages, Is.Not.Empty);
            Assert.That(registry.Pages.All(page =>
                page.GetType().IsDefined(typeof(FrameworkCenterPageExtensionAttribute), false)), Is.True);
            Assert.That(registry.Pages.Any(page => page.PageId.StartsWith("test.", StringComparison.Ordinal)), Is.False);
            Assert.That(registry.Diagnostics.Any(message => message.Contains("test.first")), Is.False);
        }

        [Test]
        public void StateSanitizer_RemovesLegacyTestTabs_AndFallsBackToOverview()
        {
            var registry = new FrameworkCenterPageRegistry();
            var state = new FrameworkCenterStateData
            {
                activePageId = "test.first",
            };
            state.openTabs.Add("test.first");
            state.openTabs.Add("test.second");
            state.recentPageIds.Add("test.first");

            FrameworkCenterStateSanitizer.Sanitize(state, registry, "framework.overview");

            CollectionAssert.AreEqual(new[] { "framework.overview" }, state.openTabs);
            Assert.That(state.recentPageIds, Is.Empty);
            Assert.That(state.activePageId, Is.EqualTo("framework.overview"));
        }

        [Test]
        public void StateStore_RoundTrips_AndMalformedJsonFallsBack()
        {
            var path = Path.Combine(m_tempDirectory, "state.json");
            var store = new FrameworkCenterStateStore(path);
            var state = new FrameworkCenterStateData
            {
                activePageId = "test.first",
            };
            state.openTabs.Add("test.first");
            state.recentPageIds.Add("test.second");
            store.Save(state);

            var loaded = store.Load();
            Assert.That(loaded.activePageId, Is.EqualTo("test.first"));
            CollectionAssert.AreEqual(new[] { "test.first" }, loaded.openTabs);

            File.WriteAllText(path, "{not-json");
            var fallback = store.Load();
            Assert.That(fallback.activePageId, Is.Empty);
            Assert.That(fallback.openTabs, Is.Empty);
        }

        public sealed class FirstTestPage : FrameworkCenterPage
        {
            public override string PageId => "test.first";
            public override string DisplayName => "First";
            public override string Description => "First";
            public override string Category => "A";
            public override int Order => 0;
            public override void OnGUI(FrameworkCenterPageContext context) { }
        }

        public sealed class SecondTestPage : FrameworkCenterPage
        {
            public override string PageId => "test.second";
            public override string DisplayName => "Second";
            public override string Description => "Second";
            public override string Category => "B";
            public override int Order => 0;
            public override void OnGUI(FrameworkCenterPageContext context) { }
        }

        public sealed class DuplicateTestPage : FrameworkCenterPage
        {
            public override string PageId => "test.first";
            public override string DisplayName => "Duplicate";
            public override string Description => "Duplicate";
            public override string Category => "Z";
            public override int Order => 100;
            public override void OnGUI(FrameworkCenterPageContext context) { }
        }
    }
}
