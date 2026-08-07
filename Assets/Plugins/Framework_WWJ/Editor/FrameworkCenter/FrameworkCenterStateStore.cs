using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 在项目 Library 中持久化 Framework Center 的本地标签与最近访问状态。
    /// </summary>
    [FrameworkArchitecture(
        "Center 本地状态",
        "保存并恢复当前页面、打开标签和最近访问，不污染版本控制。",
        FrameworkArchitectureLayer.EditorIntegration,
        130)]
    internal sealed class FrameworkCenterStateStore
    {
        internal static readonly string DefaultPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Library",
            "Framework_WWJ",
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
                var state = JsonUtility.FromJson<FrameworkCenterStateData>(File.ReadAllText(m_path));
                return state ?? new FrameworkCenterStateData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Framework_WWJ] Framework Center 状态读取失败，已回退首页：{exception.Message}");
                return new FrameworkCenterStateData();
            }
        }

        internal void Save(FrameworkCenterStateData state)
        {
            try
            {
                var directory = Path.GetDirectoryName(m_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(m_path, JsonUtility.ToJson(state, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Framework_WWJ] Framework Center 状态保存失败：{exception.Message}");
            }
        }
    }

    /// <summary>
    /// JsonUtility 使用的 Framework Center 状态数据。
    /// </summary>
    [FrameworkArchitecture(
        "Center 状态数据",
        "承载 JsonUtility 可序列化的当前页、标签与最近访问列表。",
        FrameworkArchitectureLayer.EditorIntegration,
        135)]
    [Serializable]
    internal sealed class FrameworkCenterStateData
    {
        public string activePageId = string.Empty;
        public List<string> openTabs = new List<string>();
        public List<string> recentPageIds = new List<string>();
    }
}
