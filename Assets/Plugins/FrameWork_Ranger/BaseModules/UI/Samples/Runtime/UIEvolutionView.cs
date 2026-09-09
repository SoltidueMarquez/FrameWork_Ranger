using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Samples
{
    public sealed class UIEvolutionView : UIView
    {
        [UIRequired] public UIButton Gesture;
        [UIRequired] public UIButton CloseButton;
        [UIRequired] public UIButton ScrollBottom;
        [UIRequired] public ScrollRect Scroll;
        [UIRequired] public Text Status;
    }
}
