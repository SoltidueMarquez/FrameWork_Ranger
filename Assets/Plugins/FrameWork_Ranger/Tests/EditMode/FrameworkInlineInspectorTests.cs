using System;
using System.Collections.Generic;
using FrameWork_Ranger.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Tests
{
    internal sealed class FrameworkInlineInspectorTests
    {
        private const string FirstSlot = "Test.First";
        private const string SecondSlot = "Test.Second";
        private const string ExclusiveGroup = "Test.Exclusive";

        private FrameworkInlineInspectorHost.SessionStateStore m_state;
        private InlineInspectorTestAsset m_firstTarget;
        private InlineInspectorTestAsset m_secondTarget;

        [SetUp]
        public void SetUp()
        {
            m_state = new FrameworkInlineInspectorHost.SessionStateStore($"Test:{Guid.NewGuid():N}");
            m_firstTarget = ScriptableObject.CreateInstance<InlineInspectorTestAsset>();
            m_secondTarget = ScriptableObject.CreateInstance<InlineInspectorTestAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            m_state.Clear(FirstSlot);
            m_state.Clear(SecondSlot);
            m_state.ClearExclusiveGroup(ExclusiveGroup);
            UnityEngine.Object.DestroyImmediate(m_firstTarget);
            UnityEngine.Object.DestroyImmediate(m_secondTarget);
        }

        [Test]
        public void SessionState_DefaultsCollapsed_AndRoundTripsTargetIdentity()
        {
            Assert.That(m_state.IsExpanded(FirstSlot, m_firstTarget), Is.False);

            m_state.SetExpanded(FirstSlot, m_firstTarget, true);

            Assert.That(m_state.IsExpanded(FirstSlot, m_firstTarget), Is.True);
            m_state.SetExpanded(FirstSlot, m_firstTarget, false);
            Assert.That(m_state.IsExpanded(FirstSlot, m_firstTarget), Is.False);
        }

        [Test]
        public void SessionState_KeepsSlotsIndependent()
        {
            m_state.SetExpanded(FirstSlot, m_firstTarget, true);
            m_state.SetExpanded(SecondSlot, m_secondTarget, true);

            Assert.That(m_state.IsExpanded(FirstSlot, m_firstTarget), Is.True);
            Assert.That(m_state.IsExpanded(SecondSlot, m_secondTarget), Is.True);

            m_state.SetExpanded(FirstSlot, m_firstTarget, false);
            Assert.That(m_state.IsExpanded(FirstSlot, m_firstTarget), Is.False);
            Assert.That(m_state.IsExpanded(SecondSlot, m_secondTarget), Is.True);
        }

        [Test]
        public void SessionState_TargetReplacementInvalidatesOldExpansion()
        {
            m_state.SetExpanded(FirstSlot, m_firstTarget, true);

            Assert.That(m_state.IsExpanded(FirstSlot, m_secondTarget), Is.False);
            Assert.That(m_state.IsExpanded(FirstSlot, m_firstTarget), Is.False);
        }

        [Test]
        public void SessionState_ToggleDoesNotDirtyTargetAsset()
        {
            EditorUtility.ClearDirty(m_firstTarget);

            m_state.SetExpanded(FirstSlot, m_firstTarget, true);
            m_state.SetExpanded(FirstSlot, m_firstTarget, false);

            Assert.That(EditorUtility.IsDirty(m_firstTarget), Is.False);
        }

        [Test]
        public void Host_ReusesCachedEditor_AndReleasesItWhenCollapsed()
        {
            using var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
            host.SetExpanded(FirstSlot, m_firstTarget, true);

            var firstEditor = host.GetOrCreateEditor(FirstSlot, m_firstTarget);
            var secondRequest = host.GetOrCreateEditor(FirstSlot, m_firstTarget);

            Assert.That(secondRequest, Is.SameAs(firstEditor));
            Assert.That(host.CachedEditorCount, Is.EqualTo(1));

            host.SetExpanded(FirstSlot, m_firstTarget, false);
            Assert.That(host.CachedEditorCount, Is.Zero);
            Assert.That(firstEditor == null, Is.True);
        }

        [Test]
        public void Host_ConfigReferenceUsesRegisteredModuleConfigInspector()
        {
            var config = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            try
            {
                using var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
                var childEditor = host.GetOrCreateEditor(FirstSlot, config);

                Assert.That(childEditor, Is.InstanceOf<ModuleConfigInspector>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Host_TargetReplacementReleasesOldEditorAndClearsExpansion()
        {
            using var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
            host.SetExpanded(FirstSlot, m_firstTarget, true);
            var firstEditor = host.GetOrCreateEditor(FirstSlot, m_firstTarget);

            Assert.That(host.IsExpanded(FirstSlot, m_secondTarget), Is.False);

            Assert.That(firstEditor == null, Is.True);
            Assert.That(host.CachedEditorCount, Is.Zero);
            Assert.That(host.IsExpanded(FirstSlot, m_firstTarget), Is.False);
        }

        [Test]
        public void Host_DisposeDestroysAllCachedEditors()
        {
            var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
            host.SetExpanded(FirstSlot, m_firstTarget, true);
            host.SetExpanded(SecondSlot, m_secondTarget, true);
            var firstEditor = host.GetOrCreateEditor(FirstSlot, m_firstTarget);
            var secondEditor = host.GetOrCreateEditor(SecondSlot, m_secondTarget);

            host.Dispose();

            Assert.That(firstEditor == null, Is.True);
            Assert.That(secondEditor == null, Is.True);
            Assert.That(host.CachedEditorCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => host.IsExpanded(FirstSlot, m_firstTarget));
        }

        [Test]
        public void Host_RetainSlotsReleasesRemovedReferences()
        {
            using var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
            host.SetExpanded(FirstSlot, m_firstTarget, true);
            host.SetExpanded(SecondSlot, m_secondTarget, true);
            var firstEditor = host.GetOrCreateEditor(FirstSlot, m_firstTarget);
            var secondEditor = host.GetOrCreateEditor(SecondSlot, m_secondTarget);

            host.RetainSlots("Test.", new HashSet<string> { FirstSlot });

            Assert.That(firstEditor != null, Is.True);
            Assert.That(secondEditor == null, Is.True);
            Assert.That(host.CachedEditorCount, Is.EqualTo(1));
            Assert.That(host.IsExpanded(SecondSlot, m_secondTarget), Is.False);
        }

        [Test]
        public void Host_ExclusiveExpansionReleasesPreviousEditor()
        {
            using var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
            host.SetExclusiveExpanded(ExclusiveGroup, FirstSlot, m_firstTarget, true);
            var firstEditor = host.GetOrCreateEditor(FirstSlot, m_firstTarget);

            host.SetExclusiveExpanded(ExclusiveGroup, SecondSlot, m_secondTarget, true);
            var secondEditor = host.GetOrCreateEditor(SecondSlot, m_secondTarget);

            Assert.That(firstEditor == null, Is.True);
            Assert.That(secondEditor != null, Is.True);
            Assert.That(host.CachedEditorCount, Is.EqualTo(1));
            Assert.That(
                host.IsExclusiveExpanded(ExclusiveGroup, FirstSlot, m_firstTarget),
                Is.False);
            Assert.That(
                host.IsExclusiveExpanded(ExclusiveGroup, SecondSlot, m_secondTarget),
                Is.True);
        }

        [Test]
        public void Host_RetainExclusiveSlotsClearsRemovedActiveSlot()
        {
            using var host = new FrameworkInlineInspectorHost($"TestHost:{Guid.NewGuid():N}");
            host.SetExclusiveExpanded(ExclusiveGroup, FirstSlot, m_firstTarget, true);
            var firstEditor = host.GetOrCreateEditor(FirstSlot, m_firstTarget);

            host.RetainExclusiveSlots(ExclusiveGroup, new HashSet<string> { SecondSlot });

            Assert.That(firstEditor == null, Is.True);
            Assert.That(host.CachedEditorCount, Is.Zero);
            Assert.That(
                host.IsExclusiveExpanded(ExclusiveGroup, FirstSlot, m_firstTarget),
                Is.False);
        }

        [Test]
        public void SceneBindingSlot_UsesSceneGuidAndFallsBackToIndex()
        {
            var binding = new FrameworkSceneBinding();
            Assert.That(
                FrameworkSceneBindingDrawer.BuildInlineSlotId(binding, 3),
                Is.EqualTo("Project.SceneBinding.Index:3"));

            binding.SetScene("scene-guid", "Assets/Scenes/Test.unity", null);
            Assert.That(
                FrameworkSceneBindingDrawer.BuildInlineSlotId(binding, 3),
                Is.EqualTo("Project.SceneBinding.Scene:scene-guid"));
        }

        private sealed class InlineInspectorTestAsset : ScriptableObject
        {
            [SerializeField]
            private int m_value;
        }
    }
}
