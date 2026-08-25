using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FrameWork_Ranger.Tests
{
    internal static class PlayLifecycleRecorder
    {
        internal static readonly List<string> Events = new List<string>();

        internal static int SceneUpdateCount { get; set; }

        internal static void Reset()
        {
            Events.Clear();
            SceneUpdateCount = 0;
        }
    }

    internal sealed class PlayGlobalModule : DirectModuleBase,
        IModuleUpdate,
        IModuleFixedUpdate,
        IModuleLateUpdate
    {
        internal bool ThrowOnNextUpdate { get; set; }
        internal int UpdateCount { get; private set; }
        internal int FixedUpdateCount { get; private set; }
        internal int LateUpdateCount { get; private set; }

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            PlayLifecycleRecorder.Events.Add("Global.Load");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            PlayLifecycleRecorder.Events.Add("Global.Unload");
            return UniTask.CompletedTask;
        }

        public void OnModuleUpdate(float deltaTime)
        {
            if (ThrowOnNextUpdate)
            {
                ThrowOnNextUpdate = false;
                throw new InvalidOperationException("PlayMode tick failure");
            }

            UpdateCount++;
        }

        public void OnModuleFixedUpdate(float fixedDeltaTime)
        {
            FixedUpdateCount++;
        }

        public void OnModuleLateUpdate(float deltaTime)
        {
            LateUpdateCount++;
        }
    }

    [Serializable]
    internal sealed class PlaySceneHandler : ModuleHandlerBase, IModuleUpdate
    {
        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            PlayLifecycleRecorder.Events.Add("SceneHandler.Load");
            return UniTask.CompletedTask;
        }

        protected override UniTask OnUnloadAsync()
        {
            PlayLifecycleRecorder.Events.Add("SceneHandler.Unload");
            return UniTask.CompletedTask;
        }

        public void OnModuleUpdate(float deltaTime)
        {
            PlayLifecycleRecorder.SceneUpdateCount++;
            GetOwner<PlaySceneModule>().TickCount++;
        }
    }

    internal sealed class PlaySceneModule : HandlerModuleBase<PlaySceneHandler>
    {
        private static readonly Type[] Dependencies = { typeof(PlayGlobalModule) };

        internal int TickCount { get; set; }

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;
    }

    internal sealed class PlayFailSceneModule : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(PlayGlobalModule) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Scene load failed");
        }
    }

    internal sealed class PlayFailGlobalModule : DirectModuleBase
    {
        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Global load failed");
        }
    }

    internal sealed class PlaySlowSceneModule : DirectModuleBase
    {
        private static readonly Type[] Dependencies = { typeof(PlayGlobalModule) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        protected override UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            return UniTask.DelayFrame(3, cancellationToken: cancellationToken);
        }
    }

    internal sealed class PlayTickOrderGlobalModule : DirectModuleBase, IModuleUpdate
    {
        public void OnModuleUpdate(float deltaTime)
        {
            PlayLifecycleRecorder.Events.Add("Order.Global.Update");
        }
    }

    [Serializable]
    internal sealed class PlayTickOrderHandler : ModuleHandlerBase, IModuleUpdate
    {
        public void OnModuleUpdate(float deltaTime)
        {
            PlayLifecycleRecorder.Events.Add("Order.Handler.Update");
        }
    }

    internal sealed class PlayTickOrderSceneModule : HandlerModuleBase<PlayTickOrderHandler>, IModuleUpdate
    {
        private static readonly Type[] Dependencies = { typeof(PlayTickOrderGlobalModule) };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        public void OnModuleUpdate(float deltaTime)
        {
            PlayLifecycleRecorder.Events.Add("Order.Module.Update");
        }
    }
}
