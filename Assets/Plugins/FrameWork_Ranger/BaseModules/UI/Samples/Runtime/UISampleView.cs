using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Samples
{
    public sealed class UISampleView : UIView
    {
        [UIRequired] public Text Title;
        [UIRequired] public Button AddItem;
        [UIRequired] public Button OpenConfirm;
        [UIRequired] public Button ClosePanel;
        [UIRequired] public ScrollRect Scroll;
        [UIRequired] public UISampleItemView ItemTemplate;
    }
}
