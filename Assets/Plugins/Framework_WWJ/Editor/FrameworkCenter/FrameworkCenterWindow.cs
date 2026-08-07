using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// Framework_WWJ 所有核心编辑器工具的统一窗口宿主。
    /// </summary>
    [FrameworkArchitecture(
        "Framework Center",
        "承载可发现页面、搜索、最近访问、多标签和页面级异常隔离。",
        FrameworkArchitectureLayer.EditorIntegration,
        150,
        typeof(FrameworkCenterPageRegistry),
        typeof(FrameworkCenterStateStore))]
    internal sealed class FrameworkCenterWindow : OdinEditorWindow
    {
        private const string OverviewPageId = "framework.overview";
        private const string HelpPageId = "framework.help";
        private const string SearchControlName = "FrameworkCenterSearch";
        private const float TopBarHeight = 42f;
        private const float NavigationWidth = 208f;
        private const float TabBarHeight = 30f;
        private const float NavigationItemHeight = 30f;
        private const int RecentLimit = 8;

        #region 运行时状态

        private FrameworkCenterPageRegistry m_registry;
        private FrameworkCenterStateStore m_stateStore;
        private FrameworkCenterStateData m_state;
        private FrameworkCenterPageContext m_context;
        private FrameworkCenterPage m_activePage;
        private readonly Dictionary<string, Exception> m_pageErrors =
            new Dictionary<string, Exception>(StringComparer.Ordinal);

        private string m_searchText = string.Empty;
        private Vector2 m_navigationScroll;
        private Vector2 m_contentScroll;
        private Vector2 m_tabScroll;

        #endregion

        #region 窗口生命周期

        [MenuItem("Framework_WWJ/Framework Center", priority = 0)]
        private static void OpenWindow()
        {
            var window = GetWindow<FrameworkCenterWindow>();
            window.titleContent = new GUIContent("Framework Center");
            window.minSize = new Vector2(1040f, 620f);
            window.Show();
            window.Focus();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Framework Center");
            minSize = new Vector2(1040f, 620f);
            wantsMouseMove = true;
            m_registry = new FrameworkCenterPageRegistry();
            m_stateStore = new FrameworkCenterStateStore();
            m_state = m_stateStore.Load();
            m_context = new FrameworkCenterPageContext(this);
            SanitizeState();
            ActivatePage(m_state.activePageId);
            // 旧版本可能已经把测试 PageId 写入 Library；启用窗口时立即落盘清理结果，
            // 避免异常关闭窗口后下次会话又从旧 JSON 恢复这些无效标签。
            SaveState();
        }

        protected override void OnDestroy()
        {
            m_activePage?.OnDeactivated(m_context);
            SaveState();
            base.OnDestroy();
        }

        protected override void OnImGUI()
        {
            HandleKeyboardShortcuts();

            var fullRect = new Rect(0f, 0f, position.width, position.height);
            var topRect = new Rect(0f, 0f, fullRect.width, TopBarHeight);
            var navigationRect = new Rect(
                0f,
                TopBarHeight,
                NavigationWidth,
                Mathf.Max(0f, fullRect.height - TopBarHeight));
            var workspaceRect = new Rect(
                NavigationWidth,
                TopBarHeight,
                Mathf.Max(0f, fullRect.width - NavigationWidth),
                Mathf.Max(0f, fullRect.height - TopBarHeight));

            EditorGUI.DrawRect(fullRect, FrameworkCenterStyles.WindowBackgroundColor);
            DrawToolbar(topRect);
            DrawNavigation(navigationRect);
            DrawWorkspace(workspaceRect);
        }

        #endregion

        #region 页面导航

        internal void OpenPage(string pageId)
        {
            if (!m_registry.TryGetPage(pageId, out _))
            {
                return;
            }

            if (!m_state.openTabs.Contains(pageId))
            {
                m_state.openTabs.Add(pageId);
            }

            RememberRecent(pageId);
            ActivatePage(pageId);
            SaveState();
        }

        internal void OpenHelp(string assetPath)
        {
            if (m_registry.TryGetPage(HelpPageId, out var page) && page is FrameworkHelpPage helpPage)
            {
                helpPage.SetDocumentPath(assetPath);
                OpenPage(HelpPageId);
            }
        }

        private void ActivatePage(string pageId)
        {
            if (!m_registry.TryGetPage(pageId, out var nextPage))
            {
                m_registry.TryGetPage(OverviewPageId, out nextPage);
            }

            if (nextPage == null || ReferenceEquals(m_activePage, nextPage))
            {
                return;
            }

            m_activePage?.OnDeactivated(m_context);
            m_activePage = nextPage;
            m_state.activePageId = nextPage.PageId;
            if (!m_state.openTabs.Contains(nextPage.PageId))
            {
                m_state.openTabs.Add(nextPage.PageId);
            }

            nextPage.OnActivated(m_context);
            m_contentScroll = Vector2.zero;
        }

        private void ClosePage(string pageId)
        {
            var index = m_state.openTabs.IndexOf(pageId);
            if (index < 0)
            {
                return;
            }

            var closingActive = m_activePage != null && m_activePage.PageId == pageId;
            if (closingActive)
            {
                m_activePage.OnDeactivated(m_context);
                m_activePage = null;
            }

            m_state.openTabs.RemoveAt(index);
            m_pageErrors.Remove(pageId);
            if (m_state.openTabs.Count == 0)
            {
                m_state.openTabs.Add(OverviewPageId);
            }

            if (closingActive)
            {
                ActivatePage(m_state.openTabs[Mathf.Clamp(index - 1, 0, m_state.openTabs.Count - 1)]);
            }

            SaveState();
        }

        #endregion

        #region 窗口绘制

        private void DrawToolbar(Rect rect)
        {
            FrameworkCenterStyles.DrawPanel(rect, FrameworkCenterStyles.PanelColor);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, rect.height - 10f));
            EditorGUILayout.BeginHorizontal();

            var logoRect = GUILayoutUtility.GetRect(38f, 24f, GUILayout.Width(38f), GUILayout.Height(24f));
            EditorGUI.DrawRect(logoRect, FrameworkCenterStyles.AccentColor);
            GUI.Label(logoRect, "WWJ", FrameworkCenterStyles.StatusBadge);
            GUILayout.Space(8f);

            EditorGUILayout.BeginVertical(GUILayout.Width(154f));
            GUILayout.Label("Framework Center", FrameworkCenterStyles.TopTitle, GUILayout.Height(17f));
            GUILayout.Label("模块化 Unity 框架工作台", FrameworkCenterStyles.TopSubtitle, GUILayout.Height(14f));
            EditorGUILayout.EndVertical();

            GUILayout.Space(18f);
            GUI.SetNextControlName(SearchControlName);
            m_searchText = GUILayout.TextField(
                m_searchText ?? string.Empty,
                EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(260f),
                GUILayout.MaxWidth(420f),
                GUILayout.Height(22f));
            if (!string.IsNullOrEmpty(m_searchText) &&
                GUILayout.Button(new GUIContent("×", "清空搜索"), EditorStyles.toolbarButton, GUILayout.Width(26f)))
            {
                m_searchText = string.Empty;
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();
            DrawRuntimeBadge();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawRuntimeBadge()
        {
            var text = Application.isPlaying ? Framework.State.ToString() : "Edit Mode";
            var rect = GUILayoutUtility.GetRect(92f, 24f, GUILayout.Width(92f), GUILayout.Height(24f));
            var color = Application.isPlaying && Framework.IsReady
                ? new Color(0.24f, 0.56f, 0.38f)
                : FrameworkCenterStyles.SelectedColor;
            EditorGUI.DrawRect(rect, color);
            FrameworkCenterStyles.DrawBorder(rect, FrameworkCenterStyles.BorderColor);
            GUI.Label(rect, text, FrameworkCenterStyles.StatusBadge);
        }

        private void DrawNavigation(Rect rect)
        {
            FrameworkCenterStyles.DrawPanel(rect, FrameworkCenterStyles.PanelColor);
            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 10f, rect.width - 16f, rect.height - 20f));
            m_navigationScroll = EditorGUILayout.BeginScrollView(m_navigationScroll);

            var pages = FilterPages().ToArray();
            if (!string.IsNullOrWhiteSpace(m_searchText))
            {
                DrawNavigationHeading($"搜索结果  {pages.Length}");
                DrawPageButtons(pages);
            }
            else
            {
                DrawRecentPages();
                foreach (var group in pages.GroupBy(page => page.Category))
                {
                    GUILayout.Space(8f);
                    DrawNavigationHeading(group.Key);
                    DrawPageButtons(group);
                }
            }

            DrawRegistryDiagnostics();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawWorkspace(Rect rect)
        {
            EditorGUI.DrawRect(rect, FrameworkCenterStyles.WindowBackgroundColor);
            GUILayout.BeginArea(rect);
            DrawTabs();

            m_contentScroll = EditorGUILayout.BeginScrollView(m_contentScroll);
            GUILayout.Space(12f);
            if (m_activePage == null)
            {
                EditorGUILayout.HelpBox("没有可用的 Framework Center 页面。", MessageType.Error);
            }
            else
            {
                DrawActivePage();
            }

            GUILayout.Space(18f);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTabs()
        {
            var barRect = GUILayoutUtility.GetRect(
                1f,
                TabBarHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(TabBarHeight));
            FrameworkCenterStyles.DrawPanel(barRect, FrameworkCenterStyles.PanelColor);

            GUI.BeginGroup(barRect);
            var helpWidth = 34f;
            var scrollRect = new Rect(4f, 2f, Mathf.Max(1f, barRect.width - helpWidth - 8f), TabBarHeight - 4f);
            GUILayout.BeginArea(scrollRect);
            m_tabScroll = EditorGUILayout.BeginScrollView(
                m_tabScroll,
                true,
                false,
                GUIStyle.none,
                GUIStyle.none,
                GUIStyle.none,
                GUILayout.Height(scrollRect.height));
            EditorGUILayout.BeginHorizontal();
            for (var i = 0; i < m_state.openTabs.Count; i++)
            {
                var pageId = m_state.openTabs[i];
                if (!m_registry.TryGetPage(pageId, out var page))
                {
                    continue;
                }

                if (DrawTab(page, pageId))
                {
                    break;
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            var helpRect = new Rect(barRect.width - helpWidth + 2f, 3f, 28f, TabBarHeight - 6f);
            using (new EditorGUI.DisabledScope(
                       m_activePage == null || string.IsNullOrWhiteSpace(m_activePage.HelpDocumentPath)))
            {
                if (GUI.Button(helpRect, new GUIContent("?", "打开当前页面帮助"), EditorStyles.toolbarButton) &&
                    m_activePage != null)
                {
                    OpenHelp(m_activePage.HelpDocumentPath);
                }
            }

            GUI.EndGroup();
        }

        private bool DrawTab(FrameworkCenterPage page, string pageId)
        {
            var selected = m_activePage != null && m_activePage.PageId == pageId;
            var labelWidth = FrameworkCenterStyles.Tab.CalcSize(new GUIContent(page.DisplayName)).x;
            var width = Mathf.Clamp(labelWidth + 38f, 96f, 200f);
            var rect = GUILayoutUtility.GetRect(width, 26f, GUILayout.Width(width), GUILayout.Height(26f));
            var hovered = rect.Contains(Event.current.mousePosition);

            EditorGUI.DrawRect(
                rect,
                selected ? FrameworkCenterStyles.SelectedColor :
                hovered ? FrameworkCenterStyles.HoverColor : FrameworkCenterStyles.CardColor);
            FrameworkCenterStyles.DrawBorder(rect, FrameworkCenterStyles.BorderColor);
            if (selected)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), FrameworkCenterStyles.AccentColor);
            }

            var labelRect = new Rect(rect.x + 2f, rect.y, rect.width - 24f, rect.height);
            GUI.Label(labelRect, new GUIContent(page.DisplayName, page.Description), FrameworkCenterStyles.Tab);
            if (GUI.Button(labelRect, GUIContent.none, GUIStyle.none))
            {
                ActivatePage(pageId);
                SaveState();
            }

            var closeRect = new Rect(rect.xMax - 22f, rect.y + 3f, 18f, rect.height - 6f);
            if (GUI.Button(closeRect, new GUIContent("×", "关闭标签"), EditorStyles.miniButton))
            {
                ClosePage(pageId);
                return true;
            }

            return false;
        }

        private void DrawActivePage()
        {
            DrawPageHeader();
            GUILayout.Space(10f);

            if (m_pageErrors.TryGetValue(m_activePage.PageId, out var previousError))
            {
                EditorGUILayout.HelpBox($"页面上次绘制失败：{previousError.Message}", MessageType.Error);
                if (GUILayout.Button("重新载入页面", GUILayout.Height(30f)))
                {
                    m_pageErrors.Remove(m_activePage.PageId);
                }

                return;
            }

            try
            {
                m_activePage.OnGUI(m_context);
            }
            catch (Exception exception)
            {
                m_pageErrors[m_activePage.PageId] = exception;
                Debug.LogException(exception);
            }
        }

        private void DrawPageHeader()
        {
            var availableWidth = Mathf.Max(320f, position.width - NavigationWidth - 28f);
            var titleHeight = FrameworkCenterStyles.PageTitle.CalcHeight(
                new GUIContent(m_activePage.DisplayName),
                availableWidth - 36f);
            var descriptionHeight = FrameworkCenterStyles.Description.CalcHeight(
                new GUIContent(m_activePage.Description),
                availableWidth - 36f);
            var height = Mathf.Max(68f, 22f + titleHeight + descriptionHeight);
            var rect = GUILayoutUtility.GetRect(
                availableWidth,
                height,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(height));

            FrameworkCenterStyles.DrawPanel(rect, FrameworkCenterStyles.CardColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), FrameworkCenterStyles.AccentColor);
            var titleRect = new Rect(rect.x + 18f, rect.y + 10f, rect.width - 32f, titleHeight);
            var descriptionRect = new Rect(
                rect.x + 18f,
                titleRect.yMax + 3f,
                rect.width - 32f,
                descriptionHeight);
            GUI.Label(titleRect, m_activePage.DisplayName, FrameworkCenterStyles.PageTitle);
            GUI.Label(descriptionRect, m_activePage.Description, FrameworkCenterStyles.Description);
        }

        #endregion

        #region 导航绘制

        private void DrawRecentPages()
        {
            if (m_state.recentPageIds.Count == 0)
            {
                return;
            }

            DrawNavigationHeading("最近访问");
            for (var i = 0; i < m_state.recentPageIds.Count; i++)
            {
                var pageId = m_state.recentPageIds[i];
                if (m_registry.TryGetPage(pageId, out var page))
                {
                    DrawNavigationItem(page);
                }
            }
        }

        private void DrawPageButtons(IEnumerable<FrameworkCenterPage> pages)
        {
            foreach (var page in pages)
            {
                DrawNavigationItem(page);
            }
        }

        private void DrawNavigationItem(FrameworkCenterPage page)
        {
            var rect = GUILayoutUtility.GetRect(
                1f,
                NavigationItemHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(NavigationItemHeight));
            var selected = m_activePage != null && m_activePage.PageId == page.PageId;
            var hovered = rect.Contains(Event.current.mousePosition);
            if (selected || hovered)
            {
                EditorGUI.DrawRect(
                    rect,
                    selected ? FrameworkCenterStyles.SelectedColor : FrameworkCenterStyles.HoverColor);
            }

            if (selected)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), FrameworkCenterStyles.AccentColor);
            }

            GUI.Label(rect, new GUIContent(page.DisplayName, page.Description), FrameworkCenterStyles.NavigationLabel);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                OpenPage(page.PageId);
            }
        }

        private static void DrawNavigationHeading(string text)
        {
            GUILayout.Space(4f);
            EditorGUILayout.LabelField(text, FrameworkCenterStyles.NavigationCategory);
            GUILayout.Space(2f);
        }

        private void DrawRegistryDiagnostics()
        {
            if (m_registry.Diagnostics.Count == 0)
            {
                return;
            }

            GUILayout.Space(10f);
            DrawNavigationHeading("扩展诊断");
            for (var i = 0; i < m_registry.Diagnostics.Count; i++)
            {
                EditorGUILayout.HelpBox(m_registry.Diagnostics[i], MessageType.Warning);
            }
        }

        #endregion

        #region 内部辅助

        private IEnumerable<FrameworkCenterPage> FilterPages()
        {
            if (string.IsNullOrWhiteSpace(m_searchText))
            {
                return m_registry.Pages.Where(page => page.PageId != HelpPageId);
            }

            var search = m_searchText.Trim();
            return m_registry.Pages.Where(page =>
                page.PageId != HelpPageId &&
                (Contains(page.DisplayName, search) ||
                 Contains(page.Description, search) ||
                 Contains(page.Category, search) ||
                 page.Keywords.Any(keyword => Contains(keyword, search))));
        }

        private void RememberRecent(string pageId)
        {
            if (pageId == HelpPageId)
            {
                return;
            }

            m_state.recentPageIds.Remove(pageId);
            m_state.recentPageIds.Insert(0, pageId);
            if (m_state.recentPageIds.Count > RecentLimit)
            {
                m_state.recentPageIds.RemoveRange(RecentLimit, m_state.recentPageIds.Count - RecentLimit);
            }
        }

        private void SanitizeState()
        {
            FrameworkCenterStateSanitizer.Sanitize(m_state, m_registry, OverviewPageId);
        }

        private void HandleKeyboardShortcuts()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            var command = currentEvent.control || currentEvent.command;
            if (command && currentEvent.keyCode == KeyCode.K)
            {
                EditorGUI.FocusTextInControl(SearchControlName);
                currentEvent.Use();
            }
            else if (command && currentEvent.keyCode == KeyCode.W && m_activePage != null)
            {
                ClosePage(m_activePage.PageId);
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.Escape && !string.IsNullOrEmpty(m_searchText))
            {
                m_searchText = string.Empty;
                GUI.FocusControl(null);
                currentEvent.Use();
            }
        }

        private void SaveState()
        {
            m_stateStore?.Save(m_state);
        }

        private static bool Contains(string source, string search)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
