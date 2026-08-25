using System.Collections.Generic;
using FrameWork_Ranger.Editor;
using NUnit.Framework;

namespace FrameWork_Ranger.Tests
{
    internal sealed class FrameworkCenterTabModelTests
    {
        private FrameworkCenterPageRegistry m_registry;

        [SetUp]
        public void SetUp()
        {
            m_registry = new FrameworkCenterPageRegistry(new[]
            {
                typeof(FirstPage),
                typeof(SecondPage),
                typeof(ThirdPage),
                typeof(HelpPage),
            });
        }

        [Test]
        public void EmptyState_StartsWithFallbackAsUnpinnedPreview()
        {
            var model = CreateModel();

            Assert.That(model.PinnedPageIds, Is.Empty);
            Assert.That(model.PreviewPageId, Is.EqualTo("page.first"));
            Assert.That(model.ActivePageId, Is.EqualTo("page.first"));
        }

        [Test]
        public void OpeningUnpinnedPages_ReplacesSinglePreview()
        {
            var model = CreateModel();

            model.OpenPage("page.second");
            model.OpenPage("page.third");

            Assert.That(model.PreviewPageId, Is.EqualTo("page.third"));
            Assert.That(model.ActivePageId, Is.EqualTo("page.third"));
            Assert.That(model.PinnedPageIds, Is.Empty);
        }

        [Test]
        public void OpeningPinnedPage_PreservesExistingPreview()
        {
            var state = CreateState("page.first");
            var model = CreateModel(state);
            model.OpenPage("page.second");

            model.OpenPage("page.first");

            Assert.That(model.ActivePageId, Is.EqualTo("page.first"));
            Assert.That(model.PreviewPageId, Is.EqualTo("page.second"));
        }

        [Test]
        public void PinAndUnpin_ConvertBetweenPreviewAndPinnedOwnership()
        {
            var model = CreateModel();

            Assert.That(model.PinPreview(), Is.True);
            CollectionAssert.AreEqual(new[] { "page.first" }, model.PinnedPageIds);
            Assert.That(model.PreviewPageId, Is.Empty);

            model.OpenPage("page.second");
            Assert.That(model.Unpin("page.first"), Is.True);

            Assert.That(model.PinnedPageIds, Is.Empty);
            Assert.That(model.PreviewPageId, Is.EqualTo("page.first"));
            Assert.That(model.ActivePageId, Is.EqualTo("page.first"));
        }

        [Test]
        public void ClosingActivePinnedPages_UsesLeftRightPreviewAndFallbackOrder()
        {
            var state = CreateState("page.first", "page.second", "page.third");
            state.lastActivePinnedPageId = "page.second";
            var model = CreateModel(state);
            model.OpenPage("page.help");
            model.OpenPage("page.second");

            model.Close("page.second");
            Assert.That(model.ActivePageId, Is.EqualTo("page.first"));

            model.Close("page.first");
            Assert.That(model.ActivePageId, Is.EqualTo("page.third"));

            model.Close("page.third");
            Assert.That(model.ActivePageId, Is.EqualTo("page.help"));

            model.Close("page.help");
            Assert.That(model.ActivePageId, Is.EqualTo("page.first"));
            Assert.That(model.PreviewPageId, Is.EqualTo("page.first"));
        }

        [Test]
        public void ClosingInactivePinnedPage_DoesNotChangeActivePreview()
        {
            var state = CreateState("page.first", "page.second");
            var model = CreateModel(state);
            model.OpenPage("page.third");

            model.Close("page.second");

            Assert.That(model.ActivePageId, Is.EqualTo("page.third"));
            Assert.That(model.PreviewPageId, Is.EqualTo("page.third"));
            CollectionAssert.AreEqual(new[] { "page.first" }, model.PinnedPageIds);
        }

        [Test]
        public void MovePinnedToInsertionIndex_ReordersWithoutChangingActivePage()
        {
            var state = CreateState("page.first", "page.second", "page.third");
            var model = CreateModel(state);

            Assert.That(model.MovePinnedToInsertionIndex("page.first", 3), Is.True);

            CollectionAssert.AreEqual(
                new[] { "page.second", "page.third", "page.first" },
                model.PinnedPageIds);
            Assert.That(model.ActivePageId, Is.EqualTo("page.first"));
        }

        [Test]
        public void HelpPage_UsesTheSamePreviewAndPinRules()
        {
            var model = CreateModel();

            model.OpenPage("page.help");
            Assert.That(model.PreviewPageId, Is.EqualTo("page.help"));

            model.PinPreview();
            Assert.That(model.IsPinned("page.help"), Is.True);
            Assert.That(model.PreviewPageId, Is.Empty);
        }

        private FrameworkCenterTabModel CreateModel(FrameworkCenterStateData state = null)
        {
            return new FrameworkCenterTabModel(
                state ?? new FrameworkCenterStateData(),
                m_registry,
                "page.first");
        }

        private static FrameworkCenterStateData CreateState(params string[] pinnedPageIds)
        {
            var state = new FrameworkCenterStateData();
            state.pinnedPageIds.AddRange(pinnedPageIds);
            state.lastActivePinnedPageId = pinnedPageIds.Length > 0
                ? pinnedPageIds[0]
                : string.Empty;
            return state;
        }

        public abstract class TestPageBase : FrameworkCenterPage
        {
            public override string Description => PageId;
            public override string Category => "Test";
            public override int Order => 0;
            public override IReadOnlyList<string> Keywords => new string[0];
            public override void OnGUI(FrameworkCenterPageContext context) { }
        }

        public sealed class FirstPage : TestPageBase
        {
            public override string PageId => "page.first";
            public override string DisplayName => "First";
        }

        public sealed class SecondPage : TestPageBase
        {
            public override string PageId => "page.second";
            public override string DisplayName => "Second";
        }

        public sealed class ThirdPage : TestPageBase
        {
            public override string PageId => "page.third";
            public override string DisplayName => "Third";
        }

        public sealed class HelpPage : TestPageBase
        {
            public override string PageId => "page.help";
            public override string DisplayName => "Help";
        }
    }
}
