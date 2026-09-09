using System;
namespace FrameWork_Ranger.UI
{
    /// <summary>标记 View 必须手动绑定的 Unity 对象字段，供 Editor 校验。</summary>
    [FrameworkArchitecture("UI 必需引用", "标记编辑器应检查的 View 字段。", FrameworkArchitectureLayer.Contracts, 135)]
    [AttributeUsage(AttributeTargets.Field)] public sealed class UIRequiredAttribute : Attribute { }
}
