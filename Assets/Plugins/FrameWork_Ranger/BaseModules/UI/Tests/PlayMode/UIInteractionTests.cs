using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UIInteractionTests
    {
        [UnityTest]
        public IEnumerator GlobalModal_BlocksScene_RestoresSelectionAndBusinessDisabledState()
            => UniTask.ToCoroutine(async () =>
        {
            var module = ScriptableObject.CreateInstance<GlobalUIModule>();
            var prefab = new GameObject("Prefab", typeof(RectTransform), typeof(UIView), typeof(Image), typeof(UIButton));
            prefab.SetActive(false);
            var eventsObject = new GameObject("Events", typeof(EventSystem));
            var events = eventsObject.GetComponent<EventSystem>();
            foreach (var config in new[] { module.GlobalConfiguration, module.SceneConfiguration })
            {
                config.Domains.Add(new UIDomainDefinition { SortingOrder = config == module.SceneConfiguration ? -10 : 0 });
                config.Panels.Add(new UIPanelDefinition { Key = "HUD", PrefabAddress = "Fake/HUD" });
                config.Panels.Add(new UIPanelDefinition { Key = "Modal", Layer = 10, PrefabAddress = "Fake/Modal", Modal = UIModalScope.Global });
                config.Panels.Add(new UIPanelDefinition { Key = "Child", Layer = 20, PrefabAddress = "Fake/Child", Modal = UIModalScope.Domain });
            }
            module.Initialize((key, token) => UniTask.FromResult(new UIPrefabLease(prefab, () => { })));
            try
            {
                await module.OnSceneScopeStartingAsync(new FrameworkSceneScopeInfo(1, "Test", 1), default);
                var hud = await module.Scene.OpenAsync("HUD");
                var modal = await module.Global.OpenAsync("Modal");
                Assert.That(hud.Record.Gate.interactable, Is.False);
                var button = modal.View.GetComponent<UIButton>();
                events.SetSelectedGameObject(button.gameObject);
                var child = await module.Global.OpenChildAsync(modal, "Child");
                Assert.That(events.currentSelectedGameObject, Is.Null);
                Assert.That(modal.Record.Gate.interactable, Is.False);
                Assert.That(child.Record.Gate.interactable, Is.True);
                module.Global.Close(child);
                Assert.That(events.currentSelectedGameObject, Is.SameAs(button.gameObject));
                hud.View.GetComponent<UIButton>().interactable = false;
                module.Global.Close(modal);
                Assert.That(hud.Record.Gate.interactable, Is.True);
                Assert.That(hud.View.GetComponent<UIButton>().interactable, Is.False);
            }
            finally
            {
                await module.ShutdownAsync();
                UnityEngine.Object.Destroy(module); UnityEngine.Object.Destroy(prefab); UnityEngine.Object.Destroy(eventsObject);
            }
        });
        [UnityTest]
        public IEnumerator LongPressSuppressesClick_AndInputGateCancelsPress() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Button", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(UIButton));
            var eventsObject = new GameObject("Events", typeof(EventSystem));
            try
            {
                var button = root.GetComponent<UIButton>(); button.LongPressSeconds = 0.02f;
                int clicks = 0, longs = 0;
                button.onClick.AddListener(() => clicks++); button.OnLongPress.AddListener(() => longs++);
                var pointer = new PointerEventData(eventsObject.GetComponent<EventSystem>()) { pointerId = 1, button = PointerEventData.InputButton.Left };
                button.OnPointerDown(pointer); await UniTask.Delay(50, ignoreTimeScale: true);
                button.OnPointerUp(pointer); button.OnPointerClick(pointer);
                Assert.That(longs, Is.EqualTo(1)); Assert.That(clicks, Is.Zero);
                button.OnPointerDown(pointer); root.GetComponent<CanvasGroup>().interactable = false;
                await UniTask.NextFrame(); button.OnPointerUp(pointer); button.OnPointerClick(pointer);
                Assert.That(longs, Is.EqualTo(1)); Assert.That(clicks, Is.Zero);
                root.GetComponent<CanvasGroup>().interactable = true;
                button.OnSubmit(new BaseEventData(eventsObject.GetComponent<EventSystem>()));
                Assert.That(clicks, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(eventsObject); }
        });
        [UnityTest]
        public IEnumerator SelectionGroup_QuietRefreshAndNoDuplicateNotification() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Selection"); root.SetActive(false);
            var first = new GameObject("One", typeof(RectTransform), typeof(Toggle)); first.transform.SetParent(root.transform);
            var second = new GameObject("Two", typeof(RectTransform), typeof(Toggle)); second.transform.SetParent(root.transform);
            try
            {
                var group = root.AddComponent<UISelectionGroup>();
                group.Items = new List<Toggle> { first.GetComponent<Toggle>(), second.GetComponent<Toggle>() };
                int notifications = 0; group.OnSelectionChanged.AddListener(index => notifications++);
                root.SetActive(true);
                group.SetSelected(1, false); Assert.That(notifications, Is.Zero);
                group.SetSelected(0); group.SetSelected(0); Assert.That(notifications, Is.EqualTo(1));
                first.GetComponent<Toggle>().isOn = false;
                Assert.That(first.GetComponent<Toggle>().isOn, Is.True);
                await UniTask.NextFrame();
            }
            finally { UnityEngine.Object.Destroy(root); }
        });
    }
}
