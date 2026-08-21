using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// Framework Center 的单画布可展开代码架构页面。
    /// </summary>
    [FrameworkArchitecture(
        "Center 代码架构页",
        "组合生产程序集目录、可折叠分组泳道、类型关系、搜索与节点详情。",
        FrameworkArchitectureLayer.EditorIntegration,
        230,
        typeof(FrameworkArchitectureCatalogBuilder),
        typeof(FrameworkArchitectureGraphLayout),
        typeof(FrameworkArchitectureGraphDrawer),
        typeof(FrameworkArchitectureDetailDrawer))]
    [FrameworkCenterPageExtension]
    internal sealed class FrameworkArchitecturePage : FrameworkCenterPage
    {
        private static readonly string[] s_keywords =
        {
            "类图", "节点", "职责", "源码", "Runtime", "Editor", "模块", "分组", "展开",
        };

        #region 页面状态

        private FrameworkArchitectureCatalog m_catalog;
        private FrameworkArchitectureGroupDescriptor m_selectedGroup;
        private FrameworkArchitectureTypeDescriptor m_selectedType;
        private FrameworkArchitectureGraphLayout m_lastLayout;
        private string m_searchText = string.Empty;
        private int m_searchMatchCount;
        private bool m_frameSearchMatches;
        private bool m_expansionStateRestored;
        private FrameworkArchitectureGraphLayout.RelationVisibility m_relationVisibility =
            FrameworkArchitectureGraphLayout.RelationVisibility.All;

        private readonly FrameworkArchitectureGraphLayout.ExpansionState m_expansionState =
            new FrameworkArchitectureGraphLayout.ExpansionState();
        private readonly FrameworkGraphViewportState m_graphViewport =
            new FrameworkGraphViewportState(0.10f, 2f);

        #endregion

        public FrameworkArchitecturePage()
        {
        }

        public override string PageId => "framework.architecture";
        public override string DisplayName => "代码架构";
        public override string Description => "在一张可展开画布中查看生产分组、脚本职责、关系与源码。";
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
            var result = FrameworkArchitectureGraphDrawer.Draw(
                m_catalog,
                m_graphViewport,
                m_expansionState,
                m_selectedGroup,
                m_selectedType,
                m_searchText,
                m_relationVisibility,
                m_frameSearchMatches);
            m_lastLayout = result.Layout;
            m_selectedGroup = result.SelectedGroup;
            m_selectedType = result.SelectedType;
            m_searchMatchCount = result.SearchMatchCount;
            m_frameSearchMatches = false;
            EditorGUILayout.EndVertical();

            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
        }

        #region 工具栏

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            using (new EditorGUI.DisabledScope(!string.IsNullOrWhiteSpace(m_searchText)))
            {
                if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                {
                    m_expansionState.ExpandAll(m_catalog);
                    m_graphViewport.RequestFrameAll();
                }

                if (GUILayout.Button("全部收起", EditorStyles.toolbarButton, GUILayout.Width(62f)))
                {
                    m_expansionState.CollapseAll();
                    m_selectedGroup = null;
                    m_selectedType = null;
                    m_graphViewport.RequestFrameAll();
                }
            }

            GUILayout.Space(8f);
            DrawRelationToggle(
                "继承",
                FrameworkArchitectureGraphLayout.RelationVisibility.Inheritance,
                44f);
            DrawRelationToggle(
                "接口",
                FrameworkArchitectureGraphLayout.RelationVisibility.InterfaceImplementation,
                44f);
            DrawRelationToggle(
                "协作",
                FrameworkArchitectureGraphLayout.RelationVisibility.Collaboration,
                44f);

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrWhiteSpace(m_searchText))
            {
                GUILayout.Label(
                    $"{m_searchMatchCount} 个结果",
                    FrameworkCenterStyles.ToolbarHint,
                    GUILayout.Width(62f));
            }

            EditorGUI.BeginChangeCheck();
            var nextSearch = GUILayout.TextField(
                m_searchText ?? string.Empty,
                EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(210f),
                GUILayout.MaxWidth(360f));
            if (EditorGUI.EndChangeCheck())
            {
                m_searchText = nextSearch;
                m_frameSearchMatches = !string.IsNullOrWhiteSpace(m_searchText);
            }

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                RefreshCatalog();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRelationToggle(
            string label,
            FrameworkArchitectureGraphLayout.RelationVisibility relation,
            float width)
        {
            var wasEnabled = (m_relationVisibility & relation) != 0;
            var isEnabled = GUILayout.Toggle(
                wasEnabled,
                label,
                EditorStyles.toolbarButton,
                GUILayout.Width(width));
            if (wasEnabled == isEnabled)
            {
                return;
            }

            if (isEnabled)
            {
                m_relationVisibility |= relation;
            }
            else
            {
                m_relationVisibility &= ~relation;
            }
        }

        #endregion

        #region 详情与分组操作

        private void DrawDetailPanel()
        {
            if (m_selectedType != null)
            {
                FrameworkArchitectureDetailDrawer.DrawType(m_selectedType);
                return;
            }

            FrameworkArchitectureGraphLayout.GroupEntry entry = null;
            var hasEntry = m_selectedGroup != null &&
                           m_lastLayout != null &&
                           m_lastLayout.Groups.TryGetValue(m_selectedGroup, out entry);
            FrameworkArchitectureDetailDrawer.DrawGroup(
                m_selectedGroup,
                hasEntry && entry.IsExpanded,
                hasEntry && string.IsNullOrWhiteSpace(m_searchText),
                ToggleSelectedGroup);
        }

        private void ToggleSelectedGroup(FrameworkArchitectureGroupDescriptor group)
        {
            if (group == null || m_lastLayout == null ||
                !m_lastLayout.Groups.TryGetValue(group, out var entry))
            {
                return;
            }

            if (entry.IsExpanded &&
                m_selectedType != null &&
                IsSameOrDescendant(m_selectedType.Group, group))
            {
                m_selectedType = null;
                m_selectedGroup = group;
            }

            m_expansionState.Toggle(group, entry.HeaderRect.center);
        }

        private static bool IsSameOrDescendant(
            FrameworkArchitectureGroupDescriptor candidate,
            FrameworkArchitectureGroupDescriptor ancestor)
        {
            var current = candidate;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        private void DrawDiagnostics()
        {
            if (m_catalog.Diagnostics.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                $"架构元数据存在 {m_catalog.Diagnostics.Count} 项待处理问题。刷新后仍存在时请补齐生产类型声明。",
                MessageType.Warning);
            for (var i = 0; i < m_catalog.Diagnostics.Count; i++)
            {
                EditorGUILayout.LabelField($"• {m_catalog.Diagnostics[i]}", FrameworkCenterStyles.Description);
            }
        }

        #endregion

        #region 目录生命周期

        private void EnsureCatalog()
        {
            if (m_catalog == null)
            {
                m_catalog = FrameworkArchitectureCatalogBuilder.Build();
            }

            if (m_expansionStateRestored)
            {
                return;
            }

            m_expansionState.Restore(m_catalog);
            m_expansionStateRestored = true;
        }

        private void RefreshCatalog()
        {
            var selectedGroupId = m_selectedGroup?.GroupId;
            var selectedType = m_selectedType?.Type;
            FrameworkSourceScriptIndex.Clear();
            m_catalog = FrameworkArchitectureCatalogBuilder.Build();
            m_expansionState.Sanitize(m_catalog);
            m_selectedGroup = string.IsNullOrEmpty(selectedGroupId)
                ? null
                : m_catalog.FindGroup(selectedGroupId);
            m_selectedType = selectedType == null
                ? null
                : m_catalog.Nodes.FirstOrDefault(node => node.Type == selectedType);
            m_lastLayout = null;
            m_graphViewport.RequestFrameAll();
        }

        #endregion
    }
}
