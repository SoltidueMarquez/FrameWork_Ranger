using System;
using System.Linq;
using FrameWork_Ranger.ResourceManagement;
using FrameWork_Ranger.UI.Editor;
using FrameWork_Ranger.UI.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UIConfigurationEditorTests
    {
        private string m_folder;
        private FrameworkProjectSettings m_settings;
        private FrameworkGlobalConfig m_global;
        private ResourceModule m_resource;
        private GlobalUIModule m_a, m_b;
        [SetUp]
        public void Setup()
        {
            m_folder = "Assets/UIEditorTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", m_folder.Substring("Assets/".Length));
            m_settings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
            m_global = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            m_resource = ScriptableObject.CreateInstance<ResourceModule>();
            m_resource.SetHandler(new ResourceHandler());
            m_a = ScriptableObject.CreateInstance<GlobalUIModule>();
            m_b = ScriptableObject.CreateInstance<GlobalUIModule>();
            AssetDatabase.CreateAsset(m_a, m_folder + "/A.asset");
            AssetDatabase.CreateAsset(m_b, m_folder + "/B.asset");
            AssetDatabase.CreateAsset(m_global, m_folder + "/Global.asset");
            AssetDatabase.CreateAsset(m_resource, m_folder + "/Resource.asset");
            m_global.SetModules(new[] { new ModuleConfigEntry(true, m_resource), new ModuleConfigEntry(true, m_a) });
            m_settings.SetGlobalConfig(m_global);
        }
        [TearDown]
        public void Teardown()
        {
            Undo.ClearUndo(m_global);
            UnityEngine.Object.DestroyImmediate(m_settings);
            AssetDatabase.DeleteAsset(m_folder);
        }
        [Test]
        public void ResourceDiscovery_SelectionAndSceneGuidFollowMoves_DeletedSceneStopsMatching()
        {
            var a = UIResourceAssetTools.Create(m_folder + "/CatalogA.asset");
            var b = UIResourceAssetTools.Create(m_folder + "/CatalogB.asset");
            CollectionAssert.Contains(UIResourceAssetTools.FindAll(), a);
            var assets = new[] { a, b }; string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(b));
            Assert.That(UIResourceAssetTools.ResolveSelection(assets, "", a), Is.SameAs(a));
            Assert.That(UIResourceAssetTools.ResolveSelection(assets, "", null), Is.Null);
            Assert.That(UIResourceAssetTools.ResolveSelection(new[] { b }, "", null), Is.SameAs(b));
            Assert.That(AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(b), m_folder + "/MovedCatalog.asset"), Is.Empty);
            Assert.That(UIResourceAssetTools.ResolveSelection(assets, guid, a), Is.SameAs(b));
            string scenePath = m_folder + "/Scene.unity";
            Assert.That(AssetDatabase.CopyAsset(UISampleAssetBuilder.Root + "/UI_A.unity", scenePath), Is.True);
            var entry = new UIPanelEntry { AllScenes = false }; a.Scene.Panels.Add(entry);
            var reference = new UISceneReference { Path = scenePath, Guid = AssetDatabase.AssetPathToGUID(scenePath) }; entry.Scenes.Add(reference);
            string movedPath = m_folder + "/MovedScene.unity";
            Assert.That(AssetDatabase.MoveAsset(scenePath, movedPath), Is.Empty);
            UIResourceAssetTools.SyncScenes(a); Assert.That(reference.Path, Is.EqualTo(movedPath));
            Assert.That(entry.Allows(movedPath), Is.True); Assert.That(entry.Allows(scenePath), Is.False);
            AssetDatabase.DeleteAsset(movedPath); UIResourceAssetTools.SyncScenes(a);
            Assert.That(reference.Path, Is.Empty); Assert.That(entry.Allows(movedPath), Is.False);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(b));
            Assert.That(UIResourceAssetTools.ResolveSelection(new[] { a }, guid, a), Is.SameAs(a));
        }
        [Test]
        public void IndependentCatalog_ExtractPreservesGuidAndLogic_RoundtripAndUndoUse()
        {
            m_a.GlobalConfiguration.Domains.Add(new UIDomainDefinition { Key = "Main", WorldScale = 0.03f });
            m_a.GlobalConfiguration.Panels.Add(new UIPanelDefinition { Key = "Notice", PrefabAddress = "RangerUI/Notice", Logic = new UISampleLogic() });
            string moduleGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_a));
            var root = UIResourceAssetTools.Extract(m_a);
            var panel = root.Global.Panels.Single().Panel;
            Assert.That(panel.Definition.UseDirectPrefab, Is.False);
            Assert.That(panel.Definition.Logic, Is.TypeOf<UISampleLogic>());
            Assert.That(panel.Definition.Logic, Is.Not.SameAs(m_a.GlobalConfiguration.Panels[0].Logic));
            Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_a)), Is.EqualTo(moduleGuid));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(panel), ImportAssetOptions.ForceUpdate);
            panel = AssetDatabase.LoadAssetAtPath<UIPanelConfig>(AssetDatabase.GetAssetPath(panel));
            Assert.That(panel.Definition.PrefabAddress, Is.EqualTo("RangerUI/Notice"));
            Assert.That(panel.Definition.Logic, Is.TypeOf<UISampleLogic>());
            var next = UIResourceAssetTools.Create(m_folder + "/Next.asset");
            Assert.That(next.Global.Panels, Is.Empty, "新配置不得带演示面板。");
            UIResourceAssetTools.Use(m_settings, next);
            Assert.That(m_a.UIResources, Is.SameAs(next));
            Assert.That(m_global.Modules[0].Module, Is.SameAs(m_resource));
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_a));
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(m_a.UIResources, Is.SameAs(root));
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_a));
        }
        [Test]
        public void IndependentCatalog_FailedDependencyDoesNotMutateModuleOrCreateAsset()
        {
            var root = UIResourceAssetTools.Create(m_folder + "/New.asset");
            m_global.SetModules(new[] { new ModuleConfigEntry(true, m_a) });
            Assert.Throws<InvalidOperationException>(() => UIResourceAssetTools.Use(m_settings, root));
            Assert.That(m_a.UIResources, Is.Null);
            m_global.SetModules(Array.Empty<ModuleConfigEntry>());
            string path = m_folder + "/Never.asset";
            Assert.Throws<InvalidOperationException>(() => UIResourceAssetTools.Use(m_settings, root, path));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Null);
            Assert.That(m_global.Modules, Is.Empty);
        }
        [Test]
        public void DiscoveryAndSelection_RestoreGuidAcrossMove_DefaultToActive()
        {
            CollectionAssert.Contains(UIConfigurationAssets.FindAll(), m_a);
            CollectionAssert.Contains(UIConfigurationAssets.FindAll(), m_b);
            var assets = new[] { m_a, m_b };
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_b));
            Assert.That(UIConfigurationAssets.ResolveSelection(assets, "", m_a), Is.SameAs(m_a));
            Assert.That(UIConfigurationAssets.ResolveSelection(assets, "", null), Is.Null);
            Assert.That(UIConfigurationAssets.ResolveSelection(new[] { m_b }, "", null), Is.SameAs(m_b));
            Assert.That(AssetDatabase.MoveAsset(m_folder + "/B.asset", m_folder + "/Moved.asset"), Is.Empty);
            Assert.That(UIConfigurationAssets.ResolveSelection(assets, guid, m_a), Is.SameAs(m_b));
            Assert.That(UIConfigurationAssets.Active(m_global), Is.SameAs(m_a), "选择编辑对象不能更改项目安装。");
            AssetDatabase.DeleteAsset(m_folder + "/Moved.asset");
            Assert.That(UIConfigurationAssets.ResolveSelection(new[] { m_a }, guid, m_a), Is.SameAs(m_a));
        }
        [Test]
        public void Use_ReplacesInPlace_SavesAndSupportsUndo()
        {
            UIConfigurationAssets.Use(m_settings, m_b);
            Assert.That(m_global.Modules.Count, Is.EqualTo(2));
            Assert.That(m_global.Modules[0].Module, Is.SameAs(m_resource));
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_b));
            Assert.That(m_global.Modules[1].Enabled, Is.True);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_a));
            Undo.PerformRedo();
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_b));
        }
        [Test]
        public void Use_RejectsMissingDependencyInvalidConfigAndDuplicateEntriesWithoutMutation()
        {
            m_global.SetModules(new[] { new ModuleConfigEntry(true, m_a) });
            Assert.Throws<InvalidOperationException>(() => UIConfigurationAssets.Use(m_settings, m_b));
            Assert.That(m_global.Modules.Single().Module, Is.SameAs(m_a));
            m_global.SetModules(new[] { new ModuleConfigEntry(true, m_resource), new ModuleConfigEntry(true, m_a) });
            m_b.GlobalConfiguration.Panels.Add(new UIPanelDefinition { Key = "Invalid", DomainKey = "Missing" });
            Assert.Throws<InvalidOperationException>(() => UIConfigurationAssets.Use(m_settings, m_b));
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_a));
            m_b.GlobalConfiguration.Panels.Clear();
            m_global.SetModules(new[] { new ModuleConfigEntry(true, m_a), new ModuleConfigEntry(false, m_b) });
            Assert.Throws<InvalidOperationException>(() => UIConfigurationAssets.Use(m_settings, m_b));
            Assert.That(m_global.Modules.Count, Is.EqualTo(2));
        }
        [Test]
        public void Use_AppendsWhenAbsent_AndEnablesDisabledEntry()
        {
            m_global.SetModules(new[] { new ModuleConfigEntry(true, m_resource) });
            UIConfigurationAssets.Use(m_settings, m_a);
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_a));
            m_global.SetModules(new[] { new ModuleConfigEntry(false, m_a), new ModuleConfigEntry(true, m_resource) });
            UIConfigurationAssets.Use(m_settings, m_b);
            Assert.That(m_global.Modules[0].Module, Is.SameAs(m_b));
            Assert.That(m_global.Modules[0].Enabled, Is.True);
            Assert.That(m_global.Modules[1].Module, Is.SameAs(m_resource));
        }
        [Test]
        public void InspectorSelectionAndSerialization_PreserveLogicAndModeSpecificFields()
        {
            var domain = new UIDomainDefinition { Key = "Main", PlaneDistance = 7, WorldSize = new Vector2(640, 480), WorldScale = 0.02f };
            m_a.GlobalConfiguration.Domains.Add(domain);
            m_a.GlobalConfiguration.Panels.Add(new UIPanelDefinition { Key = "Panel", PrefabAddress = "RangerUI/SamplePanel", Logic = new UISampleLogic() });
            foreach (RenderMode mode in Enum.GetValues(typeof(RenderMode)))
            {
                domain.RenderMode = mode;
                EditorUtility.SetDirty(m_a); AssetDatabase.SaveAssetIfDirty(m_a);
                Assert.That(GlobalUIModuleInspector.UsesScreenSize(mode), Is.EqualTo(mode != RenderMode.WorldSpace));
                Assert.That(GlobalUIModuleInspector.UsesCameraDistance(mode), Is.EqualTo(mode == RenderMode.ScreenSpaceCamera));
            }
            string path = AssetDatabase.GetAssetPath(m_a);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<GlobalUIModule>(path);
            Assert.That(loaded.GlobalConfiguration.Domains[0].PlaneDistance, Is.EqualTo(7));
            Assert.That(loaded.GlobalConfiguration.Domains[0].WorldSize, Is.EqualTo(new Vector2(640, 480)));
            Assert.That(loaded.GlobalConfiguration.Panels[0].Logic, Is.TypeOf<UISampleLogic>());
            Assert.That(loaded.GlobalConfiguration.Panels[0].PrefabAddress, Is.EqualTo("RangerUI/SamplePanel"));
            var inspector = UnityEditor.Editor.CreateEditor(loaded);
            try { Assert.That(inspector, Is.TypeOf<GlobalUIModuleInspector>()); }
            finally { UnityEngine.Object.DestroyImmediate(inspector); }
        }
    }
}
