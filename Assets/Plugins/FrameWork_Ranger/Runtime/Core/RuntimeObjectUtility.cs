using UnityEngine;

namespace FrameWork_Ranger
{
    /// <summary>
    /// 集中管理运行时 ScriptableObject 克隆与销毁规则，避免模板资产和运行实例被混用。
    /// </summary>
    [FrameworkArchitecture(
        "运行对象工具",
        "集中克隆、命名和销毁 GlobalConfig 与 Module 运行实例。",
        FrameworkArchitectureLayer.RuntimeDriving,
        80)]
    internal static class RuntimeObjectUtility
    {
        internal static FrameworkGlobalConfig CloneGlobalConfig(FrameworkGlobalConfig template)
        {
            var clone = Object.Instantiate(template);
            clone.name = $"{template.name} (Runtime)";
            clone.hideFlags = HideFlags.DontSave;
            return clone;
        }

        internal static ModuleBase CloneModule(ModuleBase template)
        {
            var clone = Object.Instantiate(template);
            clone.name = $"{template.name} (Runtime)";
            clone.hideFlags = HideFlags.DontSave;
            return clone;
        }

        internal static void Destroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
