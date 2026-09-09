using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/Layout/Ignore")]
    [FrameworkArchitecture("UI 布局忽略", "将条目排除于自定义布局。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIIgnoreLayout : MonoBehaviour, ILayoutIgnorer
    {
        public bool ignoreLayout => isActiveAndEnabled;
        private void OnEnable() { Refresh(); }
        private void OnDisable() { Refresh(); }
        private void Refresh() { if (transform.parent is RectTransform parent) UILayoutScheduler.MarkDirty(parent); }
    }
}
