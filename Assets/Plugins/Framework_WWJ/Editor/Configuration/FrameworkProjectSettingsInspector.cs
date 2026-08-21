using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 为固定中央设置提供场景资产选择、统一诊断和组合模块依赖图。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置 Inspector",
        "编辑中央设置并展示 Runtime 共用的校验与模块依赖图。",
        FrameworkArchitectureLayer.EditorIntegration,
        30,
        typeof(FrameworkProjectSettingsAssetUtility),
        typeof(ModuleDependencyGraphDrawer))]
    [CustomEditor(typeof(FrameworkProjectSettings))]
    internal sealed class FrameworkProjectSettingsInspector : OdinEditor
    {
        private const string PreviewSceneSessionPrefix = "Framework_WWJ.DependencyPreviewScene.";

        private readonly FrameworkGraphViewportState m_graphViewport = new FrameworkGraphViewportState();
        private bool m_showGraph = true;
        private bool m_previewInitialized;
        private SceneAsset m_previewScene;
        private string m_previewSessionKey;

        public override void OnInspectorGUI()
        {
            var settings = (FrameworkProjectSettings)target;
            DrawPrimaryConfigs(settings);
            EditorGUILayout.Space(8f);
            FrameworkSceneBindingDrawer.Draw(settings);

            if (GUILayout.Button("同步场景路径并重新校验"))
            {
                Undo.RecordObject(settings, "同步 Framework 场景路径");
                FrameworkProjectSettingsAssetUtility.SyncScenePaths(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space(8f);
            DrawDiagnostics(settings);
            DrawSelectedGraph(settings);
        }

        private static void DrawPrimaryConfigs(FrameworkProjectSettings settings)
        {
            EditorGUILayout.LabelField("中央配置", EditorStyles.boldLabel);
            var globalConfig = (FrameworkGlobalConfig)EditorGUILayout.ObjectField(
                "Global Config",
                settings.GlobalConfig,
                typeof(FrameworkGlobalConfig),
                false);
            var defaultSceneConfig = (FrameworkSceneConfig)EditorGUILayout.ObjectField(
                "Default Scene Config",
                settings.DefaultSceneConfig,
                typeof(FrameworkSceneConfig),
                false);

            if (globalConfig == settings.GlobalConfig && defaultSceneConfig == settings.DefaultSceneConfig)
            {
                return;
            }

            Undo.RecordObject(settings, "修改 Framework 中央配置");
            settings.SetGlobalConfig(globalConfig);
            settings.SetDefaultSceneConfig(defaultSceneConfig);
            EditorUtility.SetDirty(settings);
        }

        private static void DrawDiagnostics(FrameworkProjectSettings settings)
        {
            FrameworkProjectSettingsAssetUtility.Validate(settings, out var errors, out var warnings);
            EditorGUILayout.LabelField("项目校验", EditorStyles.boldLabel);
            if (errors.Count == 0 && warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("中央配置与模块图校验通过。", MessageType.Info);
                return;
            }

            DrawMessages(errors, MessageType.Error);
            DrawMessages(warnings, MessageType.Warning);
        }

        private void DrawSelectedGraph(FrameworkProjectSettings settings)
        {
            m_showGraph = EditorGUILayout.Foldout(m_showGraph, "场景组合依赖图", true);
            if (!m_showGraph || settings.GlobalConfig == null)
            {
                return;
            }

            EnsurePreviewScene(settings);
            DrawPreviewSceneSelector();

            var scenePath = m_previewScene == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(m_previewScene);
            var settingsResult = FrameworkProjectSettingsResolver.Resolve(settings, scenePath);
            DrawResolvedContext(settingsResult, scenePath);

            var graph = ModuleGraphResolver.Resolve(settings.GlobalConfig, settingsResult.SceneConfig);
            ModuleDependencyGraphDrawer.DrawDiagnostics(graph);
            ModuleDependencyGraphDrawer.DrawGraph(graph, m_graphViewport);
        }

        #region 场景组合预览

        private void EnsurePreviewScene(FrameworkProjectSettings settings)
        {
            if (m_previewInitialized)
            {
                return;
            }

            m_previewInitialized = true;
            var settingsGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings));
            m_previewSessionKey = PreviewSceneSessionPrefix + settingsGuid;
            var savedGuid = SessionState.GetString(m_previewSessionKey, string.Empty);
            if (!string.IsNullOrEmpty(savedGuid))
            {
                m_previewScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(savedGuid));
                if (m_previewScene != null)
                {
                    return;
                }
            }

            var activeScenePath = SceneManager.GetActiveScene().path;
            m_previewScene = string.IsNullOrEmpty(activeScenePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScenePath);
            SavePreviewScene();
        }

        private void DrawPreviewSceneSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("预览上下文", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "这里只选择依赖图上下文，不会修改中央设置或场景映射。",
                FrameworkCenterStyles.Description);

            var nextScene = (SceneAsset)EditorGUILayout.ObjectField(
                "Scene Asset",
                m_previewScene,
                typeof(SceneAsset),
                false);
            if (nextScene != m_previewScene)
            {
                SetPreviewScene(nextScene);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("使用当前活动场景", GUILayout.Height(26f)))
            {
                var activePath = SceneManager.GetActiveScene().path;
                SetPreviewScene(string.IsNullOrEmpty(activePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(activePath));
            }

            if (GUILayout.Button("清除并预览默认上下文", GUILayout.Height(26f)))
            {
                SetPreviewScene(null);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawResolvedContext(
            FrameworkProjectSettingsResult result,
            string scenePath)
        {
            string message;
            if (result.UsesSceneOverride && result.SceneConfig != null)
            {
                message = $"精确场景覆盖：{result.SceneConfig.name}\n{scenePath}";
            }
            else if (result.UsesSceneOverride)
            {
                // 设置诊断会报告空 SceneConfig；这里仍然保持预览页面可绘制，
                // 避免编辑错误配置时反而因为空引用丢失定位信息。
                message = $"精确场景覆盖缺少 SceneConfig\n{scenePath}";
            }
            else if (result.SceneConfig != null)
            {
                message = $"默认 SceneConfig：{result.SceneConfig.name}" +
                          (string.IsNullOrEmpty(scenePath) ? string.Empty : $"\n未登记场景：{scenePath}");
            }
            else
            {
                message = "合法空 SceneScope" +
                          (string.IsNullOrEmpty(scenePath) ? "（默认上下文）" : $"\n未登记场景：{scenePath}");
            }

            EditorGUILayout.HelpBox(message, result.IsValid ? MessageType.Info : MessageType.Error);
        }

        private void SetPreviewScene(SceneAsset scene)
        {
            if (m_previewScene == scene)
            {
                return;
            }

            m_previewScene = scene;
            SavePreviewScene();
            m_graphViewport.RequestFrameAll();
        }

        private void SavePreviewScene()
        {
            if (string.IsNullOrEmpty(m_previewSessionKey))
            {
                return;
            }

            var path = m_previewScene == null ? string.Empty : AssetDatabase.GetAssetPath(m_previewScene);
            SessionState.SetString(
                m_previewSessionKey,
                string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
        }

        #endregion

        private static void DrawMessages(IReadOnlyList<string> messages, MessageType type)
        {
            for (var i = 0; i < messages.Count; i++)
            {
                EditorGUILayout.HelpBox(messages[i], type);
            }
        }
    }
}
