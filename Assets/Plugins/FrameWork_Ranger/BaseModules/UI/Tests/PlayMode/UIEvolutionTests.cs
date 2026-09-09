using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Tests
{
    [Serializable] public sealed class SelfClosingUILogic : UILogic
    { public override void OnOpen(object data) => UI.Close("Self"); }
    public sealed class UIEvolutionTests
    {
        private GlobalUIModule m_module;
        private UIResourcesConfig m_resources;
        private GameObject m_prefab;
        private readonly List<UnityEngine.Object> m_owned = new List<UnityEngine.Object>();
        private UIPanelConfig Panel(string key, UIResourceDirectory dir, UIModalScope modal = UIModalScope.None)
        {
            var panel = ScriptableObject.CreateInstance<UIPanelConfig>(); m_owned.Add(panel);
            panel.Definition = new UIPanelDefinition { Key = key, UseDirectPrefab = true, DirectPrefab = m_prefab, Modal = modal, Logic = new CountingUILogic() };
            dir.Panels.Add(new UIPanelEntry { Panel = panel }); return panel;
        }
        [SetUp] public void Setup()
        {
            CountingUILogic.Opened = CountingUILogic.Closed = CountingUILogic.Refreshed = 0;
            m_module = ScriptableObject.CreateInstance<GlobalUIModule>();
            m_resources = ScriptableObject.CreateInstance<UIResourcesConfig>();
            m_resources.Global.Domains.Add(new UIDomainDefinition { SortingOrder = 100 });
            m_resources.Scene.Domains.Add(new UIDomainDefinition());
            m_module.UIResources = m_resources;
            m_prefab = new GameObject("Panel template", typeof(RectTransform), typeof(UIView)); m_prefab.SetActive(false);
        }
        private void Initialize() => m_module.Initialize((key, token) => throw new InvalidOperationException("Direct Prefab must not invoke address loader"));
        [UnityTearDown] public IEnumerator Cleanup() => UniTask.ToCoroutine(async () =>
        {
            await m_module.ShutdownAsync();
            foreach (var obj in m_owned) if (obj) UnityEngine.Object.Destroy(obj); m_owned.Clear();
            UnityEngine.Object.Destroy(m_module); UnityEngine.Object.Destroy(m_resources); UnityEngine.Object.Destroy(m_prefab);
            Time.timeScale = 1; await UniTask.NextFrame();
        });
        [UnityTest] public IEnumerator SceneRules_AutoOpenOnce_AndRuntimeSnapshotIsIndependent() => UniTask.ToCoroutine(async () =>
        {
            var panel = Panel("HUD", m_resources.Scene);
            var entry = m_resources.Scene.Panels[0]; entry.AllScenes = false; entry.AutoOpen = true;
            entry.Scenes.Add(new UISceneReference { Path = "Assets/A.unity", Guid = "test" });
            Initialize(); panel.Definition.Key = "EditedTemplate"; entry.AllScenes = true;
            var a = new FrameworkSceneScopeInfo(1, "Assets/A.unity", 1);
            await m_module.OnSceneScopeStartingAsync(a, default);
            var already = await m_module.Scene.OpenAsync("HUD");
            await m_module.OnSceneScopeReadyAsync(a, default);
            Assert.That(CountingUILogic.Opened, Is.EqualTo(1)); Assert.That(CountingUILogic.Refreshed, Is.Zero);
            var old = m_module.Scene; await m_module.OnSceneScopeEndingAsync(a);
            Assert.That(already.IsOpen, Is.False);
            var b = new FrameworkSceneScopeInfo(2, "Assets/B.unity", 2);
            await m_module.OnSceneScopeStartingAsync(b, default); await m_module.OnSceneScopeReadyAsync(b, default);
            Assert.Throws<KeyNotFoundException>(() => m_module.Scene.OpenAsync("HUD"));
            Assert.Throws<InvalidOperationException>(() => old.OpenAsync("HUD"));
            Assert.That(panel.Definition.Key, Is.EqualTo("EditedTemplate"));
        });
        [UnityTest] public IEnumerator AutoOpen_OptionalFailureContinues_RequiredFailureSurfaces_GlobalSurvives() => UniTask.ToCoroutine(async () =>
        {
            Panel("Global", m_resources.Global);
            m_resources.Scene.Domains.Add(new UIDomainDefinition { Key = "Camera", RenderMode = RenderMode.ScreenSpaceCamera });
            Panel("Optional", m_resources.Scene).Definition.DomainKey = "Camera";
            Panel("HUD", m_resources.Scene);
            Panel("Required", m_resources.Scene).Definition.DomainKey = "Camera";
            foreach (var entry in m_resources.Scene.Panels) entry.AutoOpen = true;
            m_resources.Scene.Panels[2].Required = true; Initialize();
            var global = await m_module.Global.OpenAsync("Global");
            var scope = new FrameworkSceneScopeInfo(1, "Assets/A.unity", 1);
            await m_module.OnSceneScopeStartingAsync(scope, default);
            LogAssert.Expect(LogType.Error, new Regex("自动打开失败（继续场景启动）：Optional"));
            Exception failure = null;
            try { await m_module.OnSceneScopeReadyAsync(scope, default); } catch (Exception ex) { failure = ex; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("Required")); Assert.That(m_module.Scene.TryGet("HUD", out _), Is.True);
            await m_module.OnSceneScopeEndingAsync(scope); Assert.That(global.IsOpen, Is.True);
        });
        [UnityTest] public IEnumerator ModalAnimation_UsesUnscaledTime_HoldsGateUntilExit_QueuesReopen() => UniTask.ToCoroutine(async () =>
        {
            Panel("HUD", m_resources.Global);
            var modal = Panel("Modal", m_resources.Global, UIModalScope.Global);
            modal.Definition.Layer = 10;
            modal.Definition.EnterAnimation = new UIPanelAnimation { Fade = true, Slide = true, Duration = 0.08f };
            modal.Definition.ExitAnimation = new UIPanelAnimation { Fade = true, Duration = 0.08f };
            Initialize(); var hud = await m_module.Global.OpenAsync("HUD"); Time.timeScale = 0;
            var opening = m_module.Global.OpenAsync("Modal");
            Assert.That(m_module.Global.TryGet("Modal", out var entering), Is.True);
            Assert.That(entering.Record.Gate.interactable, Is.False); Assert.That(hud.Record.Gate.interactable, Is.False);
            var first = await opening; Assert.That(first.Record.Gate.interactable, Is.True);
            var closing = m_module.Global.CloseAsync(first);
            Assert.That(first.IsOpen, Is.False); Assert.That(hud.Record.Gate.interactable, Is.False);
            var reopening = m_module.Global.OpenAsync("Modal"); await closing;
            var second = await reopening;
            Assert.That(second.IsOpen, Is.True); Assert.That(first.IsOpen, Is.False);
            Assert.That(CountingUILogic.Opened, Is.EqualTo(3)); Assert.That(CountingUILogic.Closed, Is.EqualTo(1));
            await m_module.Global.CloseAsync(second); Assert.That(hud.Record.Gate.interactable, Is.True);
        });
        [UnityTest] public IEnumerator ForceUnload_CancelsEnterAndQueuedReopen_WithoutWaitingLongAnimations() => UniTask.ToCoroutine(async () =>
        {
            var panel = Panel("Modal", m_resources.Global, UIModalScope.Domain);
            panel.Definition.EnterAnimation = new UIPanelAnimation { Fade = true, Duration = 30 };
            panel.Definition.ExitAnimation = new UIPanelAnimation { Fade = true, Duration = 30 };
            Initialize(); var context = m_module.Global;
            var entering = context.OpenAsync("Modal"); await UniTask.NextFrame();
            var close = context.CloseAsync("Modal"); var reopening = context.OpenAsync("Modal");
            float start = Time.realtimeSinceStartup; await m_module.ShutdownAsync();
            Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(2));
            try { await entering; Assert.Fail("入场应取消"); } catch (OperationCanceledException) { }
            await close;
            try { await reopening; Assert.Fail("旧上下文应失效"); } catch (InvalidOperationException) { }
            Assert.That(CountingUILogic.Closed, Is.EqualTo(1));
        });
        [UnityTest] public IEnumerator AllWaitersCancelDuringEntry_ImmediateReopenBelongsToNewOperation() => UniTask.ToCoroutine(async () =>
        {
            var panel = Panel("Window", m_resources.Global);
            panel.Definition.EnterAnimation = new UIPanelAnimation { Fade = true, Duration = .12f };
            Initialize();
            using (var canceled = new CancellationTokenSource())
            {
                var old = m_module.Global.OpenAsync("Window", cancellationToken: canceled.Token);
                await UniTask.NextFrame(); canceled.Cancel();
                var next = m_module.Global.OpenAsync("Window");
                try { await old; Assert.Fail("旧打开应取消"); } catch (OperationCanceledException) { }
                var opened = await next;
                Assert.That(opened.IsOpen, Is.True); Assert.That(opened.Record.Gate.interactable, Is.True);
                await UniTask.DelayFrame(2); Assert.That(opened.IsOpen, Is.True);
                Assert.That(CountingUILogic.Opened, Is.EqualTo(2)); Assert.That(CountingUILogic.Closed, Is.EqualTo(1));
            }
        });
        [UnityTest] public IEnumerator LogicCanCloseDuringOnOpen_WithoutStartingCompetingEntryAnimation() => UniTask.ToCoroutine(async () =>
        {
            var panel = Panel("Self", m_resources.Global); panel.Definition.Logic = new SelfClosingUILogic();
            panel.Definition.EnterAnimation = new UIPanelAnimation { Fade = true, Duration = .15f };
            panel.Definition.ExitAnimation = new UIPanelAnimation { Fade = true, Duration = .05f };
            Initialize();
            try { await m_module.Global.OpenAsync("Self"); Assert.Fail("自关闭的打开应取消"); } catch (OperationCanceledException) { }
            await m_module.Global.CloseAsync("Self"); Assert.That(m_module.Global.TryGet("Self", out _), Is.False);
        });
        [UnityTest] public IEnumerator ButtonDoubleClickIsAdditive_CancelSuppressesRightClick_QuietToggleUpdatesVisual() => UniTask.ToCoroutine(async () =>
        {
            var obj = new GameObject("Button", typeof(RectTransform), typeof(UIImage), typeof(UIButton)); m_owned.Add(obj);
            var ev = new GameObject("Events", typeof(EventSystem)); m_owned.Add(ev);
            var button = obj.GetComponent<UIButton>(); button.EnableDoubleClick = true;
            int normal = 0, twice = 0, right = 0;
            button.onClick.AddListener(() => normal++); button.DoubleClicked.AddListener(() => twice++); button.RightClicked.AddListener(() => right++);
            var pointer = new PointerEventData(ev.GetComponent<EventSystem>()) { pointerId = -1 };
            for (int i = 0; i < 2; i++) { button.OnPointerDown(pointer); button.OnPointerUp(pointer); button.OnPointerClick(pointer); }
            Assert.That(normal, Is.EqualTo(2)); Assert.That(twice, Is.EqualTo(1));
            pointer.button = PointerEventData.InputButton.Right; button.OnPointerDown(pointer); button.CancelPress(); button.OnPointerUp(pointer); button.OnPointerClick(pointer);
            Assert.That(right, Is.Zero);
            var toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(UIImage), typeof(UIToggle), typeof(UIControlVisual)); m_owned.Add(toggleObject);
            var toggle = toggleObject.GetComponent<UIToggle>(); toggle.transition = Selectable.Transition.None;
            var visual = toggleObject.GetComponent<UIControlVisual>(); visual.Control = toggle;
            visual.On.Normal.Colors.Add(new UIColorTarget { Target = toggleObject.GetComponent<UIImage>(), Color = Color.green });
            int changes = 0; toggle.onValueChanged.AddListener(_ => changes++); toggle.SetValue(true, false);
            Assert.That(changes, Is.Zero); Assert.That(toggleObject.GetComponent<UIImage>().color, Is.EqualTo(Color.green));
            await UniTask.NextFrame();
        });
        [UnityTest] public IEnumerator FoldoutReenableRetainsState_AndUserDragCancelsSmoothScroll() => UniTask.ToCoroutine(async () =>
        {
            var root = new GameObject("Accordion", typeof(RectTransform), typeof(UIAccordionGroup)); m_owned.Add(root); root.SetActive(false);
            UIFoldout Make(string name)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(UIFoldout)); obj.transform.SetParent(root.transform, false);
                var body = new GameObject("Body", typeof(RectTransform)); body.transform.SetParent(obj.transform, false);
                var fold = obj.GetComponent<UIFoldout>(); fold.Group = root.GetComponent<UIAccordionGroup>(); fold.Content = body; return fold;
            }
            var a = Make("A"); var b = Make("B"); root.SetActive(true);
            a.SetExpanded(true); Assert.That(b.IsExpanded, Is.False);
            a.gameObject.SetActive(false); a.gameObject.SetActive(true); Assert.That(a.IsExpanded, Is.True);
            b.SetExpanded(true); Assert.That(a.Content.activeSelf, Is.False); Assert.That(a.Content, Is.Not.Null);
            var viewport = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect)); m_owned.Add(viewport);
            var rect = (RectTransform)viewport.transform; rect.sizeDelta = new Vector2(200,100);
            var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(rect, false);
            ((RectTransform)content.transform).sizeDelta = new Vector2(200,600);
            var scroll = viewport.GetComponent<ScrollRect>(); scroll.viewport = rect; scroll.content = (RectTransform)content.transform; scroll.horizontal = false;
            var moving = scroll.ScrollToAsync(Vector2.zero, 3);
            await UniTask.NextFrame(); viewport.GetComponent<UIScrollMotion>().OnBeginDrag(null);
            try { await moving; Assert.Fail("用户拖动应取消自动定位"); } catch (OperationCanceledException) { }
        });
        [UnityTest] public IEnumerator WindowDrag_ConvertsCoordinatesAndClampsBounds_InThreeCanvasModes() => UniTask.ToCoroutine(async () =>
        {
            foreach (var mode in new[] { RenderMode.ScreenSpaceOverlay, RenderMode.ScreenSpaceCamera, RenderMode.WorldSpace })
            {
                var root = new GameObject("Drag canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster)); m_owned.Add(root);
                var cameraObject = new GameObject("Drag camera", typeof(Camera)); m_owned.Add(cameraObject); cameraObject.transform.position = new Vector3(0,0,-10);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = mode; canvas.worldCamera = cameraObject.GetComponent<Camera>(); canvas.planeDistance = 2;
                if (mode == RenderMode.WorldSpace) { ((RectTransform)root.transform).sizeDelta = new Vector2(600,400); root.transform.localScale = Vector3.one * .01f; }
                var boundsObject = new GameObject("Bounds", typeof(RectTransform)); boundsObject.transform.SetParent(root.transform, false);
                var bounds = (RectTransform)boundsObject.transform; bounds.sizeDelta = new Vector2(300,200);
                var panelObject = new GameObject("Window", typeof(RectTransform), typeof(UIWindowDrag)); panelObject.transform.SetParent(bounds, false);
                var rect = (RectTransform)panelObject.transform; rect.sizeDelta = new Vector2(80,50);
                var drag = panelObject.GetComponent<UIWindowDrag>(); drag.Target = rect; drag.Bounds = bounds;
                int canceled = 0, ended = 0; drag.DragCanceled.AddListener(() => canceled++); drag.DragEnded.AddListener(() => ended++);
                await UniTask.NextFrame(); Canvas.ForceUpdateCanvases();
                var eventCamera = mode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var pointer = new PointerEventData(null) { pointerId = 42, button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.position),
                    pointerPressRaycast = new RaycastResult { module = root.GetComponent<GraphicRaycaster>(), gameObject = panelObject } };
                drag.OnBeginDrag(pointer); pointer.position += new Vector2(10000,10000); drag.OnDrag(pointer);
                var actual = RectTransformUtility.CalculateRelativeRectTransformBounds(bounds, rect);
                Assert.That(rect.anchoredPosition.sqrMagnitude, Is.GreaterThan(1), mode.ToString());
                Assert.That(actual.max.x, Is.LessThanOrEqualTo(bounds.rect.xMax + .01f)); Assert.That(actual.max.y, Is.LessThanOrEqualTo(bounds.rect.yMax + .01f));
                root.GetComponent<CanvasGroup>().interactable = false; drag.OnDrag(pointer); drag.OnEndDrag(pointer);
                Assert.That(canceled, Is.EqualTo(1)); Assert.That(ended, Is.Zero);
            }
        });
    }
}
