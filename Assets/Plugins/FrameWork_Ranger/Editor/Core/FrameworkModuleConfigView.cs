using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 以 HTY 式紧凑行绘制一份 Global/Scene 模块配置，并把模块详情限制为单项展开。
    /// </summary>
    [FrameworkArchitecture(
        "紧凑模块配置视图",
        "提供模块搜索、筛选、重排、状态摘要和互斥真实 Inspector。",
        FrameworkArchitectureLayer.EditorIntegration,
        3,
        typeof(FrameworkModuleConfigController),
        typeof(FrameworkInlineInspectorHost))]
    internal sealed class FrameworkModuleConfigView : IDisposable
    {
        private const float RowHeight = 32f;
        private const float IconButtonSize = 24f;
        private const float StatusWidth = 58f;
        private const string ExclusiveGroupId = "ModuleDetails";
        private static readonly int AddPickerControlId =
            "FrameWork_Ranger.ModuleConfig.AddPicker".GetHashCode();

        private static readonly string[] FilterLabels = { "全部", "启用", "异常" };

        private readonly List<ModuleConfigEntry> m_entries = new List<ModuleConfigEntry>();
        private readonly List<int> m_visibleIndices = new List<int>();
        private readonly HashSet<string> m_validSlots = new HashSet<string>();

        private ModuleConfigBase m_config;
        private FrameworkInlineInspectorHost m_inlineInspectorHost;
        private ReorderableList m_reorderableList;
        private ModuleGraphResult m_graphResult;
        private int m_revision = int.MinValue;
        private string m_searchText = string.Empty;
        private FrameworkModuleConfigFilter m_filter;
        private bool m_showDiagnostics;
        private int m_pendingRemoveIndex = -1;
        private bool m_disposed;

        #region 绘制入口

        internal void Draw(ModuleConfigBase config)
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(FrameworkModuleConfigView));
            }

            if (config == null)
            {
                EditorGUILayout.HelpBox("未指定模块配置资产。", MessageType.Info);
                return;
            }

            EnsureTarget(config);
            ProcessObjectPicker();
            RefreshIfNeeded();
            DrawSummaryToolbar();
            DrawModuleList();
            ProcessPendingRemoval();
            DrawAddButton();
            DrawExpandedModule();
            DrawDiagnosticsDetails();
        }

        #endregion

        #region 列表与工具栏

        private void DrawSummaryToolbar()
        {
            CountDiagnostics(out var errorCount, out var warningCount);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"模块 · {m_config.Modules.Count}",
                FrameworkCenterStyles.CardTitle,
                GUILayout.Width(92f));

            DrawDiagnosticBadge(errorCount, warningCount);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var nextSearch = GUILayout.TextField(
                m_searchText,
                GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(120f));
            var nextFilter = (FrameworkModuleConfigFilter)GUILayout.Toolbar(
                (int)m_filter,
                FilterLabels,
                EditorStyles.toolbarButton,
                GUILayout.Width(168f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (nextSearch != m_searchText || nextFilter != m_filter)
            {
                m_searchText = nextSearch;
                m_filter = nextFilter;
                RebuildVisibleIndices();
            }

            if (!FrameworkModuleConfigController.CanReorder(m_searchText, m_filter))
            {
                EditorGUILayout.LabelField(
                    "搜索或筛选期间暂不允许重排；清空条件后恢复拖动。",
                    FrameworkCenterStyles.ToolbarHint);
            }
        }

        private void DrawModuleList()
        {
            if (m_config.Modules.Count == 0)
            {
                EditorGUILayout.HelpBox("当前配置还没有模块。使用下方加号选择 Module 资产。", MessageType.Info);
                return;
            }

            if (m_visibleIndices.Count == 0)
            {
                EditorGUILayout.HelpBox("没有符合当前搜索或筛选条件的模块。", MessageType.Info);
                return;
            }

            if (FrameworkModuleConfigController.CanReorder(m_searchText, m_filter))
            {
                m_reorderableList.DoLayoutList();
                return;
            }

            for (var i = 0; i < m_visibleIndices.Count; i++)
            {
                var configIndex = m_visibleIndices[i];
                var rect = GUILayoutUtility.GetRect(
                    0f,
                    RowHeight,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(RowHeight));
                EditorGUI.DrawRect(rect, i % 2 == 0
                    ? FrameworkCenterStyles.CardColor
                    : FrameworkCenterStyles.PanelColor);
                FrameworkCenterStyles.DrawBorder(rect, FrameworkCenterStyles.BorderColor);
                DrawRow(new Rect(rect.x + 4f, rect.y, rect.width - 8f, rect.height), configIndex);
            }
        }

        private void DrawRow(Rect rect, int configIndex)
        {
            if (configIndex < 0 || configIndex >= m_entries.Count)
            {
                return;
            }

            var entry = m_entries[configIndex] ?? new ModuleConfigEntry();
            var module = entry.Module;
            var right = rect.xMax;

            var removeRect = TakeRight(ref right, rect.y, IconButtonSize);
            var eyeRect = TakeRight(ref right, rect.y, IconButtonSize);
            var pingRect = TakeRight(ref right, rect.y, IconButtonSize);
            var diagnosticRect = TakeRight(ref right, rect.y, 20f);

            var wide = rect.width >= 650f;
            var medium = rect.width >= 510f;
            Rect dependencyRect = default;
            Rect priorityRect = default;
            Rect typeRect = default;
            if (medium)
            {
                dependencyRect = TakeRight(ref right, rect.y, 54f);
                priorityRect = TakeRight(ref right, rect.y, 54f);
            }

            if (wide)
            {
                typeRect = TakeRight(ref right, rect.y, 124f);
            }

            var statusRect = TakeRight(ref right, rect.y, StatusWidth);
            var moduleRect = new Rect(rect.x, rect.y + 3f, Mathf.Max(48f, right - rect.x - 4f), 26f);

            EditorGUI.BeginChangeCheck();
            var nextModule = (ModuleBase)EditorGUI.ObjectField(
                moduleRect,
                module,
                typeof(ModuleBase),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                FrameworkModuleConfigController.SetModule(m_config, configIndex, nextModule);
                Invalidate();
            }

            DrawStatusButton(statusRect, entry.Enabled, configIndex);
            if (wide)
            {
                GUI.Label(
                    typeRect,
                    module == null ? "<空模板>" : module.GetType().Name,
                    FrameworkCenterStyles.ToolbarHint);
            }

            if (medium)
            {
                GUI.Label(
                    priorityRect,
                    module == null ? "P —" : $"P {module.LoadPriority}",
                    FrameworkCenterStyles.ToolbarHint);
                GUI.Label(
                    dependencyRect,
                    module == null ? "依赖 —" : $"依赖 {module.GetRequiredModuleTypes().Count}",
                    FrameworkCenterStyles.ToolbarHint);
            }

            DrawRowDiagnostic(diagnosticRect, configIndex);
            DrawPingButton(pingRect, module);
            var slotId = BuildSlotId(module, configIndex);
            m_inlineInspectorHost.DrawExclusiveVisibilityButton(
                eyeRect,
                ExclusiveGroupId,
                slotId,
                module);

            var deleteContent = EditorGUIUtility.IconContent("d_Toolbar Minus");
            deleteContent.tooltip = "移除模块条目";
            if (GUI.Button(removeRect, deleteContent, EditorStyles.miniButton))
            {
                m_pendingRemoveIndex = configIndex;
            }
        }

        private void DrawAddButton()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var addContent = EditorGUIUtility.IconContent("d_Toolbar Plus");
            addContent.tooltip = "选择并添加 Module 资产";
            if (GUILayout.Button(
                    addContent,
                    EditorStyles.miniButton,
                    GUILayout.Width(28f),
                    GUILayout.Height(26f)))
            {
                EditorGUIUtility.ShowObjectPicker<ModuleBase>(
                    null,
                    false,
                    "t:ModuleBase",
                    AddPickerControlId);
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 详情与诊断

        private void DrawExpandedModule()
        {
            for (var i = 0; i < m_config.Modules.Count; i++)
            {
                var module = m_config.Modules[i]?.Module;
                if (module == null)
                {
                    continue;
                }

                var slotId = BuildSlotId(module, i);
                if (!m_inlineInspectorHost.IsExclusiveExpanded(
                        ExclusiveGroupId,
                        slotId,
                        module))
                {
                    continue;
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(
                    $"模块详情 · {module.name}",
                    FrameworkCenterStyles.CardTitle);
                DrawDiagnosticsForEntry(i);
                m_inlineInspectorHost.DrawExclusiveInlineInspector(
                    ExclusiveGroupId,
                    slotId,
                    module);
                return;
            }
        }

        private void DrawDiagnosticsDetails()
        {
            var diagnosticCount = m_graphResult?.Diagnostics.Count ?? 0;
            m_showDiagnostics = EditorGUILayout.Foldout(
                m_showDiagnostics,
                $"配置诊断 · {diagnosticCount}",
                true);
            if (!m_showDiagnostics)
            {
                return;
            }

            if (diagnosticCount == 0)
            {
                EditorGUILayout.LabelField("配置图校验通过。", FrameworkCenterStyles.Description);
                return;
            }

            for (var i = 0; i < m_graphResult.Diagnostics.Count; i++)
            {
                var diagnostic = m_graphResult.Diagnostics[i];
                EditorGUILayout.HelpBox(
                    diagnostic.ToString(),
                    ToMessageType(diagnostic.Severity));
            }
        }

        private void DrawDiagnosticsForEntry(int configIndex)
        {
            var scopeKind = m_config is FrameworkGlobalConfig
                ? ModuleScopeKind.Global
                : ModuleScopeKind.Scene;
            for (var i = 0; i < m_graphResult.Diagnostics.Count; i++)
            {
                var diagnostic = m_graphResult.Diagnostics[i];
                if (diagnostic.ScopeKind != scopeKind || diagnostic.ConfigIndex != configIndex)
                {
                    continue;
                }

                EditorGUILayout.HelpBox(
                    diagnostic.Message,
                    ToMessageType(diagnostic.Severity));
            }
        }

        #endregion

        #region 缓存与生命周期

        private void EnsureTarget(ModuleConfigBase config)
        {
            if (m_config == config && m_inlineInspectorHost != null)
            {
                return;
            }

            m_inlineInspectorHost?.Dispose();
            m_config = config;
            m_inlineInspectorHost = new FrameworkInlineInspectorHost(config);
            m_searchText = SessionState.GetString(BuildStateKey("Search"), string.Empty);
            var filterValue = SessionState.GetInt(BuildStateKey("Filter"), (int)FrameworkModuleConfigFilter.All);
            m_filter = Enum.IsDefined(typeof(FrameworkModuleConfigFilter), filterValue)
                ? (FrameworkModuleConfigFilter)filterValue
                : FrameworkModuleConfigFilter.All;
            m_showDiagnostics = false;
            m_revision = int.MinValue;
            m_reorderableList = null;
        }

        private void RefreshIfNeeded()
        {
            var revision = FrameworkModuleConfigController.ComputeRevision(m_config);
            if (revision == m_revision)
            {
                return;
            }

            m_revision = revision;
            m_entries.Clear();
            m_entries.AddRange(m_config.Modules);
            m_graphResult = ModuleGraphResolver.Inspect(m_config);
            RebuildVisibleIndices();
            BuildReorderableList();
            RetainValidModuleSlots();
        }

        private void RebuildVisibleIndices()
        {
            SessionState.SetString(BuildStateKey("Search"), m_searchText ?? string.Empty);
            SessionState.SetInt(BuildStateKey("Filter"), (int)m_filter);
            FrameworkModuleConfigController.BuildVisibleIndices(
                m_config,
                m_graphResult,
                m_searchText,
                m_filter,
                m_visibleIndices);
        }

        private void BuildReorderableList()
        {
            m_reorderableList = new ReorderableList(
                m_entries,
                typeof(ModuleConfigEntry),
                true,
                false,
                false,
                false)
            {
                elementHeight = RowHeight,
                drawElementCallback = (rect, index, _, _) => DrawRow(rect, index),
                onReorderCallbackWithDetails = (_, oldIndex, newIndex) =>
                {
                    FrameworkModuleConfigController.MoveModule(m_config, oldIndex, newIndex);
                    Invalidate();
                },
            };
        }

        private void RetainValidModuleSlots()
        {
            m_validSlots.Clear();
            for (var i = 0; i < m_config.Modules.Count; i++)
            {
                var module = m_config.Modules[i]?.Module;
                if (module != null)
                {
                    m_validSlots.Add(BuildSlotId(module, i));
                }
            }

            m_inlineInspectorHost.RetainSlots("Module:", m_validSlots);
            m_inlineInspectorHost.RetainExclusiveSlots(ExclusiveGroupId, m_validSlots);
        }

        private void ProcessObjectPicker()
        {
            if (Event.current.commandName != "ObjectSelectorClosed" ||
                EditorGUIUtility.GetObjectPickerControlID() != AddPickerControlId)
            {
                return;
            }

            var module = EditorGUIUtility.GetObjectPickerObject() as ModuleBase;
            if (module != null)
            {
                FrameworkModuleConfigController.AddModule(m_config, module);
                Invalidate();
            }
        }

        private void ProcessPendingRemoval()
        {
            if (m_pendingRemoveIndex < 0)
            {
                return;
            }

            var index = m_pendingRemoveIndex;
            m_pendingRemoveIndex = -1;
            FrameworkModuleConfigController.RemoveModule(m_config, index);
            Invalidate();
        }

        private void Invalidate()
        {
            m_revision = int.MinValue;
        }

        private string BuildStateKey(string suffix)
        {
            return "FrameWork_Ranger.ModuleConfigView." +
                   FrameworkInlineInspectorHost.BuildObjectIdentity(m_config) +
                   "." + suffix;
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            m_inlineInspectorHost?.Dispose();
            m_inlineInspectorHost = null;
            m_reorderableList = null;
            m_config = null;
        }

        #endregion

        #region 绘制辅助

        private static Rect TakeRight(ref float right, float rowY, float width)
        {
            right -= width;
            var rect = new Rect(right, rowY + 4f, width - 2f, 24f);
            right -= 2f;
            return rect;
        }

        private static string BuildSlotId(ModuleBase module, int index)
        {
            return module == null
                ? "Module:Empty:" + index
                : "Module:" + FrameworkInlineInspectorHost.BuildObjectIdentity(module);
        }

        private void DrawStatusButton(Rect rect, bool enabled, int configIndex)
        {
            var previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = enabled
                ? FrameworkCenterStyles.SuccessColor
                : FrameworkCenterStyles.MutedTextColor;
            if (GUI.Button(
                    rect,
                    enabled ? "启用" : "停用",
                    EditorStyles.miniButton))
            {
                FrameworkModuleConfigController.SetEnabled(m_config, configIndex, !enabled);
                Invalidate();
            }

            GUI.backgroundColor = previousBackground;
        }

        private void DrawRowDiagnostic(Rect rect, int configIndex)
        {
            var severity = GetEntrySeverity(configIndex);
            GUIContent content;
            switch (severity)
            {
                case ModuleGraphDiagnosticSeverity.Error:
                    content = EditorGUIUtility.IconContent("console.erroricon.sml");
                    content.tooltip = "该模块存在错误；展开详情查看原因。";
                    break;
                case ModuleGraphDiagnosticSeverity.Warning:
                    content = EditorGUIUtility.IconContent("console.warnicon.sml");
                    content.tooltip = "该模块存在警告；展开详情查看原因。";
                    break;
                default:
                    content = EditorGUIUtility.IconContent("TestPassed");
                    content.tooltip = "该模块没有诊断。";
                    break;
            }

            GUI.Label(rect, content);
        }

        private static void DrawPingButton(Rect rect, ModuleBase module)
        {
            var content = EditorGUIUtility.IconContent("d_ViewToolOrbit");
            content.tooltip = "在 Project 中定位模块资产";
            using (new EditorGUI.DisabledScope(module == null))
            {
                if (GUI.Button(rect, content, EditorStyles.miniButton) && module != null)
                {
                    EditorGUIUtility.PingObject(module);
                }
            }
        }

        private void DrawDiagnosticBadge(int errorCount, int warningCount)
        {
            var text = errorCount > 0
                ? $"错误 {errorCount}"
                : warningCount > 0
                    ? $"警告 {warningCount}"
                    : "校验通过";
            var color = errorCount > 0
                ? FrameworkCenterStyles.ErrorColor
                : warningCount > 0
                    ? FrameworkCenterStyles.WarningColor
                    : FrameworkCenterStyles.SuccessColor;
            var previousColor = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, FrameworkCenterStyles.StatusBadge, GUILayout.Width(76f));
            GUI.color = previousColor;
        }

        private void CountDiagnostics(out int errorCount, out int warningCount)
        {
            errorCount = 0;
            warningCount = 0;
            if (m_graphResult == null)
            {
                return;
            }

            for (var i = 0; i < m_graphResult.Diagnostics.Count; i++)
            {
                switch (m_graphResult.Diagnostics[i].Severity)
                {
                    case ModuleGraphDiagnosticSeverity.Error:
                        errorCount++;
                        break;
                    case ModuleGraphDiagnosticSeverity.Warning:
                        warningCount++;
                        break;
                }
            }
        }

        private ModuleGraphDiagnosticSeverity GetEntrySeverity(int configIndex)
        {
            var severity = ModuleGraphDiagnosticSeverity.Info;
            var scopeKind = m_config is FrameworkGlobalConfig
                ? ModuleScopeKind.Global
                : ModuleScopeKind.Scene;
            for (var i = 0; i < m_graphResult.Diagnostics.Count; i++)
            {
                var diagnostic = m_graphResult.Diagnostics[i];
                if (diagnostic.ScopeKind != scopeKind || diagnostic.ConfigIndex != configIndex)
                {
                    continue;
                }

                if (diagnostic.Severity == ModuleGraphDiagnosticSeverity.Error)
                {
                    return ModuleGraphDiagnosticSeverity.Error;
                }

                if (diagnostic.Severity == ModuleGraphDiagnosticSeverity.Warning)
                {
                    severity = ModuleGraphDiagnosticSeverity.Warning;
                }
            }

            return severity;
        }

        private static MessageType ToMessageType(ModuleGraphDiagnosticSeverity severity)
        {
            return severity switch
            {
                ModuleGraphDiagnosticSeverity.Error => MessageType.Error,
                ModuleGraphDiagnosticSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info,
            };
        }

        #endregion
    }
}
