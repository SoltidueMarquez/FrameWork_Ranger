using System.Collections.Generic;
using System.Linq;
using Framework_WWJ.Editor;
using Framework_WWJ.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ.Samples.Editor
{
    /// <summary>
    /// 创建或刷新第一阶段骨架示例资产。构建场景时使用 Additive 临时场景，不修改当前打开场景。
    /// </summary>
    internal static class CoreSkeletonSampleAssetBuilder
    {
        private const string ConfigDirectory = "Assets/Plugins/Framework_WWJ/Samples/CoreSkeleton/Configs";
        private const string SceneDirectory = "Assets/Plugins/Framework_WWJ/Samples/CoreSkeleton/Scenes";
        private const string SceneAPath = SceneDirectory + "/CoreSkeleton_A.unity";
        private const string SceneBPath = SceneDirectory + "/CoreSkeleton_B.unity";

        /// <summary>
        /// 从 Framework Center 显式重新生成示例配置、中央映射与场景。
        /// </summary>
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            var globalModule = LoadOrCreate<SampleGlobalClockModule>(
                ConfigDirectory + "/SampleGlobalClockModule.asset");
            var sceneModuleA = LoadOrCreate<SampleSceneModule>(
                ConfigDirectory + "/SampleSceneModule_A.asset");
            var sceneModuleB = LoadOrCreate<SampleSceneModule>(
                ConfigDirectory + "/SampleSceneModule_B.asset");
            sceneModuleA.SetHandler(new SampleCounterHandler());
            sceneModuleB.SetHandler(new SamplePulseHandler());

            var globalConfig = LoadOrCreate<FrameworkGlobalConfig>(
                ConfigDirectory + "/FrameworkGlobalConfig.asset");
            globalConfig.SetModules(new List<ModuleConfigEntry>
            {
                new ModuleConfigEntry(true, globalModule),
            });

            var sceneConfigA = LoadOrCreate<FrameworkSceneConfig>(
                ConfigDirectory + "/FrameworkSceneAConfig.asset");
            sceneConfigA.SetModules(new List<ModuleConfigEntry>
            {
                new ModuleConfigEntry(true, sceneModuleA),
            });

            var sceneConfigB = LoadOrCreate<FrameworkSceneConfig>(
                ConfigDirectory + "/FrameworkSceneBConfig.asset");
            sceneConfigB.SetModules(new List<ModuleConfigEntry>
            {
                new ModuleConfigEntry(true, sceneModuleB),
            });

            EditorUtility.SetDirty(globalModule);
            EditorUtility.SetDirty(sceneModuleA);
            EditorUtility.SetDirty(sceneModuleB);
            EditorUtility.SetDirty(globalConfig);
            EditorUtility.SetDirty(sceneConfigA);
            EditorUtility.SetDirty(sceneConfigB);
            AssetDatabase.SaveAssets();

            try
            {
                BuildScene(SceneAPath, "Framework_WWJ Core Skeleton - Scene A");
                BuildScene(SceneBPath, "Framework_WWJ Core Skeleton - Scene B");
                AppendBuildSettingsScenes();
                ConfigureProjectSettings(globalConfig, sceneConfigA, sceneConfigB);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Framework_WWJ] Phase 1.1 中央启动示例资产与场景已经生成。" );
            }
            finally
            {
                RestorePreviousSceneSetup(previousSceneSetup);
            }
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

        private static void BuildScene(
            string path,
            string displayName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var viewObject = new GameObject("CoreSkeletonSampleView");
            viewObject.AddComponent<CoreSkeletonSampleView>();

            var labelObject = new GameObject(displayName);
            labelObject.transform.SetSiblingIndex(0);

            EditorSceneManager.SaveScene(scene, path);
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

        private static void ConfigureProjectSettings(
            FrameworkGlobalConfig globalConfig,
            FrameworkSceneConfig sceneConfigA,
            FrameworkSceneConfig sceneConfigB)
        {
            var settings = FrameworkProjectSettingsAssetUtility.CreateOrLoad();
            var bindingA = new FrameworkSceneBinding();
            bindingA.SetScene(
                AssetDatabase.AssetPathToGUID(SceneAPath),
                SceneAPath,
                sceneConfigA);
            var bindingB = new FrameworkSceneBinding();
            bindingB.SetScene(
                AssetDatabase.AssetPathToGUID(SceneBPath),
                SceneBPath,
                sceneConfigB);

            settings.SetGlobalConfig(globalConfig);
            settings.SetDefaultSceneConfig(null);
            settings.SetSceneBindings(new[] { bindingA, bindingB });
            EditorUtility.SetDirty(settings);
        }

        private static void AppendBuildSettingsScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AppendSceneIfMissing(scenes, SceneAPath);
            AppendSceneIfMissing(scenes, SceneBPath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AppendSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
        {
            for (var i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    scenes[i].enabled = true;
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
