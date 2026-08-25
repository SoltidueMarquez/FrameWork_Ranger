using System.Linq;
using FrameWork_Ranger.Editor;
using NUnit.Framework;

namespace FrameWork_Ranger.Tests
{
    internal sealed class FrameworkArchitectureCatalogTests
    {
        [Test]
        public void Build_CoversRuntimeAndEditorTypes_AndCreatesExpectedRelations()
        {
            FrameworkSourceScriptIndex.Clear();
            var catalog = FrameworkArchitectureCatalogBuilder.Build();

            Assert.That(catalog.Diagnostics, Is.Empty);
            Assert.That(catalog.Nodes.Any(node => node.Type == typeof(FrameworkRuntime)), Is.True);
            Assert.That(catalog.Nodes.Any(node => node.Type == typeof(IModuleUpdate)), Is.True);
            Assert.That(catalog.Nodes.Any(node => node.Type == typeof(FrameworkCenterPage)), Is.True);
            Assert.That(catalog.Relations.Any(relation =>
                relation.Source.Type == typeof(DirectModuleBase) &&
                relation.Target.Type == typeof(ModuleBase) &&
                relation.Kind == FrameworkArchitectureRelationKind.Inheritance), Is.True);
            Assert.That(catalog.Relations.Any(relation =>
                relation.Source.Type == typeof(FrameworkRuntime) &&
                relation.Target.Type == typeof(ModuleGraphResolver) &&
                relation.Kind == FrameworkArchitectureRelationKind.Collaboration), Is.True);
        }

        [Test]
        public void SourceIndex_LocatesFrameworkRuntimeScript()
        {
            FrameworkSourceScriptIndex.Clear();
            var script = FrameworkSourceScriptIndex.Find(typeof(FrameworkRuntime));

            Assert.That(script, Is.Not.Null);
            StringAssert.EndsWith("FrameworkRuntime.cs", UnityEditor.AssetDatabase.GetAssetPath(script));
        }

        [Test]
        public void Build_CreatesCoreAndResourceManagementHierarchy()
        {
            var catalog = FrameworkArchitectureCatalogBuilder.Build();

            var core = catalog.FindGroup("core");
            var baseModules = catalog.FindGroup("base-modules");
            var resource = catalog.FindGroup("base-modules/resource-management");
            var integrations = catalog.FindGroup("base-modules/resource-management/integrations");

            Assert.That(core, Is.Not.Null);
            Assert.That(baseModules, Is.Not.Null);
            Assert.That(resource, Is.Not.Null);
            Assert.That(integrations, Is.Not.Null);
            Assert.That(catalog.RootGroup.Children.Select(group => group.GroupId), Is.EqualTo(new[]
            {
                "core",
                "base-modules",
            }));
            Assert.That(resource.Children.Select(group => group.GroupId), Does.Contain(
                "base-modules/resource-management/runtime"));
            Assert.That(resource.Children.Select(group => group.GroupId), Does.Contain(
                "base-modules/resource-management/editor"));
            Assert.That(integrations.Children.Count, Is.EqualTo(2));
        }

        [Test]
        public void Build_IncludesResourceProductionTypes_AndLocatesTheirScripts()
        {
            FrameworkSourceScriptIndex.Clear();
            var catalog = FrameworkArchitectureCatalogBuilder.Build();
            var expectedTypes = new[]
            {
                "FrameWork_Ranger.ResourceManagement.ResourceModule",
                "FrameWork_Ranger.ResourceManagement.ResourceStore",
                "FrameWork_Ranger.ResourceManagement.ResourceProviderBase",
                "FrameWork_Ranger.ResourceManagement.UnityResources.UnityResourcesProvider",
                "FrameWork_Ranger.ResourceManagement.Addressables.AddressablesResourceProvider",
                "FrameWork_Ranger.ResourceManagement.Editor.ResourceManagementCenterPage",
            };

            for (var i = 0; i < expectedTypes.Length; i++)
            {
                var descriptor = catalog.Nodes.SingleOrDefault(node => node.Type.FullName == expectedTypes[i]);
                Assert.That(descriptor, Is.Not.Null, expectedTypes[i]);
                Assert.That(descriptor.Script, Is.Not.Null, expectedTypes[i]);
                StringAssert.StartsWith(
                    "Assets/Plugins/FrameWork_Ranger/",
                    UnityEditor.AssetDatabase.GetAssetPath(descriptor.Script));
            }

            var resourceKey = catalog.Nodes.Single(node =>
                node.Type.FullName == "FrameWork_Ranger.ResourceManagement.ResourceKey");
            Assert.That(resourceKey.Kind, Is.EqualTo(FrameworkArchitectureTypeKind.Struct));
        }
    }
}
