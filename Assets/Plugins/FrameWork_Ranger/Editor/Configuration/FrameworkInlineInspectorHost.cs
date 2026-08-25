using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 在一个宿主 Inspector 内管理可独立展开的子 Inspector，并负责其会话状态与 Editor 生命周期。
    /// </summary>
    [FrameworkArchitecture(
        "内联 Inspector 宿主",
        "缓存配置引用对应的真实 Editor，并用会话状态控制眼睛按钮的展开与收起。",
        FrameworkArchitectureLayer.EditorIntegration,
        25,
        typeof(FrameworkProjectSettingsInspector),
        typeof(ModuleConfigInspector))]
    internal sealed class FrameworkInlineInspectorHost : IDisposable
    {
        private const string SessionPrefix = "FrameWork_Ranger.InlineInspector.";
        private const float VisibilityButtonSize = 24f;

        private static readonly HashSet<FrameworkInlineInspectorHost> s_liveHosts =
            new HashSet<FrameworkInlineInspectorHost>();

        private static FrameworkInlineInspectorHost s_currentDrawingHost;
        private static GUIContent s_visibilityOnLight;
        private static GUIContent s_visibilityOffLight;
        private static GUIContent s_visibilityOnDark;
        private static GUIContent s_visibilityOffDark;

        private readonly Dictionary<string, SlotEntry> m_slots = new Dictionary<string, SlotEntry>();
        private readonly Dictionary<string, string> m_exclusiveGroupsBySlot =
            new Dictionary<string, string>();
        private readonly SessionStateStore m_state;
        private bool m_disposed;

        static FrameworkInlineInspectorHost()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DisposeAll;
            EditorApplication.quitting += DisposeAll;
        }

        internal FrameworkInlineInspectorHost(UnityEngine.Object host)
            : this(BuildObjectIdentity(host))
        {
        }

        internal FrameworkInlineInspectorHost(string hostIdentity)
        {
            m_state = new SessionStateStore(hostIdentity);
            s_liveHosts.Add(this);
        }

        #region 公共属性

        /// <summary>
        /// 获取当前 Odin 属性树所属的内联 Inspector 宿主。
        /// </summary>
        internal static FrameworkInlineInspectorHost CurrentDrawingHost => s_currentDrawingHost;

        /// <summary>
        /// 获取当前仍被宿主持有的子 Editor 数量，供生命周期测试使用。
        /// </summary>
        internal int CachedEditorCount
        {
            get
            {
                var count = 0;
                foreach (var entry in m_slots.Values)
                {
                    if (entry.Editor != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        #endregion

        #region 绘制入口

        /// <summary>
        /// 绘制对象引用、眼睛按钮和引用下方的真实 Inspector，并返回可能变化后的引用。
        /// </summary>
        internal T DrawReferenceField<T>(string label, string slotId, T value)
            where T : UnityEngine.Object
        {
            EditorGUILayout.BeginHorizontal();
            var nextValue = (T)EditorGUILayout.ObjectField(label, value, typeof(T), false);
            DrawVisibilityButton(slotId, nextValue);
            EditorGUILayout.EndHorizontal();

            DrawInlineInspector(slotId, nextValue);
            return nextValue;
        }

        /// <summary>
        /// 绘制一个只改变 Editor 会话状态的眼睛按钮；空引用时按钮不可用。
        /// </summary>
        internal void DrawVisibilityButton(string slotId, UnityEngine.Object target)
        {
            if (target == null)
            {
                ClearSlot(slotId);
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Button(
                        GetVisibilityContent(false),
                        EditorStyles.miniButton,
                        GUILayout.Width(VisibilityButtonSize),
                        GUILayout.Height(VisibilityButtonSize));
                }

                return;
            }

            var expanded = IsExpanded(slotId, target);
            if (!GUILayout.Button(
                    GetVisibilityContent(expanded),
                    EditorStyles.miniButton,
                    GUILayout.Width(VisibilityButtonSize),
                    GUILayout.Height(VisibilityButtonSize)))
            {
                return;
            }

            SetExpanded(slotId, target, !expanded);
        }

        /// <summary>
        /// 在指定矩形中绘制互斥眼睛按钮。同一组只允许一个槽位处于展开状态。
        /// </summary>
        internal void DrawExclusiveVisibilityButton(
            Rect rect,
            string groupId,
            string slotId,
            UnityEngine.Object target)
        {
            if (target == null)
            {
                ClearSlot(slotId);
                using (new EditorGUI.DisabledScope(true))
                {
                    GUI.Button(rect, GetVisibilityContent(false), EditorStyles.miniButton);
                }

                return;
            }

            var expanded = IsExclusiveExpanded(groupId, slotId, target);
            if (GUI.Button(rect, GetVisibilityContent(expanded), EditorStyles.miniButton))
            {
                SetExclusiveExpanded(groupId, slotId, target, !expanded);
            }
        }

        /// <summary>
        /// 在当前引用行下方绘制已展开目标的真实自定义 Inspector。
        /// </summary>
        internal void DrawInlineInspector(string slotId, UnityEngine.Object target)
        {
            if (target == null)
            {
                ClearSlot(slotId);
                return;
            }

            DrawInlineInspectorContent(slotId, target, IsExpanded(slotId, target));
        }

        /// <summary>
        /// 绘制互斥组中当前活动槽位的真实 Inspector。
        /// </summary>
        internal void DrawExclusiveInlineInspector(
            string groupId,
            string slotId,
            UnityEngine.Object target)
        {
            if (target == null)
            {
                ClearSlot(slotId);
                return;
            }

            DrawInlineInspectorContent(
                slotId,
                target,
                IsExclusiveExpanded(groupId, slotId, target));
        }

        private void DrawInlineInspectorContent(
            string slotId,
            UnityEngine.Object target,
            bool expanded)
        {
            if (!expanded)
            {
                ReleaseEditor(GetOrCreateSlot(slotId, target));
                return;
            }

            var entry = GetOrCreateSlot(slotId, target);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                if (!string.IsNullOrEmpty(entry.ErrorMessage))
                {
                    EditorGUILayout.HelpBox(entry.ErrorMessage, MessageType.Error);
                    return;
                }

                var childEditor = GetOrCreateEditor(slotId, target);
                if (childEditor == null)
                {
                    entry.ErrorMessage = $"无法创建 {target.name} 的 Inspector。";
                    EditorGUILayout.HelpBox(entry.ErrorMessage, MessageType.Error);
                    return;
                }

                EditorGUI.indentLevel++;
                try
                {
                    childEditor.OnInspectorGUI();
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
            catch (Exception exception)
            {
                if (exception is ExitGUIException)
                {
                    throw;
                }

                entry.ErrorMessage = $"{target.name} 的 Inspector 绘制失败：{exception.Message}";
                Debug.LogException(exception);
                EditorGUILayout.HelpBox(entry.ErrorMessage, MessageType.Error);
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 让 Odin 条目 Drawer 在当前 ModuleConfigInspector 的宿主上下文中绘制。
        /// </summary>
        internal DrawingScope EnterDrawingScope()
        {
            return new DrawingScope(this);
        }

        #endregion

        #region 状态与缓存

        internal bool IsExpanded(string slotId, UnityEngine.Object target)
        {
            ThrowIfDisposed();
            if (target == null)
            {
                ClearSlot(slotId);
                return false;
            }

            GetOrCreateSlot(slotId, target);
            return m_state.IsExpanded(slotId, target);
        }

        internal void SetExpanded(string slotId, UnityEngine.Object target, bool expanded)
        {
            ThrowIfDisposed();
            if (target == null)
            {
                ClearSlot(slotId);
                return;
            }

            var entry = GetOrCreateSlot(slotId, target);
            m_state.SetExpanded(slotId, target, expanded);
            entry.ErrorMessage = null;
            if (!expanded)
            {
                ReleaseEditor(entry);
            }
        }

        internal bool IsExclusiveExpanded(
            string groupId,
            string slotId,
            UnityEngine.Object target)
        {
            ThrowIfDisposed();
            ValidateExclusiveGroup(groupId);
            if (target == null)
            {
                ClearSlot(slotId);
                return false;
            }

            GetOrCreateSlot(slotId, target);
            m_exclusiveGroupsBySlot[slotId] = groupId;
            return m_state.IsExclusiveExpanded(groupId, slotId, target);
        }

        internal void SetExclusiveExpanded(
            string groupId,
            string slotId,
            UnityEngine.Object target,
            bool expanded)
        {
            ThrowIfDisposed();
            ValidateExclusiveGroup(groupId);
            if (target == null)
            {
                ClearSlot(slotId);
                return;
            }

            var entry = GetOrCreateSlot(slotId, target);
            m_exclusiveGroupsBySlot[slotId] = groupId;
            m_state.SetExclusiveExpanded(groupId, slotId, target, expanded);
            entry.ErrorMessage = null;

            foreach (var pair in m_exclusiveGroupsBySlot)
            {
                if (pair.Value == groupId && pair.Key != slotId && m_slots.TryGetValue(pair.Key, out var other))
                {
                    ReleaseEditor(other);
                    other.ErrorMessage = null;
                }
            }

            if (!expanded)
            {
                ReleaseEditor(entry);
            }
        }

        internal UnityEditor.Editor GetOrCreateEditor(string slotId, UnityEngine.Object target)
        {
            ThrowIfDisposed();
            if (target == null)
            {
                ClearSlot(slotId);
                return null;
            }

            var entry = GetOrCreateSlot(slotId, target);
            UnityEditor.Editor.CreateCachedEditor(target, null, ref entry.Editor);
            return entry.Editor;
        }

        /// <summary>
        /// 释放指定前缀下已经不属于宿主数据的槽位，用于列表删除和绑定身份变化后的即时清理。
        /// </summary>
        internal void RetainSlots(string slotPrefix, ISet<string> validSlotIds)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(slotPrefix))
            {
                throw new ArgumentException("槽位前缀不能为空。", nameof(slotPrefix));
            }

            var staleSlotIds = new List<string>();
            foreach (var pair in m_slots)
            {
                if (pair.Key.StartsWith(slotPrefix, StringComparison.Ordinal) &&
                    (validSlotIds == null || !validSlotIds.Contains(pair.Key)))
                {
                    staleSlotIds.Add(pair.Key);
                }
            }

            for (var i = 0; i < staleSlotIds.Count; i++)
            {
                var slotId = staleSlotIds[i];
                ReleaseEditor(m_slots[slotId]);
                m_slots.Remove(slotId);
                m_exclusiveGroupsBySlot.Remove(slotId);
                m_state.Clear(slotId);
            }
        }

        /// <summary>
        /// 清理互斥组中已不属于当前配置的活动槽位和 Editor。
        /// </summary>
        internal void RetainExclusiveSlots(string groupId, ISet<string> validSlotIds)
        {
            ThrowIfDisposed();
            ValidateExclusiveGroup(groupId);
            var activeSlotId = m_state.GetExclusiveSlotId(groupId);
            if (!string.IsNullOrEmpty(activeSlotId) &&
                (validSlotIds == null || !validSlotIds.Contains(activeSlotId)))
            {
                m_state.ClearExclusiveGroup(groupId);
            }

            var staleSlotIds = new List<string>();
            foreach (var pair in m_exclusiveGroupsBySlot)
            {
                if (pair.Value == groupId &&
                    (validSlotIds == null || !validSlotIds.Contains(pair.Key)))
                {
                    staleSlotIds.Add(pair.Key);
                }
            }

            for (var i = 0; i < staleSlotIds.Count; i++)
            {
                ClearSlot(staleSlotIds[i]);
            }
        }

        internal static string BuildObjectIdentity(UnityEngine.Object target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string guid, out long localId))
            {
                return $"Asset:{guid}:{localId}";
            }

            return $"Instance:{target.GetEntityId()}";
        }

        private SlotEntry GetOrCreateSlot(string slotId, UnityEngine.Object target)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("内联 Inspector 槽位不能为空。", nameof(slotId));
            }

            var targetIdentity = BuildObjectIdentity(target);
            if (!m_slots.TryGetValue(slotId, out var entry))
            {
                entry = new SlotEntry(target, targetIdentity);
                m_slots.Add(slotId, entry);
                return entry;
            }

            if (entry.TargetIdentity == targetIdentity)
            {
                return entry;
            }

            // 引用替换后必须立即丢弃旧 Editor；SessionStateStore 会同时清理不匹配的展开目标。
            ReleaseEditor(entry);
            entry.Target = target;
            entry.TargetIdentity = targetIdentity;
            entry.ErrorMessage = null;
            return entry;
        }

        private void ClearSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("内联 Inspector 槽位不能为空。", nameof(slotId));
            }

            if (m_slots.TryGetValue(slotId, out var entry))
            {
                ReleaseEditor(entry);
                m_slots.Remove(slotId);
            }

            if (m_exclusiveGroupsBySlot.TryGetValue(slotId, out var groupId))
            {
                if (m_state.GetExclusiveSlotId(groupId) == slotId)
                {
                    m_state.ClearExclusiveGroup(groupId);
                }

                m_exclusiveGroupsBySlot.Remove(slotId);
            }

            m_state.Clear(slotId);
        }

        private static void ValidateExclusiveGroup(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentException("互斥 Inspector 组不能为空。", nameof(groupId));
            }
        }

        private static void ReleaseEditor(SlotEntry entry)
        {
            if (entry == null || entry.Editor == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(entry.Editor);
            entry.Editor = null;
        }

        private static GUIContent GetVisibilityContent(bool expanded)
        {
            if (EditorGUIUtility.isProSkin)
            {
                return expanded
                    ? s_visibilityOnDark ??= CreateVisibilityContent(true, true)
                    : s_visibilityOffDark ??= CreateVisibilityContent(false, true);
            }

            return expanded
                ? s_visibilityOnLight ??= CreateVisibilityContent(true, false)
                : s_visibilityOffLight ??= CreateVisibilityContent(false, false);
        }

        private static GUIContent CreateVisibilityContent(bool expanded, bool darkTheme)
        {
            var iconName = expanded ? "VisibilityOn" : "VisibilityOff";
            var source = EditorGUIUtility.IconContent(darkTheme ? $"d_{iconName}" : iconName);
            if (source.image == null)
            {
                source = EditorGUIUtility.IconContent($"d_{iconName}");
            }

            return new GUIContent(
                source.image,
                expanded ? "收起内联 Inspector" : "展开内联 Inspector");
        }

        private static void DisposeAll()
        {
            var hosts = new List<FrameworkInlineInspectorHost>(s_liveHosts);
            for (var i = 0; i < hosts.Count; i++)
            {
                hosts[i].Dispose();
            }

            s_currentDrawingHost = null;
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(nameof(FrameworkInlineInspectorHost));
            }
        }

        #endregion

        #region 生命周期

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            foreach (var entry in m_slots.Values)
            {
                ReleaseEditor(entry);
            }

            m_slots.Clear();
            m_exclusiveGroupsBySlot.Clear();
            s_liveHosts.Remove(this);
            if (ReferenceEquals(s_currentDrawingHost, this))
            {
                s_currentDrawingHost = null;
            }
        }

        #endregion

        internal sealed class SessionStateStore
        {
            private readonly string m_keyPrefix;

            internal SessionStateStore(string hostIdentity)
            {
                if (string.IsNullOrEmpty(hostIdentity))
                {
                    throw new ArgumentException("内联 Inspector 宿主标识不能为空。", nameof(hostIdentity));
                }

                m_keyPrefix = SessionPrefix + hostIdentity + ".";
            }

            internal bool IsExpanded(string slotId, UnityEngine.Object target)
            {
                var key = GetSessionKey(slotId);
                var savedIdentity = SessionState.GetString(key, string.Empty);
                var targetIdentity = BuildObjectIdentity(target);
                if (!string.IsNullOrEmpty(targetIdentity) && savedIdentity == targetIdentity)
                {
                    return true;
                }

                // 目标为空或引用已替换时清除旧值，避免稍后换回旧资产时意外自动展开。
                if (!string.IsNullOrEmpty(savedIdentity))
                {
                    SessionState.EraseString(key);
                }

                return false;
            }

            internal void SetExpanded(string slotId, UnityEngine.Object target, bool expanded)
            {
                var key = GetSessionKey(slotId);
                var targetIdentity = BuildObjectIdentity(target);
                if (!expanded || string.IsNullOrEmpty(targetIdentity))
                {
                    SessionState.EraseString(key);
                    return;
                }

                SessionState.SetString(key, targetIdentity);
            }

            internal void Clear(string slotId)
            {
                SessionState.EraseString(GetSessionKey(slotId));
            }

            internal bool IsExclusiveExpanded(
                string groupId,
                string slotId,
                UnityEngine.Object target)
            {
                var savedValue = SessionState.GetString(GetExclusiveSessionKey(groupId), string.Empty);
                var expectedValue = BuildExclusiveValue(slotId, target);
                if (!string.IsNullOrEmpty(expectedValue) && savedValue == expectedValue)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(savedValue) && GetExclusiveSlotId(groupId) == slotId)
                {
                    SessionState.EraseString(GetExclusiveSessionKey(groupId));
                }

                return false;
            }

            internal void SetExclusiveExpanded(
                string groupId,
                string slotId,
                UnityEngine.Object target,
                bool expanded)
            {
                var key = GetExclusiveSessionKey(groupId);
                var value = BuildExclusiveValue(slotId, target);
                if (!expanded || string.IsNullOrEmpty(value))
                {
                    SessionState.EraseString(key);
                    return;
                }

                SessionState.SetString(key, value);
            }

            internal string GetExclusiveSlotId(string groupId)
            {
                var value = SessionState.GetString(GetExclusiveSessionKey(groupId), string.Empty);
                var separatorIndex = value.IndexOf('\n');
                return separatorIndex < 0 ? string.Empty : value.Substring(0, separatorIndex);
            }

            internal void ClearExclusiveGroup(string groupId)
            {
                SessionState.EraseString(GetExclusiveSessionKey(groupId));
            }

            internal string GetSessionKey(string slotId)
            {
                if (string.IsNullOrEmpty(slotId))
                {
                    throw new ArgumentException("内联 Inspector 槽位不能为空。", nameof(slotId));
                }

                return m_keyPrefix + slotId;
            }

            internal string GetExclusiveSessionKey(string groupId)
            {
                if (string.IsNullOrEmpty(groupId))
                {
                    throw new ArgumentException("互斥 Inspector 组不能为空。", nameof(groupId));
                }

                return m_keyPrefix + "Exclusive." + groupId;
            }

            private static string BuildExclusiveValue(string slotId, UnityEngine.Object target)
            {
                if (string.IsNullOrEmpty(slotId))
                {
                    throw new ArgumentException("内联 Inspector 槽位不能为空。", nameof(slotId));
                }

                var identity = BuildObjectIdentity(target);
                return string.IsNullOrEmpty(identity) ? string.Empty : slotId + "\n" + identity;
            }
        }

        internal readonly struct DrawingScope : IDisposable
        {
            private readonly FrameworkInlineInspectorHost m_previousHost;

            internal DrawingScope(FrameworkInlineInspectorHost host)
            {
                m_previousHost = s_currentDrawingHost;
                s_currentDrawingHost = host;
            }

            public void Dispose()
            {
                s_currentDrawingHost = m_previousHost;
            }
        }

        private sealed class SlotEntry
        {
            internal SlotEntry(UnityEngine.Object target, string targetIdentity)
            {
                Target = target;
                TargetIdentity = targetIdentity;
            }

            internal UnityEngine.Object Target;
            internal string TargetIdentity;
            internal UnityEditor.Editor Editor;
            internal string ErrorMessage;
        }
    }
}
