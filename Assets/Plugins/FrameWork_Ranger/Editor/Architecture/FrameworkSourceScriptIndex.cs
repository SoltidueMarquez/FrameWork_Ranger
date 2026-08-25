using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 建立 FrameWork_Ranger 生产类型到 MonoScript 的缓存索引。
    /// </summary>
    [FrameworkArchitecture(
        "源码脚本索引",
        "在整个 FrameWork_Ranger 根目录中定位 Type 对应脚本，支持同文件辅助类型、Project Ping 和 Rider 打开。",
        FrameworkArchitectureLayer.EditorIntegration,
        330)]
    internal static class FrameworkSourceScriptIndex
    {
        private static readonly string[] s_searchFolders =
        {
            "Assets/Plugins/FrameWork_Ranger",
        };

        private static readonly Regex s_namespacePattern = new Regex(
            @"(?m)^\s*namespace\s+(?<name>[A-Za-z_][A-Za-z0-9_.]*)",
            RegexOptions.Compiled);

        private static readonly Regex s_typePattern = new Regex(
            @"(?m)^\s*(?:(?:public|internal|protected|private)\s+)?" +
            @"(?:(?:sealed|abstract|static|readonly|partial)\s+)*" +
            @"(?:class|interface|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        private static Dictionary<Type, MonoScript> s_scriptsByType;
        private static Dictionary<string, MonoScript> s_scriptsByDeclaration;

        internal static MonoScript Find(Type type)
        {
            EnsureBuilt();
            if (type == null)
            {
                return null;
            }

            if (s_scriptsByType.TryGetValue(type, out var script))
            {
                return script;
            }

            var simpleName = type.Name;
            var genericMarker = simpleName.IndexOf('`');
            if (genericMarker >= 0)
            {
                simpleName = simpleName.Substring(0, genericMarker);
            }

            var declarationName = string.IsNullOrEmpty(type.Namespace)
                ? simpleName
                : $"{type.Namespace}.{simpleName}";
            return s_scriptsByDeclaration.TryGetValue(declarationName, out script) ? script : null;
        }

        internal static void Clear()
        {
            s_scriptsByType = null;
            s_scriptsByDeclaration = null;
        }

        private static void EnsureBuilt()
        {
            if (s_scriptsByType != null)
            {
                return;
            }

            s_scriptsByType = new Dictionary<Type, MonoScript>();
            s_scriptsByDeclaration = new Dictionary<string, MonoScript>(StringComparer.Ordinal);
            var ambiguousDeclarations = new HashSet<string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:MonoScript", s_searchFolders);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null)
                {
                    continue;
                }

                var type = script.GetClass();
                if (type != null && !s_scriptsByType.ContainsKey(type))
                {
                    s_scriptsByType.Add(type, script);
                }

                IndexDeclarations(path, script, ambiguousDeclarations);
            }

            foreach (var declaration in ambiguousDeclarations)
            {
                s_scriptsByDeclaration.Remove(declaration);
            }
        }

        private static void IndexDeclarations(
            string assetPath,
            MonoScript script,
            ISet<string> ambiguousDeclarations)
        {
            string source;
            try
            {
                source = File.ReadAllText(assetPath);
            }
            catch (IOException)
            {
                return;
            }

            var namespaceMatch = s_namespacePattern.Match(source);
            var namespaceName = namespaceMatch.Success ? namespaceMatch.Groups["name"].Value : string.Empty;
            var typeMatches = s_typePattern.Matches(source);
            for (var matchIndex = 0; matchIndex < typeMatches.Count; matchIndex++)
            {
                var typeName = typeMatches[matchIndex].Groups["name"].Value;
                var declarationName = string.IsNullOrEmpty(namespaceName)
                    ? typeName
                    : $"{namespaceName}.{typeName}";
                if (ambiguousDeclarations.Contains(declarationName))
                {
                    continue;
                }

                if (s_scriptsByDeclaration.TryGetValue(declarationName, out var existing) && existing != script)
                {
                    ambiguousDeclarations.Add(declarationName);
                    s_scriptsByDeclaration.Remove(declarationName);
                    continue;
                }

                s_scriptsByDeclaration[declarationName] = script;
            }
        }
    }
}
