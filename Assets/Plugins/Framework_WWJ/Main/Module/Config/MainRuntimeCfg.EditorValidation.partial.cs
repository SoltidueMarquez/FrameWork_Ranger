using System;
using System.Collections.Generic;
using System.Text;
using Plugins.Framework_WWJ.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    [InfoBox("@GetDuplicateModulesWarning()", InfoMessageType.Warning, VisibleIf = "@HasDuplicateModulesWarning()")]
    public partial class MainRuntimeCfg
    {
#if UNITY_EDITOR
        private sealed class ModuleItemSource
        {
            public ModuleItemCfg item;
            public string source;
        }

        private bool HasDuplicateModulesWarning()
        {
            return !string.IsNullOrEmpty(GetDuplicateModulesWarning());
        }

        private string GetDuplicateModulesWarning()
        {
            var items = CollectEnabledModuleItemsWithSources();
            if (items.Count <= 1) return null;

            var sb = new StringBuilder(256);

            AppendKeyDuplicates(sb, items);
            AppendTypeDuplicates(sb, items);
            AppendInstanceDuplicates(sb, items);

            return sb.Length == 0 ? null : sb.ToString();
        }

        private List<ModuleItemSource> CollectEnabledModuleItemsWithSources()
        {
            var result = new List<ModuleItemSource>(32);

            // 1) self
            if (!moduleItemCfgs.IsEmpty())
            {
                for (int i = 0; i < moduleItemCfgs.Count; i++)
                {
                    var item = moduleItemCfgs[i];
                    if (item == null || !item.on) continue;
                    result.Add(new ModuleItemSource { item = item, source = "self" });
                }
            }

            // 2) packages (防循环引用)
            if (!mainRuntimeCfgPackages.IsEmpty())
            {
                var visited = new HashSet<MainRuntimeCfg> { this };
                for (int i = 0; i < mainRuntimeCfgPackages.Count; i++)
                {
                    var package = mainRuntimeCfgPackages[i];
                    CollectEnabledFromPackageRecursive(package, $"package:{GetCfgDisplayName(package)}", visited, result);
                }
            }

            return result;
        }

        private static void CollectEnabledFromPackageRecursive(
            MainRuntimeCfg cfg,
            string sourcePrefix,
            HashSet<MainRuntimeCfg> visited,
            List<ModuleItemSource> outList)
        {
            if (cfg == null) return;
            if (!visited.Add(cfg)) return;

            if (!cfg.moduleItemCfgs.IsEmpty())
            {
                for (int i = 0; i < cfg.moduleItemCfgs.Count; i++)
                {
                    var item = cfg.moduleItemCfgs[i];
                    if (item == null || !item.on) continue;
                    outList.Add(new ModuleItemSource { item = item, source = sourcePrefix });
                }
            }

            if (!cfg.mainRuntimeCfgPackages.IsEmpty())
            {
                for (int i = 0; i < cfg.mainRuntimeCfgPackages.Count; i++)
                {
                    var child = cfg.mainRuntimeCfgPackages[i];
                    CollectEnabledFromPackageRecursive(child, $"package:{GetCfgDisplayName(child)}", visited, outList);
                }
            }
        }

        private static string GetCfgDisplayName(MainRuntimeCfg cfg)
        {
            return cfg == null ? "null" : (!string.IsNullOrEmpty(cfg.name) ? cfg.name : cfg.GetType().Name);
        }

        private static void AppendKeyDuplicates(StringBuilder sb, List<ModuleItemSource> items)
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var key = it?.item?.moduleKey;
                if (string.IsNullOrWhiteSpace(key)) continue; // 空 key 不参与判重，避免噪音

                if (!dict.TryGetValue(key, out var sources))
                {
                    sources = new List<string>(2);
                    dict.Add(key, sources);
                }
                sources.Add(it.source);
            }

            foreach (var kv in dict)
            {
                if (kv.Value.Count < 2) continue;
                sb.Append("Key重复: ").Append(kv.Key).Append(" -> ").Append(string.Join(", ", kv.Value)).AppendLine();
            }
        }

        private static void AppendTypeDuplicates(StringBuilder sb, List<ModuleItemSource> items)
        {
            var dict = new Dictionary<Type, List<string>>();
            for (int i = 0; i < items.Count; i++)
            {
                var m = items[i]?.item?.module;
                if (m == null) continue;
                var t = m.GetType();

                if (!dict.TryGetValue(t, out var sources))
                {
                    sources = new List<string>(2);
                    dict.Add(t, sources);
                }
                sources.Add(items[i].source);
            }

            foreach (var kv in dict)
            {
                if (kv.Value.Count < 2) continue;
                sb.Append("Type重复: ").Append(kv.Key.Name).Append(" -> ").Append(string.Join(", ", kv.Value)).AppendLine();
            }
        }

        private static void AppendInstanceDuplicates(StringBuilder sb, List<ModuleItemSource> items)
        {
            var dict = new Dictionary<IModule, List<string>>(ReferenceEqualityComparer<IModule>.Default);
            for (int i = 0; i < items.Count; i++)
            {
                var m = items[i]?.item?.module;
                if (m == null) continue;

                if (!dict.TryGetValue(m, out var sources))
                {
                    sources = new List<string>(2);
                    dict.Add(m, sources);
                }
                sources.Add(items[i].source);
            }

            foreach (var kv in dict)
            {
                if (kv.Value.Count < 2) continue;
                sb.Append("Instance重复: ").Append(FormatModuleInstance(kv.Key)).Append(" -> ")
                    .Append(string.Join(", ", kv.Value)).AppendLine();
            }
        }

        private static string FormatModuleInstance(IModule module)
        {
            if (module == null) return "null";
            if (module is UnityEngine.Object uo)
            {
                return $"{uo.name}({uo.GetInstanceID()})";
            }
            return $"{module.GetType().Name}({module.GetHashCode()})";
        }

        private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
#endif
    }
}

