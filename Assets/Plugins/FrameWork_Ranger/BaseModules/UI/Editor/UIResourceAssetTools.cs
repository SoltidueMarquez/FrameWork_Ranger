using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Serialization;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;
using UnityEditor;
using UnityEngine;
namespace FrameWork_Ranger.UI.Editor
{
    /// <summary>新资源目录的发现、迁移及项目安装；不改其他模块。</summary>
    [FrameworkArchitecture("UI 资源配置工具", "发现、提取、校验并指定独立 UI 资源总配置。", FrameworkArchitectureLayer.EditorIntegration, 145)]
    public static class UIResourceAssetTools
    {
        public static UIResourcesConfig[] FindAll() => AssetDatabase.FindAssets("t:UIResourcesConfig")
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<UIResourcesConfig>).Where(x => x).ToArray();
        internal static UIResourcesConfig ResolveSelection(UIResourcesConfig[] assets, string guid, UIResourcesConfig active)
        {
            var remembered = string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<UIResourcesConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (remembered && assets.Contains(remembered)) return remembered;
            if (active && assets.Contains(active)) return active;
            return assets.Length == 1 ? assets[0] : null;
        }
        public static UIResourcesConfig Create(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path)) throw new InvalidOperationException("目标资产已存在。");
            var asset = ScriptableObject.CreateInstance<UIResourcesConfig>();
            asset.Global.Domains.Add(new UIDomainDefinition { SortingOrder = 100 });
            asset.Scene.Domains.Add(new UIDomainDefinition());
            AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssetIfDirty(asset); return asset;
        }
        public static UIResourcesConfig Extract(GlobalUIModule module)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("运行期间不能提取配置。");
            if (module.UIResources) return module.UIResources;
            var errors = module.ValidateConfiguration();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            string source = AssetDatabase.GetAssetPath(module);
            if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("请先保存模块资产。");
            string directory = System.IO.Path.GetDirectoryName(source).Replace('\\', '/');
            string path = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + module.name + "_Resources.asset");
            var resource = ScriptableObject.CreateInstance<UIResourcesConfig>();
            resource.IsSample = source.Contains("/Samples/");
            var created = new List<string>();
            try
            {
                AssetDatabase.CreateAsset(resource, path); created.Add(path);
                var old = new[] { module.GlobalConfiguration, module.SceneConfiguration };
                var next = new[] { resource.Global, resource.Scene };
                for (int i = 0; i < old.Length; i++)
                {
                    next[i].Domains = (List<UIDomainDefinition>)SerializationUtility.CreateCopy(old[i].Domains);
                    foreach (var definition in old[i].Panels)
                    {
                        var panel = ScriptableObject.CreateInstance<UIPanelConfig>();
                        panel.Definition = (UIPanelDefinition)SerializationUtility.CreateCopy(definition);
                        string safeKey = string.Concat(definition.Key.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
                        string panelPath = AssetDatabase.GenerateUniqueAssetPath(directory + "/" + (i == 0 ? "Global_" : "Scene_") + safeKey + ".asset");
                        AssetDatabase.CreateAsset(panel, panelPath); created.Add(panelPath);
                        EditorUtility.SetDirty(panel); AssetDatabase.SaveAssetIfDirty(panel);
                        next[i].Panels.Add(new UIPanelEntry { Panel = panel, AllScenes = true });
                    }
                }
                errors = resource.Validate();
                if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
                EditorUtility.SetDirty(resource); AssetDatabase.SaveAssetIfDirty(resource);
                // Reimport the persisted assets before changing the active module reference.
                foreach (string createdPath in created) AssetDatabase.ImportAsset(createdPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                resource = AssetDatabase.LoadAssetAtPath<UIResourcesConfig>(path);
                errors = resource.Validate();
                if (errors.Count > 0) throw new InvalidOperationException("保存重载校验失败：\n" + string.Join("\n", errors));
                Undo.RecordObject(module, "提取 UI 资源总配置"); module.UIResources = resource;
                EditorUtility.SetDirty(module); AssetDatabase.SaveAssetIfDirty(module);
                return resource;
            }
            catch
            {
                foreach (string createdPath in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(createdPath);
                throw;
            }
        }
        public static void Use(UIResourcesConfig resource, string newModulePath = null)
            => Use(UIConfigurationAssets.Settings, resource, newModulePath);
        internal static void Use(FrameworkProjectSettings settings, UIResourcesConfig resource, string newModulePath = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("运行期间不能切换配置。");
            if (!settings || !settings.GlobalConfig) throw new InvalidOperationException("请先在项目配置页指定中央 GlobalConfig。");
            var entries = UIConfigurationAssets.UIEntries(settings.GlobalConfig);
            if (entries.Count > 1) throw new InvalidOperationException("项目存在多个 UI 模块条目，请先整理为一个。");
            if (!resource) throw new InvalidOperationException("请选择资源总配置。");
            var errors = resource.Validate();
            foreach (var directory in new[] { resource.Global, resource.Scene })
                if (directory?.Panels != null)
                    foreach (var entry in directory.Panels)
                        if (entry?.Enabled == true && entry.Panel && entry.Panel.Definition?.UseDirectPrefab == true)
                        {
                            var prefab = entry.Panel.Definition.DirectPrefab;
                            if (prefab && !PrefabUtility.IsPartOfPrefabAsset(prefab)) errors.Add("直接引用必须是 Prefab 资产。");
                            if (prefab) errors.AddRange(UIEditorTools.ValidatePrefab(prefab));
                        }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var module = entries.Count == 1 ? entries[0].Module as GlobalUIModule : null;
            bool create = !module;
            if (create && (string.IsNullOrEmpty(newModulePath) || AssetDatabase.LoadMainAssetAtPath(newModulePath)))
                throw new InvalidOperationException("请选择尚不存在的 UI 模块资产保存位置。");
            var candidate = ScriptableObject.CreateInstance<GlobalUIModule>();
            candidate.UIResources = resource;
            try
            {
                // Validate a temporary module before recording Undo or changing any asset.
                var next = UIConfigurationAssets.Preflight(settings, candidate);
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("指定 UI 资源总配置");
                if (create)
                {
                    AssetDatabase.CreateAsset(candidate, newModulePath);
                    module = candidate;
                    Undo.RegisterCreatedObjectUndo(module, "创建 UI 模块");
                }
                else Undo.RegisterCompleteObjectUndo(module, "指定 UI 资源总配置");
                Undo.RegisterCompleteObjectUndo(settings.GlobalConfig, "指定 UI 资源总配置");
                module.UIResources = resource;
                int index = next.FindIndex(e => e.Module == candidate);
                next[index] = new ModuleConfigEntry(true, module);
                settings.GlobalConfig.SetModules(next);
                EditorUtility.SetDirty(module); AssetDatabase.SaveAssetIfDirty(module);
                EditorUtility.SetDirty(settings.GlobalConfig); AssetDatabase.SaveAssetIfDirty(settings.GlobalConfig);
                Undo.CollapseUndoOperations(group);
            }
            finally { if (module != candidate) UnityEngine.Object.DestroyImmediate(candidate); else if (!AssetDatabase.Contains(candidate)) UnityEngine.Object.DestroyImmediate(candidate); }
        }
        public static bool SyncScenes(UIResourcesConfig resource)
        {
            bool changed = false;
            foreach (var entry in resource.Scene.Panels)
                if (entry?.Scenes != null)
                    foreach (var scene in entry.Scenes)
                    {
                        if (scene == null || string.IsNullOrEmpty(scene.Guid)) continue;
                        string path = AssetDatabase.GUIDToAssetPath(scene.Guid);
                        // Unity can retain the path of a recently deleted GUID in its cache.
                        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) path = "";
                        if (scene.Path != path) { scene.Path = path; changed = true; }
                    }
            if (changed) EditorUtility.SetDirty(resource);
            return changed;
        }
    }
    internal sealed class UISceneAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] from)
        {
            if (!imported.Concat(deleted).Concat(moved).Concat(from).Any(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))) return;
            foreach (var config in UIResourceAssetTools.FindAll())
                if (UIResourceAssetTools.SyncScenes(config)) AssetDatabase.SaveAssetIfDirty(config);
        }
    }
    internal sealed class UIResourcesBuildValidation : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            var module = UIConfigurationAssets.Active(UIConfigurationAssets.Settings?.GlobalConfig);
            if (!module || !module.UIResources) return;
            UIResourceAssetTools.SyncScenes(module.UIResources);
            var errors = module.ValidateConfiguration();
            if (errors.Count > 0) throw new UnityEditor.Build.BuildFailedException(string.Join("\n", errors));
            AssetDatabase.SaveAssetIfDirty(module.UIResources);
        }
    }
}
