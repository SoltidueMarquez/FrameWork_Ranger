using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 扫描显式接入的生产程序集，构造分组目录、类型节点，并推导继承、接口实现与协作关系。
    /// </summary>
    [FrameworkArchitecture(
        "架构目录构建器",
        "根据程序集分组元数据扫描生产代码，并生成分组树、类型关系和完整性诊断。",
        FrameworkArchitectureLayer.EditorIntegration,
        340,
        typeof(FrameworkArchitectureCatalog),
        typeof(FrameworkArchitectureGroupDescriptor),
        typeof(FrameworkSourceScriptIndex))]
    internal static class FrameworkArchitectureCatalogBuilder
    {
        #region 构建入口

        internal static FrameworkArchitectureCatalog Build()
        {
            var diagnostics = new List<string>();
            var rootGroup = new FrameworkArchitectureGroupDescriptor(
                string.Empty,
                "FrameWork_Ranger",
                int.MinValue,
                null);
            var groupsById = new Dictionary<string, FrameworkArchitectureGroupDescriptor>(StringComparer.Ordinal)
            {
                [string.Empty] = rootGroup,
            };

            var registrations = GetAssemblyRegistrations(diagnostics)
                .OrderBy(registration => registration.Metadata.GroupPath, StringComparer.Ordinal)
                .ThenBy(registration => registration.Assembly.GetName().Name, StringComparer.Ordinal)
                .ToArray();

            for (var i = 0; i < registrations.Length; i++)
            {
                registrations[i].Group = GetOrCreateGroup(
                    rootGroup,
                    groupsById,
                    registrations[i],
                    diagnostics);
            }

            var descriptors = new List<FrameworkArchitectureTypeDescriptor>();
            var descriptorByType = new Dictionary<Type, FrameworkArchitectureTypeDescriptor>();
            for (var registrationIndex = 0; registrationIndex < registrations.Length; registrationIndex++)
            {
                var registration = registrations[registrationIndex];
                if (registration.Group == null)
                {
                    continue;
                }

                foreach (var type in GetTargetTypes(registration.Assembly))
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
                        registration.Group,
                        GetDirectInterfaces(type),
                        FrameworkSourceScriptIndex.Find(type));
                    descriptors.Add(descriptor);
                    descriptorByType.Add(type, descriptor);
                    registration.Group.AddNode(descriptor);
                }
            }

            descriptors.Sort(CompareDescriptors);
            rootGroup.Seal();
            var relations = BuildRelations(descriptors, descriptorByType, diagnostics);
            var groups = groupsById.Values
                .OrderBy(group => group.GroupId, StringComparer.Ordinal)
                .ToArray();
            return new FrameworkArchitectureCatalog(
                descriptors,
                relations,
                diagnostics,
                rootGroup,
                groups);
        }

        #endregion

        #region 程序集与分组

        private static IEnumerable<AssemblyRegistration> GetAssemblyRegistrations(ICollection<string> diagnostics)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                var metadata = (FrameworkArchitectureAssemblyAttribute)Attribute.GetCustomAttribute(
                    assembly,
                    typeof(FrameworkArchitectureAssemblyAttribute),
                    false);
                if (metadata == null)
                {
                    continue;
                }

                var stableSegments = SplitPath(metadata.GroupPath);
                var displaySegments = SplitPath(metadata.DisplayPath);
                if (stableSegments.Length == 0 || stableSegments.Length != displaySegments.Length)
                {
                    diagnostics.Add(
                        $"程序集 {assembly.GetName().Name} 的架构分组路径无效：" +
                        $"'{metadata.GroupPath}' / '{metadata.DisplayPath}'。");
                    continue;
                }

                yield return new AssemblyRegistration(
                    assembly,
                    metadata,
                    stableSegments,
                    displaySegments);
            }
        }

        private static FrameworkArchitectureGroupDescriptor GetOrCreateGroup(
            FrameworkArchitectureGroupDescriptor rootGroup,
            IDictionary<string, FrameworkArchitectureGroupDescriptor> groupsById,
            AssemblyRegistration registration,
            ICollection<string> diagnostics)
        {
            var parent = rootGroup;
            var path = string.Empty;
            for (var segmentIndex = 0; segmentIndex < registration.StableSegments.Length; segmentIndex++)
            {
                path = string.IsNullOrEmpty(path)
                    ? registration.StableSegments[segmentIndex]
                    : $"{path}/{registration.StableSegments[segmentIndex]}";
                var displayName = registration.DisplaySegments[segmentIndex];
                var order = segmentIndex < registration.Metadata.OrderPath.Length
                    ? registration.Metadata.OrderPath[segmentIndex]
                    : 0;

                if (!groupsById.TryGetValue(path, out var group))
                {
                    group = new FrameworkArchitectureGroupDescriptor(path, displayName, order, parent);
                    groupsById.Add(path, group);
                    parent.AddChild(group);
                }
                else if (!string.Equals(group.DisplayName, displayName, StringComparison.Ordinal) ||
                         group.Order != order ||
                         !ReferenceEquals(group.Parent, parent))
                {
                    diagnostics.Add(
                        $"架构分组 '{path}' 在程序集 {registration.Assembly.GetName().Name} 中存在名称、顺序或父级冲突。");
                    return null;
                }

                parent = group;
            }

            parent.AddAssembly(registration.Assembly.GetName().Name, registration.Metadata.Responsibility);
            return parent;
        }

        private static string[] SplitPath(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Array.Empty<string>()
                : value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(segment => segment.Trim())
                    .Where(segment => !string.IsNullOrEmpty(segment))
                    .ToArray();
        }

        #endregion

        #region 类型与关系

        private static IEnumerable<Type> GetTargetTypes(Assembly assembly)
        {
            return SafeGetTypes(assembly)
                .Where(type => type.DeclaringType == null)
                // Unity 与编译器会在每个程序集生成若干无命名空间辅助类型；它们不是项目脚本节点。
                .Where(type => type.Namespace == "FrameWork_Ranger" ||
                               (type.Namespace != null &&
                                type.Namespace.StartsWith("FrameWork_Ranger.", StringComparison.Ordinal)))
                .Where(type => type.IsClass || type.IsInterface || type.IsEnum ||
                               type.IsValueType && !type.IsPrimitive)
                .Where(type => !typeof(MulticastDelegate).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
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

        internal static int CompareDescriptors(
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

        #endregion

        private sealed class AssemblyRegistration
        {
            internal Assembly Assembly { get; }
            internal FrameworkArchitectureAssemblyAttribute Metadata { get; }
            internal string[] StableSegments { get; }
            internal string[] DisplaySegments { get; }
            internal FrameworkArchitectureGroupDescriptor Group { get; set; }

            internal AssemblyRegistration(
                Assembly assembly,
                FrameworkArchitectureAssemblyAttribute metadata,
                string[] stableSegments,
                string[] displaySegments)
            {
                Assembly = assembly;
                Metadata = metadata;
                StableSegments = stableSegments;
                DisplaySegments = displaySegments;
            }
        }
    }
}
