using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace FrameWork_Ranger.UI.Tests
{
    [Serializable]
    public sealed class CountingUILogic : UILogic
    {
        public static int Opened, Refreshed, Closed, Disposed;
        public override void OnOpen(object data) { Opened++; }
        public override void OnRefresh(object data) { Refreshed++; }
        public override void OnClose() { Closed++; }
        public override void OnDispose() { Disposed++; }
    }
    public sealed class UIRuntimeTests
    {
        private GlobalUIModule m_module;
        private GameObject m_prefab;
        private int m_loads, m_releases;
        private UniTaskCompletionSource<UIPrefabLease> m_gate;
        private readonly List<GameObject> m_objects = new List<GameObject>();
        [SetUp]
        public void Setup()
        {
            m_loads = m_releases = 0; m_gate = null;
            CountingUILogic.Opened = CountingUILogic.Refreshed = CountingUILogic.Closed = CountingUILogic.Disposed = 0;
            m_module = ScriptableObject.CreateInstance<GlobalUIModule>();
            m_prefab = new GameObject("Prefab", typeof(RectTransform), typeof(UIView));
            m_prefab.SetActive(false);
            Configure(m_module.GlobalConfiguration);
            Configure(m_module.SceneConfiguration);
            m_module.SceneConfiguration.Domains[0].SortingOrder = -10;
            m_module.Initialize((key, token) =>
            {
                m_loads++;
                return m_gate != null ? m_gate.Task.AttachExternalCancellation(token) :
                    UniTask.FromResult(new UIPrefabLease(m_prefab, () => m_releases++));
            });
        }
        private static void Configure(UIConfiguration config)
        {
            config.Domains.Add(new UIDomainDefinition { Key = "Main" });
            config.Domains.Add(new UIDomainDefinition { Key = "Camera", RenderMode = RenderMode.ScreenSpaceCamera });
            config.Domains.Add(new UIDomainDefinition { Key = "World", RenderMode = RenderMode.WorldSpace });
            config.Panels.Add(Panel("Inventory", group: "Window"));
            config.Panels.Add(Panel("Shop", group: "Window"));
            config.Panels.Add(Panel("HUD"));
            config.Panels.Add(Panel("Confirm", modal: UIModalScope.Domain));
            config.Panels.Add(Panel("CameraPanel", "Camera"));
            config.Panels.Add(Panel("WorldPanel", "World"));
        }
        private static UIPanelDefinition Panel(string key, string domain = "Main", string group = null,
            UIModalScope modal = UIModalScope.None) => new UIPanelDefinition
            { Key = key, DomainKey = domain, PrefabAddress = "Fake/" + key, MutexGroup = group,
                Modal = modal, Logic = new CountingUILogic() };
        [UnityTearDown]
        public IEnumerator Teardown() => UniTask.ToCoroutine(async () =>
        {
            await m_module.ShutdownAsync();
            UnityEngine.Object.Destroy(m_module); UnityEngine.Object.Destroy(m_prefab);
            foreach (var obj in m_objects) if (obj) UnityEngine.Object.Destroy(obj);
            m_objects.Clear();
            await UniTask.NextFrame();
        });
        [UnityTest]
        public IEnumerator DuplicateOpen_Refreshes_CacheReopenInvalidatesOldHandle() => UniTask.ToCoroutine(async () =>
        {
            var first = await m_module.Global.OpenAsync("Inventory");
            var repeat = await m_module.Global.OpenAsync("Inventory", "equipment");
            Assert.That(repeat.View, Is.SameAs(first.View));
            Assert.That(CountingUILogic.Opened, Is.EqualTo(1));
            Assert.That(CountingUILogic.Refreshed, Is.EqualTo(1));
            m_module.Global.Close(first);
            Assert.That(m_releases, Is.Zero);
            var reopened = await m_module.Global.OpenAsync("Inventory");
            m_module.Global.Close(first);
            Assert.That(reopened.IsOpen, Is.True);
            Assert.That(m_loads, Is.EqualTo(1));
        });
        [UnityTest]
        public IEnumerator ConcurrentOpen_OneCancellationDoesNotCancelOtherWaiter() => UniTask.ToCoroutine(async () =>
        {
            m_gate = new UniTaskCompletionSource<UIPrefabLease>();
            using (var cancel = new CancellationTokenSource())
            {
                var canceled = m_module.Global.OpenAsync("Inventory", null, cancel.Token);
                var live = m_module.Global.OpenAsync("Inventory");
                cancel.Cancel();
                try { await canceled; Assert.Fail("应取消"); } catch (OperationCanceledException) { }
                m_gate.TrySetResult(new UIPrefabLease(m_prefab, () => m_releases++));
                var panel = await live;
                Assert.That(panel.IsOpen, Is.True);
                Assert.That(m_loads, Is.EqualTo(1));
                Assert.That(CountingUILogic.Opened, Is.EqualTo(1));
            }
        });
        [UnityTest]
        public IEnumerator ParentClose_OnlyClosesItsChildren_AndMutexClosesOldWindow() => UniTask.ToCoroutine(async () =>
        {
            var hud = await m_module.Global.OpenAsync("HUD");
            var inventory = await m_module.Global.OpenAsync("Inventory");
            var child = await m_module.Global.OpenChildAsync(inventory, "Confirm");
            Assert.That(inventory.Record.Gate.interactable, Is.False);
            Assert.That(child.Record.Gate.interactable, Is.True);
            var shop = await m_module.Global.OpenAsync("Shop");
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(child.IsOpen, Is.False);
            Assert.That(hud.IsOpen && shop.IsOpen, Is.True);
        });
        [UnityTest]
        public IEnumerator MissingBinding_FailsBeforeLoading_ExistingGlobalResumesWithoutOpen() => UniTask.ToCoroutine(async () =>
        {
            Assert.Throws<InvalidOperationException>(() => m_module.Global.OpenAsync("CameraPanel"));
            Assert.That(m_loads, Is.Zero);
            var obj = new GameObject("CameraBinding", typeof(Camera));
            m_objects.Add(obj);
            var binding = obj.AddComponent<UIDomainBinding>();
            binding.Owner = UIOwner.Global; binding.Persistent = true; binding.DomainKey = "Camera";
            binding.Camera = obj.GetComponent<Camera>();
            var panel = await m_module.Global.OpenAsync("CameraPanel");
            Assert.That(panel.IsPresented, Is.True);
            binding.enabled = false; m_module.Pulse();
            Assert.That(panel.IsOpen, Is.True); Assert.That(panel.IsPresented, Is.False);
            await m_module.Global.OpenAsync("CameraPanel", "new data");
            binding.enabled = true; m_module.Pulse();
            Assert.That(panel.IsPresented, Is.True);
            Assert.That(CountingUILogic.Opened, Is.EqualTo(1));
            Assert.That(CountingUILogic.Refreshed, Is.EqualTo(1));
        });
        [UnityTest]
        public IEnumerator BindingLostDuringLoad_FailsAndReleasesPreparedLease() => UniTask.ToCoroutine(async () =>
        {
            var obj = new GameObject("CameraBinding", typeof(Camera));
            m_objects.Add(obj);
            var binding = obj.AddComponent<UIDomainBinding>();
            binding.Persistent = true; binding.DomainKey = "Camera"; binding.Camera = obj.GetComponent<Camera>();
            m_gate = new UniTaskCompletionSource<UIPrefabLease>();
            var pending = m_module.Global.OpenAsync("CameraPanel");
            binding.enabled = false;
            m_gate.TrySetResult(new UIPrefabLease(m_prefab, () => m_releases++));
            try { await pending; Assert.Fail("应报告绑定丢失"); } catch (InvalidOperationException) { }
            await UniTask.NextFrame();
            Assert.That(CountingUILogic.Opened, Is.Zero);
            Assert.That(m_releases, Is.EqualTo(1));
        });
        private static FrameworkSceneScopeInfo Scope(long generation)
        {
            // 只在测试构造 Runtime 所拥有的只读身份；生产业务不能伪造 Scope。
            return (FrameworkSceneScopeInfo)Activator.CreateInstance(typeof(FrameworkSceneScopeInfo),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { 1UL, "TestScene", generation }, null);
        }
        [UnityTest]
        public IEnumerator SceneEnd_CleansCacheAndRejectsOldContext_GlobalSurvives() => UniTask.ToCoroutine(async () =>
        {
            var first = Scope(1);
            await m_module.OnSceneScopeStartingAsync(first, default);
            var old = m_module.Scene;
            var scenePanel = await old.OpenAsync("HUD");
            var globalPanel = await m_module.Global.OpenAsync("HUD");
            old.Close(scenePanel);
            await m_module.OnSceneScopeEndingAsync(first);
            Assert.That(m_releases, Is.EqualTo(1));
            Assert.That(globalPanel.IsOpen, Is.True);
            await m_module.OnSceneScopeStartingAsync(Scope(2), default);
            Assert.Throws<InvalidOperationException>(() => old.OpenAsync("HUD"));
            Assert.That(m_module.Scene, Is.Not.SameAs(old));
            await m_module.OnSceneScopeEndingAsync(first);
            Assert.That(m_module.Scene.IsAlive, Is.True);
        });
        [UnityTest]
        public IEnumerator DestroyOnClose_ReleasesOnlyAfterInstanceDestroyed() => UniTask.ToCoroutine(async () =>
        {
            m_module.GlobalConfiguration.Panels.Find(p => p.Key == "HUD").ClosePolicy = UIClosePolicy.DestroyOnClose;
            var panel = await m_module.Global.OpenAsync("HUD");
            var view = panel.View;
            m_module.Global.Close(panel);
            Assert.That(m_releases, Is.Zero);
            await UniTask.NextFrame();
            Assert.That(view == null, Is.True);
            Assert.That(m_releases, Is.EqualTo(1));
        });
    }
}
