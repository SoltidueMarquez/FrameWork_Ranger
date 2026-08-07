using System;
using System.Collections.Generic;
using UnityEditor;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 建立 Framework Runtime/Editor 类型到 MonoScript 的缓存索引。
    /// </summary>
    [FrameworkArchitecture(
        "源码脚本索引",
        "按 Type 定位对应 MonoScript，支持 Project Ping 和 Rider 打开。",
        FrameworkArchitectureLayer.EditorIntegration,
        330)]
    internal static class FrameworkSourceScriptIndex
    {
        private static readonly string[] s_searchFolders =
        {
            "Assets/Plugins/Framework_WWJ/Runtime",
            "Assets/Plugins/Framework_WWJ/Editor",
        };

        private static Dictionary<Type, MonoScript> s_scriptsByType;

        internal static MonoScript Find(Type type)
        {
            EnsureBuilt();
            return type != null && s_scriptsByType.TryGetValue(type, out var script) ? script : null;
        }

        internal static void Clear()
        {
            s_scriptsByType = null;
        }

        private static void EnsureBuilt()
        {
            if (s_scriptsByType != null)
            {
                return;
            }

            s_scriptsByType = new Dictionary<Type, MonoScript>();
            var guids = AssetDatabase.FindAssets("t:MonoScript", s_searchFolders);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script == null ? null : script.GetClass();
                if (type != null && !s_scriptsByType.ContainsKey(type))
                {
                    s_scriptsByType.Add(type, script);
                }
            }
        }
    }
}
