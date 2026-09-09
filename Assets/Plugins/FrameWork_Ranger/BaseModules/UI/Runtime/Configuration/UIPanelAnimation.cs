using System;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [Serializable]
    [FrameworkArchitecture("面板动画配置", "入场与退场的透明度、缩放、位移组合。", FrameworkArchitectureLayer.Configuration, 135)]
    public sealed class UIPanelAnimation
    {
        public bool Fade;
        public bool Scale;
        public bool Slide;
        [Min(0)] public float Duration = 0.2f;
        public Vector2 Offset = new Vector2(0, -80);
        public Vector3 HiddenScale = new Vector3(0.85f, 0.85f, 1);
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool Enabled => Duration > 0 && (Fade || Scale || Slide);
    }
}
