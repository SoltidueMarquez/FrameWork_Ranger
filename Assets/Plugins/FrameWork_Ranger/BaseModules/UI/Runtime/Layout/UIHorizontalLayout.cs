using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/Layout/Horizontal")]
    [FrameworkArchitecture("UI 横向布局", "横向排列并分配条目尺寸。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIHorizontalLayout : UIStackLayout { protected override int MainAxis => 0; }
}
