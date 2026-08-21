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
        private string m_sessionKey;
        private FrameworkArchitectureCatalog m_catalog;
        private FrameworkArchitectureGraphLayout.ExpansionState m_expansionState;

        [SetUp]
        public void SetUp()
        {
            m_sessionKey = $"Framework_WWJ.Tests.ArchitectureExpansion.{Guid.NewGuid():N}";
            SessionState.EraseString(m_sessionKey);
            m_catalog = FrameworkArchitectureCatalogBuilder.Build();
            m_expansionState = new FrameworkArchitectureGraphLayout.ExpansionState(m_sessionKey);
            m_expansionState.Restore(m_catalog);
        }

        [TearDown]
        public void TearDown()
        {
            SessionState.EraseString(m_sessionKey);
        }

        [Test]
        public void DefaultState_ShowsOnlyCollapsedRootGroups()
        {
            var layout = Build(string.Empty);

            Assert.That(m_expansionState.UserExpandedGroupIds, Is.Empty);
            Assert.That(layout.TypeRects, Is.Empty);
            Assert.That(layout.Groups.Count, Is.EqualTo(m_catalog.RootGroup.Children.Count));
            Assert.That(layout.Groups.Values.All(entry => !entry.IsExpanded), Is.True);
        }

        [Test]
        public void SessionState_RoundTripsAndRemovesUnknownGroupIds()
        {
            var core = m_catalog.FindGroup("core");
            var collapsedLayout = Build(string.Empty);
            m_expansionState.Toggle(core, collapsedLayout.Groups[core].HeaderRect.center);

            SessionState.SetString(m_sessionKey, SessionState.GetString(m_sessionKey, string.Empty) + "\nmissing");
            var restored = new FrameworkArchitectureGraphLayout.ExpansionState(m_sessionKey);
            restored.Restore(m_catalog);

            Assert.That(restored.UserExpandedGroupIds, Is.EquivalentTo(new[] { "core" }));
            Assert.That(SessionState.GetString(m_sessionKey, string.Empty), Is.EqualTo("core"));
        }

        [Test]
        public void Search_TemporarilyExpandsMatchPathWithoutChangingUserState()
        {
            var resourceModule = m_catalog.Nodes.Single(node =>
                node.Type.FullName == "Framework_WWJ.ResourceManagement.ResourceModule");

            var searchLayout = Build("ResourceModule");
            var restoredLayout = Build(string.Empty);

            Assert.That(searchLayout.Search.MatchedTypes, Does.Contain(resourceModule));
            Assert.That(searchLayout.TypeRects.ContainsKey(resourceModule), Is.True);
            Assert.That(searchLayout.Search.EffectiveExpandedGroupIds, Does.Contain(resourceModule.Group.GroupId));
            Assert.That(m_expansionState.UserExpandedGroupIds, Is.Empty);
            Assert.That(restoredLayout.TypeRects, Is.Empty);
        }

        [Test]
        public void ExpandAll_ContainsEveryTypeOnceAndAlignsGlobalLayerColumns()
        {
            m_expansionState.ExpandAll(m_catalog);
            var layout = Build(string.Empty);

            Assert.That(layout.TypeRects.Count, Is.EqualTo(m_catalog.Nodes.Count));
            foreach (var pair in layout.TypeRects)
            {
                var groupBounds = layout.Groups[pair.Key.Group].Bounds;
                Assert.That(pair.Value.x, Is.EqualTo(layout.LayerXs[pair.Key.Metadata.Layer]).Within(0.001f));
                Assert.That(pair.Value.xMin, Is.GreaterThanOrEqualTo(groupBounds.xMin));
                Assert.That(pair.Value.xMax, Is.LessThanOrEqualTo(groupBounds.xMax));
                Assert.That(pair.Value.yMin, Is.GreaterThanOrEqualTo(groupBounds.yMin));
                Assert.That(pair.Value.yMax, Is.LessThanOrEqualTo(groupBounds.yMax));
            }

            foreach (var group in m_catalog.Groups.Where(group => !group.IsRoot))
            {
                var groupNodes = group.Nodes.Where(layout.TypeRects.ContainsKey).ToArray();
                for (var i = 0; i < groupNodes.Length; i++)
                {
                    for (var otherIndex = i + 1; otherIndex < groupNodes.Length; otherIndex++)
                    {
                        Assert.That(
                            layout.TypeRects[groupNodes[i]].Overlaps(layout.TypeRects[groupNodes[otherIndex]]),
                            Is.False,
                            $"{groupNodes[i].Type.FullName} / {groupNodes[otherIndex].Type.FullName}");
                    }
                }
            }
        }

        [Test]
        public void ExpandAll_NestsVisibleChildGroupsInsideParents()
        {
            m_expansionState.ExpandAll(m_catalog);
            var layout = Build(string.Empty);

            foreach (var entry in layout.Groups.Values)
            {
                if (entry.Group.Parent == null || entry.Group.Parent.IsRoot)
                {
                    continue;
                }

                var parent = layout.Groups[entry.Group.Parent].Bounds;
                Assert.That(entry.Bounds.xMin, Is.GreaterThanOrEqualTo(parent.xMin));
                Assert.That(entry.Bounds.xMax, Is.LessThanOrEqualTo(parent.xMax));
                Assert.That(entry.Bounds.yMin, Is.GreaterThanOrEqualTo(parent.yMin));
                Assert.That(entry.Bounds.yMax, Is.LessThanOrEqualTo(parent.yMax));
            }
        }

        [Test]
        public void CollapsedRelations_UseVisibleGroupProxiesAndHideInternalRelations()
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
        public void RelationFilters_KeepOnlyRequestedKinds()
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
            Assert.That(inheritance.Relations.All(relation => !relation.IsAggregated), Is.True);
        }

        private FrameworkArchitectureGraphLayout Build(
            string searchText,
            FrameworkArchitectureGraphLayout.RelationVisibility visibility =
                FrameworkArchitectureGraphLayout.RelationVisibility.All)
        {
            return FrameworkArchitectureGraphLayout.Build(
                m_catalog,
                m_expansionState,
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
    }
}
