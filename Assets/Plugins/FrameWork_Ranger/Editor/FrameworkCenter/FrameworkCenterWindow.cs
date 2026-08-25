using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// FrameWork_Ranger 所有核心编辑器工具的统一窗口与页签宿主。
    /// </summary>
    [FrameworkArchitecture(
        "Framework Center",
        "承载可发现页面、搜索、预览/固定页签和页面级异常隔离。",
        FrameworkArchitectureLayer.EditorIntegration,
        150,
        typeof(FrameworkCenterPageRegistry),
        typeof(FrameworkCenterStateStore),
        typeof(FrameworkCenterTabModel))]
    internal sealed class FrameworkCenterWindow : OdinEditorWindow
    {
        private const string OverviewPageId = "framework.overview";
        private const string HelpPageId = "framework.help";
        private const string SearchControlName = "FrameworkCenterSearch";

        private const float TopBarHeight = 42f;
        private const float NavigationWidth = 208f;
        private const float TabBarHeight = 30f;
        private const float TabHeight = 26f;
        private const float TabGap = 2f;
        private const float NavigationItemHeight = 30f;
        private const float DragThreshold = 4f;
        private const float DragEdgeWidth = 22f;
        private const float DragEdgeScrollStep = 14f;
        private const int PinnedTabDragControlHash = 0x57AA13;

        #region 运行时状态

        private FrameworkCenterPageRegistry m_registry;
        private FrameworkCenterStateStore m_stateStore;
        private FrameworkCenterStateData m_state;
        private FrameworkCenterTabModel m_tabModel;
        private FrameworkCenterPageContext m_context;
        private FrameworkCenterPage m_activePage;
        private readonly Dictionary<string, Exception> m_pageErrors =
            new Dictionary<string, Exception>(StringComparer.Ordinal);

        private string m_searchText = string.Empty;
        private Vector2 m_navigationScroll;
        private Vector2 m_contentScroll;
        private float m_pinnedTabScrollX;

        private string m_dragCandidatePageId = string.Empty;
        private Vector2 m_dragStartPosition;
        private bool m_isDraggingPinnedTab;
        private int m_dragInsertionIndex = -1;

        #endregion

        #region 窗口生命周期

        [MenuItem("FrameWork_Ranger/Framework Center", priority = 0)]
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
            m_tabModel = new FrameworkCenterTabModel(m_state, m_registry, OverviewPageId);

            TransitionActivePage(m_tabModel.ActivePageId);
            // 状态加载时会清理失效或重复的固定页；立即保存，避免异常退出后重复迁移。
            SaveState();
        }

        protected override void OnDestroy()
        {
            CancelPinnedTabDrag();
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
            ApplyTabMutation(() => m_tabModel.OpenPage(pageId));
        }

        internal void OpenHelp(string assetPath)
        {
            if (m_registry.TryGetPage(HelpPageId, out var page) && page is FrameworkHelpPage helpPage)
            {
                helpPage.SetDocumentPath(assetPath);
                OpenPage(HelpPageId);
            }
        }

        private void ApplyTabMutation(Func<bool> mutation)
        {
            var previousActivePageId = m_tabModel.ActivePageId;
            if (!mutation())
            {
                return;
            }

            if (!string.Equals(previousActivePageId, m_tabModel.ActivePageId, StringComparison.Ordinal))
            {
                TransitionActivePage(m_tabModel.ActivePageId);
            }

            SaveState();
            Repaint();
        }

        private void TransitionActivePage(string pageId)
        {
            if (m_activePage != null && m_activePage.PageId == pageId)
            {
                return;
            }

            m_activePage?.OnDeactivated(m_context);
            m_activePage = null;
            if (!m_registry.TryGetPage(pageId, out var nextPage))
            {
                return;
            }

            m_activePage = nextPage;
            nextPage.OnActivated(m_context);
            m_contentScroll = Vector2.zero;
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

            if (m_activePage == null || m_activePage.UseHostContentScroll)
            {
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
            }
            else
            {
                // 主从工作台需要让标题保持在宿主区域内，并把剩余高度交给页面自己的左右滚动区。
                GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                GUILayout.Space(12f);
                DrawActivePage();
                GUILayout.Space(12f);
                GUILayout.EndVertical();
            }

            GUILayout.EndArea();
        }

        #endregion

        #region 页签绘制

        private void DrawTabs()
        {
            var barRect = GUILayoutUtility.GetRect(
                1f,
                TabBarHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(TabBarHeight));
            FrameworkCenterStyles.DrawPanel(barRect, FrameworkCenterStyles.PanelColor);

            GUI.BeginGroup(barRect);
            var helpRect = new Rect(barRect.width - 32f, 3f, 28f, TabHeight - 2f);
            var hasPreview = m_registry.TryGetPage(m_tabModel.PreviewPageId, out var previewPage);
            var previewWidth = hasPreview ? CalculateTabWidth(previewPage) : 0f;
            var previewRect = hasPreview
                ? new Rect(helpRect.x - previewWidth - 4f, 2f, previewWidth, TabHeight)
                : Rect.zero;
            var pinnedRight = hasPreview ? previewRect.x - 4f : helpRect.x - 4f;
            var pinnedViewport = new Rect(4f, 2f, Mathf.Max(1f, pinnedRight - 4f), TabHeight);

            DrawPinnedTabs(pinnedViewport);
            if (hasPreview)
            {
                DrawPreviewTab(previewPage, previewRect);
            }

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

        private void DrawPinnedTabs(Rect viewport)
        {
            var layouts = BuildPinnedTabLayouts();
            var contentWidth = layouts.Count == 0 ? 0f : layouts[layouts.Count - 1].Rect.xMax;
            var maxScroll = Mathf.Max(0f, contentWidth - viewport.width);
            m_pinnedTabScrollX = Mathf.Clamp(m_pinnedTabScrollX, 0f, maxScroll);
            HandlePinnedAreaWheel(viewport, maxScroll);

            GUI.BeginGroup(viewport);
            var stateChanged = false;
            for (var i = 0; i < layouts.Count; i++)
            {
                var layout = layouts[i];
                var drawRect = OffsetForPinnedScroll(layout.Rect);
                if (drawRect.xMax < 0f || drawRect.xMin > viewport.width)
                {
                    continue;
                }

                var selected = m_tabModel.ActivePageId == layout.PageId;
                var hovered = drawRect.Contains(Event.current.mousePosition);
                var dragging = m_isDraggingPinnedTab && m_dragCandidatePageId == layout.PageId;
                DrawTabBackground(drawRect, selected, hovered || dragging);

                var labelRect = GetTabLabelRect(drawRect);
                GUI.Label(
                    labelRect,
                    new GUIContent(layout.Page.DisplayName, layout.Page.Description),
                    FrameworkCenterStyles.Tab);
                EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

                if (FrameworkCenterStyles.DrawPinButton(GetPinRect(drawRect), true))
                {
                    CancelPinnedTabDrag();
                    ApplyTabMutation(() => m_tabModel.Unpin(layout.PageId));
                    stateChanged = true;
                    break;
                }

                if (GUI.Button(
                        GetCloseRect(drawRect),
                        new GUIContent("×", "关闭并取消固定"),
                        EditorStyles.miniButton))
                {
                    CancelPinnedTabDrag();
                    ApplyTabMutation(() => m_tabModel.Close(layout.PageId));
                    stateChanged = true;
                    break;
                }
            }

            if (!stateChanged)
            {
                HandlePinnedTabInput(new Rect(Vector2.zero, viewport.size), layouts, maxScroll);
                DrawPinnedDropIndicator(new Rect(Vector2.zero, viewport.size), layouts);
            }

            GUI.EndGroup();
        }

        private void DrawPreviewTab(FrameworkCenterPage page, Rect rect)
        {
            var selected = m_tabModel.ActivePageId == page.PageId;
            var hovered = rect.Contains(Event.current.mousePosition);
            DrawTabBackground(rect, selected, hovered);

            var labelRect = GetTabLabelRect(rect);
            GUI.Label(
                labelRect,
                new GUIContent(page.DisplayName, $"预览页签 · {page.Description}"),
                FrameworkCenterStyles.PreviewTab);
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);
            if (GUI.Button(labelRect, GUIContent.none, GUIStyle.none))
            {
                OpenPage(page.PageId);
            }

            if (FrameworkCenterStyles.DrawPinButton(GetPinRect(rect), false))
            {
                ApplyTabMutation(m_tabModel.PinPreview);
            }

            if (GUI.Button(
                    GetCloseRect(rect),
                    new GUIContent("×", "关闭预览页签"),
                    EditorStyles.miniButton))
            {
                ApplyTabMutation(() => m_tabModel.Close(page.PageId));
            }
        }

        private List<PinnedTabLayout> BuildPinnedTabLayouts()
        {
            var layouts = new List<PinnedTabLayout>();
            var x = 0f;
            for (var i = 0; i < m_tabModel.PinnedPageIds.Count; i++)
            {
                var pageId = m_tabModel.PinnedPageIds[i];
                if (!m_registry.TryGetPage(pageId, out var page))
                {
                    continue;
                }

                var width = CalculateTabWidth(page);
                layouts.Add(new PinnedTabLayout(pageId, page, new Rect(x, 0f, width, TabHeight)));
                x += width + TabGap;
            }

            return layouts;
        }

        private void HandlePinnedTabInput(
            Rect viewport,
            IReadOnlyList<PinnedTabLayout> layouts,
            float maxScroll)
        {
            var currentEvent = Event.current;
            var controlId = GUIUtility.GetControlID(PinnedTabDragControlHash, FocusType.Passive, viewport);

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                var layout = FindLayoutAtPointer(layouts, currentEvent.mousePosition);
                if (layout != null && GetTabLabelRect(OffsetForPinnedScroll(layout.Rect)).Contains(currentEvent.mousePosition))
                {
                    m_dragCandidatePageId = layout.PageId;
                    m_dragStartPosition = currentEvent.mousePosition;
                    m_isDraggingPinnedTab = false;
                    m_dragInsertionIndex = -1;
                    GUIUtility.hotControl = controlId;
                    currentEvent.Use();
                }

                return;
            }

            if (GUIUtility.hotControl != controlId || string.IsNullOrEmpty(m_dragCandidatePageId))
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDrag)
            {
                if (!m_isDraggingPinnedTab &&
                    Vector2.Distance(m_dragStartPosition, currentEvent.mousePosition) >= DragThreshold)
                {
                    m_isDraggingPinnedTab = true;
                }

                if (m_isDraggingPinnedTab)
                {
                    ScrollPinnedTabsNearDragEdge(currentEvent.mousePosition.x, viewport.width, maxScroll);
                    m_dragInsertionIndex = CalculateInsertionIndex(
                        currentEvent.mousePosition.x + m_pinnedTabScrollX,
                        layouts);
                    Repaint();
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0)
            {
                return;
            }

            var candidatePageId = m_dragCandidatePageId;
            var wasDragging = m_isDraggingPinnedTab;
            var insertionIndex = m_dragInsertionIndex;
            var droppedInside = viewport.Contains(currentEvent.mousePosition);
            var clickedLayout = FindLayout(candidatePageId, layouts);
            var clickedInside = clickedLayout != null &&
                                GetTabLabelRect(OffsetForPinnedScroll(clickedLayout.Rect))
                                    .Contains(currentEvent.mousePosition);
            CancelPinnedTabDrag();
            currentEvent.Use();

            if (wasDragging && droppedInside && insertionIndex >= 0)
            {
                ApplyTabMutation(() =>
                    m_tabModel.MovePinnedToInsertionIndex(candidatePageId, insertionIndex));
            }
            else if (!wasDragging && clickedInside)
            {
                OpenPage(candidatePageId);
            }
        }

        private void HandlePinnedAreaWheel(Rect viewport, float maxScroll)
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.ScrollWheel || !viewport.Contains(currentEvent.mousePosition))
            {
                return;
            }

            var delta = Mathf.Abs(currentEvent.delta.x) > Mathf.Abs(currentEvent.delta.y)
                ? currentEvent.delta.x
                : currentEvent.delta.y;
            m_pinnedTabScrollX = Mathf.Clamp(m_pinnedTabScrollX + delta * 24f, 0f, maxScroll);
            currentEvent.Use();
            Repaint();
        }

        private void ScrollPinnedTabsNearDragEdge(float pointerX, float viewportWidth, float maxScroll)
        {
            if (pointerX < DragEdgeWidth)
            {
                m_pinnedTabScrollX = Mathf.Max(0f, m_pinnedTabScrollX - DragEdgeScrollStep);
            }
            else if (pointerX > viewportWidth - DragEdgeWidth)
            {
                m_pinnedTabScrollX = Mathf.Min(maxScroll, m_pinnedTabScrollX + DragEdgeScrollStep);
            }
        }

        private void DrawPinnedDropIndicator(Rect viewport, IReadOnlyList<PinnedTabLayout> layouts)
        {
            if (!m_isDraggingPinnedTab || m_dragInsertionIndex < 0 || layouts.Count == 0)
            {
                return;
            }

            var insertionIndex = Mathf.Clamp(m_dragInsertionIndex, 0, layouts.Count);
            var contentX = insertionIndex == layouts.Count
                ? layouts[layouts.Count - 1].Rect.xMax + TabGap * 0.5f
                : layouts[insertionIndex].Rect.xMin - TabGap * 0.5f;
            var drawX = contentX - m_pinnedTabScrollX;
            if (drawX < 0f || drawX > viewport.width)
            {
                return;
            }

            EditorGUI.DrawRect(
                new Rect(drawX - 1f, 2f, 2f, viewport.height - 4f),
                FrameworkCenterStyles.AccentColor);
        }

        private void DrawTabBackground(Rect rect, bool selected, bool hovered)
        {
            EditorGUI.DrawRect(
                rect,
                selected
                    ? FrameworkCenterStyles.SelectedColor
                    : hovered ? FrameworkCenterStyles.HoverColor : FrameworkCenterStyles.CardColor);
            FrameworkCenterStyles.DrawBorder(rect, FrameworkCenterStyles.BorderColor);
            if (selected)
            {
                EditorGUI.DrawRect(
                    new Rect(rect.x, rect.yMax - 3f, rect.width, 3f),
                    FrameworkCenterStyles.AccentColor);
            }
        }

        #endregion

        #region 页面内容

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

        private void HandleKeyboardShortcuts()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.Escape && !string.IsNullOrEmpty(m_dragCandidatePageId))
            {
                CancelPinnedTabDrag();
                currentEvent.Use();
                Repaint();
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
                ApplyTabMutation(() => m_tabModel.Close(m_activePage.PageId));
                currentEvent.Use();
            }
            else if (currentEvent.keyCode == KeyCode.Escape && !string.IsNullOrEmpty(m_searchText))
            {
                m_searchText = string.Empty;
                GUI.FocusControl(null);
                currentEvent.Use();
            }
        }

        private void CancelPinnedTabDrag()
        {
            if (!string.IsNullOrEmpty(m_dragCandidatePageId))
            {
                GUIUtility.hotControl = 0;
            }

            m_dragCandidatePageId = string.Empty;
            m_isDraggingPinnedTab = false;
            m_dragInsertionIndex = -1;
        }

        private PinnedTabLayout FindLayoutAtPointer(
            IReadOnlyList<PinnedTabLayout> layouts,
            Vector2 pointer)
        {
            for (var i = 0; i < layouts.Count; i++)
            {
                if (OffsetForPinnedScroll(layouts[i].Rect).Contains(pointer))
                {
                    return layouts[i];
                }
            }

            return null;
        }

        private static PinnedTabLayout FindLayout(
            string pageId,
            IReadOnlyList<PinnedTabLayout> layouts)
        {
            for (var i = 0; i < layouts.Count; i++)
            {
                if (layouts[i].PageId == pageId)
                {
                    return layouts[i];
                }
            }

            return null;
        }

        private static int CalculateInsertionIndex(
            float contentPointerX,
            IReadOnlyList<PinnedTabLayout> layouts)
        {
            for (var i = 0; i < layouts.Count; i++)
            {
                if (contentPointerX < layouts[i].Rect.center.x)
                {
                    return i;
                }
            }

            return layouts.Count;
        }

        private Rect OffsetForPinnedScroll(Rect rect)
        {
            rect.x -= m_pinnedTabScrollX;
            return rect;
        }

        private static Rect GetTabLabelRect(Rect rect)
        {
            return new Rect(rect.x + 2f, rect.y, Mathf.Max(1f, rect.width - 44f), rect.height);
        }

        private static Rect GetPinRect(Rect rect)
        {
            return new Rect(rect.xMax - 42f, rect.y + 3f, 18f, rect.height - 6f);
        }

        private static Rect GetCloseRect(Rect rect)
        {
            return new Rect(rect.xMax - 22f, rect.y + 3f, 18f, rect.height - 6f);
        }

        private static float CalculateTabWidth(FrameworkCenterPage page)
        {
            var labelWidth = FrameworkCenterStyles.Tab.CalcSize(new GUIContent(page.DisplayName)).x;
            return Mathf.Clamp(labelWidth + 58f, 112f, 200f);
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

        private sealed class PinnedTabLayout
        {
            internal string PageId { get; }
            internal FrameworkCenterPage Page { get; }
            internal Rect Rect { get; }

            internal PinnedTabLayout(string pageId, FrameworkCenterPage page, Rect rect)
            {
                PageId = pageId;
                Page = page;
                Rect = rect;
            }
        }
    }
}
