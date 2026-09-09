using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    public static class UIInputUtility
    {
        public static bool IsAllowed(Component component)
        {
            if (!component || !component.gameObject.activeInHierarchy) return false;
            if (component is Behaviour behaviour && !behaviour.isActiveAndEnabled) return false;
            var selectable = component.GetComponent<Selectable>();
            if (selectable && !selectable.IsInteractable()) return false;
            foreach (var group in component.GetComponentsInParent<CanvasGroup>(false))
                if (group.enabled && !group.interactable) return false;
            return true;
        }
    }
}
