using System.Collections.Generic;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 清理 Framework Center 本地状态中的失效、重复页面 ID，并保证存在可激活页面。
    /// 独立于窗口后，测试可以直接验证旧测试标签不会再次恢复。
    /// </summary>
    [FrameworkArchitecture(
        "Center 状态清理器",
        "移除失效和重复 PageId，并为标签与当前页建立稳定回退。",
        FrameworkArchitectureLayer.EditorIntegration,
        138,
        typeof(FrameworkCenterStateData),
        typeof(FrameworkCenterPageRegistry))]
    internal static class FrameworkCenterStateSanitizer
    {
        internal static void Sanitize(
            FrameworkCenterStateData state,
            FrameworkCenterPageRegistry registry,
            string fallbackPageId)
        {
            state.openTabs = SanitizeList(state.openTabs, registry);
            state.recentPageIds = SanitizeList(state.recentPageIds, registry);

            if (state.openTabs.Count == 0 && registry.TryGetPage(fallbackPageId, out _))
            {
                state.openTabs.Add(fallbackPageId);
            }

            if (!registry.TryGetPage(state.activePageId, out _))
            {
                state.activePageId = state.openTabs.Count > 0 ? state.openTabs[0] : string.Empty;
            }

            if (!string.IsNullOrEmpty(state.activePageId) && !state.openTabs.Contains(state.activePageId))
            {
                state.openTabs.Add(state.activePageId);
            }
        }

        private static List<string> SanitizeList(
            IReadOnlyList<string> source,
            FrameworkCenterPageRegistry registry)
        {
            var result = new List<string>();
            var seen = new HashSet<string>();
            if (source == null)
            {
                return result;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var pageId = source[i];
                if (registry.TryGetPage(pageId, out _) && seen.Add(pageId))
                {
                    result.Add(pageId);
                }
            }

            return result;
        }
    }
}
