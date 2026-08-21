using System;
using System.Collections.Generic;
using Framework_WWJ.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Tests
{
    internal sealed class FrameworkConfigurationWorkspaceTests
    {
        private FrameworkProjectSettings m_settings;
        private FrameworkGlobalConfig m_global;
        private FrameworkSceneConfig m_scene;
        private TestModuleA m_moduleA;
        private TestModuleB m_moduleB;
        private FrameworkConfigurationWorkspaceState m_state;

        [SetUp]
        public void SetUp()
        {
            m_settings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
            m_global = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            m_scene = ScriptableObject.CreateInstance<FrameworkSceneConfig>();
            m_moduleA = ScriptableObject.CreateInstance<TestModuleA>();
            m_moduleB = ScriptableObject.CreateInstance<TestModuleB>();
            m_settings.SetGlobalConfig(m_global);
            m_state = new FrameworkConfigurationWorkspaceState($"Test:{Guid.NewGuid():N}");
        }

        [TearDown]
        public void TearDown()
        {
            m_state.Clear();
            Undo.ClearAll();
            UnityEngine.Object.DestroyImmediate(m_moduleB);
            UnityEngine.Object.DestroyImmediate(m_moduleA);
            UnityEngine.Object.DestroyImmediate(m_scene);
            UnityEngine.Object.DestroyImmediate(m_global);
            UnityEngine.Object.DestroyImmediate(m_settings);
        }

        [Test]
        public void WorkspaceState_DefaultsToGlobal_AndRoundTripsTab()
        {
            var selection = m_state.Resolve(m_settings);

            Assert.That(selection.Kind, Is.EqualTo(FrameworkConfigurationSelectionKind.Global));
            Assert.That(m_state.ActiveTab, Is.EqualTo(FrameworkConfigurationWorkspaceTab.Edit));

            m_state.ActiveTab = FrameworkConfigurationWorkspaceTab.DependencyGraph;
            Assert.That(
                m_state.ActiveTab,
                Is.EqualTo(FrameworkConfigurationWorkspaceTab.DependencyGraph));
        }

        [Test]
        public void WorkspaceState_UsesSceneGuid_AndRepairsRemovedSelection()
        {
            var binding = new FrameworkSceneBinding();
            binding.SetScene("scene-guid", "Assets/Scenes/Test.unity", m_scene);
            m_settings.SetSceneBindings(new[] { binding });
            var selected = m_state.SelectSceneBinding(binding, 0);

            Assert.That(selected.ContextId, Is.EqualTo("Scene:scene-guid"));
            Assert.That(m_state.Resolve(m_settings).BindingIndex, Is.EqualTo(0));

            m_settings.SetSceneBindings(Array.Empty<FrameworkSceneBinding>());
            var repaired = m_state.Resolve(m_settings);
            Assert.That(repaired.Kind, Is.EqualTo(FrameworkConfigurationSelectionKind.Global));
        }

        [Test]
        public void WorkspaceState_UpgradesTemporarySceneIndexToGuid()
        {
            var binding = new FrameworkSceneBinding();
            m_settings.SetSceneBindings(new[] { binding });
            var temporary = m_state.SelectSceneBinding(binding, 0);
            Assert.That(temporary.ContextId, Is.EqualTo("SceneIndex:0"));

            binding.SetScene("stable-guid", "Assets/Scenes/Test.unity", m_scene);
            var upgraded = m_state.Resolve(m_settings);

            Assert.That(upgraded.ContextId, Is.EqualTo("Scene:stable-guid"));
            Assert.That(m_state.Resolve(m_settings).ContextId, Is.EqualTo("Scene:stable-guid"));
        }

        [Test]
        public void ModuleController_AddToggleReplaceMoveAndRemove_PreserveStableOrder()
        {
            FrameworkModuleConfigController.AddModule(m_scene, m_moduleA);
            FrameworkModuleConfigController.AddModule(m_scene, m_moduleB);
            FrameworkModuleConfigController.SetEnabled(m_scene, 0, false);
            FrameworkModuleConfigController.MoveModule(m_scene, 1, 0);

            Assert.That(m_scene.Modules.Count, Is.EqualTo(2));
            Assert.That(m_scene.Modules[0].Module, Is.SameAs(m_moduleB));
            Assert.That(m_scene.Modules[1].Module, Is.SameAs(m_moduleA));
            Assert.That(m_scene.Modules[1].Enabled, Is.False);

            FrameworkModuleConfigController.SetModule(m_scene, 1, m_moduleB);
            FrameworkModuleConfigController.RemoveModule(m_scene, 0);
            Assert.That(m_scene.Modules.Count, Is.EqualTo(1));
            Assert.That(m_scene.Modules[0].Module, Is.SameAs(m_moduleB));
            Assert.That(EditorUtility.IsDirty(m_scene), Is.True);
        }

        [Test]
        public void ModuleController_UndoRestoresPreviousList()
        {
            FrameworkModuleConfigController.AddModule(m_scene, m_moduleA);
            Assert.That(m_scene.Modules.Count, Is.EqualTo(1));

            Undo.PerformUndo();

            Assert.That(m_scene.Modules.Count, Is.Zero);
        }

        [Test]
        public void ModuleController_FilterMapsOriginalIndices_AndDisablesFilteredReorder()
        {
            m_moduleA.name = "Alpha Module";
            m_moduleB.name = "Beta Module";
            m_scene.SetModules(new[]
            {
                new ModuleConfigEntry(true, m_moduleA),
                new ModuleConfigEntry(false, m_moduleB),
                new ModuleConfigEntry(true, null),
            });
            var result = ModuleGraphResolver.Inspect(m_scene);
            var indices = new List<int>();

            FrameworkModuleConfigController.BuildVisibleIndices(
                m_scene,
                result,
                "Beta",
                FrameworkModuleConfigFilter.All,
                indices);
            CollectionAssert.AreEqual(new[] { 1 }, indices);

            FrameworkModuleConfigController.BuildVisibleIndices(
                m_scene,
                result,
                string.Empty,
                FrameworkModuleConfigFilter.Problems,
                indices);
            CollectionAssert.Contains(indices, 2);
            Assert.That(
                FrameworkModuleConfigController.CanReorder("Beta", FrameworkModuleConfigFilter.All),
                Is.False);
            Assert.That(
                FrameworkModuleConfigController.CanReorder(string.Empty, FrameworkModuleConfigFilter.All),
                Is.True);
        }

        [Test]
        public void ModuleController_OdinSerializedList_RoundTripsThroughCopiedAsset()
        {
            var folder = "Assets/FrameworkWWJ_Phase18_Test_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            var sourcePath = folder + "/Source.asset";
            var copyPath = folder + "/Copy.asset";
            var source = ScriptableObject.CreateInstance<FrameworkSceneConfig>();
            try
            {
                AssetDatabase.CreateAsset(source, sourcePath);
                FrameworkModuleConfigController.AddModule(source, null);
                AssetDatabase.SaveAssets();

                Assert.That(AssetDatabase.CopyAsset(sourcePath, copyPath), Is.True);
                AssetDatabase.ImportAsset(copyPath, ImportAssetOptions.ForceUpdate);
                var copy = AssetDatabase.LoadAssetAtPath<FrameworkSceneConfig>(copyPath);

                Assert.That(copy, Is.Not.Null);
                Assert.That(copy.Modules.Count, Is.EqualTo(1));
                Assert.That(copy.Modules[0].Enabled, Is.True);
                Assert.That(copy.Modules[0].Module, Is.Null);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
