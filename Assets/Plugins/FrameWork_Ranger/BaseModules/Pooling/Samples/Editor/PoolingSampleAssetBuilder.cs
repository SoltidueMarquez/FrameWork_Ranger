using System;
using System.Collections.Generic;
using System.Linq;
using FrameWork_Ranger.Editor;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using FrameWork_Ranger.ResourceManagement;
using FrameWork_Ranger.ResourceManagement.Addressables;
using FrameWork_Ranger.ResourceManagement.UnityResources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Samples.Editor
{
    /// <summary>
    /// 追加式创建 Pooling 独立场景、双后端 Prefab、双 Module 模板与场景绑定。
    /// </summary>
    internal static class PoolingSampleAssetBuilder
    {
        internal const string RootDirectory =
            "Assets/Plugins/FrameWork_Ranger/BaseModules/Pooling/Samples";
        internal const string ReferenceModulePath = RootDirectory + "/Configs/ReferencePoolModule.asset";
        internal const string GameObjectModulePath = RootDirectory + "/Configs/GameObjectPoolModule.asset";
        internal const string SceneConfigPath = RootDirectory + "/Configs/PoolingSceneConfig.asset";
        internal const string AddressablesPrefabPath = RootDirectory + "/Prefabs/AddressablesPoolPrefab.prefab";
        internal const string ResourcesPrefabPath =
            RootDirectory + "/Runtime/Resources/FrameWork_Ranger/Pooling/ResourcesPoolPrefab.prefab";
        internal const string SampleScenePath = RootDirectory + "/Scenes/PoolingSample.unity";
        internal const string AddressablesGroupName = "FrameWork_Ranger Pooling Samples";
        internal const string AddressablesAddress =
            "framework-ranger/samples/pooling/addressables-prefab";

        private const string GlobalConfigPath =
            "Assets/Plugins/FrameWork_Ranger/Resources/FrameworkGlobalConfig.asset";
        private const string ResourceModuleFallbackPath =
            "Assets/Plugins/FrameWork_Ranger/BaseModules/ResourceManagement/Samples/Configs/ResourceModule.asset";

        internal static void Build()
        {
            Build(true);
        }

        internal static void BuildWithoutPrompt()
        {
            Build(false);
        }

        public static void BuildFromCommandLine()
        {
            Build(false);
        }

        private static void Build(bool confirmOpenSceneSave)
        {
            if (confirmOpenSceneSave &&
                !Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureAllFolders();
            var previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
                var global = EnsureGlobalConfig(settings);
                var resource = FindEnabledModule<ResourceModule>(global) ?? EnsureResourceModule();
                var reference = ConfigureReferenceModule();
                var gameObject = ConfigureGameObjectModule();
                var sceneConfig = ConfigureSceneConfig(gameObject);

                CreateSamplePrefab(
                    ResourcesPrefabPath,
                    "Pooling Resources Prefab",
                    new Color(0.25f, 0.75f, 1f, 1f));
                CreateSamplePrefab(
                    AddressablesPrefabPath,
                    "Pooling Addressables Prefab",
                    new Color(1f, 0.55f, 0.2f, 1f));
                ConfigureAddressables();
                ConfigureGlobal(settings, global, resource, reference);

                // 打开或新建场景会触发资源卸载，必须先提交当前 ScriptableObject 变更，
                // 否则局部变量可能变成 Unity 的“已销毁对象”引用。
                EditorUtility.SetDirty(reference);
                EditorUtility.SetDirty(gameObject);
                EditorUtility.SetDirty(sceneConfig);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();

                ConfigureSampleScene();

                settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
                sceneConfig = AssetDatabase.LoadAssetAtPath<FrameworkSceneConfig>(SceneConfigPath);
                if (sceneConfig == null)
                {
                    throw new InvalidOperationException($"无法重新加载 Pooling SceneConfig：{SceneConfigPath}");
                }

                ConfigureSceneBinding(settings, sceneConfig);
                EnsureSampleBuildSettingsScene();

                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[FrameWork_Ranger] Pooling 双 Module 独立示例资产已刷新。" );
            }
            finally
            {
                RestorePreviousSceneSetup(previousSceneSetup);
            }
        }

        private static ReferencePoolModule ConfigureReferenceModule()
        {
            var module = LoadOrCreate<ReferencePoolModule>(ReferenceModulePath);
            var handler = new ReferencePoolHandler();
            handler.SetConfiguration(
                PoolCapacitySettings.CreateReferenceDefaults(),
                new[]
                {
                    new ReferencePoolTypeOverride(
                        typeof(PoolingSampleReferencePayload),
                        new PoolCapacitySettings(5, 5, -1, 15f)),
                });
            module.SetHandler(handler);
            return module;
        }

        private static GameObjectPoolModule ConfigureGameObjectModule()
        {
            var module = LoadOrCreate<GameObjectPoolModule>(GameObjectModulePath);
            var handler = new GameObjectPoolHandler();
            handler.SetConfiguration(new[]
            {
                new GameObjectPoolDefinition(
                    "Resources Prefab",
                    ResourceBackendKind.UnityResources,
                    "FrameWork_Ranger/Pooling/ResourcesPoolPrefab",
                    PoolCapacitySettings.CreateGameObjectDefaults()),
                new GameObjectPoolDefinition(
                    "Addressables Prefab",
                    ResourceBackendKind.Addressables,
                    AddressablesAddress,
                    PoolCapacitySettings.CreateGameObjectDefaults()),
            }, GameObjectPoolHandler.DefaultPrewarmBudgetPerFrame);
            module.SetHandler(handler);
            return module;
        }

        private static FrameworkSceneConfig ConfigureSceneConfig(GameObjectPoolModule module)
        {
            var config = LoadOrCreate<FrameworkSceneConfig>(SceneConfigPath);
            config.SetModules(new[] { new ModuleConfigEntry(true, module) });
            return config;
        }

        private static ResourceModule EnsureResourceModule()
        {
            var module = LoadOrCreate<ResourceModule>(ResourceModuleFallbackPath);
            if (!module.HasConfiguredHandler)
            {
                var handler = new ResourceHandler();
                handler.SetProviders(new ResourceProviderBase[]
                {
                    new UnityResourcesProvider(),
                    new AddressablesResourceProvider(),
                });
                module.SetHandler(handler);
                EditorUtility.SetDirty(module);
            }

            return module;
        }

        private static FrameworkGlobalConfig EnsureGlobalConfig(FrameworkProjectSettings settings)
        {
            var global = settings.GlobalConfig ??
                         AssetDatabase.LoadAssetAtPath<FrameworkGlobalConfig>(GlobalConfigPath);
            if (global == null)
            {
                global = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
                AssetDatabase.CreateAsset(global, GlobalConfigPath);
            }

            return global;
        }

        private static void ConfigureGlobal(
            FrameworkProjectSettings settings,
            FrameworkGlobalConfig global,
            ResourceModule resource,
            ReferencePoolModule reference)
        {
            var entries = new List<ModuleConfigEntry>(global.Modules);
            if (!entries.Any(entry => entry?.Enabled == true && entry.Module is ResourceModule))
            {
                entries.Add(new ModuleConfigEntry(true, resource));
            }

            if (!entries.Any(entry => entry?.Enabled == true && entry.Module is ReferencePoolModule))
            {
                entries.Add(new ModuleConfigEntry(true, reference));
            }

            global.SetModules(entries);
            settings.SetGlobalConfig(global);
            EditorUtility.SetDirty(global);
        }

        private static void ConfigureSceneBinding(
            FrameworkProjectSettings settings,
            FrameworkSceneConfig sceneConfig)
        {
            var sceneGuid = AssetDatabase.AssetPathToGUID(SampleScenePath);
            var bindings = settings.SceneBindings
                .Where(binding => binding != null &&
                                  !string.Equals(binding.SceneGuid, sceneGuid, StringComparison.Ordinal) &&
                                  !string.Equals(binding.ScenePath, SampleScenePath, StringComparison.Ordinal))
                .ToList();
            var binding = new FrameworkSceneBinding();
            binding.SetScene(sceneGuid, SampleScenePath, sceneConfig);
            bindings.Add(binding);
            settings.SetSceneBindings(bindings);
        }

        private static void ConfigureAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(AddressablesGroupName) ?? settings.CreateGroup(
                AddressablesGroupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
            var entry = settings.CreateOrMoveEntry(
                AssetDatabase.AssetPathToGUID(AddressablesPrefabPath),
                group,
                false,
                false);
            entry.address = AddressablesAddress;
            EditorUtility.SetDirty(settings);
        }

        private static void CreateSamplePrefab(string path, string displayName, Color color)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                instance.name = displayName;
                instance.transform.localScale = new Vector3(1f, 1.25f, 1f);
                instance.AddComponent<PoolingSampleCallbacks>().ConfigureSample("Root", color);
                var child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                child.name = "Callback Child";
                child.transform.SetParent(instance.transform, false);
                child.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                child.transform.localScale = Vector3.one * 0.35f;
                child.AddComponent<PoolingSampleCallbacks>().ConfigureSample("Child", color);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ConfigureSampleScene()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
            var scene = existing == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            var hasView = scene.GetRootGameObjects()
                .Any(root => root.GetComponentInChildren<PoolingSampleView>(true) != null);
            if (!hasView)
            {
                new GameObject("PoolingAcceptance").AddComponent<PoolingSampleView>();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (!scene.GetRootGameObjects().Any(root => root.GetComponent<Camera>() != null))
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetPositionAndRotation(
                    new Vector3(0f, 2f, -8f),
                    Quaternion.Euler(10f, 0f, 0f));
                cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (!scene.GetRootGameObjects().Any(root => root.GetComponent<Light>() != null))
            {
                var lightObject = new GameObject("Directional Light");
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                EditorSceneManager.MarkSceneDirty(scene);
            }

            EditorSceneManager.SaveScene(scene, SampleScenePath);
        }

        private static void EnsureSampleBuildSettingsScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene =>
                string.Equals(scene.path, SampleScenePath, StringComparison.Ordinal));
            scenes.Insert(0, new EditorBuildSettingsScene(SampleScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T FindEnabledModule<T>(ModuleConfigBase config) where T : ModuleBase
        {
            if (config == null)
            {
                return null;
            }

            for (var i = 0; i < config.Modules.Count; i++)
            {
                if (config.Modules[i]?.Enabled == true && config.Modules[i].Module is T module)
                {
                    return module;
                }
            }

            return null;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureAllFolders()
        {
            EnsureFolder(RootDirectory + "/Configs");
            EnsureFolder(RootDirectory + "/Prefabs");
            EnsureFolder(RootDirectory + "/Scenes");
            EnsureFolder(RootDirectory + "/Runtime/Resources/FrameWork_Ranger/Pooling");
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static void RestorePreviousSceneSetup(SceneSetup[] previousSceneSetup)
        {
            if (previousSceneSetup != null && previousSceneSetup.Any(setup => !string.IsNullOrEmpty(setup.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
