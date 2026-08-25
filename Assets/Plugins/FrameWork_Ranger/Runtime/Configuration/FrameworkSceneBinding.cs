using System;
using Sirenix.Serialization;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 将一个 Unity 场景资产映射到对应的 SceneScope 配置。
    /// Editor 使用 GUID 维护资产身份，Runtime 使用缓存路径完成无 UnityEditor 依赖的匹配。
    /// </summary>
    [FrameworkArchitecture(
        "场景配置绑定",
        "使用 GUID 与缓存路径将 Unity 场景映射到 SceneConfig。",
        FrameworkArchitectureLayer.Configuration,
        10,
        typeof(FrameworkSceneConfig))]
    [Serializable]
    public sealed class FrameworkSceneBinding
    {
        #region 序列化配置

        [OdinSerialize]
        private string m_sceneGuid;

        [OdinSerialize]
        private string m_scenePath;

        [OdinSerialize]
        private FrameworkSceneConfig m_sceneConfig;

        #endregion

        #region 公开属性

        /// <summary>
        /// 获取场景资产的稳定 GUID。
        /// </summary>
        public string SceneGuid => m_sceneGuid;

        /// <summary>
        /// 获取供 Runtime 匹配的场景资产路径。
        /// </summary>
        public string ScenePath => m_scenePath;

        /// <summary>
        /// 获取该场景需要加载的 SceneScope 配置。
        /// </summary>
        public FrameworkSceneConfig SceneConfig => m_sceneConfig;

        #endregion

        #region Editor 与测试装配

        internal void SetScene(string sceneGuid, string scenePath, FrameworkSceneConfig sceneConfig)
        {
            m_sceneGuid = sceneGuid;
            m_scenePath = FrameworkProjectSettingsResolver.NormalizeScenePath(scenePath);
            m_sceneConfig = sceneConfig;
        }

        #endregion
    }
}
