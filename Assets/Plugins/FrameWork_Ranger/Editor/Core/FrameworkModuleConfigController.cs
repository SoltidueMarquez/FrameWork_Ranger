using System;
using System.Collections.Generic;
using UnityEditor;

namespace FrameWork_Ranger.Editor
{
    [FrameworkArchitecture(
        "模块配置筛选",
        "区分全部、启用和异常模块行。",
        FrameworkArchitectureLayer.EditorIntegration,
        1)]
    internal enum FrameworkModuleConfigFilter
    {
        All = 0,
        Enabled = 1,
        Problems = 2,
    }

    /// <summary>
    /// 集中执行模块条目的结构修改和过滤映射，保证 IMGUI 列表不会绕过 Undo 或 Odin 序列化入口。
    /// </summary>
    [FrameworkArchitecture(
        "模块配置列表控制器",
        "处理模块条目的添加、替换、启用、删除、重排和筛选索引。",
        FrameworkArchitectureLayer.EditorIntegration,
        2,
        typeof(ModuleConfigBase),
        typeof(ModuleGraphResolver))]
    internal static class FrameworkModuleConfigController
    {
        internal static bool CanReorder(string searchText, FrameworkModuleConfigFilter filter)
        {
            return string.IsNullOrWhiteSpace(searchText) && filter == FrameworkModuleConfigFilter.All;
        }

        internal static void BuildVisibleIndices(
            ModuleConfigBase config,
            ModuleGraphResult result,
            string searchText,
            FrameworkModuleConfigFilter filter,
            List<int> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Clear();
            if (config == null)
            {
                return;
            }

            var normalizedSearch = string.IsNullOrWhiteSpace(searchText)
                ? string.Empty
                : searchText.Trim();
            for (var i = 0; i < config.Modules.Count; i++)
            {
                var entry = config.Modules[i];
                if (filter == FrameworkModuleConfigFilter.Enabled && !(entry?.Enabled ?? false))
                {
                    continue;
                }

                if (filter == FrameworkModuleConfigFilter.Problems && !HasProblem(config, result, i))
                {
                    continue;
                }

                if (!MatchesSearch(entry?.Module, normalizedSearch))
                {
                    continue;
                }

                output.Add(i);
            }
        }

        internal static bool HasProblem(ModuleConfigBase config, ModuleGraphResult result, int configIndex)
        {
            if (config == null || result == null)
            {
                return false;
            }

            var scopeKind = config is FrameworkGlobalConfig
                ? ModuleScopeKind.Global
                : ModuleScopeKind.Scene;
            for (var i = 0; i < result.Diagnostics.Count; i++)
            {
                var diagnostic = result.Diagnostics[i];
                if (diagnostic.ScopeKind == scopeKind && diagnostic.ConfigIndex == configIndex)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void AddModule(ModuleConfigBase config, ModuleBase module)
        {
            var modules = CopyEntries(config);
            modules.Add(new ModuleConfigEntry(true, module));
            Apply(config, modules, "添加 Framework 模块");
        }

        internal static void SetModule(ModuleConfigBase config, int index, ModuleBase module)
        {
            var modules = CopyEntries(config);
            ValidateIndex(modules, index);
            var entry = modules[index] ?? new ModuleConfigEntry();
            modules[index] = new ModuleConfigEntry(entry.Enabled, module);
            Apply(config, modules, "替换 Framework 模块");
        }

        internal static void SetEnabled(ModuleConfigBase config, int index, bool enabled)
        {
            var modules = CopyEntries(config);
            ValidateIndex(modules, index);
            var entry = modules[index] ?? new ModuleConfigEntry();
            modules[index] = new ModuleConfigEntry(enabled, entry.Module);
            Apply(config, modules, enabled ? "启用 Framework 模块" : "停用 Framework 模块");
        }

        internal static void RemoveModule(ModuleConfigBase config, int index)
        {
            var modules = CopyEntries(config);
            ValidateIndex(modules, index);
            modules.RemoveAt(index);
            Apply(config, modules, "移除 Framework 模块");
        }

        internal static void MoveModule(ModuleConfigBase config, int oldIndex, int newIndex)
        {
            var modules = CopyEntries(config);
            ValidateIndex(modules, oldIndex);
            if (newIndex < 0 || newIndex >= modules.Count || oldIndex == newIndex)
            {
                return;
            }

            var entry = modules[oldIndex];
            modules.RemoveAt(oldIndex);
            modules.Insert(newIndex, entry);
            Apply(config, modules, "重排 Framework 模块");
        }

        internal static int ComputeRevision(ModuleConfigBase config)
        {
            if (config == null)
            {
                return 0;
            }

            unchecked
            {
                var revision = EditorUtility.GetDirtyCount(config);
                revision = revision * 397 ^ config.Modules.Count;
                for (var i = 0; i < config.Modules.Count; i++)
                {
                    var entry = config.Modules[i];
                    var module = entry?.Module;
                    revision = revision * 397 ^ (entry?.Enabled == true ? 1 : 0);
                    revision = revision * 397 ^
                               (module == null ? 0 : module.GetEntityId().GetHashCode());
                    revision = revision * 397 ^ (module == null ? 0 : EditorUtility.GetDirtyCount(module));
                }

                return revision;
            }
        }

        private static List<ModuleConfigEntry> CopyEntries(ModuleConfigBase config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new List<ModuleConfigEntry>(config.Modules);
        }

        private static void Apply(
            ModuleConfigBase config,
            IReadOnlyList<ModuleConfigEntry> modules,
            string undoName)
        {
            Undo.RecordObject(config, undoName);
            config.SetModules(modules);
            EditorUtility.SetDirty(config);
        }

        private static void ValidateIndex(IReadOnlyList<ModuleConfigEntry> modules, int index)
        {
            if (index < 0 || index >= modules.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static bool MatchesSearch(ModuleBase module, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            if (module == null)
            {
                return "空模板".IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            var type = module.GetType();
            return module.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   type.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (type.FullName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }
    }
}
