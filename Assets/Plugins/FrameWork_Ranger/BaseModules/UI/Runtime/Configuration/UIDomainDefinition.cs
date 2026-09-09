using System;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [Serializable]
    [FrameworkArchitecture("UI 域定义", "配置 Canvas 模式、排序与缩放。", FrameworkArchitectureLayer.Configuration, 130)]
    public sealed class UIDomainDefinition
    {
        public string Key = "Main";
        public RenderMode RenderMode = RenderMode.ScreenSpaceOverlay;
        public int SortingOrder;
        public Vector2 ReferenceResolution = new Vector2(1920, 1080);
        [Range(0, 1)] public float MatchWidthOrHeight = 0.5f;
        public float PlaneDistance = 100;
        public Vector2 WorldSize = new Vector2(800, 600);
        [Min(0.0001f)] public float WorldScale = 0.001f;
    }
}
