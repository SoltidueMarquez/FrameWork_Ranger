using System;
using System.Collections.Generic;

namespace FrameWork_Ranger.Editor
{
    /// <summary>
    /// Framework Center 的纯页签状态模型。
    /// 它只决定固定、预览、活动和回退关系，不调用页面生命周期或绘制 Unity GUI。
    /// </summary>
    [FrameworkArchitecture(
        "Center 页签模型",
        "管理单预览页、固定页顺序、关闭回退和固定状态转换。",
        FrameworkArchitectureLayer.EditorIntegration,
        137,
        typeof(FrameworkCenterStateData),
        typeof(FrameworkCenterPageRegistry))]
    internal sealed class FrameworkCenterTabModel
    {
        private readonly FrameworkCenterStateData m_persistedState;
        private readonly FrameworkCenterPageRegistry m_registry;
        private readonly string m_fallbackPageId;

        private string m_previewPageId = string.Empty;
        private string m_activePageId = string.Empty;

        internal FrameworkCenterTabModel(
            FrameworkCenterStateData persistedState,
            FrameworkCenterPageRegistry registry,
            string fallbackPageId)
        {
            m_persistedState = persistedState ?? throw new ArgumentNullException(nameof(persistedState));
            m_registry = registry ?? throw new ArgumentNullException(nameof(registry));
            m_fallbackPageId = fallbackPageId;

            FrameworkCenterStateSanitizer.Sanitize(m_persistedState, m_registry);
            if (IsPinned(m_persistedState.lastActivePinnedPageId))
            {
                m_activePageId = m_persistedState.lastActivePinnedPageId;
            }
            else
            {
                OpenFallbackPreview();
            }
        }

        #region 公开状态

        internal IReadOnlyList<string> PinnedPageIds => m_persistedState.pinnedPageIds;

        internal string PreviewPageId => m_previewPageId;

        internal string ActivePageId => m_activePageId;

        internal bool IsPinned(string pageId)
        {
            return !string.IsNullOrEmpty(pageId) && m_persistedState.pinnedPageIds.Contains(pageId);
        }

        #endregion

        #region 页面操作

        internal bool OpenPage(string pageId)
        {
            if (!IsValidPage(pageId))
            {
                return false;
            }

            if (IsPinned(pageId))
            {
                m_activePageId = pageId;
                m_persistedState.lastActivePinnedPageId = pageId;
                return true;
            }

            m_previewPageId = pageId;
            m_activePageId = pageId;
            return true;
        }

        internal bool PinPreview()
        {
            if (!IsValidPage(m_previewPageId) || IsPinned(m_previewPageId))
            {
                return false;
            }

            var pageId = m_previewPageId;
            m_persistedState.pinnedPageIds.Add(pageId);
            m_persistedState.lastActivePinnedPageId = pageId;
            m_previewPageId = string.Empty;
            m_activePageId = pageId;
            return true;
        }

        internal bool Unpin(string pageId)
        {
            var index = m_persistedState.pinnedPageIds.IndexOf(pageId);
            if (index < 0)
            {
                return false;
            }

            m_persistedState.pinnedPageIds.RemoveAt(index);
            m_persistedState.lastActivePinnedPageId = GetNeighborPinnedPageId(index);

            // 取消固定表达了用户希望继续临时查看该页，因此它主动成为唯一预览，
            // 即使原来不是活动页也会替换旧预览并获得焦点。
            m_previewPageId = pageId;
            m_activePageId = pageId;
            return true;
        }

        internal bool Close(string pageId)
        {
            var pinnedIndex = m_persistedState.pinnedPageIds.IndexOf(pageId);
            if (pinnedIndex >= 0)
            {
                return ClosePinned(pageId, pinnedIndex);
            }

            if (m_previewPageId != pageId)
            {
                return false;
            }

            m_previewPageId = string.Empty;
            if (m_activePageId == pageId)
            {
                ActivateLastPinnedOrFallback();
            }

            return true;
        }

        internal bool MovePinnedToInsertionIndex(string pageId, int insertionIndex)
        {
            var sourceIndex = m_persistedState.pinnedPageIds.IndexOf(pageId);
            if (sourceIndex < 0)
            {
                return false;
            }

            insertionIndex = Math.Max(0, Math.Min(insertionIndex, m_persistedState.pinnedPageIds.Count));
            m_persistedState.pinnedPageIds.RemoveAt(sourceIndex);
            if (insertionIndex > sourceIndex)
            {
                insertionIndex--;
            }

            insertionIndex = Math.Max(0, Math.Min(insertionIndex, m_persistedState.pinnedPageIds.Count));
            m_persistedState.pinnedPageIds.Insert(insertionIndex, pageId);
            return insertionIndex != sourceIndex;
        }

        #endregion

        #region 内部回退

        private bool ClosePinned(string pageId, int pinnedIndex)
        {
            var closingActive = m_activePageId == pageId;
            m_persistedState.pinnedPageIds.RemoveAt(pinnedIndex);

            if (m_persistedState.lastActivePinnedPageId == pageId)
            {
                m_persistedState.lastActivePinnedPageId = GetNeighborPinnedPageId(pinnedIndex);
            }

            if (!closingActive)
            {
                return true;
            }

            var neighbor = GetNeighborPinnedPageId(pinnedIndex);
            if (!string.IsNullOrEmpty(neighbor))
            {
                m_activePageId = neighbor;
                m_persistedState.lastActivePinnedPageId = neighbor;
            }
            else if (IsValidPage(m_previewPageId))
            {
                m_activePageId = m_previewPageId;
            }
            else
            {
                OpenFallbackPreview();
            }

            return true;
        }

        private void ActivateLastPinnedOrFallback()
        {
            if (IsPinned(m_persistedState.lastActivePinnedPageId))
            {
                m_activePageId = m_persistedState.lastActivePinnedPageId;
                return;
            }

            if (m_persistedState.pinnedPageIds.Count > 0)
            {
                m_activePageId = m_persistedState.pinnedPageIds[0];
                m_persistedState.lastActivePinnedPageId = m_activePageId;
                return;
            }

            OpenFallbackPreview();
        }

        private string GetNeighborPinnedPageId(int removedIndex)
        {
            if (m_persistedState.pinnedPageIds.Count == 0)
            {
                return string.Empty;
            }

            var leftIndex = removedIndex - 1;
            if (leftIndex >= 0)
            {
                return m_persistedState.pinnedPageIds[leftIndex];
            }

            var rightIndex = Math.Min(removedIndex, m_persistedState.pinnedPageIds.Count - 1);
            return m_persistedState.pinnedPageIds[rightIndex];
        }

        private void OpenFallbackPreview()
        {
            if (!IsValidPage(m_fallbackPageId))
            {
                m_previewPageId = string.Empty;
                m_activePageId = string.Empty;
                return;
            }

            m_previewPageId = m_fallbackPageId;
            m_activePageId = m_fallbackPageId;
        }

        private bool IsValidPage(string pageId)
        {
            return !string.IsNullOrEmpty(pageId) && m_registry.TryGetPage(pageId, out _);
        }

        #endregion
    }
}
