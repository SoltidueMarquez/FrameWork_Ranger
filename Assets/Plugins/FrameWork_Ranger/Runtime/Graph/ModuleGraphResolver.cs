using System;
using System.Collections.Generic;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 模块配置的唯一校验与稳定拓扑排序算法。该类型不创建 Unity 对象，因此可由 Runtime、Editor 和测试共同调用。
    /// </summary>
    [FrameworkArchitecture(
        "模块图解析器",
        "执行唯一的配置校验、依赖分析和稳定拓扑排序。",
        FrameworkArchitectureLayer.GraphAndScope,
        30,
        typeof(ModuleGraphNode),
        typeof(ModuleGraphResult))]
    internal static class ModuleGraphResolver
    {
        #region 分析入口

        internal static ModuleGraphResult Resolve(
            FrameworkGlobalConfig globalConfig,
            FrameworkSceneConfig sceneConfig)
        {
            var nodes = new List<ModuleGraphNode>();
            var diagnostics = new List<ModuleGraphDiagnostic>();

            AppendConfigNodes(globalConfig, ModuleScopeKind.Global, true, nodes, diagnostics);
            // 中央设置允许默认 SceneConfig 为空；这明确表示一个合法的零模块 SceneScope。
            AppendConfigNodes(sceneConfig, ModuleScopeKind.Scene, false, nodes, diagnostics);

            return Analyze(nodes, diagnostics, false);
        }

        internal static ModuleGraphResult Inspect(ModuleConfigBase config)
        {
            var nodes = new List<ModuleGraphNode>();
            var diagnostics = new List<ModuleGraphDiagnostic>();
            var scopeKind = config is FrameworkGlobalConfig ? ModuleScopeKind.Global : ModuleScopeKind.Scene;

            AppendConfigNodes(config, scopeKind, true, nodes, diagnostics);
            return Analyze(nodes, diagnostics, scopeKind == ModuleScopeKind.Scene);
        }

        #endregion

        #region 内部实现

        private static ModuleGraphResult Analyze(
            List<ModuleGraphNode> nodes,
            List<ModuleGraphDiagnostic> diagnostics,
            bool allowExternalSceneDependencies)
        {
            var enabledByType = BuildEnabledTypeMap(nodes, diagnostics);
            ValidateImplementations(nodes, diagnostics);
            ValidateDependencies(nodes, enabledByType, diagnostics, allowExternalSceneDependencies);

            var orderedGlobal = SortScope(
                nodes,
                enabledByType,
                ModuleScopeKind.Global,
                diagnostics);

            var orderedScene = SortScope(
                nodes,
                enabledByType,
                ModuleScopeKind.Scene,
                diagnostics);

            return new ModuleGraphResult(nodes, orderedGlobal, orderedScene, diagnostics);
        }

        private static void AppendConfigNodes(
            ModuleConfigBase config,
            ModuleScopeKind scopeKind,
            bool configRequired,
            List<ModuleGraphNode> nodes,
            List<ModuleGraphDiagnostic> diagnostics)
        {
            if (config == null)
            {
                if (configRequired)
                {
                    diagnostics.Add(new ModuleGraphDiagnostic(
                        ModuleGraphDiagnosticSeverity.Error,
                        ModuleGraphDiagnosticCode.MissingConfig,
                        scopeKind,
                        -1,
                        null,
                        $"缺少 {scopeKind} 配置资产。"));
                }

                return;
            }

            var entries = config.Modules;
            for (var i = 0; i < entries.Count; i++)
            {
                var node = new ModuleGraphNode(entries[i], scopeKind, i);
                nodes.Add(node);

                if (node.Enabled && node.Template == null)
                {
                    diagnostics.Add(new ModuleGraphDiagnostic(
                        ModuleGraphDiagnosticSeverity.Error,
                        ModuleGraphDiagnosticCode.MissingTemplate,
                        scopeKind,
                        i,
                        null,
                        "启用的配置条目没有指定 Module 模板。"));
                }
            }
        }

        private static Dictionary<Type, ModuleGraphNode> BuildEnabledTypeMap(
            IReadOnlyList<ModuleGraphNode> nodes,
            List<ModuleGraphDiagnostic> diagnostics)
        {
            var result = new Dictionary<Type, ModuleGraphNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!node.Enabled || node.ModuleType == null)
                {
                    continue;
                }

                if (result.TryGetValue(node.ModuleType, out var existing))
                {
                    diagnostics.Add(new ModuleGraphDiagnostic(
                        ModuleGraphDiagnosticSeverity.Error,
                        ModuleGraphDiagnosticCode.DuplicateType,
                        node.ScopeKind,
                        node.ConfigIndex,
                        node.ModuleType,
                        $"具体类型已经由 {existing.ScopeKind}[{existing.ConfigIndex}] 启用；第一阶段不允许同类型多实例或 Scene 覆盖 Global。"));
                    continue;
                }

                result.Add(node.ModuleType, node);
            }

            return result;
        }

        private static void ValidateImplementations(
            IReadOnlyList<ModuleGraphNode> nodes,
            List<ModuleGraphDiagnostic> diagnostics)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!node.Enabled || node.Template == null)
                {
                    continue;
                }

                if (!node.Template.ValidateTemplate(out var error))
                {
                    diagnostics.Add(new ModuleGraphDiagnostic(
                        ModuleGraphDiagnosticSeverity.Error,
                        ModuleGraphDiagnosticCode.MissingHandler,
                        node.ScopeKind,
                        node.ConfigIndex,
                        node.ModuleType,
                        error));
                }
            }
        }

        private static void ValidateDependencies(
            IReadOnlyList<ModuleGraphNode> nodes,
            IReadOnlyDictionary<Type, ModuleGraphNode> enabledByType,
            List<ModuleGraphDiagnostic> diagnostics,
            bool allowExternalSceneDependencies)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!node.Enabled || node.Template == null)
                {
                    continue;
                }

                for (var dependencyIndex = 0; dependencyIndex < node.Dependencies.Count; dependencyIndex++)
                {
                    var dependencyType = node.Dependencies[dependencyIndex];
                    if (dependencyType == null ||
                        !typeof(ModuleBase).IsAssignableFrom(dependencyType) ||
                        dependencyType.IsAbstract)
                    {
                        diagnostics.Add(new ModuleGraphDiagnostic(
                            ModuleGraphDiagnosticSeverity.Error,
                            ModuleGraphDiagnosticCode.InvalidDependencyType,
                            node.ScopeKind,
                            node.ConfigIndex,
                            node.ModuleType,
                            $"依赖项 #{dependencyIndex} 必须是非抽象 ModuleBase 具体类型。"));
                        continue;
                    }

                    if (!enabledByType.TryGetValue(dependencyType, out var dependencyNode))
                    {
                        var severity = allowExternalSceneDependencies && node.ScopeKind == ModuleScopeKind.Scene
                            ? ModuleGraphDiagnosticSeverity.Warning
                            : ModuleGraphDiagnosticSeverity.Error;
                        diagnostics.Add(new ModuleGraphDiagnostic(
                            severity,
                            ModuleGraphDiagnosticCode.MissingDependency,
                            node.ScopeKind,
                            node.ConfigIndex,
                            node.ModuleType,
                            severity == ModuleGraphDiagnosticSeverity.Warning
                                ? $"依赖 {dependencyType.FullName} 不在当前 SceneConfig 中，需要在 Framework Center 中与 GlobalConfig 联合验证。"
                                : $"找不到已启用的依赖 {dependencyType.FullName}。"));
                        continue;
                    }

                    if (node.ScopeKind == ModuleScopeKind.Global &&
                        dependencyNode.ScopeKind == ModuleScopeKind.Scene)
                    {
                        diagnostics.Add(new ModuleGraphDiagnostic(
                            ModuleGraphDiagnosticSeverity.Error,
                            ModuleGraphDiagnosticCode.InvalidScopeDirection,
                            node.ScopeKind,
                            node.ConfigIndex,
                            node.ModuleType,
                            $"Global 模块不能依赖 Scene 模块 {dependencyType.FullName}。"));
                    }
                }
            }
        }

        private static List<ModuleGraphNode> SortScope(
            IReadOnlyList<ModuleGraphNode> allNodes,
            IReadOnlyDictionary<Type, ModuleGraphNode> enabledByType,
            ModuleScopeKind scopeKind,
            List<ModuleGraphDiagnostic> diagnostics)
        {
            var scopeNodes = new List<ModuleGraphNode>();
            var indegrees = new Dictionary<ModuleGraphNode, int>();
            var dependents = new Dictionary<ModuleGraphNode, List<ModuleGraphNode>>();

            for (var i = 0; i < allNodes.Count; i++)
            {
                var node = allNodes[i];
                if (!node.Enabled || node.ModuleType == null || node.ScopeKind != scopeKind)
                {
                    continue;
                }

                // 重复类型中的后续条目不会进入有效类型表，也不能进入运行排序。
                if (!enabledByType.TryGetValue(node.ModuleType, out var registered) || registered != node)
                {
                    continue;
                }

                scopeNodes.Add(node);
                indegrees[node] = 0;
                dependents[node] = new List<ModuleGraphNode>();
            }

            for (var i = 0; i < scopeNodes.Count; i++)
            {
                var node = scopeNodes[i];
                for (var dependencyIndex = 0; dependencyIndex < node.Dependencies.Count; dependencyIndex++)
                {
                    var dependencyType = node.Dependencies[dependencyIndex];
                    if (dependencyType == null || !enabledByType.TryGetValue(dependencyType, out var dependencyNode))
                    {
                        continue;
                    }

                    if (dependencyNode.ScopeKind == scopeKind && dependents.ContainsKey(dependencyNode))
                    {
                        indegrees[node]++;
                        dependents[dependencyNode].Add(node);
                    }
                }
            }

            var ready = new List<ModuleGraphNode>();
            for (var i = 0; i < scopeNodes.Count; i++)
            {
                if (indegrees[scopeNodes[i]] == 0)
                {
                    ready.Add(scopeNodes[i]);
                }
            }

            var ordered = new List<ModuleGraphNode>(scopeNodes.Count);
            while (ready.Count > 0)
            {
                ready.Sort(CompareReadyNodes);
                var node = ready[0];
                ready.RemoveAt(0);

                node.TopologicalLevel = CalculateLevel(node, enabledByType, scopeKind);
                ordered.Add(node);

                var nodeDependents = dependents[node];
                for (var i = 0; i < nodeDependents.Count; i++)
                {
                    var dependent = nodeDependents[i];
                    indegrees[dependent]--;
                    if (indegrees[dependent] == 0)
                    {
                        ready.Add(dependent);
                    }
                }
            }

            if (ordered.Count != scopeNodes.Count)
            {
                for (var i = 0; i < scopeNodes.Count; i++)
                {
                    var node = scopeNodes[i];
                    if (ordered.Contains(node))
                    {
                        continue;
                    }

                    diagnostics.Add(new ModuleGraphDiagnostic(
                        ModuleGraphDiagnosticSeverity.Error,
                        ModuleGraphDiagnosticCode.DependencyCycle,
                        node.ScopeKind,
                        node.ConfigIndex,
                        node.ModuleType,
                        "模块位于循环依赖中，无法生成加载顺序。"));
                }
            }

            return ordered;
        }

        private static int CalculateLevel(
            ModuleGraphNode node,
            IReadOnlyDictionary<Type, ModuleGraphNode> enabledByType,
            ModuleScopeKind scopeKind)
        {
            var level = scopeKind == ModuleScopeKind.Scene ? 1 : 0;
            for (var i = 0; i < node.Dependencies.Count; i++)
            {
                if (!enabledByType.TryGetValue(node.Dependencies[i], out var dependencyNode))
                {
                    continue;
                }

                level = Math.Max(level, dependencyNode.TopologicalLevel + 1);
            }

            return level;
        }

        private static int CompareReadyNodes(ModuleGraphNode left, ModuleGraphNode right)
        {
            var priority = left.LoadPriority.CompareTo(right.LoadPriority);
            if (priority != 0)
            {
                return priority;
            }

            var configOrder = left.ConfigIndex.CompareTo(right.ConfigIndex);
            if (configOrder != 0)
            {
                return configOrder;
            }

            return string.Compare(left.ModuleType.FullName, right.ModuleType.FullName, StringComparison.Ordinal);
        }

        #endregion
    }
}
