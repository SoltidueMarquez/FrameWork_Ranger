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
        public void StateSanitizer_RemovesInvalidAndDuplicatePinnedPages()
        {
            var registry = new FrameworkCenterPageRegistry(new[]
            {
                typeof(FirstTestPage),
                typeof(SecondTestPage),
            });
            var state = new FrameworkCenterStateData
            {
                lastActivePinnedPageId = "test.missing",
            };
            state.pinnedPageIds.Add("test.first");
            state.pinnedPageIds.Add("test.missing");
            state.pinnedPageIds.Add("test.first");
            state.pinnedPageIds.Add("test.second");

            FrameworkCenterStateSanitizer.Sanitize(state, registry);

            CollectionAssert.AreEqual(new[] { "test.first", "test.second" }, state.pinnedPageIds);
            Assert.That(state.lastActivePinnedPageId, Is.EqualTo("test.first"));
        }

        [Test]
        public void StateStore_RoundTripsVersionTwoState()
        {
            var path = Path.Combine(m_tempDirectory, "state.json");
            var store = new FrameworkCenterStateStore(path);
            var state = new FrameworkCenterStateData
            {
                lastActivePinnedPageId = "test.second",
            };
            state.pinnedPageIds.Add("test.first");
            state.pinnedPageIds.Add("test.second");
            store.Save(state);

            var loaded = store.Load();
            Assert.That(loaded.stateVersion, Is.EqualTo(FrameworkCenterStateData.CurrentVersion));
            Assert.That(loaded.lastActivePinnedPageId, Is.EqualTo("test.second"));
            CollectionAssert.AreEqual(new[] { "test.first", "test.second" }, loaded.pinnedPageIds);
        }

        [Test]
        public void StateStore_LegacyAndMalformedJsonFallBackToCleanVersionTwoState()
        {
            var path = Path.Combine(m_tempDirectory, "state.json");
            var store = new FrameworkCenterStateStore(path);

            File.WriteAllText(
                path,
                "{\"activePageId\":\"test.first\",\"openTabs\":[\"test.first\"],\"recentPageIds\":[\"test.second\"]}");
            var legacyFallback = store.Load();
            Assert.That(legacyFallback.stateVersion, Is.EqualTo(FrameworkCenterStateData.CurrentVersion));
            Assert.That(legacyFallback.pinnedPageIds, Is.Empty);
            Assert.That(legacyFallback.lastActivePinnedPageId, Is.Empty);

            File.WriteAllText(path, "{not-json");
            var fallback = store.Load();
            Assert.That(fallback.stateVersion, Is.EqualTo(FrameworkCenterStateData.CurrentVersion));
            Assert.That(fallback.pinnedPageIds, Is.Empty);
            Assert.That(fallback.lastActivePinnedPageId, Is.Empty);
        }

        [Test]
        public void PageScroll_DefaultsToHost_AndSettingsWorkspaceOwnsItsScroll()
        {
            Assert.That(new FirstTestPage().UseHostContentScroll, Is.True);
            Assert.That(new FrameworkSettingsPage().UseHostContentScroll, Is.False);
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
