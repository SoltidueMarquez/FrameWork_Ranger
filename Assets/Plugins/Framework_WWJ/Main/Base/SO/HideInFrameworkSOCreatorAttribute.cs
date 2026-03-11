using System;

namespace Plugins.Framework_WWJ.Main.Base
{
    /// <summary>
    /// 标记后将不会出现在 Framework 的 SO Creator 可创建类型列表中。
    /// 仅隐藏当前类型本身（不影响派生类）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HideInFrameworkSOCreatorAttribute : Attribute
    {
    }
}

