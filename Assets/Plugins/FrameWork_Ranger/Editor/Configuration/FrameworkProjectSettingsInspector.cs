using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 为固定中央设置提供与 Framework Center 相同的主从配置工作台。
    /// </summary>
    [FrameworkArchitecture(
        "项目设置 Inspector",
        "以作用域导航编辑中央设置、模块配置与组合依赖图。",
        FrameworkArchitectureLayer.EditorIntegration,
        30,
        typeof(FrameworkProjectSettingsAssetUtility),
        typeof(FrameworkConfigurationWorkspace))]
    [CustomEditor(typeof(FrameworkProjectSettings))]
    internal sealed class FrameworkProjectSettingsInspector : OdinEditor
    {
        private FrameworkConfigurationWorkspace m_workspace;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_workspace?.Dispose();
            m_workspace = new FrameworkConfigurationWorkspace(target);
        }

        public override void OnInspectorGUI()
        {
            if (m_workspace == null)
            {
                m_workspace = new FrameworkConfigurationWorkspace(target);
            }

            m_workspace.Draw((FrameworkProjectSettings)target);
        }

        protected override void OnDisable()
        {
            m_workspace?.Dispose();
            m_workspace = null;
            base.OnDisable();
        }
    }
}
