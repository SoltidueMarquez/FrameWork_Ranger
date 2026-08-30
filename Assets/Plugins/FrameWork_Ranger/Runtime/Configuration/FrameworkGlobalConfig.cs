using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 保存跨场景常驻模块模板与 Framework DriverHandler 的项目级配置。
    /// </summary>
    [FrameworkArchitecture(
        "全局模块配置",
        "保存跨场景模块模板和可替换 Framework DriverHandler。",
        FrameworkArchitectureLayer.Configuration,
        20,
        typeof(ModuleConfigBase),
        typeof(FrameworkDriverHandlerBase))]
    [CreateAssetMenu(fileName = "FrameworkGlobalConfig", menuName = "FrameWork_Ranger/Global Config")]
    public sealed class FrameworkGlobalConfig : ModuleConfigBase
    {
        #region Inspector 配置

        [OdinSerialize]
        [HideLabel]
        [InlineProperty]
        private FrameworkDriverHandlerBase m_driverHandler = new DefaultFrameworkDriverHandler();

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取随 GlobalConfig 一起克隆的 Framework 驱动扩展实现。
        /// </summary>
        public FrameworkDriverHandlerBase DriverHandler => m_driverHandler;

        #endregion

        internal void SetDriverHandler(FrameworkDriverHandlerBase driverHandler)
        {
            m_driverHandler = driverHandler;
        }
    }
}
