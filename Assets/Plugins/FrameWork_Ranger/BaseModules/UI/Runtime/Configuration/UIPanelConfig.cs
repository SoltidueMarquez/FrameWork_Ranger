using UnityEngine;
namespace FrameWork_Ranger.UI
{
    /// <summary>窗口资源及行为模板；目录决定所有者和适用场景。</summary>
    [CreateAssetMenu(fileName = "UIPanel", menuName = "FrameWork_Ranger/UI/面板配置")]
    [FrameworkArchitecture("面板配置资产", "独立配置 Prefab、Logic 与窗口行为。", FrameworkArchitectureLayer.Configuration, 130)]
    public sealed class UIPanelConfig : ScriptableObject
    {
        public UIPanelDefinition Definition = new UIPanelDefinition { UseDirectPrefab = true };
    }
}
