using System;
using System.Collections.Generic;
using Framework_WWJ.Editor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Framework_WWJ.ResourceManagement.Editor
{
    /// <summary>
    /// 只读验证 ResourceModule 的作用域、双 Provider 与 Addressables 项目配置。
    /// </summary>
    internal static class ResourceManagementConfigurationValidator
    {
        internal static IReadOnlyList<ResourceManagementConfigurationDiagnostic> ValidateCurrentProject()
        {
            return Validate(FrameworkProjectSettingsAssetUtility.Load());
        }

        internal static IReadOnlyList<ResourceManagementConfigurationDiagnostic> Validate(
            FrameworkProjectSettings settings)
        {
            return Validate(settings, AddressableAssetSettingsDefaultObject.Settings != null);
        }

        internal static IReadOnlyList<ResourceManagementConfigurationDiagnostic> Validate(
            FrameworkProjectSettings settings,
            bool addressablesSettingsExist)
        {
            var diagnostics = new List<ResourceManagementConfigurationDiagnostic>();
            if (settings == null)
            {
                diagnostics.Add(Error("找不到固定 FrameworkProjectSettings。"));
                return diagnostics;
            }

            var globalModules = FindResourceModules(settings.GlobalConfig);
            if (globalModules.Count == 0)
            {
                diagnostics.Add(Error("GlobalConfig 没有安装 ResourceModule。", settings.GlobalConfig));
            }
            else if (globalModules.Count > 1)
            {
                diagnostics.Add(Error("GlobalConfig 重复安装了 ResourceModule。", settings.GlobalConfig));
            }

            for (var i = 0; i < globalModules.Count; i++)
            {
                ValidateProviders(globalModules[i], diagnostics);
            }

            ValidateSceneConfig(settings.DefaultSceneConfig, "DefaultSceneConfig", diagnostics);
            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                ValidateSceneConfig(
                    settings.SceneBindings[i]?.SceneConfig,
                    $"SceneBindings[{i}]",
                    diagnostics);
            }

            if (!addressablesSettingsExist)
            {
                diagnostics.Add(Error("Addressables Settings 尚未创建。"));
            }

            return diagnostics;
        }

        internal static ResourceModule FindGlobalModule(FrameworkProjectSettings settings)
        {
            var modules = FindResourceModules(settings?.GlobalConfig);
            return modules.Count == 1 ? modules[0] : null;
        }

        private static List<ResourceModule> FindResourceModules(ModuleConfigBase config)
        {
            var modules = new List<ResourceModule>();
            if (config == null)
            {
                return modules;
            }

            for (var i = 0; i < config.Modules.Count; i++)
            {
                var entry = config.Modules[i];
                if (entry?.Enabled == true && entry.Module is ResourceModule module)
                {
                    modules.Add(module);
                }
            }

            return modules;
        }

        private static void ValidateProviders(
            ResourceModule module,
            ICollection<ResourceManagementConfigurationDiagnostic> diagnostics)
        {
            var providers = module.GetConfiguredProviders();
            var backends = new HashSet<ResourceBackendKind>();
            for (var i = 0; i < providers.Count; i++)
            {
                var provider = providers[i];
                if (provider == null)
                {
                    diagnostics.Add(Error($"ResourceModule Providers[{i}] 为空。", module));
                    continue;
                }

                if (!backends.Add(provider.Backend))
                {
                    diagnostics.Add(Error($"ResourceModule 重复配置了 {provider.Backend} Provider。", module));
                }
            }

            foreach (ResourceBackendKind backend in Enum.GetValues(typeof(ResourceBackendKind)))
            {
                if (!backends.Contains(backend))
                {
                    diagnostics.Add(Error($"ResourceModule 缺少必须的 {backend} Provider。", module));
                }
            }
        }

        private static void ValidateSceneConfig(
            ModuleConfigBase config,
            string location,
            ICollection<ResourceManagementConfigurationDiagnostic> diagnostics)
        {
            var modules = FindResourceModules(config);
            if (modules.Count > 0)
            {
                diagnostics.Add(Error(
                    $"{location} 安装了 ResourceModule；该模块只允许存在于 GlobalConfig。",
                    config));
            }
        }

        private static ResourceManagementConfigurationDiagnostic Error(
            string message,
            UnityEngine.Object context = null)
        {
            return new ResourceManagementConfigurationDiagnostic(
                ResourceManagementDiagnosticSeverity.Error,
                message,
                context);
        }
    }
}
