using System;
using System.Collections.Generic;
using UnityEditor;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 代码架构图中的一个不可变类型节点。
    /// </summary>
    [FrameworkArchitecture(
        "架构类型描述",
        "保存架构节点的类型、声明元数据、直接契约和源码位置。",
        FrameworkArchitectureLayer.EditorIntegration,
        300)]
    internal sealed class FrameworkArchitectureTypeDescriptor
    {
        internal Type Type { get; }
        internal FrameworkArchitectureAttribute Metadata { get; }
        internal string AssemblyName { get; }
        internal Type BaseType { get; }
        internal IReadOnlyList<Type> DirectInterfaces { get; }
        internal MonoScript Script { get; }

        internal bool IsInterface => Type.IsInterface;

        internal FrameworkArchitectureTypeDescriptor(
            Type type,
            FrameworkArchitectureAttribute metadata,
            IReadOnlyList<Type> directInterfaces,
            MonoScript script)
        {
            Type = type;
            Metadata = metadata;
            AssemblyName = type.Assembly.GetName().Name;
            BaseType = type.BaseType;
            DirectInterfaces = directInterfaces;
            Script = script;
        }
    }
}
