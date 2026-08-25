using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 为 Global/Scene 配置资产追加只读结构校验摘要。
    /// </summary>
    [FrameworkArchitecture(
        "模块配置 Inspector",
        "为 Global/Scene 配置资产展示共享 Resolver 诊断。",
        FrameworkArchitectureLayer.EditorIntegration,
        0,
        typeof(ModuleGraphResolver))]
    [CustomEditor(typeof(ModuleConfigBase), true)]
    internal sealed class ModuleConfigInspector : OdinEditor
    {
        private FrameworkModuleConfigView m_configView;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_configView?.Dispose();
            m_configView = new FrameworkModuleConfigView();
        }

        public override void OnInspectorGUI()
        {
            EnsureConfigView();
            // m_modules 已由共享紧凑视图接管；Odin 默认树只负责 GlobalConfig 的 DriverHandler 等其余字段。
            base.OnInspectorGUI();
            EditorGUILayout.Space(6f);
            m_configView.Draw((ModuleConfigBase)target);

            if (target is FrameworkSceneConfig)
            {
                EditorGUILayout.HelpBox(
                    "SceneConfig 单独查看时，未在本资产中找到的依赖会作为 Warning；请在 Framework Center 的项目配置页完成 Global 联合验证。",
                    MessageType.Info);
            }
        }

        protected override void OnDisable()
        {
            m_configView?.Dispose();
            m_configView = null;
            base.OnDisable();
        }

        private void EnsureConfigView()
        {
            if (m_configView == null)
            {
                m_configView = new FrameworkModuleConfigView();
            }
        }
    }
}
