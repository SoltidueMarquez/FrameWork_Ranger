using System;
using System.Collections.Generic;
using FrameWork_Ranger.Editor;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using FrameWork_Ranger.ResourceManagement;

namespace FrameWork_Ranger.Pooling.Editor
{
    /// <summary>
    /// 只读验证 Reference/GameObject 双 Module 的作用域、配置和 Resource 依赖。
    /// </summary>
    [FrameworkArchitecture(
        "对象池配置校验器",
        "验证双 Module 双作用域、类型与池名唯一性、ResourceKey、容量和无限缓存风险。",
        FrameworkArchitectureLayer.EditorIntegration,
        535,
        typeof(ReferencePoolModule),
        typeof(GameObjectPoolModule),
        typeof(ResourceModule),
        typeof(PoolingConfigurationDiagnostic))]
    internal static class PoolingConfigurationValidator
    {
        internal static IReadOnlyList<PoolingConfigurationDiagnostic> ValidateCurrentProject()
        {
            return Validate(FrameworkProjectSettingsAssetUtility.Load());
        }

        internal static IReadOnlyList<PoolingConfigurationDiagnostic> Validate(
            FrameworkProjectSettings settings)
        {
            var diagnostics = new List<PoolingConfigurationDiagnostic>();
            if (settings == null)
            {
                diagnostics.Add(Error("找不到固定 FrameworkProjectSettings。"));
                return diagnostics;
            }

            var globalReferences = FindModules<ReferencePoolModule>(settings.GlobalConfig);
            var globalGameObjects = FindModules<GameObjectPoolModule>(settings.GlobalConfig);
            var globalResources = FindModules<ResourceModule>(settings.GlobalConfig);
            if (globalReferences.Count == 0)
            {
                diagnostics.Add(Error(
                    "GlobalConfig 没有安装 ReferencePoolModule。",
                    settings.GlobalConfig));
            }
            else if (globalReferences.Count > 1)
            {
                diagnostics.Add(Error(
                    "GlobalConfig 重复安装了 ReferencePoolModule。",
                    settings.GlobalConfig));
            }

            if (globalGameObjects.Count > 0)
            {
                diagnostics.Add(Error(
                    "GlobalConfig 安装了 GameObjectPoolModule；该模块只允许存在于 SceneConfig。",
                    settings.GlobalConfig));
            }

            for (var i = 0; i < globalReferences.Count; i++)
            {
                ValidateReferenceModule(globalReferences[i], diagnostics);
            }

            var scenePoolCount = 0;
            var visited = new HashSet<ModuleConfigBase>();
            ValidateSceneConfig(
                settings.DefaultSceneConfig,
                "DefaultSceneConfig",
                globalResources.Count,
                diagnostics,
                visited,
                ref scenePoolCount);
            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                ValidateSceneConfig(
                    settings.SceneBindings[i]?.SceneConfig,
                    $"SceneBindings[{i}]",
                    globalResources.Count,
                    diagnostics,
                    visited,
                    ref scenePoolCount);
            }

            if (scenePoolCount == 0)
            {
                diagnostics.Add(Warning("当前没有任何 SceneConfig 安装 GameObjectPoolModule。"));
            }

            return diagnostics;
        }

        internal static ReferencePoolModule FindGlobalReferenceModule(
            FrameworkProjectSettings settings)
        {
            var modules = FindModules<ReferencePoolModule>(settings?.GlobalConfig);
            return modules.Count == 1 ? modules[0] : null;
        }

        internal static IReadOnlyList<GameObjectPoolModule> FindSceneGameObjectModules(
            FrameworkProjectSettings settings)
        {
            var result = new List<GameObjectPoolModule>();
            if (settings == null)
            {
                return result;
            }

            var visited = new HashSet<ModuleConfigBase>();
            AddSceneModules(settings.DefaultSceneConfig, visited, result);
            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                AddSceneModules(settings.SceneBindings[i]?.SceneConfig, visited, result);
            }

            return result;
        }

        private static void ValidateSceneConfig(
            ModuleConfigBase config,
            string location,
            int globalResourceCount,
            ICollection<PoolingConfigurationDiagnostic> diagnostics,
            ISet<ModuleConfigBase> visited,
            ref int scenePoolCount)
        {
            if (config == null || !visited.Add(config))
            {
                return;
            }

            var references = FindModules<ReferencePoolModule>(config);
            if (references.Count > 0)
            {
                diagnostics.Add(Error(
                    $"{location} 安装了 ReferencePoolModule；该模块只允许存在于 GlobalConfig。",
                    config));
            }

            var gameObjects = FindModules<GameObjectPoolModule>(config);
            scenePoolCount += gameObjects.Count;
            if (gameObjects.Count > 1)
            {
                diagnostics.Add(Error($"{location} 重复安装了 GameObjectPoolModule。", config));
            }

            if (gameObjects.Count > 0 && globalResourceCount != 1)
            {
                diagnostics.Add(Error(
                    $"{location} 的 GameObjectPoolModule 需要 GlobalConfig 恰好安装一个 ResourceModule。",
                    config));
            }

            for (var i = 0; i < gameObjects.Count; i++)
            {
                ValidateGameObjectModule(gameObjects[i], location, diagnostics);
            }
        }

        private static void ValidateReferenceModule(
            ReferencePoolModule module,
            ICollection<PoolingConfigurationDiagnostic> diagnostics)
        {
            if (!module.HasConfiguredHandler)
            {
                diagnostics.Add(Error("ReferencePoolModule 没有配置 Handler。", module));
                return;
            }

            var defaults = module.GetDefaultCapacity();
            ValidateCapacity("ReferencePool 默认容量", defaults, module, diagnostics);
            var overrides = module.GetTypeOverrides();
            if (overrides == null)
            {
                diagnostics.Add(Error("ReferencePoolModule 类型覆盖列表为空引用。", module));
                return;
            }

            var types = new HashSet<Type>();
            for (var i = 0; i < overrides.Count; i++)
            {
                var entry = overrides[i];
                if (entry == null)
                {
                    diagnostics.Add(Error($"ReferencePool TypeOverrides[{i}] 为空。", module));
                    continue;
                }

                if (!ReferencePoolRuntime.TryValidateItemType(entry.ItemType, out var typeError))
                {
                    diagnostics.Add(Error(
                        $"ReferencePool TypeOverrides[{i}] 类型无效：{typeError}",
                        module));
                }
                else if (!types.Add(entry.ItemType))
                {
                    diagnostics.Add(Error(
                        $"ReferencePool 重复配置了类型 {entry.ItemType.FullName}。",
                        module));
                }

                ValidateCapacity(
                    $"ReferencePool TypeOverrides[{i}]",
                    entry.Capacity,
                    module,
                    diagnostics);
            }
        }

        private static void ValidateGameObjectModule(
            GameObjectPoolModule module,
            string location,
            ICollection<PoolingConfigurationDiagnostic> diagnostics)
        {
            if (!module.HasConfiguredHandler)
            {
                diagnostics.Add(Error($"{location} 的 GameObjectPoolModule 没有配置 Handler。", module));
                return;
            }

            if (module.GetPrewarmBudgetPerFrame() <= 0)
            {
                diagnostics.Add(Error(
                    $"{location} 的 GameObjectPoolModule 预热预算必须大于 0。",
                    module));
            }

            var definitions = module.GetDefinitions();
            if (definitions == null)
            {
                diagnostics.Add(Error($"{location} 的 GameObject 池定义列表为空引用。", module));
                return;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    diagnostics.Add(Error($"{location} Definitions[{i}] 为空。", module));
                    continue;
                }

                if (!definition.TryValidate(out var error))
                {
                    diagnostics.Add(Error(
                        $"{location} Definitions[{i}] 无效：{error}",
                        module));
                }

                if (!string.IsNullOrEmpty(definition.PoolName) && !names.Add(definition.PoolName))
                {
                    diagnostics.Add(Error(
                        $"{location} 重复配置了池名 {definition.PoolName}。",
                        module));
                }

                ValidateCapacity(
                    $"{location} 的池 {definition.PoolName ?? $"Definitions[{i}]"}",
                    definition.Capacity,
                    module,
                    diagnostics);
            }
        }

        private static void ValidateCapacity(
            string location,
            PoolCapacitySettings capacity,
            UnityEngine.Object context,
            ICollection<PoolingConfigurationDiagnostic> diagnostics)
        {
            if (capacity == null)
            {
                diagnostics.Add(Error(
                    $"{location} 容量配置为空。",
                    context));
                return;
            }

            if (!capacity.TryValidate(out var error))
            {
                diagnostics.Add(Error($"{location} 容量无效：{error}", context));
                return;
            }

            if (capacity.MaxRetained == PoolCapacitySettings.UnlimitedRetained)
            {
                diagnostics.Add(Warning(
                    $"{location} 使用 MaxRetained=-1；长期峰值可能形成无限空闲缓存。",
                    context));
            }
        }

        private static List<TModule> FindModules<TModule>(ModuleConfigBase config)
            where TModule : ModuleBase
        {
            var modules = new List<TModule>();
            if (config == null)
            {
                return modules;
            }

            for (var i = 0; i < config.Modules.Count; i++)
            {
                var entry = config.Modules[i];
                if (entry?.Enabled == true && entry.Module is TModule module)
                {
                    modules.Add(module);
                }
            }

            return modules;
        }

        private static void AddSceneModules(
            ModuleConfigBase config,
            ISet<ModuleConfigBase> visited,
            ICollection<GameObjectPoolModule> result)
        {
            if (config == null || !visited.Add(config))
            {
                return;
            }

            var modules = FindModules<GameObjectPoolModule>(config);
            for (var i = 0; i < modules.Count; i++)
            {
                result.Add(modules[i]);
            }
        }

        private static PoolingConfigurationDiagnostic Error(
            string message,
            UnityEngine.Object context = null)
        {
            return new PoolingConfigurationDiagnostic(
                PoolingDiagnosticSeverity.Error,
                message,
                context);
        }

        private static PoolingConfigurationDiagnostic Warning(
            string message,
            UnityEngine.Object context = null)
        {
            return new PoolingConfigurationDiagnostic(
                PoolingDiagnosticSeverity.Warning,
                message,
                context);
        }
    }
}
