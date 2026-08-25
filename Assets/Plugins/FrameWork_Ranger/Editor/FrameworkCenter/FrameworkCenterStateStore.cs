using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// 在项目 Library 中持久化 Framework Center 的固定页签状态。
    /// 预览页签属于当前窗口会话，不进入该文件。
    /// </summary>
    [FrameworkArchitecture(
        "Center 本地状态",
        "保存并恢复固定页签顺序和最后活动固定页，不污染版本控制。",
        FrameworkArchitectureLayer.EditorIntegration,
        130)]
    internal sealed class FrameworkCenterStateStore
    {
        internal static readonly string DefaultPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Library",
            "FrameWork_Ranger",
            "FrameworkCenterState.json");

        private readonly string m_path;

        internal FrameworkCenterStateStore(string path = null)
        {
            m_path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
        }

        internal FrameworkCenterStateData Load()
        {
            if (!File.Exists(m_path))
            {
                return new FrameworkCenterStateData();
            }

            try
            {
                var json = File.ReadAllText(m_path);
                // Phase 1.2 的状态没有版本字段。普通打开页不等于用户主动固定，
                // 因此旧 openTabs/recentPageIds 不能自动升级为固定快捷入口。
                if (json.IndexOf("\"stateVersion\"", StringComparison.Ordinal) < 0)
                {
                    return new FrameworkCenterStateData();
                }

                var state = JsonUtility.FromJson<FrameworkCenterStateData>(json);
                if (state == null || state.stateVersion != FrameworkCenterStateData.CurrentVersion)
                {
                    return new FrameworkCenterStateData();
                }

                state.pinnedPageIds = state.pinnedPageIds ?? new List<string>();
                return state;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FrameWork_Ranger] Framework Center 状态读取失败，已回退首页：{exception.Message}");
                return new FrameworkCenterStateData();
            }
        }

        internal void Save(FrameworkCenterStateData state)
        {
            try
            {
                state.stateVersion = FrameworkCenterStateData.CurrentVersion;
                state.pinnedPageIds = state.pinnedPageIds ?? new List<string>();

                var directory = Path.GetDirectoryName(m_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(m_path, JsonUtility.ToJson(state, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FrameWork_Ranger] Framework Center 状态保存失败：{exception.Message}");
            }
        }
    }

    /// <summary>
    /// JsonUtility 使用的 Framework Center v2 状态数据。
    /// 这里只保存用户显式固定的页面，不保存临时预览页。
    /// </summary>
    [FrameworkArchitecture(
        "Center 状态数据",
        "承载版本化的固定页签顺序和最后活动固定页。",
        FrameworkArchitectureLayer.EditorIntegration,
        135)]
    [Serializable]
    internal sealed class FrameworkCenterStateData
    {
        internal const int CurrentVersion = 2;

        public int stateVersion = CurrentVersion;
        public string lastActivePinnedPageId = string.Empty;
        public List<string> pinnedPageIds = new List<string>();
    }
}
