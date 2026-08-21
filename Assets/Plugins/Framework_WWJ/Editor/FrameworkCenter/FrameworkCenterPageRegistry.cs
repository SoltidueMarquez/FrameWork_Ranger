using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 使用 TypeCache 构建 Framework Center 页面目录，并保证发现结果稳定且错误可见。
    /// </summary>
    [FrameworkArchitecture(
        "Center 页面注册表",
        "自动发现、实例化、稳定排序页面，并处理重复 ID 和无效扩展。",
        FrameworkArchitectureLayer.EditorIntegration,
        120,
        typeof(FrameworkCenterPage))]
    internal sealed class FrameworkCenterPageRegistry
    {
        private readonly List<FrameworkCenterPage> m_pages = new List<FrameworkCenterPage>();
        private readonly Dictionary<string, FrameworkCenterPage> m_pagesById =
            new Dictionary<string, FrameworkCenterPage>(StringComparer.Ordinal);
        private readonly List<string> m_diagnostics = new List<string>();

        internal IReadOnlyList<FrameworkCenterPage> Pages => m_pages;

        internal IReadOnlyList<string> Diagnostics => m_diagnostics;

        internal FrameworkCenterPageRegistry()
        {
            DiscoverPages(TypeCache.GetTypesDerivedFrom<FrameworkCenterPage>()
                .Where(type => type.IsDefined(typeof(FrameworkCenterPageExtensionAttribute), false)));
        }

        internal FrameworkCenterPageRegistry(IEnumerable<Type> candidateTypes)
        {
            DiscoverPages(candidateTypes);
        }

        internal bool TryGetPage(string pageId, out FrameworkCenterPage page)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                page = null;
                return false;
            }

            return m_pagesById.TryGetValue(pageId, out page);
        }

        private void DiscoverPages(IEnumerable<Type> candidateTypes)
        {
            var instances = new List<FrameworkCenterPage>();
            foreach (var type in candidateTypes.OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    m_diagnostics.Add($"跳过页面类型 {type.FullName}：必须是非抽象、非泛型并具有公共无参构造。" );
                    continue;
                }

                try
                {
                    instances.Add((FrameworkCenterPage)Activator.CreateInstance(type));
                }
                catch (Exception exception)
                {
                    m_diagnostics.Add($"页面类型 {type.FullName} 创建失败：{exception.Message}");
                }
            }

            instances.Sort(ComparePages);
            for (var i = 0; i < instances.Count; i++)
            {
                var page = instances[i];
                if (string.IsNullOrWhiteSpace(page.PageId))
                {
                    m_diagnostics.Add($"页面类型 {page.GetType().FullName} 没有有效 PageId。" );
                    continue;
                }

                if (m_pagesById.ContainsKey(page.PageId))
                {
                    m_diagnostics.Add($"页面 ID '{page.PageId}' 重复，已保留排序靠前的实现。" );
                    continue;
                }

                m_pages.Add(page);
                m_pagesById.Add(page.PageId, page);
            }
        }

        private static int ComparePages(FrameworkCenterPage left, FrameworkCenterPage right)
        {
            var category = string.Compare(left.Category, right.Category, StringComparison.Ordinal);
            if (category != 0)
            {
                return category;
            }

            var order = left.Order.CompareTo(right.Order);
            return order != 0
                ? order
                : string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal);
        }
    }
}
