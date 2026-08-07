using System.Linq;
using Framework_WWJ.Editor;
using NUnit.Framework;

namespace Framework_WWJ.Tests
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
    }
}
