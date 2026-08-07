using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 扫描声明式 Attribute，构造稳定架构节点并推导继承、接口实现与协作关系。
    /// </summary>
    [FrameworkArchitecture(
        "架构目录构建器",
        "扫描 Runtime/Editor 类型并生成节点、关系和 Attribute 覆盖诊断。",
        FrameworkArchitectureLayer.EditorIntegration,
        340,
        typeof(FrameworkArchitectureCatalog),
        typeof(FrameworkSourceScriptIndex))]
    internal static class FrameworkArchitectureCatalogBuilder
    {
        private static readonly HashSet<string> s_targetAssemblies = new HashSet<string>(StringComparer.Ordinal)
        {
            "Framework_WWJ.Runtime",
            "Framework_WWJ.Editor",
        };

        internal static FrameworkArchitectureCatalog Build()
        {
            var diagnostics = new List<string>();
            var descriptors = new List<FrameworkArchitectureTypeDescriptor>();
            var descriptorByType = new Dictionary<Type, FrameworkArchitectureTypeDescriptor>();

            foreach (var type in GetTargetTypes())
            {
                var metadata = (FrameworkArchitectureAttribute)Attribute.GetCustomAttribute(
                    type,
                    typeof(FrameworkArchitectureAttribute),
                    false);
                if (metadata == null)
                {
                    diagnostics.Add($"缺少 FrameworkArchitectureAttribute：{type.FullName}");
                    continue;
                }

                var descriptor = new FrameworkArchitectureTypeDescriptor(
                    type,
                    metadata,
                    GetDirectInterfaces(type),
                    FrameworkSourceScriptIndex.Find(type));
                descriptors.Add(descriptor);
                descriptorByType.Add(type, descriptor);
            }

            descriptors.Sort(CompareDescriptors);
            var relations = BuildRelations(descriptors, descriptorByType, diagnostics);
            return new FrameworkArchitectureCatalog(descriptors, relations, diagnostics);
        }

        private static IEnumerable<Type> GetTargetTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => s_targetAssemblies.Contains(assembly.GetName().Name))
                .SelectMany(SafeGetTypes)
                .Where(type => type.DeclaringType == null)
                // Unity 与编译器会在每个程序集生成若干无命名空间辅助类；它们不是项目脚本节点。
                .Where(type => type.Namespace == "Framework_WWJ" ||
                               (type.Namespace != null && type.Namespace.StartsWith("Framework_WWJ.", StringComparison.Ordinal)))
                .Where(type => type.IsClass || type.IsInterface)
                .Where(type => !typeof(MulticastDelegate).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
        }

        private static Type[] SafeGetTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
        }

        private static IReadOnlyList<Type> GetDirectInterfaces(Type type)
        {
            var interfaces = new HashSet<Type>(type.GetInterfaces());
            if (type.BaseType != null)
            {
                interfaces.ExceptWith(type.BaseType.GetInterfaces());
            }

            var candidates = interfaces.ToArray();
            for (var i = 0; i < candidates.Length; i++)
            {
                for (var otherIndex = 0; otherIndex < candidates.Length; otherIndex++)
                {
                    if (i != otherIndex && candidates[otherIndex].GetInterfaces().Contains(candidates[i]))
                    {
                        interfaces.Remove(candidates[i]);
                        break;
                    }
                }
            }

            return interfaces.OrderBy(interfaceType => interfaceType.FullName, StringComparer.Ordinal).ToArray();
        }

        private static List<FrameworkArchitectureRelation> BuildRelations(
            IReadOnlyList<FrameworkArchitectureTypeDescriptor> descriptors,
            IReadOnlyDictionary<Type, FrameworkArchitectureTypeDescriptor> descriptorByType,
            ICollection<string> diagnostics)
        {
            var relations = new List<FrameworkArchitectureRelation>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                AddRelation(
                    descriptor,
                    descriptor.BaseType,
                    FrameworkArchitectureRelationKind.Inheritance,
                    descriptorByType,
                    relations,
                    keys);

                for (var interfaceIndex = 0; interfaceIndex < descriptor.DirectInterfaces.Count; interfaceIndex++)
                {
                    AddRelation(
                        descriptor,
                        descriptor.DirectInterfaces[interfaceIndex],
                        FrameworkArchitectureRelationKind.InterfaceImplementation,
                        descriptorByType,
                        relations,
                        keys);
                }

                var relatedTypes = descriptor.Metadata.RelatedTypes;
                for (var relatedIndex = 0; relatedIndex < relatedTypes.Length; relatedIndex++)
                {
                    var relatedType = relatedTypes[relatedIndex];
                    if (relatedType == null || !descriptorByType.ContainsKey(relatedType))
                    {
                        diagnostics.Add($"{descriptor.Type.FullName} 的协作类型未进入架构图：{relatedType?.FullName ?? "<null>"}");
                        continue;
                    }

                    AddRelation(
                        descriptor,
                        relatedType,
                        FrameworkArchitectureRelationKind.Collaboration,
                        descriptorByType,
                        relations,
                        keys);
                }
            }

            return relations;
        }

        private static void AddRelation(
            FrameworkArchitectureTypeDescriptor source,
            Type targetType,
            FrameworkArchitectureRelationKind kind,
            IReadOnlyDictionary<Type, FrameworkArchitectureTypeDescriptor> descriptorByType,
            ICollection<FrameworkArchitectureRelation> relations,
            ISet<string> keys)
        {
            if (targetType == null || !descriptorByType.TryGetValue(targetType, out var target))
            {
                return;
            }

            var key = $"{source.Type.AssemblyQualifiedName}|{targetType.AssemblyQualifiedName}|{kind}";
            if (keys.Add(key))
            {
                relations.Add(new FrameworkArchitectureRelation(source, target, kind));
            }
        }

        private static int CompareDescriptors(
            FrameworkArchitectureTypeDescriptor left,
            FrameworkArchitectureTypeDescriptor right)
        {
            var layer = left.Metadata.Layer.CompareTo(right.Metadata.Layer);
            if (layer != 0)
            {
                return layer;
            }

            var order = left.Metadata.Order.CompareTo(right.Metadata.Order);
            if (order != 0)
            {
                return order;
            }

            var displayName = string.Compare(
                left.Metadata.DisplayName,
                right.Metadata.DisplayName,
                StringComparison.Ordinal);
            return displayName != 0
                ? displayName
                : string.Compare(left.Type.FullName, right.Type.FullName, StringComparison.Ordinal);
        }
    }
}
