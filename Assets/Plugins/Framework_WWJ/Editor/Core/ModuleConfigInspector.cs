using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Framework_WWJ.Editor
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
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("配置校验摘要", EditorStyles.boldLabel);
            var result = ModuleGraphResolver.Inspect((ModuleConfigBase)target);
            ModuleDependencyGraphDrawer.DrawDiagnostics(result);

            if (target is FrameworkSceneConfig)
            {
                EditorGUILayout.HelpBox(
                    "SceneConfig 单独查看时，未在本资产中找到的依赖会作为 Warning；请在 Framework Center 的项目配置页完成 Global 联合验证。",
                    MessageType.Info);
            }
        }
    }
}
