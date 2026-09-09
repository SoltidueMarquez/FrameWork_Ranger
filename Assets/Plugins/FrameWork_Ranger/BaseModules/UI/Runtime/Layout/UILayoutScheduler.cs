using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>合并根刷新后交给 uGUI。布局期间的递归刷新延后，不另建全树每帧计算循环。</summary>
    [FrameworkArchitecture("UI 布局调度", "合并脏根并接入 uGUI 布局刷新。", FrameworkArchitectureLayer.RuntimeDriving, 130)]
    public static class UILayoutScheduler
    {
        private static readonly HashSet<RectTransform> Dirty = new HashSet<RectTransform>();
        private static int s_batch;
        private static bool s_registered, s_flushing;
        public static IDisposable BeginBatch() { s_batch++; return new Batch(); }
        private sealed class Batch : IDisposable
        {
            private bool m_disposed;
            public void Dispose() { if (m_disposed) return; m_disposed = true; s_batch--; if (s_batch == 0) Submit(); }
        }
        public static void MarkDirty(RectTransform rect)
        {
            if (!rect || !rect.gameObject.activeInHierarchy) return;
            Dirty.Add(FindRoot(rect));
            if (!s_registered) { Canvas.preWillRenderCanvases += Submit; s_registered = true; }
        }
        public static void FlushNow(RectTransform rect)
        {
            if (!rect) return;
            var root = FindRoot(rect);
            if (s_flushing || CanvasUpdateRegistry.IsRebuildingLayout()) { MarkDirty(root); return; }
            Dirty.Remove(root);
            s_flushing = true;
            try { LayoutRebuilder.ForceRebuildLayoutImmediate(root); }
            finally { s_flushing = false; }
        }
        internal static void ForgetSubtree(Transform root)
        {
            Dirty.RemoveWhere(rect => !rect || rect == root || rect.IsChildOf(root));
            if (Dirty.Count == 0 && s_registered) { Canvas.preWillRenderCanvases -= Submit; s_registered = false; }
        }
        private static RectTransform FindRoot(RectTransform rect)
        {
            var root = rect;
            while (root.parent is RectTransform parent && parent.GetComponent<LayoutGroup>() is LayoutGroup group &&
                group.isActiveAndEnabled) root = parent;
            return root;
        }
        private static void Submit()
        {
            if (s_batch > 0 || s_flushing || CanvasUpdateRegistry.IsRebuildingLayout()) return;
            var roots = new List<RectTransform>(Dirty); Dirty.Clear();
            foreach (var root in roots) if (root && root.gameObject.activeInHierarchy) LayoutRebuilder.MarkLayoutForRebuild(root);
            if (Dirty.Count == 0 && s_registered) { Canvas.preWillRenderCanvases -= Submit; s_registered = false; }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { Canvas.preWillRenderCanvases -= Submit; Dirty.Clear(); s_batch = 0; s_registered = s_flushing = false; }
    }
}
