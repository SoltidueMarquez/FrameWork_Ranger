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

namespace Framework_WWJ.ResourceManagement.Samples.Editor
{
    /// <summary>
    /// 创建双后端 Resource 验收资产，并将默认场景配置为唯一验收入口。
    /// </summary>
    internal static class ResourceManagementSampleAssetBuilder
    {
        internal const string RootDirectory =
            "Assets/Plugins/Framework_WWJ/BaseModules/ResourceManagement/Samples";
        internal const string ModulePath = RootDirectory + "/Configs/ResourceModule.asset";
        internal const string AddressablesPrefabPath = RootDirectory + "/Prefabs/AddressablesSamplePrefab.prefab";
        internal const string ResourcesPrefabPath =
            RootDirectory + "/Runtime/Resources/Framework_WWJ/ResourceManagement/ResourcesSamplePrefab.prefab";
        internal const string SampleScenePath = RootDirectory + "/Scenes/ResourceManagementSample.unity";
        internal const string AddressablesGroupName = "Framework_WWJ ResourceManagement Samples";
        internal const string AddressablesAddress =
            "framework-wwj/samples/resource-management/addressables-prefab";

        private const string GlobalConfigPath =
            "Assets/Plugins/Framework_WWJ/Resources/FrameworkGlobalConfig.asset";
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

                CreateSamplePrefab(AddressablesPrefabPath, "Addressables Sample Prefab", new Vector3(1f, 1f, 1f));
                CreateSamplePrefab(ResourcesPrefabPath, "Resources Sample Prefab", new Vector3(1f, 1.5f, 1f));
                ConfigureAddressables();
                ConfigureGlobal(module);
                ConfigureProjectSettings();
                ConfigureSampleScene();
                EnsureSampleBuildSettingsScene();

                module = AssetDatabase.LoadAssetAtPath<ResourceModule>(ModulePath);
                if (module != null)
                {
                    EditorUtility.SetDirty(module);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Framework_WWJ] Resource Management 双后端验收资产已刷新到模块示例场景。" );
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
                               AssetDatabase.LoadAssetAtPath<FrameworkGlobalConfig>(GlobalConfigPath);
            if (globalConfig == null)
            {
                globalConfig = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
                AssetDatabase.CreateAsset(globalConfig, GlobalConfigPath);
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

        private static void ConfigureProjectSettings()
        {
            var settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
            var validBindings = settings.SceneBindings
                .Where(binding =>
                    binding != null &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(binding.ScenePath) != null)
                .ToList();
            settings.SetSceneBindings(validBindings);
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

        private static void ConfigureSampleScene()
        {
            var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            var hasAcceptanceView = scene.GetRootGameObjects()
                .Any(root => root.GetComponentInChildren<ResourceManagementSampleView>(true) != null);
            if (!hasAcceptanceView)
            {
                new GameObject("ResourceManagementAcceptance").AddComponent<ResourceManagementSampleView>();
                EditorSceneManager.MarkSceneDirty(scene);
            }

            EditorSceneManager.SaveScene(scene, SampleScenePath);
        }

        private static void EnsureSampleBuildSettingsScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene =>
                string.Equals(scene.path, SampleScenePath, StringComparison.Ordinal) ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null);
            scenes.Insert(0, new EditorBuildSettingsScene(SampleScenePath, true));
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
