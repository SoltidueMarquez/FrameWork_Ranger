using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/效果/线性渐变")]
    [FrameworkArchitecture("UI 线性渐变", "以方向和两端颜色调制图形，保留图片透明形状。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIGradient : BaseMeshEffect
    {
        [LabelText("起始颜色")] public Color StartColor = Color.white;
        [LabelText("结束颜色")] public Color EndColor = Color.gray;
        [LabelText("方向角度"), Range(-180, 180)] public float Angle = 90;
        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0) return;
            var direction = new Vector2(Mathf.Cos(Angle * Mathf.Deg2Rad), Mathf.Sin(Angle * Mathf.Deg2Rad));
            float min = float.MaxValue, max = float.MinValue;
            UIVertex vertex = default;
            for (int i = 0; i < helper.currentVertCount; i++)
            { helper.PopulateUIVertex(ref vertex, i); float p = Vector2.Dot(vertex.position, direction); min = Mathf.Min(min, p); max = Mathf.Max(max, p); }
            float span = Mathf.Max(0.0001f, max - min);
            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);
                vertex.color = (Color)vertex.color * Color.Lerp(StartColor, EndColor, (Vector2.Dot(vertex.position, direction) - min) / span);
                helper.SetUIVertex(vertex, i);
            }
        }
        public void Refresh() { if (graphic) graphic.SetVerticesDirty(); }
    }
}
