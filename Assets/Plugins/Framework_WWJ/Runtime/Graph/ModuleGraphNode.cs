using System;
using System.Collections.Generic;

namespace Framework_WWJ
{
    /// <summary>
    /// 配置图中的一个只读节点。它描述模板和排序信息，不拥有 Module 运行克隆。
    /// </summary>
    [FrameworkArchitecture(
        "模块图节点",
        "保存模块模板、作用域、优先级、依赖和拓扑层级。",
        FrameworkArchitectureLayer.GraphAndScope,
        20)]
    internal sealed class ModuleGraphNode
    {
        internal ModuleConfigEntry Entry { get; }

        internal ModuleBase Template { get; }

        internal Type ModuleType { get; }

        internal ModuleScopeKind ScopeKind { get; }

        internal int ConfigIndex { get; }

        internal bool Enabled { get; }

        internal int LoadPriority { get; }

        internal IReadOnlyList<Type> Dependencies { get; }

        internal int TopologicalLevel { get; set; }

        internal ModuleGraphNode(ModuleConfigEntry entry, ModuleScopeKind scopeKind, int configIndex)
        {
            Entry = entry;
            Template = entry?.Module;
            ModuleType = Template == null ? null : Template.GetType();
            ScopeKind = scopeKind;
            ConfigIndex = configIndex;
            Enabled = entry?.Enabled ?? false;
            LoadPriority = Template == null ? 0 : Template.LoadPriority;
            Dependencies = Template == null ? Array.Empty<Type>() : Template.GetRequiredModuleTypes();
        }
    }
}
