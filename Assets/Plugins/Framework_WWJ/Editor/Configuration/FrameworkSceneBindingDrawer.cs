using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 使用 SceneAsset 选择器绘制场景覆盖列表，并隐藏不应手工编辑的 GUID 与缓存路径。
    /// </summary>
    [FrameworkArchitecture(
        "场景绑定绘制器",
        "将 SceneAsset 选择转换为稳定 GUID、路径与 SceneConfig 绑定。",
        FrameworkArchitectureLayer.EditorIntegration,
        20,
        typeof(FrameworkSceneBinding))]
    internal static class FrameworkSceneBindingDrawer
    {
        internal static bool Draw(FrameworkProjectSettings settings)
        {
            var bindings = new List<FrameworkSceneBinding>(settings.SceneBindings);
            var changed = false;

            EditorGUILayout.LabelField("场景覆盖", EditorStyles.boldLabel);
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i] ?? new FrameworkSceneBinding();
                bindings[i] = binding;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"场景 #{i}", EditorStyles.boldLabel);
                if (GUILayout.Button("移除", GUILayout.Width(56f)))
                {
                    Undo.RecordObject(settings, "移除 Framework 场景覆盖");
                    bindings.RemoveAt(i);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                var oldScene = FrameworkProjectSettingsAssetUtility.LoadSceneAsset(binding);
                var newScene = (SceneAsset)EditorGUILayout.ObjectField(
                    "Scene",
                    oldScene,
                    typeof(SceneAsset),
                    false);
                var newConfig = (FrameworkSceneConfig)EditorGUILayout.ObjectField(
                    "Scene Config",
                    binding.SceneConfig,
                    typeof(FrameworkSceneConfig),
                    false);

                if (newScene != oldScene || newConfig != binding.SceneConfig)
                {
                    Undo.RecordObject(settings, "修改 Framework 场景覆盖");
                    var scenePath = newScene == null ? string.Empty : AssetDatabase.GetAssetPath(newScene);
                    var sceneGuid = string.IsNullOrEmpty(scenePath)
                        ? string.Empty
                        : AssetDatabase.AssetPathToGUID(scenePath);
                    binding.SetScene(sceneGuid, scenePath, newConfig);
                    changed = true;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("GUID", binding.SceneGuid ?? string.Empty);
                    EditorGUILayout.TextField("Path", binding.ScenePath ?? string.Empty);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加场景覆盖"))
            {
                Undo.RecordObject(settings, "添加 Framework 场景覆盖");
                bindings.Add(new FrameworkSceneBinding());
                changed = true;
            }

            if (changed)
            {
                settings.SetSceneBindings(bindings);
                EditorUtility.SetDirty(settings);
            }

            return changed;
        }
    }
}
