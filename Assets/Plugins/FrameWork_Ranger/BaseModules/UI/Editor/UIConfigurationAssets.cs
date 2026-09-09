using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.UI.Editor
{
    [FrameworkArchitecture("UI 配置资产目录", "发现配置并校验、撤销和保存项目 UI 模块切换。", FrameworkArchitectureLayer.EditorIntegration, 140)]
    internal static class UIConfigurationAssets
    {
        internal const string SelectionKey = "FrameWork_Ranger.UI.SelectedConfig";
        internal static GlobalUIModule[] FindAll() => AssetDatabase.FindAssets("t:GlobalUIModule")
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<GlobalUIModule>).Where(asset => asset).ToArray();

        internal static FrameworkProjectSettings Settings => Resources.Load<FrameworkProjectSettings>(FrameworkProjectSettings.ResourcesLoadPath);
        internal static List<ModuleConfigEntry> UIEntries(FrameworkGlobalConfig global) => global
            ? global.Modules.Where(entry => entry?.Module is GlobalUIModule).ToList() : new List<ModuleConfigEntry>();
        internal static GlobalUIModule Active(FrameworkGlobalConfig global)
        {
            var entries = UIEntries(global);
            return entries.Count == 1 && entries[0].Enabled ? (GlobalUIModule)entries[0].Module : null;
        }
        internal static GlobalUIModule ResolveSelection(GlobalUIModule[] assets, string guid, GlobalUIModule active)
        {
            var remembered = string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<GlobalUIModule>(AssetDatabase.GUIDToAssetPath(guid));
            if (remembered && assets.Contains(remembered)) return remembered;
            if (active && assets.Contains(active)) return active;
            return assets.Length == 1 ? assets[0] : null;
        }
        internal static void Use(FrameworkProjectSettings settings, GlobalUIModule module)
        {
            var entries = Preflight(settings, module);
            var global = settings.GlobalConfig;
            Undo.RegisterCompleteObjectUndo(global, "切换项目 UI 配置");
            global.SetModules(entries);
            EditorUtility.SetDirty(global);
            AssetDatabase.SaveAssetIfDirty(global);
        }
        internal static List<ModuleConfigEntry> Preflight(FrameworkProjectSettings settings, GlobalUIModule module)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("运行期间不能切换项目 UI 配置。");
            if (!settings || !settings.GlobalConfig) throw new InvalidOperationException("中央项目设置缺少全局模块配置，请先在项目配置页指定。");
            if (!module) throw new InvalidOperationException("请先选择 UI 配置。");
            var global = settings.GlobalConfig;
            if (UIEntries(global).Count > 1) throw new InvalidOperationException("全局配置存在多个 UI 条目，请先在项目配置页整理为一个。");
            var errors = module.ValidateConfiguration();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var entries = global.Modules.ToList();
            int index = entries.FindIndex(entry => entry?.Module is GlobalUIModule);
            if (index >= 0) entries[index] = new ModuleConfigEntry(true, module);
            else entries.Add(new ModuleConfigEntry(true, module));
            // 在临时配置上预检，失败时不写入项目资产。
            var candidate = ScriptableObject.CreateInstance<FrameworkGlobalConfig>();
            try
            {
                candidate.SetModules(entries);
                candidate.SetDriverHandler(global.DriverHandler);
                var sceneConfigs = new List<FrameworkSceneConfig> { settings.DefaultSceneConfig };
                sceneConfigs.AddRange(settings.SceneBindings.Where(binding => binding != null).Select(binding => binding.SceneConfig));
                foreach (var scene in sceneConfigs.Distinct())
                    foreach (var diagnostic in ModuleGraphResolver.Resolve(candidate, scene).Diagnostics)
                        if (diagnostic.Severity == ModuleGraphDiagnosticSeverity.Error) errors.Add(diagnostic.Message);
            }
            finally { UnityEngine.Object.DestroyImmediate(candidate); }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors.Distinct()));
            return entries;
        }
    }
}
