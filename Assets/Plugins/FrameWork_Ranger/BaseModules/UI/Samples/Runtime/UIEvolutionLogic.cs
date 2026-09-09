using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace FrameWork_Ranger.UI.Samples
{
    [Serializable] public sealed class UIEvolutionLogic : UILogic<UIEvolutionView>
    {
        [NonSerialized] private List<IDisposable> m_subscriptions;
        [NonSerialized] private int m_clicks;
        public override void OnOpen(object data)
        {
            m_subscriptions = new List<IDisposable>
            {
                View.Gesture.onClick.Subscribe(() => { m_clicks++; View.Status.text = "Click " + m_clicks; }),
                View.Gesture.DoubleClicked.Subscribe(() => View.Status.text = "Double click (normal clicks also sent)"),
                View.Gesture.OnLongPress.Subscribe(() => View.Status.text = "Long press / release does not click"),
                View.Gesture.RightClicked.Subscribe(() => View.Status.text = "Right click"),
                View.CloseButton.onClick.Subscribe(() => UI.Close("Components")),
                View.ScrollBottom.onClick.Subscribe(() => View.Scroll.ScrollToAsync(Vector2.zero, .45f).Forget())
            };
        }
        public override void OnClose() { foreach (var subscription in m_subscriptions) subscription.Dispose(); m_subscriptions.Clear(); }
    }
}
