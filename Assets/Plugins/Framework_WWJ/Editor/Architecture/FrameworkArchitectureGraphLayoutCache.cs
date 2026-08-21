using System;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 缓存代码架构布局，避免普通 IMGUI Repaint 重复执行递归布局和关系聚合。
    /// </summary>
    [FrameworkArchitecture(
        "架构图布局缓存",
        "只在目录、展开、位置、搜索或关系筛选变化时重建复合布局。",
        FrameworkArchitectureLayer.EditorIntegration,
        348,
        typeof(FrameworkArchitectureGraphLayout),
        typeof(FrameworkArchitectureGraphPositionState))]
    internal sealed class FrameworkArchitectureGraphLayoutCache
    {
        private FrameworkArchitectureCatalog m_catalog;
        private FrameworkArchitectureGraphLayout m_layout;
        private int m_expansionRevision = -1;
        private int m_positionRevision = -1;
        private string m_searchText = string.Empty;
        private FrameworkArchitectureGraphLayout.RelationVisibility m_relationVisibility;

        internal int BuildCount { get; private set; }

        internal FrameworkArchitectureGraphLayout GetOrBuild(
            FrameworkArchitectureCatalog catalog,
            FrameworkArchitectureGraphLayout.ExpansionState expansionState,
            FrameworkArchitectureGraphPositionState positionState,
            string searchText,
            FrameworkArchitectureGraphLayout.RelationVisibility relationVisibility)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (expansionState == null)
            {
                throw new ArgumentNullException(nameof(expansionState));
            }

            if (positionState == null)
            {
                throw new ArgumentNullException(nameof(positionState));
            }

            var normalizedSearch = searchText?.Trim() ?? string.Empty;
            if (m_layout != null &&
                ReferenceEquals(m_catalog, catalog) &&
                m_expansionRevision == expansionState.Revision &&
                m_positionRevision == positionState.Revision &&
                string.Equals(m_searchText, normalizedSearch, StringComparison.Ordinal) &&
                m_relationVisibility == relationVisibility)
            {
                return m_layout;
            }

            m_catalog = catalog;
            m_expansionRevision = expansionState.Revision;
            m_positionRevision = positionState.Revision;
            m_searchText = normalizedSearch;
            m_relationVisibility = relationVisibility;
            m_layout = FrameworkArchitectureGraphLayout.Build(
                catalog,
                expansionState,
                positionState,
                normalizedSearch,
                relationVisibility);
            BuildCount++;
            return m_layout;
        }

        internal void Invalidate()
        {
            m_catalog = null;
            m_layout = null;
        }
    }
}
