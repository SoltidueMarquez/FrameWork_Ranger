using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Framework_WWJ
{
    /// <summary>
    /// Global 与 Scene 配置的公共基类，只保存模块模板列表，不持有任何运行时状态。
    /// </summary>
    [FrameworkArchitecture(
        "模块配置基类",
        "保存稳定有序的模块模板条目，不持有运行状态。",
        FrameworkArchitectureLayer.Configuration,
        40,
        typeof(ModuleConfigEntry))]
    public abstract class ModuleConfigBase : SerializedScriptableObject
    {
        #region Inspector 配置

        [OdinSerialize]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        private List<ModuleConfigEntry> m_modules = new List<ModuleConfigEntry>();

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取按 Inspector 顺序保存的模块条目。相同优先级时，此顺序参与稳定排序。
        /// </summary>
        public IReadOnlyList<ModuleConfigEntry> Modules => m_modules;

        #endregion

        internal void SetModules(IReadOnlyList<ModuleConfigEntry> modules)
        {
            m_modules.Clear();
            if (modules == null)
            {
                return;
            }

            for (var i = 0; i < modules.Count; i++)
            {
                m_modules.Add(modules[i]);
            }
        }
    }
}
