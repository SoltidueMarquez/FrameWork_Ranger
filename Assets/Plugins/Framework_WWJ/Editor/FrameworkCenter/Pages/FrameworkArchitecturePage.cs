using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// Framework Center 的声明式代码架构图页面。
    /// </summary>
    [FrameworkArchitecture(
        "Center 代码架构页",
        "组合架构目录、分层节点图、筛选与节点详情。",
        FrameworkArchitectureLayer.EditorIntegration,
        230,
        typeof(FrameworkArchitectureCatalogBuilder),
        typeof(FrameworkArchitectureGraphDrawer),
        typeof(FrameworkArchitectureDetailDrawer))]
    [FrameworkCenterPageExtension]
    internal sealed class FrameworkArchitecturePage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords = { "类图", "节点", "职责", "源码", "Runtime", "Editor" };

        private FrameworkArchitectureCatalog m_catalog;
        private FrameworkArchitectureTypeDescriptor m_selected;
        private FrameworkArchitectureAssemblyFilter m_filter;
        private string m_searchText = string.Empty;
        private readonly FrameworkGraphViewportState m_graphViewport = new FrameworkGraphViewportState();

        public FrameworkArchitecturePage()
        {
        }

        public override string PageId => "framework.architecture";
        public override string DisplayName => "代码架构";
        public override string Description => "查看 Runtime 与 Editor 类/接口的分层职责、关系和源码。";
        public override string Category => "架构";
        public override int Order => 0;
        public override IReadOnlyList<string> Keywords => s_keywords;
        public override string HelpDocumentPath =>
            "Assets/Plugins/Framework_WWJ/Docs/03_Architecture/EditorCenter/02_Declarative_Code_Graph.md";

        public override void OnActivated(FrameworkCenterPageContext context)
        {
            EnsureCatalog();
        }

        public override void OnGUI(FrameworkCenterPageContext context)
        {
            EnsureCatalog();
            DrawToolbar();
            DrawDiagnostics();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            m_selected = FrameworkArchitectureGraphDrawer.Draw(
                m_catalog,
                m_graphViewport,
                m_selected,
                m_searchText,
                m_filter);
            EditorGUILayout.EndVertical();
            FrameworkArchitectureDetailDrawer.Draw(m_selected);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var nextFilter = (FrameworkArchitectureAssemblyFilter)EditorGUILayout.EnumPopup(
                m_filter,
                EditorStyles.toolbarPopup,
                GUILayout.Width(105f));
            if (nextFilter != m_filter)
            {
                m_filter = nextFilter;
                m_graphViewport.RequestFrameAll();
            }
            m_searchText = GUILayout.TextField(
                m_searchText ?? string.Empty,
                EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(220f));
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                FrameworkSourceScriptIndex.Clear();
                m_catalog = FrameworkArchitectureCatalogBuilder.Build();
                m_selected = null;
                m_graphViewport.RequestFrameAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiagnostics()
        {
            if (m_catalog.Diagnostics.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                $"架构元数据存在 {m_catalog.Diagnostics.Count} 项待处理问题。展开 Console 或刷新后检查节点详情。",
                MessageType.Warning);
            for (var i = 0; i < m_catalog.Diagnostics.Count; i++)
            {
                EditorGUILayout.LabelField($"• {m_catalog.Diagnostics[i]}", FrameworkCenterStyles.Description);
            }
        }

        private void EnsureCatalog()
        {
            if (m_catalog == null)
            {
                m_catalog = FrameworkArchitectureCatalogBuilder.Build();
            }
        }
    }
}
