using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace FrameWork_Ranger
{
    /// <summary>
    /// FrameWork_Ranger 的项目级唯一配置入口。
    /// 它只保存配置资产引用，不保存模块实例、加载状态或其他运行时数据。
    /// </summary>
    [FrameworkArchitecture(
        "中央项目设置",
        "统一引用 GlobalConfig、默认 SceneConfig 与场景覆盖表。",
        FrameworkArchitectureLayer.Configuration,
        0,
        typeof(FrameworkGlobalConfig),
        typeof(FrameworkSceneBinding))]
    public sealed class FrameworkProjectSettings : SerializedScriptableObject
    {
        /// <summary>
        /// Runtime 使用 Resources.Load 时采用的固定资源名。
        /// </summary>
        public const string ResourcesLoadPath = "FrameworkProjectSettings";

        #region 序列化配置

        [OdinSerialize]
        [Required]
        private FrameworkGlobalConfig m_globalConfig;

        [OdinSerialize]
        private FrameworkSceneConfig m_defaultSceneConfig;

        [OdinSerialize]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        private List<FrameworkSceneBinding> m_sceneBindings = new List<FrameworkSceneBinding>();

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取跨场景常驻模块配置。
        /// </summary>
        public FrameworkGlobalConfig GlobalConfig => m_globalConfig;

        /// <summary>
        /// 获取未登记场景使用的默认 SceneScope 配置；为空表示合法的零模块场景作用域。
        /// </summary>
        public FrameworkSceneConfig DefaultSceneConfig => m_defaultSceneConfig;

        /// <summary>
        /// 获取按 Inspector 顺序保存的场景覆盖配置。
        /// </summary>
        public IReadOnlyList<FrameworkSceneBinding> SceneBindings => m_sceneBindings;

        #endregion

        #region Editor 与测试装配

        internal void SetGlobalConfig(FrameworkGlobalConfig globalConfig)
        {
            m_globalConfig = globalConfig;
        }

        internal void SetDefaultSceneConfig(FrameworkSceneConfig sceneConfig)
        {
            m_defaultSceneConfig = sceneConfig;
        }

        internal void SetSceneBindings(IReadOnlyList<FrameworkSceneBinding> bindings)
        {
            m_sceneBindings.Clear();
            if (bindings == null)
            {
                return;
            }

            for (var i = 0; i < bindings.Count; i++)
            {
                m_sceneBindings.Add(bindings[i]);
            }
        }

        #endregion
    }
}
