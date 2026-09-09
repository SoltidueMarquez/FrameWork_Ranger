using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Events.Samples;
using FrameWork_Ranger.Pooling.Reference;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.Events.Tests
{
    internal sealed class EventModuleLifecycleTests
    {
        private readonly List<UnityEngine.Object> m_created = new List<UnityEngine.Object>();
        private FrameworkProjectSettings m_settings;
        private EventModule m_template;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            FrameworkBootstrap.ResetForTests();
            ProbeEvent.Reset();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return Framework.ShutdownAsync().ToCoroutine();
            yield return null;
            FrameworkBootstrap.ResetForTests();
            for (var i = m_created.Count - 1; i >= 0; i--)
                if (m_created[i] != null) UnityEngine.Object.DestroyImmediate(m_created[i]);
            m_created.Clear();
            ProbeEvent.Reset();
        }

        [UnityTest]
        public IEnumerator StaticApi_RejectsEarlyCalls_AndShutdownCleanupDoesNotCreateCenter()
        {
            Assert.Throws<InvalidOperationException>(() => LevelChangedEvent.Subscribe(_ => { }));
            Assert.Throws<InvalidOperationException>(() => LevelChangedEvent.Throw("hero", 1, 2));
            CreateSettings();
            yield return Activate(1);
            var module = Framework.GetModule<EventModule>();
            var calls = 0;
            Action<LevelChangedEvent> listener = payload => { calls++; Assert.That(payload.CurrentLevel, Is.EqualTo(2)); };
            LevelChangedEvent.Subscribe(listener);
            LevelChangedEvent.Subscribe(listener);
            LevelChangedEvent.Throw("hero", 1, 2);
            Assert.That(calls, Is.EqualTo(1));
            LevelChangedEvent.Unsubscribe(listener);
            LevelChangedEvent.Unsubscribe(listener);
            LevelChangedEvent.Throw("hero", 1, 2);
            Assert.That(calls, Is.EqualTo(1));
            Assert.Throws<ArgumentNullException>(() => module.Subscribe<ProbeEvent>(null));
            Assert.DoesNotThrow(() => module.Unsubscribe<ProbeEvent>(null));
            yield return Framework.ShutdownAsync().ToCoroutine();
            Assert.DoesNotThrow(() => module.Unsubscribe(listener));
            Assert.DoesNotThrow(() => LevelChangedEvent.Unsubscribe(listener));
            Assert.That(Framework.TryGetModule<EventModule>(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => module.Publish<ProbeEvent>());
        }

        [UnityTest]
        public IEnumerator NestedPayloads_AreDistinct_AndReturnBeforeOuterContinuation()
        {
            CreateSettings();
            yield return Activate(2);
            var module = Framework.GetModule<EventModule>();
            ProbeEvent outer = null;
            ProbeEvent inner = null;
            var calls = new List<string>();
            module.Subscribe<ProbeEvent>(payload =>
            {
                if (payload.Value == 1)
                {
                    outer = payload;
                    calls.Add("outer A start");
                    module.Publish<ProbeEvent>(nested => nested.Value = 2);
                    Assert.That(inner, Is.Not.SameAs(outer));
                    Assert.That(inner.Value, Is.Zero);
                    Assert.That(outer.Value, Is.EqualTo(1));
                    calls.Add("outer A end");
                }
                else { inner = payload; calls.Add("inner A"); }
            });
            module.Subscribe<ProbeEvent>(payload => calls.Add(payload.Value == 1 ? "outer B" : "inner B"));
            module.Publish<ProbeEvent>(payload => payload.Value = 1);
            CollectionAssert.AreEqual(new[] { "outer A start", "inner A", "inner B", "outer A end", "outer B" }, calls);
            Assert.That(outer.Value, Is.Zero);
            Assert.That(ProbeEvent.Returns, Is.EqualTo(2));
            Assert.That(ProbeEvent.Rents, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator InitializationFailure_ReturnsPayload_AndNoListenerPublishStillInitializes()
        {
            CreateSettings();
            yield return Activate(3);
            var module = Framework.GetModule<EventModule>();
            ProbeEvent first = null;
            var failure = new ApplicationException("initialize boom");
            var observed = Assert.Throws<ApplicationException>(() => module.Publish<ProbeEvent>(payload =>
            {
                first = payload;
                payload.Value = 9;
                throw failure;
            }));
            Assert.That(observed, Is.SameAs(failure));
            Assert.That(first.Value, Is.Zero);
            Assert.That(ProbeEvent.Returns, Is.EqualTo(1));
            var initialized = false;
            module.Publish<ProbeEvent>(payload => { initialized = true; Assert.That(payload, Is.SameAs(first)); });
            Assert.That(initialized, Is.True);
            Assert.That(ProbeEvent.Returns, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ListenerFailure_ContinuesAndReturns_WhileCleanupFailurePropagatesOnce()
        {
            CreateSettings();
            yield return Activate(4);
            var module = Framework.GetModule<EventModule>();
            var calls = 0;
            Action<ProbeEvent> failing = _ => throw new ApplicationException("listener boom");
            module.Subscribe(failing);
            module.Subscribe<ProbeEvent>(_ => calls++);
            LogAssert.Expect(LogType.Exception, new Regex("listener boom"));
            Assert.DoesNotThrow(() => module.Publish<ProbeEvent>());
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(ProbeEvent.Returns, Is.EqualTo(1));
            module.Unsubscribe(failing);
            ProbeEvent.FailReturn = true;
            Assert.Throws<ApplicationException>(() => module.Publish<ProbeEvent>());
            Assert.That(ProbeEvent.Returns, Is.EqualTo(2));
            var aggregate = Assert.Throws<AggregateException>(() => module.Publish<ProbeEvent>(_ =>
                throw new InvalidOperationException("initialize failure")));
            Assert.That(aggregate.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(aggregate.InnerExceptions[0].Message, Does.Contain("initialize failure"));
            Assert.That(aggregate.InnerExceptions[1].Message, Does.Contain("return failure"));
            Assert.That(ProbeEvent.Returns, Is.EqualTo(3));
            ProbeEvent.FailReturn = false;
            yield return Framework.ShutdownAsync().ToCoroutine();
            Assert.That(ProbeEvent.Returns, Is.EqualTo(3), "失败载荷应已从池中移除，关停不再次归还");
        }

        [UnityTest]
        public IEnumerator RentFailure_DoesNotAttemptReturn_AndNextPublishStillWorks()
        {
            CreateSettings();
            yield return Activate(5);
            var module = Framework.GetModule<EventModule>();
            ProbeEvent.FailRent = true;
            Assert.Throws<ApplicationException>(() => module.Publish<ProbeEvent>());
            Assert.That(ProbeEvent.Returns, Is.Zero);
            ProbeEvent.FailRent = false;
            module.Publish<ProbeEvent>();
            Assert.That(ProbeEvent.Returns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WrongThread_RejectsSubscribePublishAndUnsubscribe()
        {
            CreateSettings();
            yield return Activate(6);
            var module = Framework.GetModule<EventModule>();
            var errors = new List<Exception>();
            Action<ProbeEvent> callback = _ => { };
            var task = Task.Run(() =>
            {
                foreach (var action in new Action[]
                {
                    () => module.Subscribe(callback), () => module.Publish<ProbeEvent>(), () => module.Unsubscribe(callback)
                })
                {
                    try { action(); } catch (Exception exception) { errors.Add(exception); }
                }
            });
            while (!task.IsCompleted) yield return null;
            Assert.That(errors.Count, Is.EqualTo(3));
            foreach (var error in errors) Assert.That(error.Message, Does.Contain("Unity 主线程"));
            Assert.That(ProbeEvent.Rents, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SceneReplacement_KeepsGlobalSubscriptions_AndTemplateHasNoRuntime()
        {
            CreateSettings();
            yield return Activate(7);
            var module = Framework.GetModule<EventModule>();
            Assert.That(module, Is.Not.SameAs(m_template));
            Assert.That(m_template.State, Is.EqualTo(ModuleLifecycleState.None));
            Assert.Throws<InvalidOperationException>(() => m_template.Subscribe<ProbeEvent>(_ => { }));
            var calls = 0;
            module.Subscribe<ProbeEvent>(_ => calls++);
            yield return Activate(8);
            Assert.That(Framework.GetModule<EventModule>(), Is.SameAs(module));
            module.Publish<ProbeEvent>();
            Assert.That(calls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SceneInstallation_IsRejected()
        {
            CreateSettings(eventInScene: true);
            LogAssert.Expect(LogType.Error, new Regex("SceneScope 加载失败"));
            Exception observed = null;
            yield return UniTask.ToCoroutine(async () =>
            {
                try { await Framework.StartProjectSceneAsync(m_settings, new FrameworkSceneDescriptor(9, "Tests/Event.unity", "Event")); }
                catch (Exception exception) { observed = exception; }
            });
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.ToString(), Does.Contain("只能安装在 GlobalScope"));
            Assert.That(Framework.TryGetModule<EventModule>(out _), Is.False);
        }

        [UnityTest]
        public IEnumerator ShutdownInsideNestedCallback_WaitsForBothPayloads_AndSkipsRemainingListeners()
        {
            CreateSettings();
            yield return Activate(10);
            var module = Framework.GetModule<EventModule>();
            var pool = Framework.GetModule<ReferencePoolModule>();
            var shutdown = UniTask.CompletedTask;
            var lateCalls = 0;
            module.Subscribe<ProbeEvent>(payload =>
            {
                if (payload.Value == 1)
                {
                    module.Publish<ProbeEvent>(inner => inner.Value = 2);
                    Assert.That(payload.Value, Is.EqualTo(1));
                    Assert.That(ProbeEvent.Returns, Is.EqualTo(1));
                }
                else
                {
                    shutdown = Framework.ShutdownAsync();
                    Assert.That(shutdown.Status, Is.EqualTo(UniTaskStatus.Pending));
                    Assert.That(module.State, Is.EqualTo(ModuleLifecycleState.Unloading));
                    Assert.That(pool.State, Is.EqualTo(ModuleLifecycleState.Loaded));
                    Assert.That(payload.Value, Is.EqualTo(2));
                    Assert.That(ProbeEvent.Returns, Is.Zero);
                    Assert.Throws<InvalidOperationException>(() => module.Publish<ProbeEvent>());
                    Assert.Throws<InvalidOperationException>(() => module.Subscribe<ProbeEvent>(_ => { }));
                }
            });
            module.Subscribe<ProbeEvent>(_ => lateCalls++);
            module.Publish<ProbeEvent>(payload => payload.Value = 1);
            yield return shutdown.ToCoroutine();
            Assert.That(ProbeEvent.Returns, Is.EqualTo(2));
            Assert.That(lateCalls, Is.Zero);
            Assert.That(Framework.State, Is.EqualTo(FrameworkState.Shutdown));
        }

        [UnityTest]
        public IEnumerator ShutdownInsideInitialization_StillReturnsWithoutDispatch()
        {
            CreateSettings();
            yield return Activate(11);
            var module = Framework.GetModule<EventModule>();
            var calls = 0;
            module.Subscribe<ProbeEvent>(_ => calls++);
            var shutdown = UniTask.CompletedTask;
            module.Publish<ProbeEvent>(payload =>
            {
                payload.Value = 3;
                shutdown = Framework.ShutdownAsync();
                Assert.That(payload.Value, Is.EqualTo(3));
                Assert.That(ProbeEvent.Returns, Is.Zero);
            });
            yield return shutdown.ToCoroutine();
            Assert.That(calls, Is.Zero);
            Assert.That(ProbeEvent.Returns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SampleView_DisabledWhileWaiting_DoesNotSubscribe_AndReenableWorks()
        {
            var go = Track(new GameObject("Event sample"));
            var view = go.AddComponent<EventCenterSampleView>();
            go.SetActive(false);
            CreateSettings();
            yield return Activate(12);
            LevelChangedEvent.Throw("hero", 1, 2);
            ResourcesReloadedEvent.Throw();
            Assert.That(view.IsListening, Is.False);
            Assert.That(view.ReceivedLevelChanges, Is.Zero);
            go.SetActive(true);
            yield return null;
            Assert.That(view.IsListening, Is.True);
            view.SendLevelChange();
            view.SendReload();
            Assert.That(view.ReceivedLevelChanges, Is.EqualTo(1));
            Assert.That(view.ReceivedReloads, Is.EqualTo(1));
            go.SetActive(false);
            LevelChangedEvent.Throw("hero", 2, 3);
            ResourcesReloadedEvent.Throw();
            Assert.That(view.ReceivedLevelChanges, Is.EqualTo(1));
            Assert.That(view.ReceivedReloads, Is.EqualTo(1));
        }

        private void CreateSettings(bool eventInScene = false)
        {
            var reference = Track(ScriptableObject.CreateInstance<ReferencePoolModule>());
            reference.SetHandler(new ReferencePoolHandler());
            m_template = Track(ScriptableObject.CreateInstance<EventModule>());
            var global = Track(ScriptableObject.CreateInstance<FrameworkGlobalConfig>());
            // 故意把 Event 放在依赖前，验证加载顺序来自依赖声明。
            global.SetModules(eventInScene
                ? new[] { new ModuleConfigEntry(true, reference) }
                : new[] { new ModuleConfigEntry(true, m_template), new ModuleConfigEntry(true, reference) });
            var scene = Track(ScriptableObject.CreateInstance<FrameworkSceneConfig>());
            if (eventInScene) scene.SetModules(new[] { new ModuleConfigEntry(true, m_template) });
            m_settings = Track(ScriptableObject.CreateInstance<FrameworkProjectSettings>());
            m_settings.SetGlobalConfig(global);
            m_settings.SetDefaultSceneConfig(scene);
        }

        private IEnumerator Activate(ulong handle)
        {
            yield return Framework.StartProjectSceneAsync(m_settings,
                new FrameworkSceneDescriptor(handle, "Tests/Event.unity", "Event")).ToCoroutine();
            Assert.That(Framework.IsReady, Is.True, Framework.LastException?.ToString());
        }

        private T Track<T>(T value) where T : UnityEngine.Object { m_created.Add(value); return value; }

        public sealed class ProbeEvent : EventBase
        {
            internal static int Rents;
            internal static int Returns;
            internal static bool FailRent;
            internal static bool FailReturn;
            internal int Value;
            public override void OnRent()
            {
                Rents++;
                if (FailRent) throw new ApplicationException("rent failure");
            }
            public override void OnReturn()
            {
                Returns++;
                Value = 0;
                if (FailReturn) throw new ApplicationException("return failure");
            }
            internal static void Reset() { Rents = Returns = 0; FailRent = FailReturn = false; }
        }
    }
}
