using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FrameWork_Ranger.Tests
{
    internal sealed class FrameworkProjectSettingsResolverTests
    {
        private readonly List<Object> m_createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = m_createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(m_createdObjects[i]);
            }

            m_createdObjects.Clear();
        }

        [Test]
        public void Resolve_ExactSceneOverrideWinsOverDefault()
        {
            var global = Create<FrameworkGlobalConfig>();
            var defaultScene = Create<FrameworkSceneConfig>();
            var overrideScene = Create<FrameworkSceneConfig>();
            var settings = Create<FrameworkProjectSettings>();
            var binding = new FrameworkSceneBinding();
            binding.SetScene("scene-guid", "Assets/Scenes/Target.unity", overrideScene);
            settings.SetGlobalConfig(global);
            settings.SetDefaultSceneConfig(defaultScene);
            settings.SetSceneBindings(new[] { binding });

            var result = FrameworkProjectSettingsResolver.Resolve(
                settings,
                "Assets\\Scenes\\Target.unity");

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.SceneConfig, Is.SameAs(overrideScene));
            Assert.That(result.UsesSceneOverride, Is.True);
        }

        [Test]
        public void Resolve_UnmappedSceneUsesDefaultOrLegalEmptyScope()
        {
            var global = Create<FrameworkGlobalConfig>();
            var defaultScene = Create<FrameworkSceneConfig>();
            var settings = Create<FrameworkProjectSettings>();
            settings.SetGlobalConfig(global);
            settings.SetDefaultSceneConfig(defaultScene);

            var defaultResult = FrameworkProjectSettingsResolver.Resolve(settings, "Assets/Scenes/Other.unity");
            Assert.That(defaultResult.IsValid, Is.True);
            Assert.That(defaultResult.SceneConfig, Is.SameAs(defaultScene));

            settings.SetDefaultSceneConfig(null);
            var emptyResult = FrameworkProjectSettingsResolver.Resolve(settings, "Assets/Scenes/Other.unity");
            Assert.That(emptyResult.IsValid, Is.True);
            Assert.That(emptyResult.SceneConfig, Is.Null);
            Assert.That(ModuleGraphResolver.Resolve(global, null).IsValid, Is.True);
        }

        [Test]
        public void Resolve_RejectsMissingGlobalAndInvalidBinding()
        {
            var settings = Create<FrameworkProjectSettings>();
            settings.SetSceneBindings(new[] { new FrameworkSceneBinding() });

            var result = FrameworkProjectSettingsResolver.Resolve(settings, string.Empty);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Exists(FrameworkProjectSettingsDiagnosticCode.MissingGlobalConfig), Is.True);
            Assert.That(result.Diagnostics.Exists(FrameworkProjectSettingsDiagnosticCode.MissingSceneGuid), Is.True);
            Assert.That(result.Diagnostics.Exists(FrameworkProjectSettingsDiagnosticCode.MissingScenePath), Is.True);
            Assert.That(result.Diagnostics.Exists(FrameworkProjectSettingsDiagnosticCode.MissingSceneConfig), Is.True);
        }

        [Test]
        public void Resolve_RejectsDuplicateSceneIdentity()
        {
            var settings = Create<FrameworkProjectSettings>();
            settings.SetGlobalConfig(Create<FrameworkGlobalConfig>());
            var sceneConfig = Create<FrameworkSceneConfig>();
            var first = new FrameworkSceneBinding();
            first.SetScene("same-guid", "Assets/Scenes/Same.unity", sceneConfig);
            var second = new FrameworkSceneBinding();
            second.SetScene("same-guid", "Assets/Scenes/Same.unity", sceneConfig);
            settings.SetSceneBindings(new[] { first, second });

            var result = FrameworkProjectSettingsResolver.Resolve(settings, "Assets/Scenes/Same.unity");

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Exists(FrameworkProjectSettingsDiagnosticCode.DuplicateSceneGuid), Is.True);
            Assert.That(result.Diagnostics.Exists(FrameworkProjectSettingsDiagnosticCode.DuplicateScenePath), Is.True);
        }

        private T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            m_createdObjects.Add(instance);
            return instance;
        }
    }

    internal static class ProjectSettingsDiagnosticTestExtensions
    {
        internal static bool Exists(
            this IReadOnlyList<FrameworkProjectSettingsDiagnostic> diagnostics,
            FrameworkProjectSettingsDiagnosticCode code)
        {
            for (var i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
