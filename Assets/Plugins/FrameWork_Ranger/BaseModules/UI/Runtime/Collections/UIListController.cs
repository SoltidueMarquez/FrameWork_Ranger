using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>按稳定 key 同步并复用存量条目；不扫除父节点下的手放对象。</summary>
    [FrameworkArchitecture("UI 列表同步", "按稳定 key 复用、排序和释放条目。", FrameworkArchitectureLayer.PublicFacade, 130)]
    public sealed class UIListController<TKey, TData, TView> : IDisposable where TView : UIItemView
    {
        private readonly Dictionary<TKey, TView> m_items = new Dictionary<TKey, TView>();
        private readonly RectTransform m_parent;
        private readonly TView m_template;
        private readonly Func<TData, TKey> m_key;
        private readonly Action<TView, TData> m_bind;
        private bool m_disposed;
        public IReadOnlyDictionary<TKey, TView> Items => m_items;
        public UIListController(RectTransform parent, TView template, Func<TData, TKey> key,
            Action<TView, TData> bind = null)
        {
            m_parent = parent ? parent : throw new ArgumentNullException(nameof(parent));
            m_template = template ? template : throw new ArgumentNullException(nameof(template));
            m_key = key ?? throw new ArgumentNullException(nameof(key));
            m_bind = bind ?? ((view, data) => view.Bind(data));
        }
        public void Sync(IReadOnlyList<TData> data)
        {
            if (m_disposed) throw new ObjectDisposedException(GetType().Name);
            if (data == null) throw new ArgumentNullException(nameof(data));
            var keys = new HashSet<TKey>();
            var order = new List<TKey>(data.Count);
            for (int i = 0; i < data.Count; i++)
            {
                var key = m_key(data[i]);
                if (ReferenceEquals(key, null) || !keys.Add(key)) throw new ArgumentException("列表 key 为空或重复。");
                order.Add(key);
            }
            using (UILayoutScheduler.BeginBatch())
            {
                foreach (var key in new List<TKey>(m_items.Keys))
                    if (!keys.Contains(key)) Remove(key);
                for (int i = 0; i < order.Count; i++)
                {
                    if (!m_items.TryGetValue(order[i], out var view) || !view)
                    {
                        view = UnityEngine.Object.Instantiate(m_template, m_parent, false);
                        m_items[order[i]] = view;
                    }
                    view.gameObject.SetActive(true);
                    m_bind(view, data[i]);
                    view.transform.SetAsLastSibling();
                }
                UILayoutScheduler.MarkDirty(m_parent);
            }
        }
        private void Remove(TKey key)
        {
            var view = m_items[key]; m_items.Remove(key);
            if (!view) return;
            try { view.Unbind(); }
            finally { view.gameObject.SetActive(false); GlobalUIModule.DestroyOwned(view.gameObject); }
        }
        /// <summary>更新数据时保留最近的存活可见项；删除锚点时使用邻近存活项。</summary>
        public void Sync(IReadOnlyList<TData> data, ScrollRect preservePosition)
        {
            if (!preservePosition || preservePosition.content != m_parent) throw new ArgumentException("ScrollRect.content 必须是列表父节点。");
            if (data == null) throw new ArgumentNullException(nameof(data));
            UILayoutScheduler.FlushNow(m_parent);
            var viewport = preservePosition.viewport ? preservePosition.viewport : (RectTransform)preservePosition.transform;
            var survivors = new HashSet<TKey>();
            foreach (var value in data) survivors.Add(m_key(value));
            TView anchor = null; Vector3 oldPosition = default; float nearest = float.MaxValue;
            foreach (var pair in m_items)
            {
                if (!survivors.Contains(pair.Key) || !pair.Value) continue;
                var rect = (RectTransform)pair.Value.transform;
                var point = viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                float distance = preservePosition.vertical ? Mathf.Abs(point.y - viewport.rect.yMax) : Mathf.Abs(point.x - viewport.rect.xMin);
                if (distance < nearest) { nearest = distance; anchor = pair.Value; oldPosition = point; }
            }
            var normalized = preservePosition.normalizedPosition;
            Sync(data); UILayoutScheduler.FlushNow(m_parent);
            if (anchor)
            {
                var rect = (RectTransform)anchor.transform;
                var delta = oldPosition - viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                if (!preservePosition.horizontal) delta.x = 0;
                if (!preservePosition.vertical) delta.y = 0;
                delta = m_parent.parent.InverseTransformVector(viewport.TransformVector(delta));
                m_parent.anchoredPosition += new Vector2(delta.x, delta.y);
                normalized = preservePosition.normalizedPosition;
            }
            preservePosition.ScrollToNormalized(normalized);
        }
        public void Clear()
        {
            foreach (var key in new List<TKey>(m_items.Keys))
            {
                try { Remove(key); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            if (m_parent) UILayoutScheduler.MarkDirty(m_parent);
        }
        public void Dispose() { if (m_disposed) return; m_disposed = true; Clear(); }
    }
}
