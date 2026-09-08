using System;
using System.Linq;
using FrameWork_Ranger.Pooling.Editor;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using FrameWork_Ranger.ResourceManagement;
using NUnit.Framework;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Tests
{
    internal sealed class PoolingConfigurationValidatorTests
    {
        private FrameworkProjectSettings m_settings;
        private FrameworkGlobalConfig m_global;
        private FrameworkSceneConfig m_scene;
        private ResourceModule m_resource;
        private ReferencePoolModule m_reference;
        private GameObjectPoolModule m_gameObject;

        [TearDown]
        public void TearDown()
        {
            Destroy(m_gameObject);
            Destroy(m_reference);
            Destroy(m_resource);
            Destroy(m_scene);
            Destroy(m_global);
            Destroy(m_settings);
        }

        [Test]
        public void CorrectDualScopeConfiguration_HasNoErrors()
        {
            CreateValidConfiguration();

            var diagnostics = PoolingConfigurationValidator.Validate(m_settings);

            Assert.That(diagnostics.Any(item => item.Severity == PoolingDiagnosticSeverity.Error), Is.False);
            Assert.That(diagnostics.Any(item => item.Message.Contains("MaxRetained=-1")), Is.True);
        }

        [Test]
        public void ReversedScopes_AreRejected()
        {
            CreateValidConfiguration();
            m_global.SetModules(new[]
            {
                new ModuleConfigEntry(true, m_resource),
                new ModuleConfigEntry(true, m_gameObject),
            });
            m_scene.SetModules(new[] { new ModuleConfigEntry(true, m_reference) });

            var diagnostics = PoolingConfigurationValidator.Validate(m_settings);

            Assert.That(diagnostics.Any(item => item.Message.Contains("只允许存在于 SceneConfig")), Is.True);
            Assert.That(diagnostics.Any(item => item.Message.Contains("只允许存在于 GlobalConfig")), Is.True);
        }

        [Test]
        public void DuplicatePoolNameAndType_AreReported()
        {
            CreateValidConfiguration();
            var referenceHandler = new ReferencePoolHandler();
            var overrideEntry = new ReferencePoolTypeOverride(
                typeof(ConfigurationItem),
                new PoolCapacitySettings(0, 1, 4, 0f));
            referenceHandler.SetConfiguration(
                new PoolCapacitySettings(0, 1, 4, 0f),
                new[] { overrideEntry, overrideEntry });
            m_reference.SetHandler(referenceHandler);

            var gameObjectHandler = new GameObjectPoolHandler();
            var definition = CreateDefinition("Shared");
            gameObjectHandler.SetConfiguration(new[] { definition, definition });
            m_gameObject.SetHandler(gameObjectHandler);

            var diagnostics = PoolingConfigurationValidator.Validate(m_settings);

            Assert.That(diagnostics.Any(item => item.Message.Contains("重复配置了类型")), Is.True);
            Assert.That(diagnostics.Any(item => item.Message.Contains("重复配置了池名")), Is.True);
        }

        [Test]
        public void GameObjectModule_DeclaresOnlyResourceDependency()
        {
            m_gameObject = ScriptableObject.CreateInstance<GameObjectPoolModule>();
            m_gameObject.SetHandler(new GameObjectPoolHandler());

            var dependencies = m_gameObject.GetRequiredModuleTypes();

            Assert.That(dependencies, Is.EqualTo(new[] { typeof(ResourceModule) }));
        }

        [Test]
        public void InvalidResourceKey_IsReportedBeforeSceneLoad()
        {
            CreateValidConfiguration();
            var handler = new GameObjectPoolHandler();
            handler.SetConfiguration(new[]
            {
                new GameObjectPoolDefinition(
                    "Invalid Resources Key",
                    ResourceBackendKind.UnityResources,
                    "Resources/Pooling/Prefab.prefab",
                    PoolCapacitySettings.CreateGameObjectDefaults()),
            });
            m_gameObject.SetHandler(handler);

            var diagnostics = PoolingConfigurationValidator.Validate(m_settings);

            Assert.That(
                diagnostics.Any(item => item.Message.Contains("ResourceKey 无效")),
                Is.True);
        }

        private void CreateValidConfiguration()
        {
            m_resource = ScriptableObject.CreateInstance<ResourceModule>();
            m_reference = ScriptableObject.CreateInstance<ReferencePoolModule>();
            var referenceHandler = new ReferencePoolHandler();
            referenceHandler.SetConfiguration(
                PoolCapacitySettings.CreateReferenceDefaults(),
                Array.Empty<ReferencePoolTypeOverride>());
            m_reference.SetHandler(referenceHandler);

            m_gameObject = ScriptableObject.CreateInstance<GameObjectPoolModule>();
            var gameObjectHandler = new GameObjectPoolHandler();
            gameObjectHandler.SetConfiguration(new[] { CreateDefinition("ResourcesSample") });
            m_gameObject.SetHandler(gameObjectHandler);

            m_global = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            m_global.SetModules(new ModuleConfigEntry[]
            {
                new ModuleConfigEntry(true, m_resource),
                new ModuleConfigEntry(true, m_reference),
            });
            m_scene = ScriptableObject.CreateInstance<FrameworkSceneConfig>();
            m_scene.SetModules(new[] { new ModuleConfigEntry(true, m_gameObject) });
            m_settings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
            m_settings.SetGlobalConfig(m_global);
            m_settings.SetDefaultSceneConfig(m_scene);
        }

        private static GameObjectPoolDefinition CreateDefinition(string name)
        {
            return new GameObjectPoolDefinition(
                name,
                ResourceBackendKind.UnityResources,
                "Pooling/TestPrefab",
                PoolCapacitySettings.CreateGameObjectDefaults());
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        private sealed class ConfigurationItem : IReferencePoolItem
        {
            public ConfigurationItem()
            {
            }

            public void OnRent()
            {
            }

            public void OnReturn()
            {
            }
        }
    }
}
