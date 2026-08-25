namespace FrameWork_Ranger
{
    /// <summary>
    /// 将配置图节点与它创建的 Module 运行克隆关联起来。
    /// </summary>
    [FrameworkArchitecture(
        "模块运行记录",
        "关联模板、运行克隆、依赖和实际加载状态。",
        FrameworkArchitectureLayer.GraphAndScope,
        50,
        typeof(ModuleBase))]
    internal sealed class ModuleRuntimeRecord
    {
        internal ModuleGraphNode GraphNode { get; }

        internal ModuleBase Template => GraphNode.Template;

        internal ModuleBase Instance { get; }

        internal bool IsLoaded { get; set; }

        internal ModuleRuntimeRecord(ModuleGraphNode graphNode, ModuleBase instance)
        {
            GraphNode = graphNode;
            Instance = instance;
        }
    }
}
