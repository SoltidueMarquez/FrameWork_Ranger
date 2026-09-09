using System.Collections;
using System.Threading;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace FrameWork_Ranger.Tests
{
    internal sealed class PlayScopeObserver : DirectModuleBase, ISceneScopeLifecycle
    {
        public FrameworkSceneScopeInfo Current { get; private set; }
        public int Starts { get; private set; }
        public int Ends { get; private set; }
        public UniTask OnSceneScopeStartingAsync(FrameworkSceneScopeInfo scope, CancellationToken token)
        { Current = scope; Starts++; PlayLifecycleRecorder.Events.Add("Observer.Start"); return UniTask.CompletedTask; }
        public UniTask OnSceneScopeEndingAsync(FrameworkSceneScopeInfo scope)
        { Assert.That(scope, Is.SameAs(Current)); Current = null; Ends++; PlayLifecycleRecorder.Events.Add("Observer.End"); return UniTask.CompletedTask; }
    }
    internal sealed class FrameworkSceneParticipantTests : FrameworkPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator RuntimeCallsObserverBeforeSceneModules_AndIgnoresLateDetach()
        {
            var config = Factory.CreateGlobal(Factory.CreateModule<PlayGlobalModule>(), Factory.CreateModule<PlayScopeObserver>());
            int old = Factory.ActivateScene(config, Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();
            var observer = Framework.GetModule<PlayScopeObserver>();
            var first = observer.Current;
            Assert.That(PlayLifecycleRecorder.Events.IndexOf("Observer.Start"), Is.LessThan(PlayLifecycleRecorder.Events.IndexOf("SceneHandler.Load")));
            Factory.ActivateScene(config, Factory.CreateScene(Factory.CreateSceneModule()));
            yield return WaitForReady();
            Assert.That(observer.Starts, Is.EqualTo(2)); Assert.That(observer.Ends, Is.EqualTo(1));
            Assert.That(observer.Current, Is.Not.SameAs(first));
            Assert.That(PlayLifecycleRecorder.Events.IndexOf("Observer.End"), Is.LessThan(PlayLifecycleRecorder.Events.IndexOf("SceneHandler.Unload")));
            Factory.DeactivateScene(old); yield return null;
            Assert.That(observer.Ends, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator RejectedGlobalConfigurationDoesNotEndCurrentParticipant()
        {
            var config = Factory.CreateGlobal(Factory.CreateModule<PlayScopeObserver>());
            Factory.ActivateScene(config, Factory.CreateScene()); yield return WaitForReady();
            var observer = Framework.GetModule<PlayScopeObserver>(); var scope = observer.Current;
            LogAssert.Expect(LogType.Exception, new Regex("GlobalConfig 已发生变化"));
            Factory.ActivateScene(Factory.CreateGlobal(), Factory.CreateScene()); yield return null;
            Assert.That(observer.Current, Is.SameAs(scope)); Assert.That(observer.Ends, Is.Zero);
        }
    }
}
