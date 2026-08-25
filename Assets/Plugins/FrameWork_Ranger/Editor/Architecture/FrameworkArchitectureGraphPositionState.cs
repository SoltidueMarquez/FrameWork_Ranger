using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 保存代码架构图在当前 Unity 会话中的人工位置偏移。
    /// 位置只属于开发者的临时阅读上下文，不写入项目资产或版本控制文件。
    /// </summary>
    [FrameworkArchitecture(
        "架构图会话位置",
        "按稳定分组和类型键保存节点临时偏移，并在目录变化后清理失效记录。",
        FrameworkArchitectureLayer.EditorIntegration,
        347,
        typeof(FrameworkArchitectureCatalog),
        typeof(FrameworkArchitectureGraphLayout))]
    internal sealed class FrameworkArchitectureGraphPositionState
    {
        internal const string DefaultSessionKey =
            "FrameWork_Ranger.FrameworkCenter.Architecture.NodePositions.v1";

        private const float Epsilon = 0.01f;

        #region 运行时状态

        private readonly string m_sessionKey;
        private readonly Dictionary<string, Vector2> m_offsets =
            new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private bool m_isDirty;
        private int m_revision;

        #endregion

        #region 公共属性

        internal int Revision => m_revision;

        internal bool IsDirty => m_isDirty;

        internal int OffsetCount => m_offsets.Count;

        #endregion

        internal FrameworkArchitectureGraphPositionState(string sessionKey = DefaultSessionKey)
        {
            m_sessionKey = string.IsNullOrWhiteSpace(sessionKey)
                ? DefaultSessionKey
                : sessionKey;
        }

        #region 会话生命周期

        internal void Restore(FrameworkArchitectureCatalog catalog)
        {
            m_offsets.Clear();
            var serialized = SessionState.GetString(m_sessionKey, string.Empty);
            var lines = serialized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var fields = lines[i].Split('|');
                if (fields.Length != 3 ||
                    !float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                    !float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    continue;
                }

                var value = new Vector2(x, y);
                if (value.sqrMagnitude > Epsilon * Epsilon)
                {
                    m_offsets[fields[0]] = value;
                }
            }

            m_isDirty = false;
            m_revision++;
            Sanitize(catalog);
        }

        internal void Sanitize(FrameworkArchitectureCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var validKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in catalog.Groups.Where(group => !group.IsRoot))
            {
                validKeys.Add(GetGroupKey(group));
            }

            foreach (var type in catalog.Nodes)
            {
                validKeys.Add(GetTypeKey(type));
            }

            var removed = m_offsets.Keys.Where(key => !validKeys.Contains(key)).ToArray();
            for (var i = 0; i < removed.Length; i++)
            {
                m_offsets.Remove(removed[i]);
            }

            if (removed.Length > 0)
            {
                MarkChanged();
                Save();
            }
        }

        internal void Save()
        {
            if (!m_isDirty)
            {
                return;
            }

            var lines = m_offsets
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}|{1:R}|{2:R}",
                    pair.Key,
                    pair.Value.x,
                    pair.Value.y));
            SessionState.SetString(m_sessionKey, string.Join("\n", lines));
            m_isDirty = false;
        }

        #endregion

        #region 位置操作

        internal Vector2 GetOffset(FrameworkArchitectureGroupDescriptor group)
        {
            return group != null && m_offsets.TryGetValue(GetGroupKey(group), out var value)
                ? value
                : Vector2.zero;
        }

        internal Vector2 GetOffset(FrameworkArchitectureTypeDescriptor type)
        {
            return type != null && m_offsets.TryGetValue(GetTypeKey(type), out var value)
                ? value
                : Vector2.zero;
        }

        internal bool HasOffset(FrameworkArchitectureGroupDescriptor group)
        {
            return group != null && m_offsets.ContainsKey(GetGroupKey(group));
        }

        internal bool HasOffset(FrameworkArchitectureTypeDescriptor type)
        {
            return type != null && m_offsets.ContainsKey(GetTypeKey(type));
        }

        internal void MoveGroup(FrameworkArchitectureGroupDescriptor group, Vector2 delta)
        {
            if (group == null || group.IsRoot || delta.sqrMagnitude <= Epsilon * Epsilon)
            {
                return;
            }

            var next = GetOffset(group) + delta;
            SetOffset(GetGroupKey(group), next);
        }

        internal void MoveType(FrameworkArchitectureTypeDescriptor type, Vector2 delta)
        {
            if (type == null || delta.sqrMagnitude <= Epsilon * Epsilon)
            {
                return;
            }

            var next = GetOffset(type) + delta;
            SetOffset(GetTypeKey(type), next);
        }

        internal void Reset(FrameworkArchitectureGroupDescriptor group)
        {
            if (group != null && m_offsets.Remove(GetGroupKey(group)))
            {
                MarkChanged();
            }
        }

        internal void Reset(FrameworkArchitectureTypeDescriptor type)
        {
            if (type != null && m_offsets.Remove(GetTypeKey(type)))
            {
                MarkChanged();
            }
        }

        internal void ResetAll()
        {
            if (m_offsets.Count == 0)
            {
                return;
            }

            m_offsets.Clear();
            MarkChanged();
            Save();
        }

        internal static string GetGroupKey(FrameworkArchitectureGroupDescriptor group)
        {
            return $"G:{group?.GroupId ?? string.Empty}";
        }

        internal static string GetTypeKey(FrameworkArchitectureTypeDescriptor type)
        {
            return $"T:{type?.Type?.AssemblyQualifiedName ?? string.Empty}";
        }

        private void SetOffset(string key, Vector2 value)
        {
            if (value.sqrMagnitude <= Epsilon * Epsilon)
            {
                if (m_offsets.Remove(key))
                {
                    MarkChanged();
                }

                return;
            }

            if (m_offsets.TryGetValue(key, out var current) &&
                (current - value).sqrMagnitude <= Epsilon * Epsilon)
            {
                return;
            }

            m_offsets[key] = value;
            MarkChanged();
        }

        private void MarkChanged()
        {
            m_isDirty = true;
            m_revision++;
        }

        #endregion
    }
}
