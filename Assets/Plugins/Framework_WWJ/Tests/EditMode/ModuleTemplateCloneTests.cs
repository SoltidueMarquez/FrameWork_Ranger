using NUnit.Framework;
using UnityEngine;

namespace Framework_WWJ.Tests
{
    internal sealed class ModuleTemplateCloneTests
    {
        [Test]
        public void CloneModule_CopiesEmbeddedHandlerWithoutMutatingTemplate()
        {
            var template = ScriptableObject.CreateInstance<TestHandlerModule>();
            var templateHandler = new TestModuleHandler();
            template.SetHandler(templateHandler);

            var clone = (TestHandlerModule)RuntimeObjectUtility.CloneModule(template);
            var cloneHandler = clone.GetAdditionalTickTarget();

            Assert.That(clone, Is.Not.SameAs(template));
            Assert.That(cloneHandler, Is.Not.Null);
            Assert.That(cloneHandler, Is.Not.SameAs(templateHandler));
            Assert.That(template.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.That(template.IsRuntimeInstance, Is.False);

            Object.DestroyImmediate(clone);
            Object.DestroyImmediate(template);
        }
    }
}
