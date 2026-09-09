using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace FrameWork_Ranger.UI.Samples
{
    [Serializable]
    public sealed class UISampleLogic : UILogic<UISampleView>
    {
        [NonSerialized] private UIListController<int, int, UISampleItemView> m_list;
        [NonSerialized] private string m_key;
        [NonSerialized] private int m_count;
        public override void OnCreate()
        {
            m_list = new UIListController<int, int, UISampleItemView>(View.Scroll.content, View.ItemTemplate,
                value => value, (view, value) => view.Bind("Item " + value + " — stable key / reusable View"));
        }
        public override void OnOpen(object data)
        {
            m_key = data as string ?? "Inventory";
            View.Title.text = m_key + "  /  " + UI.Owner;
            View.AddItem.onClick.AddListener(Add);
            View.OpenConfirm.onClick.AddListener(Confirm);
            View.ClosePanel.onClick.AddListener(Close);
            View.OpenConfirm.interactable = !m_key.EndsWith("Confirm", StringComparison.Ordinal);
            m_count = 3; RefreshItems();
        }
        public override void OnRefresh(object data) { View.Title.text = m_key + " / refreshed"; }
        private void Add()
        {
            m_count++; RefreshItems();
            View.Scroll.ScrollToItem(m_list.Items[m_count].RectTransform);
        }
        private void RefreshItems()
        {
            var values = new List<int>();
            for (int i = 1; i <= m_count; i++) values.Add(i);
            m_list.Sync(values);
        }
        private void Confirm()
        {
            if (m_key.EndsWith("Confirm", StringComparison.Ordinal) || !UI.TryGet(m_key, out var parent)) return;
            var key = m_key == "CameraPanel" ? "CameraConfirm" :
                m_key == "WorldPanel" || m_key == "GlobalWorld" ? "WorldConfirm" : "Confirm";
            UI.OpenChildAsync(parent, key, key).Forget();
        }
        private void Close() { if (UI.TryGet(m_key, out var panel)) UI.Close(panel); }
        public override void OnClose()
        {
            View.AddItem.onClick.RemoveListener(Add);
            View.OpenConfirm.onClick.RemoveListener(Confirm);
            View.ClosePanel.onClick.RemoveListener(Close);
            m_list.Clear();
        }
        public override void OnDispose() { m_list?.Dispose(); }
    }
}
