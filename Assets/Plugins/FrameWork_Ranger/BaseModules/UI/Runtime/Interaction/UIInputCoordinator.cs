using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>只控制 UI 自有门禁，不改写业务 Selectable.interactable。</summary>
    [FrameworkArchitecture("UI 输入协调", "计算模态限制与焦点恢复。", FrameworkArchitectureLayer.RuntimeDriving, 130)]
    internal sealed class UIInputCoordinator
    {
        private readonly List<UIPanelRecord> m_panels = new List<UIPanelRecord>();
        private readonly Dictionary<UIDomain, UIPanelRecord> m_domainModals = new Dictionary<UIDomain, UIPanelRecord>();
        private UIPanelRecord m_focus;
        private bool m_updating;

        internal void Update(UIContext global, UIContext scene)
        {
            if (m_updating) return;
            m_updating = true;
            try
            {
                m_panels.Clear(); m_domainModals.Clear();
                global?.CollectOpen(m_panels); scene?.CollectOpen(m_panels);
                UIPanelRecord globalModal = null;
                foreach (var panel in m_panels)
                {
                    if (!panel.Domain.IsPresented) continue;
                    if (panel.Definition.Modal == UIModalScope.Global &&
                        (globalModal == null || panel.Sequence > globalModal.Sequence)) globalModal = panel;
                }
                foreach (var panel in m_panels)
                {
                    if (!panel.Domain.IsPresented || panel.Definition.Modal != UIModalScope.Domain ||
                        (globalModal != null && !Within(panel, globalModal))) continue;
                    if (!m_domainModals.TryGetValue(panel.Domain, out var previous) || panel.Sequence > previous.Sequence)
                        m_domainModals[panel.Domain] = panel;
                }
                UIPanelRecord focus = null;
                foreach (var panel in m_panels)
                {
                    bool allowed = panel.Domain.IsPresented && (globalModal == null || Within(panel, globalModal));
                    if (allowed && m_domainModals.TryGetValue(panel.Domain, out var modal)) allowed = Within(panel, modal);
                    panel.InputAllowed = allowed && panel.Open && !panel.Entering && !panel.Closing;
                    if (panel.Gate) { panel.Gate.interactable = panel.InputAllowed; panel.Gate.blocksRaycasts = allowed; }
                    if (panel.InputAllowed && (focus == null || panel.Sequence > focus.Sequence)) focus = panel;
                }
                var events = EventSystem.current;
                var selected = events ? events.currentSelectedGameObject : null;
                if (selected)
                {
                    foreach (var panel in m_panels)
                        if (panel.Host && selected.transform.IsChildOf(panel.Host.transform))
                        {
                            panel.LastSelection = selected;
                            if (!panel.InputAllowed) events.SetSelectedGameObject(null);
                            break;
                        }
                }
                if (m_focus != focus)
                {
                    var old = m_focus;
                    m_focus = focus;
                    if (old != null && old.Focused && !old.Disposed)
                    { old.Focused = false; old.Context.InvokeCleanup(old.Logic.OnBlur); }
                    if (focus != null && focus.Open && !focus.Closing)
                    {
                        focus.Focused = true;
                        focus.Context.InvokeCleanup(focus.Logic.OnFocus);
                        var restore = focus.LastSelection;
                        var selectable = restore ? restore.GetComponent<Selectable>() : null;
                        if (focus.Open && !focus.Closing && events && selectable && selectable.IsActive() && selectable.IsInteractable())
                            events.SetSelectedGameObject(restore);
                    }
                }
            }
            finally { m_updating = false; }
        }
        private static bool Within(UIPanelRecord panel, UIPanelRecord ancestor)
        {
            for (var current = panel; current != null; current = current.Parent)
                if (current == ancestor) return true;
            return false;
        }
    }
}
