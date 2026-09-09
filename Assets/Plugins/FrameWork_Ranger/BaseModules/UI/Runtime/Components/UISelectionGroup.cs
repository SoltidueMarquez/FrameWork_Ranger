using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>显式 Toggle 列表的单选组；支持静默数据刷新，不接管未登记控件。</summary>
    [AddComponentMenu("Ranger UI/Selection Group")]
    [FrameworkArchitecture("UI 选择组", "维护切换项与静默刷新。", FrameworkArchitectureLayer.PublicFacade, 130)]
    public sealed class UISelectionGroup : MonoBehaviour
    {
        public List<Toggle> Items = new List<Toggle>();
        public bool AllowNone;
        public UnityEvent<int> OnSelectionChanged = new UnityEvent<int>();
        public int SelectedIndex { get; private set; } = -1;
        private readonly List<(Toggle toggle, UnityAction<bool> listener)> m_bindings =
            new List<(Toggle, UnityAction<bool>)>();
        private void OnEnable()
        {
            var chosen = -1;
            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i]) continue;
                int index = i;
                UnityAction<bool> listener = on => Changed(index, on);
                Items[i].onValueChanged.AddListener(listener);
                m_bindings.Add((Items[i], listener));
                if (Items[i].isOn && chosen < 0) chosen = i;
            }
            if (chosen < 0 && !AllowNone) chosen = Items.FindIndex(item => item);
            SetSelected(chosen, false);
        }
        private void OnDisable()
        {
            foreach (var pair in m_bindings) if (pair.toggle) pair.toggle.onValueChanged.RemoveListener(pair.listener);
            m_bindings.Clear();
        }
        private void Changed(int index, bool on)
        {
            if (on) SetSelected(index);
            else if (SelectedIndex == index)
            {
                if (AllowNone) SetSelected(-1);
                else if (Items[index]) Items[index].SetIsOnWithoutNotify(true);
            }
        }
        public void SetSelected(int index, bool notify = true)
        {
            if (index < -1 || index >= Items.Count || (index >= 0 && !Items[index]))
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index == -1 && !AllowNone && Items.Exists(item => item)) return;
            bool changed = index != SelectedIndex;
            SelectedIndex = index;
            for (int i = 0; i < Items.Count; i++) if (Items[i])
            {
                if (Items[i] is UIToggle toggle) toggle.SetValue(i == index, false);
                else Items[i].SetIsOnWithoutNotify(i == index);
            }
            if (notify && changed) OnSelectionChanged.Invoke(index);
        }
    }
}
