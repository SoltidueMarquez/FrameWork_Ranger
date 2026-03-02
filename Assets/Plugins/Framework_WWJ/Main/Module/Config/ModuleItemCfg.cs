using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 单条“模块配置”的数据结构。
    /// 一般挂在某个 ModuleCfg/MainRuntimeCfg 的列表中，用来描述：
    /// - 这个模块在“逻辑上的名字”（moduleKey）
    /// - 实际挂载到场景 / SO 中的模块实例（module）
    /// - 当前是否启用（on）
    ///
    /// MainLoader 在 Born 阶段会遍历 cfg.modules（List&lt;ModuleItemCfg&gt;）：
    /// - 对每一条 on == true 的配置，按 moduleKey 将 module 注册进模块系统；
    /// - moduleKey 主要用来在运行时按字符串查找模块（如 GetModule("Audio")）。
    /// </summary>
    [System.Serializable]
    public class ModuleItemCfg
    {
        /// <summary>
        /// 模块在配置中的唯一标识。
        /// - 建议使用稳定的字符串（如 "Audio"、"UI"），方便脚本或表格中以字符串引用该模块。
        /// - MainLoader 一般会用 moduleKey 作为字典的 key。
        /// </summary>
        [OdinSerialize, LabelText("模块Key")] public string moduleKey;

        /// <summary>
        /// 具体的模块实例引用。
        /// - 类型为 IModule，通常是某个继承 MonoBehaviour 的模块基类（例如 GlobalModuleBase 的子类）；
        /// - 通过 Inspector 将场景中的组件拖进来，或引用某个 SO 模块。
        /// </summary>
        [OdinSerialize, LabelText("模块")] public IModule module;

        /// <summary>
        /// 是否启用该模块。
        /// - true：MainLoader 在初始化时会把该模块加入管理列表并执行生命周期；
        /// - false：MainLoader 会跳过该配置，相当于“配置存在但暂时不开启”。
        /// </summary>
        [OdinSerialize, LabelText("是否启用")] public bool on;

#if UNITY_EDITOR
        /// <summary>
        /// 仅编辑器使用的预览标记，用于工具或自定义 Inspector 展示。
        /// 运行时不会参与逻辑。
        /// </summary>
        [HideInInspector] public bool preview;
#endif
    }
}