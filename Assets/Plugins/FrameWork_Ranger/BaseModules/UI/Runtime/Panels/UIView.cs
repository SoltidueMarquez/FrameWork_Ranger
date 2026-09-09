using UnityEngine;
namespace FrameWork_Ranger.UI
{
    /// <summary>Prefab 引用与局部表现。业务在 UILogic 中，引用在 Inspector 手动绑定。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    [FrameworkArchitecture("UI 视图", "承载 Prefab 引用与局部表现。", FrameworkArchitectureLayer.Contracts, 130)]
    public class UIView : MonoBehaviour
    {
        public RectTransform RectTransform => (RectTransform)transform;
    }
}
