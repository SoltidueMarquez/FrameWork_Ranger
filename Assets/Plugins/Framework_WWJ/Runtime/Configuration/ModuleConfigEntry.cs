using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Framework_WWJ
{
    /// <summary>
    /// 配置资产中的单个模块条目。优先级与依赖属于 Module 模板本身，条目只决定是否启用和使用哪个模板。
    /// </summary>
    [FrameworkArchitecture(
        "模块配置条目",
        "保存启用开关和一个 Module SO 模板引用。",
        FrameworkArchitectureLayer.Configuration,
        45,
        typeof(ModuleBase))]
    [Serializable]
    public sealed class ModuleConfigEntry
    {
        #region Inspector 配置

        [OdinSerialize]
        [ToggleLeft]
        private bool m_enabled = true;

        [OdinSerialize]
        [Required]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        private ModuleBase m_module;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取该条目是否参与当前作用域装配。
        /// </summary>
        public bool Enabled => m_enabled;

        /// <summary>
        /// 获取用于创建运行克隆的 Module SO 模板。
        /// </summary>
        public ModuleBase Module => m_module;

        #endregion

        /// <summary>
        /// 创建一个供 Odin 反序列化使用的空条目。
        /// </summary>
        public ModuleConfigEntry()
        {
        }

        /// <summary>
        /// 创建一个明确指定启用状态和模板的配置条目。
        /// </summary>
        public ModuleConfigEntry(bool enabled, ModuleBase module)
        {
            m_enabled = enabled;
            m_module = module;
        }
    }
}
