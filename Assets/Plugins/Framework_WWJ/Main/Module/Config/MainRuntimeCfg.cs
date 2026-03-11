using System;
using System.Collections.Generic;
using Plugins.Framework_WWJ.Utils;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Plugins.Framework_WWJ
{
    /// <summary>
    /// 运行时模块配置（主配置入口）。
    /// 在原框架中，它是“某一套运行时模块组合”的 ScriptableObject 资产，职责包括：
    /// 1. 作为 ModuleCfg 的具体实现，真正提供 modules 列表：
    ///    - 将本地列表 moduleItemCfgs 和若干“子包” mainRuntimeCfgPackages 中的 modules 合并；
    ///    - MainLoader 只关心 MainRuntimeCfg.modules，不需要知道你是如何拆包/复用的。
    /// 2. 提供一些标记信息（isInternal / isGlobal / globalCfgName）：
    ///    - 这些主要用于编辑器工具或全局配置系统（例如 Global.cs 中按名字查找某个 MainRuntimeCfg）。
    /// 3. 参与静态配置合并：
    ///    - 通过重写 GetExtraStaticCfgData，把各个子包的静态配置 cfgDatas 合并进来。
    ///
    /// 对你当前的精简骨架来说，可以直接把它当成“模块配置入口 SO”：
    /// - 在 Inspector 中维护一份 moduleItemCfgs 列表；
    /// - 可选择是否使用 mainRuntimeCfgPackages 做“模块包复用”。
    /// </summary>
    public partial class MainRuntimeCfg : ModuleCfg
    {
        /// <summary>
        /// 对外暴露的模块列表实现。
        /// - 优先从本地列表 moduleItemCfgs 收集模块；
        /// - 然后从每个“包体” MainRuntimeCfg.mainRuntimeCfgPackages 中继续收集它们的 modules；
        /// - 通过缓存 m_currentAllModuleCfgs 避免每次访问都重新组合列表。
        /// </summary>
        public override List<ModuleItemCfg> modules
        {
            get
            {
                if (m_currentAllModuleCfgs == null)
                {
                    m_currentAllModuleCfgs = new List<ModuleItemCfg>();

                    // 1. 先加入当前资产自身维护的模块列表
                    if (!moduleItemCfgs.IsEmpty())
                    {
                        m_currentAllModuleCfgs.AddRange(moduleItemCfgs);
                    }

                    // 2. 再把“引用的运行时配置包”中的模块也平铺进来
                    if (!mainRuntimeCfgPackages.IsEmpty())
                    {
                        for (int i = 0; i < mainRuntimeCfgPackages.Count; i++)
                        {
                            var package = mainRuntimeCfgPackages[i];
                            if (package != null && !package.modules.IsEmpty())
                            {
                                m_currentAllModuleCfgs.AddRange(package.modules);
                            }
                        }
                    }
                }
                return m_currentAllModuleCfgs;
            }
        }
        
        /// <summary>
        /// 运行时真正使用的“合并后模块列表”的缓存。
        /// - 初次访问 modules 时构建；
        /// - 当编辑器侧修改了 moduleItemCfgs 或 mainRuntimeCfgPackages 后，
        ///   可通过 InvalidateModulesCache() 主动清空缓存，让下次访问重新构建。
        /// </summary>
        private List<ModuleItemCfg> m_currentAllModuleCfgs;

        /// <summary>
        /// 是否标记为“内部使用”配置。
        /// - 常用于区分 demo、测试配置与正式配置，具体含义由项目约定。
        /// </summary>
        [OdinSerialize, NonSerialized] public bool isInternal;

        /// <summary>
        /// 是否标记为“全局配置”。
        /// - 当为 true 时，一般会通过某种全局入口（如 Global）按名字查找并使用该配置。
        /// </summary>
        [OdinSerialize, NonSerialized] public bool isGlobal;

        /// <summary>
        /// 作为“全局配置”时在全局系统中的名字。
        /// - 例如可用于 Global 按字符串查找一个 MainRuntimeCfg 实例。
        /// </summary>
        [OdinSerialize, NonSerialized, ShowIf(nameof(isGlobal))] public string globalCfgName;

        /// <summary>
        /// 当前资产本身维护的运行模块列表。
        /// - 这是你在 Inspector 中最常编辑的一份 List；
        /// - 每个元素是一个 ModuleItemCfg：包含 moduleKey / module / on。
        /// </summary>
        [OdinSerialize, NonSerialized,
         ListDrawerSettings(ShowItemCount = true, ShowIndexLabels = true, ListElementLabelName = "moduleKey"),
         LabelText("运行模块"),
         InfoBox("@GetDuplicateModulesWarning()", InfoMessageType.Warning, VisibleIf = "@HasDuplicateModulesWarning()")]
        public List<ModuleItemCfg> moduleItemCfgs;

        /// <summary>
        /// 引用的“模块化配置包”列表。
        /// - 你可以把一些常用模块组合单独做成一个 MainRuntimeCfg 资产，然后在这里引用，以达到“复用模块组合”的效果；
        /// - modules 属性会自动把这些包里的模块平铺合并到一个总列表中。
        /// </summary>
        [Title("引用的模块化")]
        [OdinSerialize, NonSerialized,
         InlineEditor(InlineEditorModes.GUIAndHeader, InlineEditorObjectFieldModes.Foldout),
         ListDrawerSettings(ShowItemCount = true, ShowIndexLabels = true, ListElementLabelName = "name"),
         LabelText("运行模块包体")]
        public List<MainRuntimeCfg> mainRuntimeCfgPackages;
        
        /// <summary>
        /// 手动让 modules 缓存失效。
        /// - 编辑器工具或代码修改 moduleItemCfgs / mainRuntimeCfgPackages 后，应调用本方法；
        /// - 下一次访问 modules 时会重新组合列表。
        /// </summary>
        public void InvalidateModulesCache() => m_currentAllModuleCfgs = null;

        /// <summary>
        /// 将“引用的模块化配置包”中的静态配置也一并合并进来。
        /// - ModuleCfg.GetStaticCfgData() 会调用此方法，以获得额外的 cfgDatas 条目；
        /// - 这样主配置就能同时管理子包里的 GeneralSO 配置生命周期。
        /// </summary>
        protected override Dictionary<string, CfgData> GetExtraStaticCfgData()
        {
            if (!mainRuntimeCfgPackages.IsEmpty())
            {
                Dictionary<string, CfgData> cfgDs = new Dictionary<string, CfgData>();
                for (int i = 0; i < mainRuntimeCfgPackages.Count; i++)
                {
                    cfgDs.SafeAddRange(mainRuntimeCfgPackages[i].GetStaticCfgData());
                }
                return cfgDs;
            }
            return null;
        }
    }
}
