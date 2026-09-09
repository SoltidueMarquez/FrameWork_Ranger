using System.Linq;
using FrameWork_Ranger.Pooling.Reference;
using NUnit.Framework;
using UnityEditor;

namespace FrameWork_Ranger.Events.Tests
{
    internal sealed class EventConfigurationTests
    {
        [Test]
        public void GlobalAsset_ContainsEnabledEventTemplateAndReferencePool()
        {
            var config = AssetDatabase.LoadAssetAtPath<FrameworkGlobalConfig>(
                "Assets/Plugins/FrameWork_Ranger/Resources/FrameworkGlobalConfig.asset");
            var template = AssetDatabase.LoadAssetAtPath<EventModule>(
                "Assets/Plugins/FrameWork_Ranger/BaseModules/EventCenter/Configuration/DefaultEventModule.asset");
            Assert.That(template, Is.Not.Null);
            Assert.That(config.Modules.Count(entry => entry.Enabled && entry.Module == template), Is.EqualTo(1));
            Assert.That(config.Modules.Any(entry => entry.Enabled && entry.Module is ReferencePoolModule), Is.True);
            CollectionAssert.AreEqual(new[] { typeof(ReferencePoolModule) }, template.GetRequiredModuleTypes());
            Assert.That(template.IsRuntimeInstance, Is.False);
            Assert.That(template.State, Is.EqualTo(ModuleLifecycleState.None));
        }
    }
}
