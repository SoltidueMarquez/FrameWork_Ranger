using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Samples
{
    public sealed class UISampleItemView : UIItemView
    {
        [UIRequired] public Text Label;
        public override void Bind(object data) { Label.text = data.ToString(); }
        public override void Unbind() { Label.text = string.Empty; }
    }
}
