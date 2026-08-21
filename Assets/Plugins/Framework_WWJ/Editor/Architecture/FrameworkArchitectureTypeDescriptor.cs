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
        internal FrameworkArchitectureGroupDescriptor Group { get; }
        internal string AssemblyName { get; }
        internal Type BaseType { get; }
        internal IReadOnlyList<Type> DirectInterfaces { get; }
        internal MonoScript Script { get; }

        internal FrameworkArchitectureTypeKind Kind { get; }

        internal FrameworkArchitectureTypeDescriptor(
            Type type,
            FrameworkArchitectureAttribute metadata,
            FrameworkArchitectureGroupDescriptor group,
            IReadOnlyList<Type> directInterfaces,
            MonoScript script)
        {
            Type = type;
            Metadata = metadata;
            Group = group;
            AssemblyName = type.Assembly.GetName().Name;
            BaseType = type.BaseType;
            DirectInterfaces = directInterfaces;
            Script = script;
            Kind = ResolveKind(type);
        }

        private static FrameworkArchitectureTypeKind ResolveKind(Type type)
        {
            if (type.IsInterface)
            {
                return FrameworkArchitectureTypeKind.Interface;
            }

            if (type.IsEnum)
            {
                return FrameworkArchitectureTypeKind.Enum;
            }

            return type.IsValueType
                ? FrameworkArchitectureTypeKind.Struct
                : FrameworkArchitectureTypeKind.Class;
        }
    }
}
