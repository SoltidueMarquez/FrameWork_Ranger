using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [AddComponentMenu("Ranger UI/手风琴折叠组")]
    [FrameworkArchitecture("手风琴组", "协调登记折叠项，可切换单展开与同时展开。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIAccordionGroup : MonoBehaviour
    {
        [LabelText("允许同时展开")] public bool AllowMultiple;
        private readonly List<UIFoldout> m_items = new List<UIFoldout>();
        internal void Register(UIFoldout item) { if (!m_items.Contains(item)) m_items.Add(item); }
        internal void Unregister(UIFoldout item) => m_items.Remove(item);
        internal void Expanded(UIFoldout current)
        {
            if (AllowMultiple) return;
            foreach (var item in m_items.ToArray()) if (item && item != current) item.SetExpanded(false);
        }
    }
}
