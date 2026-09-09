using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FrameWork_Ranger.Tests
{
    internal sealed class SceneScopeLifecycleTests
    {
        private sealed class Participant : ISceneScopeLifecycle, ISceneScopeReady
        {
            internal string Name;
            internal List<string> Events;
            internal bool FailStart;
            internal bool FailEnd;
            internal bool TrackReady, FailReady;
            internal FrameworkSceneScopeInfo Seen;
            public UniTask OnSceneScopeStartingAsync(FrameworkSceneScopeInfo scope, CancellationToken token)
            {
                Seen = scope;
                Events.Add(Name + ".Start");
                if (FailStart) throw new InvalidOperationException("start failure");
                return UniTask.CompletedTask;
            }
            public UniTask OnSceneScopeEndingAsync(FrameworkSceneScopeInfo scope)
            {
                Assert.That(scope, Is.SameAs(Seen));
                Events.Add(Name + ".End");
                if (FailEnd) throw new InvalidOperationException("end failure");
                return UniTask.CompletedTask;
            }
            public UniTask OnSceneScopeReadyAsync(FrameworkSceneScopeInfo scope, CancellationToken token)
            {
                Assert.That(scope, Is.SameAs(Seen));
                if (TrackReady) Events.Add(Name + ".Ready");
                if (FailReady) throw new InvalidOperationException("ready failure");
                return UniTask.CompletedTask;
            }
        }
        private sealed class ReadyDriver : FrameworkDriverHandlerBase
        {
            internal List<string> Events;
            protected override UniTask OnAfterScopeLoadAsync(ModuleScopeKind kind, IReadOnlyList<ModuleBase> modules, CancellationToken token)
            { Events.Add("Driver.AfterLoad"); return UniTask.CompletedTask; }
        }
        [UnityTest] public IEnumerator ReadyRunsAfterDriver_FailureStillPairsCleanup() => UniTask.ToCoroutine(async () =>
        {
            var events = new List<string>();
            var a = new Participant { Name = "A", Events = events, TrackReady = true, FailReady = true };
            var b = new Participant { Name = "B", Events = events, TrackReady = true };
            var scope = new ModuleScopeRuntime(new FrameworkRuntime(), ModuleScopeKind.Scene, Array.Empty<ModuleGraphNode>());
            scope.ConfigureSceneLifecycle(new FrameworkSceneScopeInfo(1, "SceneA", 3), new[] { a, b });
            var driver = new ReadyDriver { Events = events };
            Exception failure = null;
            try { await scope.LoadAsync(driver, default); } catch (Exception ex) { failure = ex; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            await scope.UnloadAndDestroyAsync(driver);
            CollectionAssert.AreEqual(new[] { "A.Start", "B.Start", "Driver.AfterLoad", "A.Ready", "B.End", "A.End" }, events);
        });

        [UnityTest]
        public IEnumerator EmptyScene_PairsReverseCleanup_EvenWhenOneEndFails()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var events = new List<string>();
                var a = new Participant { Name = "A", Events = events };
                var b = new Participant { Name = "B", Events = events, FailEnd = true };
                var scope = new ModuleScopeRuntime(new FrameworkRuntime(), ModuleScopeKind.Scene,
                    Array.Empty<ModuleGraphNode>());
                scope.ConfigureSceneLifecycle(new FrameworkSceneScopeInfo(1, "SceneA", 1), new[] { a, b });
                var driver = new DefaultFrameworkDriverHandler();
                await scope.LoadAsync(driver, default);
                var errors = await scope.UnloadAndDestroyAsync(driver);
                Assert.That(errors.Count, Is.EqualTo(1));
                await scope.UnloadAndDestroyAsync(driver);
                CollectionAssert.AreEqual(new[] { "A.Start", "B.Start", "B.End", "A.End" }, events);
            });
        }

        [UnityTest]
        public IEnumerator PartialStartFailure_CleansOnlyEnteredParticipants_Once()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var events = new List<string>();
                var a = new Participant { Name = "A", Events = events };
                var b = new Participant { Name = "B", Events = events, FailStart = true };
                var c = new Participant { Name = "C", Events = events };
                var scope = new ModuleScopeRuntime(new FrameworkRuntime(), ModuleScopeKind.Scene,
                    Array.Empty<ModuleGraphNode>());
                scope.ConfigureSceneLifecycle(new FrameworkSceneScopeInfo(1, "SceneA", 2), new[] { a, b, c });
                var driver = new DefaultFrameworkDriverHandler();
                Exception failure = null;
                try { await scope.LoadAsync(driver, default); }
                catch (Exception ex) { failure = ex; }
                Assert.That(failure, Is.TypeOf<InvalidOperationException>());
                await scope.UnloadAndDestroyAsync(driver);
                CollectionAssert.AreEqual(new[] { "A.Start", "B.Start", "B.End", "A.End" }, events);
            });
        }
    }
}
