using System;
using System.Linq;
using Framework_WWJ.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Tests
{
    internal sealed class FrameworkArchitectureCompoundGraphTests
    {
        private string m_expansionSessionKey;
        private string m_positionSessionKey;
        private FrameworkArchitectureCatalog m_catalog;
        private FrameworkArchitectureGraphLayout.ExpansionState m_expansionState;
        private FrameworkArchitectureGraphPositionState m_positionState;

        [SetUp]
        public void SetUp()
        {
            m_expansionSessionKey = $"Framework_WWJ.Tests.ArchitectureExpansion.{Guid.NewGuid():N}";
            m_positionSessionKey = $"Framework_WWJ.Tests.ArchitecturePosition.{Guid.NewGuid():N}";
            SessionState.EraseString(m_expansionSessionKey);
            SessionState.EraseString(m_positionSessionKey);
            m_catalog = FrameworkArchitectureCatalogBuilder.Build();
            m_expansionState = new FrameworkArchitectureGraphLayout.ExpansionState(m_expansionSessionKey);
            m_positionState = new FrameworkArchitectureGraphPositionState(m_positionSessionKey);
            m_expansionState.Restore(m_catalog);
            m_positionState.Restore(m_catalog);
        }

        [TearDown]
        public void TearDown()
        {
            SessionState.EraseString(m_expansionSessionKey);
            SessionState.EraseString(m_positionSessionKey);
        }

        [Test]
        public void DefaultState_ShowsCompactNonOverlappingRootCards()
        {
            var layout = Build(string.Empty);
            var entries = layout.Groups.Values.ToArray();

            Assert.That(m_expansionState.UserExpandedGroupIds, Is.Empty);
            Assert.That(layout.TypeRects, Is.Empty);
            Assert.That(entries.Length, Is.EqualTo(m_catalog.RootGroup.Children.Count));
            Assert.That(entries.All(entry => !entry.IsExpanded), Is.True);
            Assert.That(entries.All(entry =>
                Mathf.Approximately(entry.Bounds.width, FrameworkArchitectureGraphLayout.CollapsedGroupWidth) &&
                Mathf.Approximately(entry.Bounds.height, FrameworkArchitectureGraphLayout.CollapsedGroupHeight)), Is.True);
            AssertNoOverlap(entries.Select(entry => entry.Bounds).ToArray());
        }

        [Test]
        public void SessionState_RoundTripsAndRemovesUnknownGroupIds()
        {
            var core = m_catalog.FindGroup("core");
            var collapsedLayout = Build(string.Empty);
            m_expansionState.Toggle(
                core,
                FrameworkArchitectureGraphLayout.ExpansionState.GetHeaderAnchor(
                    collapsedLayout.Groups[core].HeaderRect));

            SessionState.SetString(
                m_expansionSessionKey,
                SessionState.GetString(m_expansionSessionKey, string.Empty) + "\nmissing");
            var restored = new FrameworkArchitectureGraphLayout.ExpansionState(m_expansionSessionKey);
            restored.Restore(m_catalog);

            Assert.That(restored.UserExpandedGroupIds, Is.EquivalentTo(new[] { "core" }));
            Assert.That(SessionState.GetString(m_expansionSessionKey, string.Empty), Is.EqualTo("core"));
        }

        [Test]
        public void Search_TemporarilyExpandsMatchPathWithoutChangingUserOrPositionState()
        {
            var resourceModule = m_catalog.Nodes.Single(node =>
                node.Type.FullName == "Framework_WWJ.ResourceManagement.ResourceModule");
            var positionRevision = m_positionState.Revision;

            var searchLayout = Build("ResourceModule");
            var restoredLayout = Build(string.Empty);

            Assert.That(searchLayout.Search.MatchedTypes, Does.Contain(resourceModule));
            Assert.That(searchLayout.TypeRects.ContainsKey(resourceModule), Is.True);
            Assert.That(searchLayout.Search.EffectiveExpandedGroupIds, Does.Contain(resourceModule.Group.GroupId));
            Assert.That(m_expansionState.UserExpandedGroupIds, Is.Empty);
            Assert.That(m_positionState.Revision, Is.EqualTo(positionRevision));
            Assert.That(restoredLayout.TypeRects, Is.Empty);
        }

        [Test]
        public void ExpandAll_ContainsEveryTypeOnceAndUsesOnlyLocalLayerColumns()
        {
            m_expansionState.ExpandAll(m_catalog);
            var layout = Build(string.Empty);

            Assert.That(layout.TypeRects.Count, Is.EqualTo(m_catalog.Nodes.Count));
            Assert.That(layout.TypeRects.Keys.Distinct().Count(), Is.EqualTo(m_catalog.Nodes.Count));
            foreach (var group in m_catalog.Groups.Where(group => !group.IsRoot))
            {
                var visibleNodes = group.Nodes.Where(layout.TypeRects.ContainsKey).ToArray();
                foreach (var layerGroup in visibleNodes.GroupBy(node => node.Metadata.Layer))
                {
                    var expectedX = layout.TypeRects[layerGroup.First()].x;
                    Assert.That(
                        layerGroup.All(node => Mathf.Approximately(layout.TypeRects[node].x, expectedX)),
                        Is.True,
                        $"{group.GroupId} / {layerGroup.Key}");
                    Assert.That(layout.LayerHeaders.Count(header =>
                        ReferenceEquals(header.Group, group) && header.Layer == layerGroup.Key), Is.EqualTo(1));
                }
            }

            AssertNoOverlap(layout.TypeRects.Values.ToArray());
        }

        [Test]
        public void ExpandAll_NestsVisibleChildGroupsAndTypesInsideParents()
        {
            m_expansionState.ExpandAll(m_catalog);
            var layout = Build(string.Empty);

            foreach (var entry in layout.Groups.Values)
            {
                if (entry.Group.Parent == null || entry.Group.Parent.IsRoot)
                {
                    continue;
                }

                var parent = layout.Groups[entry.Group.Parent];
                AssertContains(parent.Bounds, entry.Bounds);
                Assert.That(entry.Bounds.xMin, Is.GreaterThanOrEqualTo(parent.ContentRect.xMin - 0.001f));
                Assert.That(entry.Bounds.yMin, Is.GreaterThanOrEqualTo(parent.ContentRect.yMin - 0.001f));
            }

            foreach (var pair in layout.TypeRects)
            {
                AssertContains(layout.Groups[pair.Key.Group].Bounds, pair.Value);
            }
        }

        [Test]
        public void NestedGroupMovement_IsClampedAndExpandsParentToContainSubtree()
        {
            m_expansionState.ExpandAll(m_catalog);
            var nested = m_catalog.Groups.First(group =>
                !group.IsRoot && group.Parent != null && !group.Parent.IsRoot);
            var baseline = Build(string.Empty);
            var baselineRect = baseline.Groups[nested].Bounds;

            m_positionState.MoveGroup(nested, new Vector2(-80f, -40f));
            var clamped = Build(string.Empty);
            AssertVector2(clamped.Groups[nested].Bounds.position, baselineRect.position);
            m_positionState.MoveGroup(nested, new Vector2(170f, 95f));
            var moved = Build(string.Empty);
            var movedRect = moved.Groups[nested].Bounds;
            var parent = moved.Groups[nested.Parent];

            AssertVector2(movedRect.position - baselineRect.position, new Vector2(90f, 55f));
            AssertContains(parent.Bounds, movedRect);
            Assert.That(movedRect.xMin, Is.GreaterThanOrEqualTo(parent.ContentRect.xMin - 0.001f));
            Assert.That(movedRect.yMin, Is.GreaterThanOrEqualTo(parent.ContentRect.yMin - 0.001f));
        }

        [Test]
        public void MovingExpandedRootGroup_MovesItsWholeSubtree()
        {
            m_expansionState.ExpandAll(m_catalog);
            var rootBranch = m_catalog.FindGroup("base-modules");
            var descendant = m_catalog.Nodes.First(node => IsSameOrDescendant(node.Group, rootBranch));
            var baseline = Build(string.Empty);
            var groupPosition = baseline.Groups[rootBranch].Bounds.position;
            var typePosition = baseline.TypeRects[descendant].position;
            var delta = new Vector2(73f, 41f);

            m_positionState.MoveGroup(rootBranch, delta);
            var moved = Build(string.Empty);

            AssertVector2(moved.Groups[rootBranch].Bounds.position - groupPosition, delta);
            AssertVector2(moved.TypeRects[descendant].position - typePosition, delta);
        }

        [Test]
        public void PositionState_RoundTripsSanitizesAndResetsStableKeys()
        {
            var core = m_catalog.FindGroup("core");
            var type = m_catalog.Nodes.First(node => IsSameOrDescendant(node.Group, core));
            m_positionState.MoveGroup(core, new Vector2(30f, -20f));
            m_positionState.MoveType(type, new Vector2(12f, 18f));
            m_positionState.Save();
            SessionState.SetString(
                m_positionSessionKey,
                SessionState.GetString(m_positionSessionKey, string.Empty) + "\nG:missing|9|9");

            var restored = new FrameworkArchitectureGraphPositionState(m_positionSessionKey);
            restored.Restore(m_catalog);

            Assert.That(FrameworkArchitectureGraphPositionState.GetGroupKey(core), Is.EqualTo("G:core"));
            Assert.That(FrameworkArchitectureGraphPositionState.GetTypeKey(type), Does.StartWith("T:"));
            AssertVector2(restored.GetOffset(core), new Vector2(30f, -20f));
            AssertVector2(restored.GetOffset(type), new Vector2(12f, 18f));
            Assert.That(SessionState.GetString(m_positionSessionKey, string.Empty), Does.Not.Contain("G:missing"));

            restored.ResetAll();
            Assert.That(restored.OffsetCount, Is.Zero);
            Assert.That(SessionState.GetString(m_positionSessionKey, string.Empty), Is.Empty);
        }

        [Test]
        public void CollapsedRelations_UseVisibleGroupProxiesAndAggregateKinds()
        {
            var layout = Build(string.Empty);
            var resourceModule = m_catalog.Nodes.Single(node =>
                node.Type.FullName == "Framework_WWJ.ResourceManagement.ResourceModule");
            var expectedCrossRootRelations = m_catalog.Relations.Count(relation =>
                !ReferenceEquals(
                    GetRootBranch(relation.Source.Group),
                    GetRootBranch(relation.Target.Group)));

            Assert.That(layout.Relations, Is.Not.Empty);
            Assert.That(layout.Relations.All(relation => relation.Source.IsGroup && relation.Target.IsGroup), Is.True);
            Assert.That(layout.Relations.All(relation => relation.Source.Key != relation.Target.Key), Is.True);
            Assert.That(layout.Relations.Sum(relation => relation.Count), Is.EqualTo(expectedCrossRootRelations));
            Assert.That(layout.TryGetVisibleEndpoint(resourceModule, out var endpoint), Is.True);
            Assert.That(endpoint.Group.GroupId, Is.EqualTo("base-modules"));
        }

        [Test]
        public void RelationFiltersAndGeometry_KeepValidPortsAndCurveBounds()
        {
            m_expansionState.ExpandAll(m_catalog);
            var none = Build(
                string.Empty,
                FrameworkArchitectureGraphLayout.RelationVisibility.None);
            var inheritance = Build(
                string.Empty,
                FrameworkArchitectureGraphLayout.RelationVisibility.Inheritance);

            Assert.That(none.Relations, Is.Empty);
            Assert.That(inheritance.Relations, Is.Not.Empty);
            Assert.That(inheritance.Relations.All(relation =>
                relation.Kind == FrameworkArchitectureRelationKind.Inheritance), Is.True);
            foreach (var relation in inheritance.Relations)
            {
                Assert.That(IsOnBoundary(relation.Source.Rect, relation.Geometry.Start), Is.True);
                Assert.That(IsOnBoundary(relation.Target.Rect, relation.Geometry.End), Is.True);
                AssertContains(relation.Geometry.Bounds, relation.Geometry.Control1);
                AssertContains(relation.Geometry.Bounds, relation.Geometry.Control2);
                Assert.That(
                    inheritance.IsRelatedToSelection(relation, relation.Source.Type.Group, null),
                    Is.True);
            }
        }

        [Test]
        public void DragDelta_ConvertsViewportPixelsByCurrentZoom()
        {
            AssertVector2(
                FrameworkArchitectureGraphDrawer.ToCanvasDragDelta(new Vector2(20f, -10f), 0.5f),
                new Vector2(40f, -20f));
            AssertVector2(
                FrameworkArchitectureGraphDrawer.ToCanvasDragDelta(new Vector2(20f, -10f), 2f),
                new Vector2(10f, -5f));
            AssertVector2(
                FrameworkArchitectureGraphDrawer.ClampDragDeltaToUpperLeftBoundary(
                    new Rect(100f, 100f, 40f, 30f),
                    new Vector2(80f, 90f),
                    new Vector2(-50f, -50f)),
                new Vector2(-20f, -10f));
        }

        [Test]
        public void LayoutCache_ReusesRepaintAndInvalidatesOnStateChanges()
        {
            var cache = new FrameworkArchitectureGraphLayoutCache();
            var first = cache.GetOrBuild(
                m_catalog,
                m_expansionState,
                m_positionState,
                string.Empty,
                FrameworkArchitectureGraphLayout.RelationVisibility.All);
            var repaint = cache.GetOrBuild(
                m_catalog,
                m_expansionState,
                m_positionState,
                string.Empty,
                FrameworkArchitectureGraphLayout.RelationVisibility.All);

            Assert.That(repaint, Is.SameAs(first));
            Assert.That(cache.BuildCount, Is.EqualTo(1));

            var core = m_catalog.FindGroup("core");
            m_positionState.MoveGroup(core, new Vector2(10f, 0f));
            var moved = cache.GetOrBuild(
                m_catalog,
                m_expansionState,
                m_positionState,
                string.Empty,
                FrameworkArchitectureGraphLayout.RelationVisibility.All);
            Assert.That(moved, Is.Not.SameAs(first));
            Assert.That(cache.BuildCount, Is.EqualTo(2));

            m_expansionState.Toggle(
                core,
                FrameworkArchitectureGraphLayout.ExpansionState.GetHeaderAnchor(
                    moved.Groups[core].HeaderRect));
            cache.GetOrBuild(
                m_catalog,
                m_expansionState,
                m_positionState,
                string.Empty,
                FrameworkArchitectureGraphLayout.RelationVisibility.All);
            Assert.That(cache.BuildCount, Is.EqualTo(3));

            cache.GetOrBuild(
                m_catalog,
                m_expansionState,
                m_positionState,
                "Runtime",
                FrameworkArchitectureGraphLayout.RelationVisibility.All);
            Assert.That(cache.BuildCount, Is.EqualTo(4));

            cache.GetOrBuild(
                m_catalog,
                m_expansionState,
                m_positionState,
                "Runtime",
                FrameworkArchitectureGraphLayout.RelationVisibility.Collaboration);
            Assert.That(cache.BuildCount, Is.EqualTo(5));

            cache.GetOrBuild(
                FrameworkArchitectureCatalogBuilder.Build(),
                m_expansionState,
                m_positionState,
                "Runtime",
                FrameworkArchitectureGraphLayout.RelationVisibility.Collaboration);
            Assert.That(cache.BuildCount, Is.EqualTo(6));
        }

        private FrameworkArchitectureGraphLayout Build(
            string searchText,
            FrameworkArchitectureGraphLayout.RelationVisibility visibility =
                FrameworkArchitectureGraphLayout.RelationVisibility.All)
        {
            return FrameworkArchitectureGraphLayout.Build(
                m_catalog,
                m_expansionState,
                m_positionState,
                searchText,
                visibility);
        }

        private static FrameworkArchitectureGroupDescriptor GetRootBranch(
            FrameworkArchitectureGroupDescriptor group)
        {
            var current = group;
            while (current?.Parent != null && !current.Parent.IsRoot)
            {
                current = current.Parent;
            }

            return current;
        }

        private static bool IsSameOrDescendant(
            FrameworkArchitectureGroupDescriptor candidate,
            FrameworkArchitectureGroupDescriptor ancestor)
        {
            var current = candidate;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        private static void AssertNoOverlap(Rect[] rects)
        {
            for (var i = 0; i < rects.Length; i++)
            {
                for (var otherIndex = i + 1; otherIndex < rects.Length; otherIndex++)
                {
                    Assert.That(rects[i].Overlaps(rects[otherIndex]), Is.False,
                        $"{rects[i]} overlaps {rects[otherIndex]}");
                }
            }
        }

        private static void AssertContains(Rect outer, Rect inner)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 0.001f));
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 0.001f));
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 0.001f));
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 0.001f));
        }

        private static void AssertContains(Rect outer, Vector2 point)
        {
            Assert.That(point.x, Is.InRange(outer.xMin - 0.001f, outer.xMax + 0.001f));
            Assert.That(point.y, Is.InRange(outer.yMin - 0.001f, outer.yMax + 0.001f));
        }

        private static void AssertVector2(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
        }

        private static bool IsOnBoundary(Rect rect, Vector2 point)
        {
            var onVertical =
                (Mathf.Approximately(point.x, rect.xMin) || Mathf.Approximately(point.x, rect.xMax)) &&
                point.y >= rect.yMin && point.y <= rect.yMax;
            var onHorizontal =
                (Mathf.Approximately(point.y, rect.yMin) || Mathf.Approximately(point.y, rect.yMax)) &&
                point.x >= rect.xMin && point.x <= rect.xMax;
            return onVertical || onHorizontal;
        }
    }
}
