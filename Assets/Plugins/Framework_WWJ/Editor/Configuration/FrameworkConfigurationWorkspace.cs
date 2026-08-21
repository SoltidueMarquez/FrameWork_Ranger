using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 在一张主从页面中编辑中央设置、Global/Scene 模块配置和对应的组合依赖图。
    /// </summary>
    [FrameworkArchitecture(
        "主从配置工作台",
        "使用作用域导航组织项目入口、Global、默认 Scene 与场景覆盖配置。",
        FrameworkArchitectureLayer.EditorIntegration,
        211,
        typeof(FrameworkConfigurationWorkspaceState),
        typeof(ModuleConfigInspector),
        typeof(ModuleDependencyGraphDrawer))]
    internal sealed class FrameworkConfigurationWorkspace : IDisposable
    {
        private const float NavigationWidth = 260f;
        private const float NavigationHeaderHeight = 40f;
        private const float NavigationRowHeight = 44f;
        private const float RightHeaderHeight = 44f;
        private const float MinimumWorkspaceHeight = 460f;
        private const float CompactThreshold = 760f;
        private const string PreviewSceneSessionPrefix =
            "Framework_WWJ.ConfigurationWorkspace.PreviewScene.";

        private readonly FrameworkConfigurationWorkspaceState m_state;
        private readonly FrameworkGraphViewportState m_graphViewport = new FrameworkGraphViewportState();
        private readonly List<string> m_contextLabels = new List<string>();
        private readonly List<FrameworkConfigurationSelection> m_contextSelections =
            new List<FrameworkConfigurationSelection>();
        private readonly List<string> m_projectErrors = new List<string>();
        private readonly List<string> m_projectWarnings = new List<string>();
        private readonly Dictionary<string, int> m_contextErrorCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);

        private Vector2 m_navigationScroll;
        private Vector2 m_contentScroll;
        private UnityEditor.Editor m_configEditor;
        private ModuleConfigBase m_configEditorTarget;
        private FrameworkConfigurationSelection m_selection;
        private string m_lastSelectionId = string.Empty;
        private bool m_showProjectDiagnostics;
        private bool m_previewInitialized;
        private SceneAsset m_previewScene;
        private string m_previewSceneSessionKey;
        private string m_visibleSceneConfigContext = string.Empty;
        private int m_diagnosticsRevision = int.MinValue;
        private bool m_disposed;

        internal FrameworkConfigurationWorkspace(UnityEngine.Object host)
        {
            m_state = new FrameworkConfigurationWorkspaceState(
                FrameworkInlineInspectorHost.BuildObjectIdentity(host));
        }

        #region 绘制入口

        internal void Draw(
            FrameworkProjectSettings settings,
            FrameworkCenterPageContext context = null)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(FrameworkConfigurationWorkspace));
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("固定 FrameworkProjectSettings 资产不存在。", MessageType.Error);
                return;
            }

            m_selection = m_state.Resolve(settings);
            HandleSelectionChange();
            RefreshDiagnosticsIfNeeded(settings);
            var compact = EditorGUIUtility.currentViewWidth < CompactThreshold;

            if (compact)
            {
                DrawCompactContextSelector(settings);
                DrawRightPane(settings, context);
                return;
            }

            EditorGUILayout.BeginHorizontal(
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true),
                GUILayout.MinHeight(MinimumWorkspaceHeight));
            DrawNavigation(settings, context);
            GUILayout.Space(8f);
            DrawRightPane(settings, context);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 左侧导航

        private void DrawNavigation(
            FrameworkProjectSettings settings,
            FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginVertical(
                GUILayout.Width(NavigationWidth),
                GUILayout.ExpandHeight(true));
            DrawNavigationHeader(settings, context);

            m_navigationScroll = EditorGUILayout.BeginScrollView(
                m_navigationScroll,
                GUILayout.Width(NavigationWidth),
                GUILayout.ExpandHeight(true));

            DrawNavigationSection("项 目 入 口");
            DrawNavigationRow(
                "中央项目设置",
                "Global、默认 Scene 与覆盖表",
                "Project",
                m_selection.Kind == FrameworkConfigurationSelectionKind.Project,
                m_projectErrors.Count,
                () => Select(m_state.SelectProject()));

            DrawNavigationSection("模 块 作 用 域");
            DrawConfigNavigationRow(
                "Global Config",
                settings.GlobalConfig,
                "Global",
                m_selection.Kind == FrameworkConfigurationSelectionKind.Global,
                GetContextErrorCount("Global"),
                () => Select(m_state.SelectGlobal()));
            DrawConfigNavigationRow(
                "Default Scene Config",
                settings.DefaultSceneConfig,
                "Default",
                m_selection.Kind == FrameworkConfigurationSelectionKind.DefaultScene,
                GetContextErrorCount("DefaultScene"),
                () => Select(m_state.SelectDefaultScene()));

            DrawNavigationSection($"场 景 覆 盖 · {settings.SceneBindings.Count}");
            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                var binding = settings.SceneBindings[i];
                var index = i;
                var scene = FrameworkProjectSettingsAssetUtility.LoadSceneAsset(binding);
                var title = scene == null ? $"场景覆盖 #{i}" : scene.name;
                var subtitle = binding?.SceneConfig == null
                    ? "未指定 Scene Config"
                    : $"{binding.SceneConfig.name} · {binding.SceneConfig.Modules.Count} 模块";
                var contextId = FrameworkConfigurationWorkspaceState.BuildSceneContextId(binding, i);
                var errors = GetContextErrorCount(contextId);
                DrawNavigationRow(
                    title,
                    subtitle,
                    "Override",
                    m_selection.Kind == FrameworkConfigurationSelectionKind.SceneBinding &&
                    m_selection.BindingIndex == i,
                    errors,
                    () => Select(m_state.SelectSceneBinding(binding, index)));
            }

            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("＋ 添加场景覆盖", EditorStyles.toolbarButton, GUILayout.Width(118f)))
            {
                AddSceneBinding(settings);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawNavigationHeader(
            FrameworkProjectSettings settings,
            FrameworkCenterPageContext context)
        {
            var rect = GUILayoutUtility.GetRect(
                NavigationWidth,
                NavigationHeaderHeight,
                GUILayout.Width(NavigationWidth),
                GUILayout.Height(NavigationHeaderHeight));
            EditorGUI.DrawRect(rect, FrameworkCenterStyles.PanelColor);
            FrameworkCenterStyles.DrawBorder(rect, FrameworkCenterStyles.BorderColor);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y, rect.width - 82f, rect.height),
                "配 置 作 用 域",
                FrameworkCenterStyles.NavigationCategory);

            var pingRect = new Rect(rect.xMax - 64f, rect.y + 8f, 52f, 24f);
            if (GUI.Button(pingRect, "定位", EditorStyles.miniButton))
            {
                if (context != null)
                {
                    context.SelectObject(settings);
                }
                else
                {
                    FrameworkProjectSettingsAssetUtility.Ping(settings);
                }
            }
        }

        private static void DrawNavigationSection(string title)
        {
            var rect = GUILayoutUtility.GetRect(
                NavigationWidth,
                25f,
                GUILayout.Width(NavigationWidth),
                GUILayout.Height(25f));
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 4f, rect.width - 24f, 16f),
                title,
                FrameworkCenterStyles.NavigationCategory);
            EditorGUI.DrawRect(
                new Rect(rect.x + 12f, rect.yMax - 2f, rect.width - 24f, 1f),
                FrameworkCenterStyles.BorderColor);
        }

        private void DrawConfigNavigationRow(
            string title,
            ModuleConfigBase config,
            string badge,
            bool selected,
            int errorCount,
            Action onClick)
        {
            var subtitle = config == null
                ? "未指定配置资产"
                : $"{config.name} · {config.Modules.Count} 模块";
            DrawNavigationRow(title, subtitle, badge, selected, errorCount, onClick);
        }

        private static void DrawNavigationRow(
            string title,
            string subtitle,
            string badge,
            bool selected,
            int errorCount,
            Action onClick)
        {
            var rect = GUILayoutUtility.GetRect(
                NavigationWidth,
                NavigationRowHeight,
                GUILayout.Width(NavigationWidth),
                GUILayout.Height(NavigationRowHeight));
            EditorGUI.DrawRect(
                rect,
                selected ? FrameworkCenterStyles.SelectedColor : FrameworkCenterStyles.PanelColor);
            if (selected)
            {
                EditorGUI.DrawRect(
                    new Rect(rect.x, rect.y, 3f, rect.height),
                    FrameworkCenterStyles.AccentColor);
            }
            else if (rect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(rect, FrameworkCenterStyles.HoverColor);
            }

            GUI.Label(
                new Rect(rect.x + 13f, rect.y + 5f, rect.width - 100f, 18f),
                title,
                FrameworkCenterStyles.CardTitle);
            GUI.Label(
                new Rect(rect.x + 13f, rect.y + 23f, rect.width - 26f, 16f),
                subtitle,
                FrameworkCenterStyles.ToolbarHint);

            var badgeRect = new Rect(rect.xMax - 76f, rect.y + 6f, 64f, 18f);
            GUI.Label(
                badgeRect,
                errorCount > 0 ? $"! {errorCount}  {badge}" : badge,
                FrameworkCenterStyles.StatusBadge);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                onClick?.Invoke();
            }
        }

        private void DrawCompactContextSelector(FrameworkProjectSettings settings)
        {
            BuildContextOptions(settings);
            var selectedIndex = 0;
            for (var i = 0; i < m_contextSelections.Count; i++)
            {
                if (m_contextSelections[i].ContextId == m_selection.ContextId)
                {
                    selectedIndex = i;
                    break;
                }
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("配置作用域", FrameworkCenterStyles.ToolbarLabel, GUILayout.Width(64f));
            var nextIndex = EditorGUILayout.Popup(selectedIndex, m_contextLabels.ToArray());
            if (nextIndex != selectedIndex)
            {
                SelectContextOption(settings, m_contextSelections[nextIndex]);
            }

            if (GUILayout.Button("＋场景", EditorStyles.toolbarButton, GUILayout.Width(54f)))
            {
                AddSceneBinding(settings);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        private void BuildContextOptions(FrameworkProjectSettings settings)
        {
            m_contextLabels.Clear();
            m_contextSelections.Clear();
            m_contextLabels.Add("中央项目设置");
            m_contextSelections.Add(new FrameworkConfigurationSelection(
                FrameworkConfigurationSelectionKind.Project,
                "Project"));
            m_contextLabels.Add("Global Config");
            m_contextSelections.Add(new FrameworkConfigurationSelection(
                FrameworkConfigurationSelectionKind.Global,
                "Global"));
            m_contextLabels.Add("Default Scene Config");
            m_contextSelections.Add(new FrameworkConfigurationSelection(
                FrameworkConfigurationSelectionKind.DefaultScene,
                "DefaultScene"));
            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                var binding = settings.SceneBindings[i];
                var scene = FrameworkProjectSettingsAssetUtility.LoadSceneAsset(binding);
                m_contextLabels.Add(scene == null ? $"场景覆盖 #{i}" : scene.name);
                m_contextSelections.Add(new FrameworkConfigurationSelection(
                    FrameworkConfigurationSelectionKind.SceneBinding,
                    FrameworkConfigurationWorkspaceState.BuildSceneContextId(binding, i),
                    i));
            }
        }

        private void SelectContextOption(
            FrameworkProjectSettings settings,
            FrameworkConfigurationSelection selection)
        {
            switch (selection.Kind)
            {
                case FrameworkConfigurationSelectionKind.Global:
                    Select(m_state.SelectGlobal());
                    break;
                case FrameworkConfigurationSelectionKind.DefaultScene:
                    Select(m_state.SelectDefaultScene());
                    break;
                case FrameworkConfigurationSelectionKind.SceneBinding:
                    var binding = selection.BindingIndex >= 0 &&
                                  selection.BindingIndex < settings.SceneBindings.Count
                        ? settings.SceneBindings[selection.BindingIndex]
                        : null;
                    Select(m_state.SelectSceneBinding(binding, selection.BindingIndex));
                    break;
                default:
                    Select(m_state.SelectProject());
                    break;
            }
        }

        #endregion

        #region 右侧内容

        private void DrawRightPane(
            FrameworkProjectSettings settings,
            FrameworkCenterPageContext context)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawRightHeader(settings);
            m_contentScroll = EditorGUILayout.BeginScrollView(
                m_contentScroll,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            GUILayout.Space(6f);

            if (m_state.ActiveTab == FrameworkConfigurationWorkspaceTab.DependencyGraph)
            {
                DrawDependencyGraph(settings);
            }
            else
            {
                DrawEditContent(settings, context);
            }

            GUILayout.Space(8f);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightHeader(FrameworkProjectSettings settings)
        {
            var rect = GUILayoutUtility.GetRect(
                0f,
                RightHeaderHeight,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(RightHeaderHeight));
            EditorGUI.DrawRect(rect, FrameworkCenterStyles.PanelColor);
            FrameworkCenterStyles.DrawBorder(rect, FrameworkCenterStyles.BorderColor);

            var editRect = new Rect(rect.x + 10f, rect.y + 8f, 100f, 28f);
            var graphRect = new Rect(editRect.xMax + 6f, editRect.y, 112f, 28f);
            DrawTabButton(
                editRect,
                "配置编辑",
                m_state.ActiveTab == FrameworkConfigurationWorkspaceTab.Edit,
                () => m_state.ActiveTab = FrameworkConfigurationWorkspaceTab.Edit);
            DrawTabButton(
                graphRect,
                "组合依赖图",
                m_state.ActiveTab == FrameworkConfigurationWorkspaceTab.DependencyGraph,
                () =>
                {
                    m_state.ActiveTab = FrameworkConfigurationWorkspaceTab.DependencyGraph;
                    m_graphViewport.RequestFrameAll();
                });

            GUI.Label(
                new Rect(graphRect.xMax + 10f, rect.y, rect.xMax - graphRect.xMax - 20f, rect.height),
                BuildHeaderSummary(settings),
                FrameworkCenterStyles.ToolbarHint);
        }

        private static void DrawTabButton(Rect rect, string text, bool active, Action onClick)
        {
            var previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = active
                ? FrameworkCenterStyles.AccentColor
                : FrameworkCenterStyles.CardColor;
            if (GUI.Button(rect, text, EditorStyles.miniButton))
            {
                onClick?.Invoke();
            }

            GUI.backgroundColor = previousBackground;
        }

        private void DrawEditContent(
            FrameworkProjectSettings settings,
            FrameworkCenterPageContext context)
        {
            switch (m_selection.Kind)
            {
                case FrameworkConfigurationSelectionKind.Global:
                    DrawGlobalConfig(settings);
                    break;
                case FrameworkConfigurationSelectionKind.DefaultScene:
                    DrawDefaultSceneConfig(settings);
                    break;
                case FrameworkConfigurationSelectionKind.SceneBinding:
                    DrawSceneBinding(settings);
                    break;
                default:
                    DrawProjectOverview(settings, context);
                    break;
            }
        }

        private void DrawProjectOverview(
            FrameworkProjectSettings settings,
            FrameworkCenterPageContext context)
        {
            EditorGUILayout.LabelField("中央配置入口", FrameworkCenterStyles.CardTitle);
            EditorGUILayout.LabelField(
                "眼睛按钮会把目标配置切换到右侧编辑区，不再向当前页面继续嵌套完整 Inspector。",
                FrameworkCenterStyles.Description);
            GUILayout.Space(4f);

            var nextGlobal = DrawConfigReferenceField(
                "Global Config",
                settings.GlobalConfig,
                false,
                () => Select(m_state.SelectGlobal()));
            var nextDefault = DrawConfigReferenceField(
                "Default Scene Config",
                settings.DefaultSceneConfig,
                false,
                () => Select(m_state.SelectDefaultScene()));
            if (nextGlobal != settings.GlobalConfig || nextDefault != settings.DefaultSceneConfig)
            {
                Undo.RecordObject(settings, "修改 Framework 中央配置");
                settings.SetGlobalConfig(nextGlobal as FrameworkGlobalConfig);
                settings.SetDefaultSceneConfig(nextDefault as FrameworkSceneConfig);
                EditorUtility.SetDirty(settings);
                ReleaseConfigEditor();
                InvalidateDiagnostics();
            }

            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("同步场景路径", GUILayout.Height(28f)))
            {
                Undo.RecordObject(settings, "同步 Framework 场景路径");
                FrameworkProjectSettingsAssetUtility.SyncScenePaths(settings);
                InvalidateDiagnostics();
            }

            if (GUILayout.Button("保存并重新校验", GUILayout.Height(28f)))
            {
                FrameworkProjectSettingsAssetUtility.SyncScenePaths(settings);
                AssetDatabase.SaveAssets();
                InvalidateDiagnostics();
            }

            if (GUILayout.Button("定位设置资产", GUILayout.Height(28f)))
            {
                if (context != null)
                {
                    context.SelectObject(settings);
                }
                else
                {
                    FrameworkProjectSettingsAssetUtility.Ping(settings);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                m_projectErrors.Count == 0 && m_projectWarnings.Count == 0
                    ? "项目校验 · 通过"
                    : $"项目校验 · 错误 {m_projectErrors.Count} / 警告 {m_projectWarnings.Count}",
                FrameworkCenterStyles.CardTitle);
            m_showProjectDiagnostics = EditorGUILayout.Foldout(
                m_showProjectDiagnostics,
                "查看完整诊断",
                true);
            if (m_showProjectDiagnostics)
            {
                DrawMessages(m_projectErrors, MessageType.Error);
                DrawMessages(m_projectWarnings, MessageType.Warning);
                if (m_projectErrors.Count == 0 && m_projectWarnings.Count == 0)
                {
                    EditorGUILayout.LabelField(
                        "中央配置与所有场景组合校验通过。",
                        FrameworkCenterStyles.Description);
                }
            }
        }

        private void DrawGlobalConfig(FrameworkProjectSettings settings)
        {
            EditorGUILayout.LabelField("Global Config", FrameworkCenterStyles.CardTitle);
            var nextConfig = (FrameworkGlobalConfig)EditorGUILayout.ObjectField(
                "配置资产",
                settings.GlobalConfig,
                typeof(FrameworkGlobalConfig),
                false);
            if (nextConfig != settings.GlobalConfig)
            {
                Undo.RecordObject(settings, "修改 Framework Global Config");
                settings.SetGlobalConfig(nextConfig);
                EditorUtility.SetDirty(settings);
                ReleaseConfigEditor();
                InvalidateDiagnostics();
            }

            DrawConfigEditor(nextConfig);
        }

        private void DrawDefaultSceneConfig(FrameworkProjectSettings settings)
        {
            EditorGUILayout.LabelField("Default Scene Config", FrameworkCenterStyles.CardTitle);
            var nextConfig = (FrameworkSceneConfig)EditorGUILayout.ObjectField(
                "配置资产",
                settings.DefaultSceneConfig,
                typeof(FrameworkSceneConfig),
                false);
            if (nextConfig != settings.DefaultSceneConfig)
            {
                Undo.RecordObject(settings, "修改 Framework Default Scene Config");
                settings.SetDefaultSceneConfig(nextConfig);
                EditorUtility.SetDirty(settings);
                ReleaseConfigEditor();
                InvalidateDiagnostics();
            }

            DrawConfigEditor(nextConfig);
        }

        private void DrawSceneBinding(FrameworkProjectSettings settings)
        {
            var index = m_selection.BindingIndex;
            if (index < 0 || index >= settings.SceneBindings.Count)
            {
                Select(m_state.Resolve(settings));
                return;
            }

            var binding = settings.SceneBindings[index] ?? new FrameworkSceneBinding();
            var oldScene = FrameworkProjectSettingsAssetUtility.LoadSceneAsset(binding);
            EditorGUILayout.LabelField($"场景覆盖 #{index}", FrameworkCenterStyles.CardTitle);
            var nextScene = (SceneAsset)EditorGUILayout.ObjectField(
                "Scene Asset",
                oldScene,
                typeof(SceneAsset),
                false);

            EditorGUILayout.BeginHorizontal();
            var nextConfig = (FrameworkSceneConfig)EditorGUILayout.ObjectField(
                "Scene Config",
                binding.SceneConfig,
                typeof(FrameworkSceneConfig),
                false);
            var showingConfig = m_visibleSceneConfigContext == m_selection.ContextId;
            var eyeContent = GetVisibilityContent(showingConfig);
            using (new EditorGUI.DisabledScope(nextConfig == null))
            {
                if (GUILayout.Button(
                        eyeContent,
                        EditorStyles.miniButton,
                        GUILayout.Width(24f),
                        GUILayout.Height(24f)))
                {
                    m_visibleSceneConfigContext = showingConfig
                        ? string.Empty
                        : m_selection.ContextId;
                    ReleaseConfigEditor();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (nextScene != oldScene || nextConfig != binding.SceneConfig)
            {
                Undo.RecordObject(settings, "修改 Framework 场景覆盖");
                var path = nextScene == null ? string.Empty : AssetDatabase.GetAssetPath(nextScene);
                var guid = string.IsNullOrEmpty(path)
                    ? string.Empty
                    : AssetDatabase.AssetPathToGUID(path);
                binding.SetScene(guid, path, nextConfig);
                ReplaceSceneBinding(settings, index, binding);
                EditorUtility.SetDirty(settings);
                m_selection = m_state.SelectSceneBinding(binding, index);
                m_visibleSceneConfigContext = m_selection.ContextId;
                ReleaseConfigEditor();
                InvalidateDiagnostics();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("GUID", binding.SceneGuid ?? string.Empty);
                EditorGUILayout.TextField("Path", binding.ScenePath ?? string.Empty);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("在 Build Settings 中查看", GUILayout.Height(26f)))
            {
                if (!EditorApplication.ExecuteMenuItem("File/Build Profiles"))
                {
                    EditorApplication.ExecuteMenuItem("File/Build Settings...");
                }
            }

            var previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = FrameworkCenterStyles.ErrorColor;
            if (GUILayout.Button("移除场景覆盖", GUILayout.Height(26f)))
            {
                RemoveSceneBinding(settings, index);
                GUI.backgroundColor = previousBackground;
                EditorGUILayout.EndHorizontal();
                return;
            }

            GUI.backgroundColor = previousBackground;
            EditorGUILayout.EndHorizontal();

            if (m_visibleSceneConfigContext == m_selection.ContextId)
            {
                DrawConfigEditor(binding.SceneConfig);
            }
        }

        private void DrawConfigEditor(ModuleConfigBase config)
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("当前作用域没有指定配置资产。", MessageType.Info);
                ReleaseConfigEditor();
                return;
            }

            EnsureConfigEditor(config);
            if (m_configEditor == null)
            {
                EditorGUILayout.HelpBox($"无法为 {config.name} 创建 Inspector。", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(6f);
            try
            {
                m_configEditor.OnInspectorGUI();
            }
            catch (Exception exception)
            {
                if (exception is ExitGUIException)
                {
                    throw;
                }

                Debug.LogException(exception);
                EditorGUILayout.HelpBox(
                    $"{config.name} 的 Inspector 绘制失败：{exception.Message}",
                    MessageType.Error);
            }
        }

        #endregion

        #region 依赖图

        private void DrawDependencyGraph(FrameworkProjectSettings settings)
        {
            ModuleGraphResult graph;
            if (m_selection.Kind == FrameworkConfigurationSelectionKind.Project)
            {
                EnsurePreviewScene(settings);
                DrawPreviewSceneSelector();
                var scenePath = m_previewScene == null
                    ? string.Empty
                    : AssetDatabase.GetAssetPath(m_previewScene);
                var resolved = FrameworkProjectSettingsResolver.Resolve(settings, scenePath);
                DrawResolvedContext(resolved, scenePath);
                graph = ModuleGraphResolver.Resolve(settings.GlobalConfig, resolved.SceneConfig);
            }
            else if (m_selection.Kind == FrameworkConfigurationSelectionKind.Global)
            {
                graph = ModuleGraphResolver.Resolve(settings.GlobalConfig, null);
            }
            else if (m_selection.Kind == FrameworkConfigurationSelectionKind.DefaultScene)
            {
                graph = ModuleGraphResolver.Resolve(settings.GlobalConfig, settings.DefaultSceneConfig);
            }
            else
            {
                var binding = GetSelectedBinding(settings);
                graph = ModuleGraphResolver.Resolve(settings.GlobalConfig, binding?.SceneConfig);
            }

            ModuleDependencyGraphDrawer.DrawDiagnostics(graph);
            ModuleDependencyGraphDrawer.DrawGraph(graph, m_graphViewport);
        }

        private void EnsurePreviewScene(FrameworkProjectSettings settings)
        {
            if (m_previewInitialized)
            {
                return;
            }

            m_previewInitialized = true;
            var settingsGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings));
            m_previewSceneSessionKey = PreviewSceneSessionPrefix + settingsGuid;
            var savedGuid = SessionState.GetString(m_previewSceneSessionKey, string.Empty);
            if (!string.IsNullOrEmpty(savedGuid))
            {
                m_previewScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    AssetDatabase.GUIDToAssetPath(savedGuid));
            }

            if (m_previewScene == null)
            {
                var activePath = SceneManager.GetActiveScene().path;
                m_previewScene = string.IsNullOrEmpty(activePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(activePath);
            }

            SavePreviewScene();
        }

        private void DrawPreviewSceneSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("项目组合预览", FrameworkCenterStyles.CardTitle);
            var nextScene = (SceneAsset)EditorGUILayout.ObjectField(
                "Scene Asset",
                m_previewScene,
                typeof(SceneAsset),
                false);
            if (nextScene != m_previewScene)
            {
                SetPreviewScene(nextScene);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("使用当前活动场景"))
            {
                var activePath = SceneManager.GetActiveScene().path;
                SetPreviewScene(string.IsNullOrEmpty(activePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(activePath));
            }

            if (GUILayout.Button("默认上下文"))
            {
                SetPreviewScene(null);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawResolvedContext(
            FrameworkProjectSettingsResult result,
            string scenePath)
        {
            string message;
            if (result.UsesSceneOverride)
            {
                message = result.SceneConfig == null
                    ? $"精确场景覆盖缺少 SceneConfig\n{scenePath}"
                    : $"精确场景覆盖：{result.SceneConfig.name}\n{scenePath}";
            }
            else if (result.SceneConfig != null)
            {
                message = $"默认 SceneConfig：{result.SceneConfig.name}";
            }
            else
            {
                message = "合法空 SceneScope";
            }

            EditorGUILayout.HelpBox(message, result.IsValid ? MessageType.Info : MessageType.Error);
        }

        private void SetPreviewScene(SceneAsset scene)
        {
            if (m_previewScene == scene)
            {
                return;
            }

            m_previewScene = scene;
            SavePreviewScene();
            m_graphViewport.RequestFrameAll();
        }

        private void SavePreviewScene()
        {
            if (string.IsNullOrEmpty(m_previewSceneSessionKey))
            {
                return;
            }

            var path = m_previewScene == null ? string.Empty : AssetDatabase.GetAssetPath(m_previewScene);
            SessionState.SetString(
                m_previewSceneSessionKey,
                string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
        }

        #endregion

        #region 状态修改与缓存

        private void Select(FrameworkConfigurationSelection selection)
        {
            m_selection = selection;
            HandleSelectionChange();
        }

        private void HandleSelectionChange()
        {
            if (m_lastSelectionId == m_selection.ContextId)
            {
                return;
            }

            m_lastSelectionId = m_selection.ContextId;
            m_contentScroll = Vector2.zero;
            m_visibleSceneConfigContext = m_selection.Kind == FrameworkConfigurationSelectionKind.SceneBinding
                ? m_selection.ContextId
                : string.Empty;
            ReleaseConfigEditor();
            m_graphViewport.RequestFrameAll();
        }

        private void AddSceneBinding(FrameworkProjectSettings settings)
        {
            var bindings = new List<FrameworkSceneBinding>(settings.SceneBindings)
            {
                new FrameworkSceneBinding(),
            };
            Undo.RecordObject(settings, "添加 Framework 场景覆盖");
            settings.SetSceneBindings(bindings);
            EditorUtility.SetDirty(settings);
            InvalidateDiagnostics();
            Select(m_state.SelectSceneBinding(bindings[bindings.Count - 1], bindings.Count - 1));
        }

        private void RemoveSceneBinding(FrameworkProjectSettings settings, int index)
        {
            var bindings = new List<FrameworkSceneBinding>(settings.SceneBindings);
            if (index < 0 || index >= bindings.Count)
            {
                return;
            }

            Undo.RecordObject(settings, "移除 Framework 场景覆盖");
            bindings.RemoveAt(index);
            settings.SetSceneBindings(bindings);
            EditorUtility.SetDirty(settings);
            ReleaseConfigEditor();
            InvalidateDiagnostics();
            Select(settings.GlobalConfig != null ? m_state.SelectGlobal() : m_state.SelectProject());
        }

        private static void ReplaceSceneBinding(
            FrameworkProjectSettings settings,
            int index,
            FrameworkSceneBinding binding)
        {
            var bindings = new List<FrameworkSceneBinding>(settings.SceneBindings);
            bindings[index] = binding;
            settings.SetSceneBindings(bindings);
        }

        private FrameworkSceneBinding GetSelectedBinding(FrameworkProjectSettings settings)
        {
            return m_selection.BindingIndex >= 0 &&
                   m_selection.BindingIndex < settings.SceneBindings.Count
                ? settings.SceneBindings[m_selection.BindingIndex]
                : null;
        }

        private void EnsureConfigEditor(ModuleConfigBase config)
        {
            if (m_configEditorTarget == config && m_configEditor != null)
            {
                return;
            }

            ReleaseConfigEditor();
            m_configEditorTarget = config;
            UnityEditor.Editor.CreateCachedEditor(config, null, ref m_configEditor);
        }

        private void ReleaseConfigEditor()
        {
            if (m_configEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_configEditor);
            }

            m_configEditor = null;
            m_configEditorTarget = null;
        }

        private void RefreshDiagnosticsIfNeeded(FrameworkProjectSettings settings)
        {
            var revision = ComputeDiagnosticsRevision(settings);
            if (revision == m_diagnosticsRevision)
            {
                return;
            }

            m_diagnosticsRevision = revision;
            FrameworkProjectSettingsAssetUtility.Validate(
                settings,
                out var projectErrors,
                out var projectWarnings);
            m_projectErrors.Clear();
            m_projectErrors.AddRange(projectErrors);
            m_projectWarnings.Clear();
            m_projectWarnings.AddRange(projectWarnings);
            m_contextErrorCounts.Clear();
            m_contextErrorCounts["Global"] = settings.GlobalConfig == null
                ? 1
                : CountGraphErrors(ModuleGraphResolver.Inspect(settings.GlobalConfig));
            m_contextErrorCounts["DefaultScene"] = settings.DefaultSceneConfig == null
                ? 0
                : CountGraphErrors(ModuleGraphResolver.Resolve(
                    settings.GlobalConfig,
                    settings.DefaultSceneConfig));

            for (var i = 0; i < settings.SceneBindings.Count; i++)
            {
                var binding = settings.SceneBindings[i];
                var contextId = FrameworkConfigurationWorkspaceState.BuildSceneContextId(binding, i);
                m_contextErrorCounts[contextId] = binding?.SceneConfig == null
                    ? 1
                    : CountGraphErrors(ModuleGraphResolver.Resolve(
                        settings.GlobalConfig,
                        binding.SceneConfig));
            }
        }

        private int ComputeDiagnosticsRevision(FrameworkProjectSettings settings)
        {
            unchecked
            {
                var revision = EditorUtility.GetDirtyCount(settings);
                revision = revision * 397 ^ FrameworkModuleConfigController.ComputeRevision(settings.GlobalConfig);
                revision = revision * 397 ^ FrameworkModuleConfigController.ComputeRevision(settings.DefaultSceneConfig);
                for (var i = 0; i < settings.SceneBindings.Count; i++)
                {
                    var binding = settings.SceneBindings[i];
                    revision = revision * 397 ^ (binding?.SceneGuid?.GetHashCode() ?? 0);
                    revision = revision * 397 ^ (binding?.ScenePath?.GetHashCode() ?? 0);
                    revision = revision * 397 ^
                               FrameworkModuleConfigController.ComputeRevision(binding?.SceneConfig);
                }

                var buildScenes = EditorBuildSettings.scenes;
                revision = revision * 397 ^ buildScenes.Length;
                for (var i = 0; i < buildScenes.Length; i++)
                {
                    revision = revision * 397 ^ (buildScenes[i].enabled ? 1 : 0);
                    revision = revision * 397 ^ (buildScenes[i].path?.GetHashCode() ?? 0);
                }

                return revision;
            }
        }

        private int GetContextErrorCount(string contextId)
        {
            return m_contextErrorCounts.TryGetValue(contextId, out var count) ? count : 0;
        }

        private void InvalidateDiagnostics()
        {
            m_diagnosticsRevision = int.MinValue;
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            ReleaseConfigEditor();
        }

        #endregion

        #region 显示辅助

        private string BuildHeaderSummary(FrameworkProjectSettings settings)
        {
            switch (m_selection.Kind)
            {
                case FrameworkConfigurationSelectionKind.Global:
                    return settings.GlobalConfig == null
                        ? "Global · 未配置"
                        : $"Global · {settings.GlobalConfig.Modules.Count} 模块";
                case FrameworkConfigurationSelectionKind.DefaultScene:
                    return settings.DefaultSceneConfig == null
                        ? "Default Scene · 空作用域"
                        : $"Default Scene · {settings.DefaultSceneConfig.Modules.Count} 模块";
                case FrameworkConfigurationSelectionKind.SceneBinding:
                    var binding = GetSelectedBinding(settings);
                    return binding?.SceneConfig == null
                        ? "Scene Override · 未配置"
                        : $"Scene Override · {binding.SceneConfig.Modules.Count} 模块";
                default:
                    return $"场景覆盖 · {settings.SceneBindings.Count}";
            }
        }

        private static ModuleConfigBase DrawConfigReferenceField(
            string label,
            ModuleConfigBase config,
            bool active,
            Action onOpen)
        {
            EditorGUILayout.BeginHorizontal();
            var expectedType = config is FrameworkSceneConfig || label.Contains("Scene")
                ? typeof(FrameworkSceneConfig)
                : typeof(FrameworkGlobalConfig);
            var nextConfig = (ModuleConfigBase)EditorGUILayout.ObjectField(
                label,
                config,
                expectedType,
                false);
            using (new EditorGUI.DisabledScope(nextConfig == null))
            {
                if (GUILayout.Button(
                        GetVisibilityContent(active),
                        EditorStyles.miniButton,
                        GUILayout.Width(24f),
                        GUILayout.Height(24f)))
                {
                    onOpen?.Invoke();
                }
            }

            EditorGUILayout.EndHorizontal();
            return nextConfig;
        }

        private static GUIContent GetVisibilityContent(bool active)
        {
            var iconName = active ? "VisibilityOn" : "VisibilityOff";
            var content = EditorGUIUtility.IconContent(
                EditorGUIUtility.isProSkin ? "d_" + iconName : iconName);
            if (content.image == null)
            {
                content = EditorGUIUtility.IconContent("d_" + iconName);
            }

            return new GUIContent(
                content.image,
                active ? "关闭右侧配置 Inspector" : "在右侧打开真实配置 Inspector");
        }

        private static int CountGraphErrors(ModuleGraphResult result)
        {
            var count = 0;
            for (var i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Severity == ModuleGraphDiagnosticSeverity.Error)
                {
                    count++;
                }
            }

            return count;
        }

        private static void DrawMessages(IReadOnlyList<string> messages, MessageType type)
        {
            for (var i = 0; i < messages.Count; i++)
            {
                EditorGUILayout.HelpBox(messages[i], type);
            }
        }

        #endregion
    }
}
