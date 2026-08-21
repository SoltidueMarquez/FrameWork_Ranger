using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework_WWJ.Tests
{
    internal static class TestLifecycleRecorder
    {
        internal static readonly List<string> Events = new List<string>();

        internal static void Reset()
        {
            Events.Clear();
        }
    }

    internal sealed class TestModuleA : DirectModuleBase
    {
        internal bool WasVisibleDuringUnload { get; private set; }

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            TestLifecycleRecorder.Events.Add("A.Load");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            WasVisibleDuringUnload = Context.TryGetModule<TestModuleA>(out _);
            TestLifecycleRecorder.Events.Add("A.Unload");
            return UniTask.CompletedTask;
        }
    }

    internal sealed class TestModuleB : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestModuleA) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            TestLifecycleRecorder.Events.Add("B.Load");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            TestLifecycleRecorder.Events.Add("B.Unload");
            return UniTask.CompletedTask;
        }
    }

    internal sealed class TestModuleC : DirectModuleBase
    {
    }

    internal sealed class TestUnloadFailModule : DirectModuleBase
    {
        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            TestLifecycleRecorder.Events.Add("Fail.Load");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            TestLifecycleRecorder.Events.Add("Fail.Unload");
            throw new InvalidOperationException("EditMode unload failure");
        }
    }

    internal sealed class TestSceneDependsGlobalModule : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestModuleA) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    internal sealed class TestSceneOnlyModule : DirectModuleBase
    {
    }

    internal sealed class TestGlobalDependsSceneModule : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestSceneOnlyModule) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    internal sealed class TestSelfDependencyModule : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestSelfDependencyModule) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    internal sealed class TestMissingDependencyModule : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestModuleC) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    internal sealed class TestCycleModuleA : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestCycleModuleB) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    internal sealed class TestCycleModuleB : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(TestCycleModuleA) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    [Serializable]
    internal sealed class TestModuleHandler : ModuleHandlerBase
    {
        internal bool WasLoaded { get; private set; }

        internal bool HasRuntimeBinding
        {
            get
            {
                try
                {
                    _ = Owner;
                    _ = Context;
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            WasLoaded = GetOwner<TestHandlerModule>() != null;
            TestLifecycleRecorder.Events.Add("Handler.Load");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            TestLifecycleRecorder.Events.Add("Handler.Unload");
            return UniTask.CompletedTask;
        }
    }

    internal sealed class TestHandlerModule : HandlerModuleBase<TestModuleHandler>
    {
    }
}
