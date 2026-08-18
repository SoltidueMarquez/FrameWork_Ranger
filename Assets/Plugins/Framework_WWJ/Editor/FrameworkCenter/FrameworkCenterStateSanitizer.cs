using System.Collections.Generic;

namespace Framework_WWJ.Editor
{
    /// <summary>
    /// 清理 Framework Center 固定页签中的失效或重复页面 ID。
    /// 预览页不持久化，因此回退首页由运行时页签模型负责。
    /// </summary>
    [FrameworkArchitecture(
        "Center 状态清理器",
        "清理固定 PageId，并为最后活动固定页建立稳定回退。",
        FrameworkArchitectureLayer.EditorIntegration,
        138,
        typeof(FrameworkCenterStateData),
        typeof(FrameworkCenterPageRegistry))]
    internal static class FrameworkCenterStateSanitizer
    {
        internal static void Sanitize(
            FrameworkCenterStateData state,
            FrameworkCenterPageRegistry registry)
        {
            state.stateVersion = FrameworkCenterStateData.CurrentVersion;
            state.pinnedPageIds = SanitizeList(state.pinnedPageIds, registry);

            if (!state.pinnedPageIds.Contains(state.lastActivePinnedPageId))
            {
                state.lastActivePinnedPageId = state.pinnedPageIds.Count > 0
                    ? state.pinnedPageIds[0]
                    : string.Empty;
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
