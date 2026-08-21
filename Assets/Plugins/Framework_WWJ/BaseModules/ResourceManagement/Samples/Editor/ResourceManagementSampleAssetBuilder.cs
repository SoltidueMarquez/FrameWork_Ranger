using System;
using System.Collections.Generic;
using System.Linq;
using Framework_WWJ.Editor;
using Framework_WWJ.ResourceManagement.Addressables;
using Framework_WWJ.ResourceManagement.Samples;
using Framework_WWJ.ResourceManagement.UnityResources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ.ResourceManagement.Samples.Editor
{
    /// <summary>
    /// 创建双后端 Resource 示例资产，并以追加方式接入既有中央配置。
    /// </summary>
    internal static class ResourceManagementSampleAssetBuilder
    {
        internal const string RootDirectory =
            "Assets/Plugins/Framework_WWJ/BaseModules/ResourceManagement/Samples";
        internal const string ModulePath = RootDirectory + "/Configs/ResourceModule.asset";
        internal const string SceneConfigPath = RootDirectory + "/Configs/ResourceManagementSampleSceneConfig.asset";
        internal const string AddressablesPrefabPath = RootDirectory + "/Prefabs/AddressablesSamplePrefab.prefab";
        internal const string ResourcesPrefabPath =
            RootDirectory + "/Runtime/Resources/Framework_WWJ/ResourceManagement/ResourcesSamplePrefab.prefab";
        internal const string ScenePath = RootDirectory + "/Scenes/ResourceManagementSample.unity";
        internal const string AddressablesGroupName = "Framework_WWJ ResourceManagement Samples";
        internal const string AddressablesAddress =
            "framework-wwj/samples/resource-management/addressables-prefab";

        private const string CoreGlobalConfigPath =
            "Assets/Plugins/Framework_WWJ/Samples/CoreSkeleton/Configs/FrameworkGlobalConfig.asset";

        internal static void Build()
        {
            Build(true);
        }

        internal static void BuildWithoutPrompt()
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
                var module = LoadOrCreate<ResourceModule>(ModulePath, out var moduleCreated);
                if (moduleCreated || !module.HasConfiguredHandler)
                {
                    var handler = new ResourceHandler();
                    handler.SetProviders(new ResourceProviderBase[]
                    {
                        new UnityResourcesProvider(),
                        new AddressablesResourceProvider(),
                    });
                    module.SetHandler(handler);
                }

                var sceneConfig = LoadOrCreate<FrameworkSceneConfig>(SceneConfigPath, out _);
                sceneConfig.SetModules(Array.Empty<ModuleConfigEntry>());
                CreateSamplePrefab(AddressablesPrefabPath, "Addressables Sample Prefab", new Vector3(1f, 1f, 1f));
                CreateSamplePrefab(ResourcesPrefabPath, "Resources Sample Prefab", new Vector3(1f, 1.5f, 1f));
                ConfigureAddressables();
                ConfigureGlobal(module);
                BuildScene();
                module = AssetDatabase.LoadAssetAtPath<ResourceModule>(ModulePath);
                sceneConfig = AssetDatabase.LoadAssetAtPath<FrameworkSceneConfig>(SceneConfigPath);
                ConfigureProjectSettings(sceneConfig);
                AppendBuildSettingsScene();

                if (module != null)
                {
                    EditorUtility.SetDirty(module);
                }

                if (sceneConfig != null)
                {
                    EditorUtility.SetDirty(sceneConfig);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Framework_WWJ] Resource Management 双后端示例已生成。" );
            }
            finally
            {
                RestorePreviousSceneSetup(previousSceneSetup);
            }
        }

        public static void BuildFromCommandLine()
        {
            Build(false);
        }

        private static void ConfigureGlobal(ResourceModule module)
        {
            var settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
            var globalConfig = settings.GlobalConfig ??
                               AssetDatabase.LoadAssetAtPath<FrameworkGlobalConfig>(CoreGlobalConfigPath);
            if (globalConfig == null)
            {
                globalConfig = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
                AssetDatabase.CreateAsset(globalConfig, CoreGlobalConfigPath);
            }

            var entries = globalConfig.Modules
                .Where(entry => entry?.Module != null && !(entry.Module is ResourceModule))
                .ToList();
            entries.Add(new ModuleConfigEntry(true, module));
            globalConfig.SetModules(entries);
            settings.SetGlobalConfig(globalConfig);
            EditorUtility.SetDirty(globalConfig);
            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureProjectSettings(FrameworkSceneConfig sceneConfig)
        {
            var settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
            var bindings = settings.SceneBindings
                .Where(binding => binding != null && binding.ScenePath != ScenePath)
                .ToList();
            var resourceBinding = new FrameworkSceneBinding();
            resourceBinding.SetScene(
                AssetDatabase.AssetPathToGUID(ScenePath),
                ScenePath,
                sceneConfig);
            bindings.Add(resourceBinding);
            settings.SetSceneBindings(bindings);
            EditorUtility.SetDirty(settings);
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

        private static void CreateSamplePrefab(string path, string displayName, Vector3 scale)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                instance.name = displayName;
                instance.transform.localScale = scale;
                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ResourceManagementSampleView").AddComponent<ResourceManagementSampleView>();
            new GameObject("Framework_WWJ Resource Management Sample").transform.SetSiblingIndex(0);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void AppendBuildSettingsScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != ScenePath)
                {
                    continue;
                }

                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (!created)
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
            EnsureFolder(RootDirectory + "/Runtime/Resources/Framework_WWJ/ResourceManagement");
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
