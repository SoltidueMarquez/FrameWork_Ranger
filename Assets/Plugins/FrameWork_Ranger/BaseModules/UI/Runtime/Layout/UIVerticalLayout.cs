using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/Layout/Vertical")]
    [FrameworkArchitecture("UI 纵向布局", "纵向排列并分配条目尺寸。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIVerticalLayout : UIStackLayout { protected override int MainAxis => 1; }
}
