using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/切换项 UIToggle")]
    [FrameworkArchitecture("UI 切换项", "开关值独立于导航焦点，兼容选择组和静默刷新。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIToggle : Toggle, IUIControlState
    {
        public UIControlState VisualState => (UIControlState)(int)currentSelectionState;
        public void SetValue(bool value, bool notify = true)
        {
            if (notify) isOn = value; else SetIsOnWithoutNotify(value);
            foreach (var visual in GetComponents<UIControlVisual>()) visual.Refresh();
        }
    }
}
