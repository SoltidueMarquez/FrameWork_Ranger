using System;
using UnityEditor;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 场景资产移动、重命名或删除后刷新中央设置中的缓存路径。
    /// </summary>
    [FrameworkArchitecture(
        "场景路径同步器",
        "监听场景资产变化并通过 GUID 刷新 Runtime 路径缓存。",
        FrameworkArchitectureLayer.EditorIntegration,
        40,
        typeof(FrameworkProjectSettingsAssetUtility))]
    internal sealed class FrameworkSceneBindingPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ContainsScenePath(importedAssets) &&
                !ContainsScenePath(deletedAssets) &&
                !ContainsScenePath(movedAssets) &&
                !ContainsScenePath(movedFromAssetPaths))
            {
                return;
            }

            var settings = FrameworkProjectSettingsAssetUtility.Load();
            if (FrameworkProjectSettingsAssetUtility.SyncScenePaths(settings))
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool ContainsScenePath(string[] paths)
        {
            if (paths == null)
            {
                return false;
            }

            for (var i = 0; i < paths.Length; i++)
            {
                if (paths[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
