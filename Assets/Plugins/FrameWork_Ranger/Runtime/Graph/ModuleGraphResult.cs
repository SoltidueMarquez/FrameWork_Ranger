using System.Collections.Generic;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 一次配置分析的完整只读结果。
    /// </summary>
    [FrameworkArchitecture(
        "模块图结果",
        "保存显示节点、Global/Scene 排序结果和统一诊断。",
        FrameworkArchitectureLayer.GraphAndScope,
        40)]
    internal sealed class ModuleGraphResult
    {
        internal IReadOnlyList<ModuleGraphNode> Nodes { get; }

        internal IReadOnlyList<ModuleGraphNode> OrderedGlobalNodes { get; }

        internal IReadOnlyList<ModuleGraphNode> OrderedSceneNodes { get; }

        internal IReadOnlyList<ModuleGraphDiagnostic> Diagnostics { get; }

        internal bool IsValid
        {
            get
            {
                for (var i = 0; i < Diagnostics.Count; i++)
                {
                    if (Diagnostics[i].Severity == ModuleGraphDiagnosticSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal ModuleGraphResult(
            IReadOnlyList<ModuleGraphNode> nodes,
            IReadOnlyList<ModuleGraphNode> orderedGlobalNodes,
            IReadOnlyList<ModuleGraphNode> orderedSceneNodes,
            IReadOnlyList<ModuleGraphDiagnostic> diagnostics)
        {
            Nodes = nodes;
            OrderedGlobalNodes = orderedGlobalNodes;
            OrderedSceneNodes = orderedSceneNodes;
            Diagnostics = diagnostics;
        }
    }
}
