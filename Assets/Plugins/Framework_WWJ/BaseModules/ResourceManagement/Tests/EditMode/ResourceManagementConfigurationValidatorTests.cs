using System;
using System.Linq;
using Framework_WWJ.ResourceManagement.Addressables;
using Framework_WWJ.ResourceManagement.Editor;
using Framework_WWJ.ResourceManagement.UnityResources;
using NUnit.Framework;
using UnityEngine;

namespace Framework_WWJ.ResourceManagement.Tests
{
    internal sealed class ResourceManagementConfigurationValidatorTests
    {
        private FrameworkProjectSettings m_settings;
        private FrameworkGlobalConfig m_global;
        private FrameworkSceneConfig m_scene;
        private ResourceModule m_module;

        [TearDown]
        public void TearDown()
        {
            Destroy(m_module);
            Destroy(m_scene);
            Destroy(m_global);
            Destroy(m_settings);
        }

        [Test]
        public void GlobalModuleWithBothProviders_IsValid()
        {
            CreateConfiguration(CreateModule(
                new UnityResourcesProvider(),
                new AddressablesResourceProvider()));

            var diagnostics = ResourceManagementConfigurationValidator.Validate(m_settings, true);

            Assert.That(diagnostics.Any(item =>
                item.Severity == ResourceManagementDiagnosticSeverity.Error), Is.False);
        }

        [Test]
        public void SceneInstallation_IsRejected()
        {
            CreateConfiguration(CreateModule(
                new UnityResourcesProvider(),
                new AddressablesResourceProvider()));
            m_scene.SetModules(new[] { new ModuleConfigEntry(true, m_module) });

            var diagnostics = ResourceManagementConfigurationValidator.Validate(m_settings, true);

            Assert.That(diagnostics.Any(item => item.Message.Contains("只允许存在于 GlobalConfig")), Is.True);
        }

        [Test]
        public void MissingAndDuplicateProviders_AreReported()
        {
            CreateConfiguration(CreateModule(
                new UnityResourcesProvider(),
                new UnityResourcesProvider()));

            var diagnostics = ResourceManagementConfigurationValidator.Validate(m_settings, true);

            Assert.That(diagnostics.Any(item => item.Message.Contains("重复配置")), Is.True);
            Assert.That(diagnostics.Any(item => item.Message.Contains("缺少必须的 Addressables")), Is.True);
        }

        [Test]
        public void MissingAddressablesSettings_IsReportedWithoutChangingAssets()
        {
            CreateConfiguration(CreateModule(
                new UnityResourcesProvider(),
                new AddressablesResourceProvider()));

            var diagnostics = ResourceManagementConfigurationValidator.Validate(m_settings, false);

            Assert.That(diagnostics.Any(item => item.Message.Contains("Addressables Settings 尚未创建")), Is.True);
        }

        private ResourceModule CreateModule(params ResourceProviderBase[] providers)
        {
            m_module = ScriptableObject.CreateInstance<ResourceModule>();
            var handler = new ResourceHandler();
            handler.SetProviders(providers);
            m_module.SetHandler(handler);
            return m_module;
        }

        private void CreateConfiguration(ResourceModule module)
        {
            m_global = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            m_global.SetModules(new[] { new ModuleConfigEntry(true, module) });
            m_scene = ScriptableObject.CreateInstance<FrameworkSceneConfig>();
            m_scene.SetModules(Array.Empty<ModuleConfigEntry>());
            m_settings = ScriptableObject.CreateInstance<FrameworkProjectSettings>();
            m_settings.SetGlobalConfig(m_global);
            m_settings.SetDefaultSceneConfig(m_scene);
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value != null)
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
