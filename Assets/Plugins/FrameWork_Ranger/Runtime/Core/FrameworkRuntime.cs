using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FrameWork_Ranger
{
    /// <summary>
    /// Framework 的纯 C# 运行核心，统一拥有配置克隆、Global/Scene Scope、状态机和结构性操作队列。
    /// </summary>
    [FrameworkArchitecture(
        "框架运行核心",
        "拥有状态机、Global/Scene Scope、装配、回滚、查询和 Tick 算法。",
        FrameworkArchitectureLayer.RuntimeDriving,
        70,
        typeof(ModuleGraphResolver),
        typeof(ModuleScopeRuntime),
        typeof(FrameworkOperationQueue))]
    internal sealed class FrameworkRuntime
    {
        #region 运行时状态

        private readonly FrameworkOperationQueue m_operationQueue = new FrameworkOperationQueue();
        private readonly CancellationTokenSource m_shutdownCancellation = new CancellationTokenSource();
        private readonly UniTaskCompletionSource m_shutdownCompletion = new UniTaskCompletionSource();

        private FrameworkGlobalConfig m_globalConfigTemplate;
        private FrameworkGlobalConfig m_globalConfigRuntime;
        private FrameworkDriverHandlerBase m_driverHandler;
        private FrameworkDriverContext m_driverContext;
        private ModuleScopeRuntime m_globalScope;
        private ModuleScopeRuntime m_sceneScope;
        private ulong m_sceneOwnerId;

        private UniTaskCompletionSource m_readyCompletion = new UniTaskCompletionSource();
        private bool m_readyCompletionFinished;
        private bool m_shutdownRequested;

        #endregion

        #region 公开属性

        internal FrameworkState State { get; private set; } = FrameworkState.Uninitialized;

        internal Exception LastException { get; private set; }

        internal bool IsReady => State == FrameworkState.Ready;

        #endregion

        #region 场景与关停

        internal UniTask AttachSceneAsync(
            FrameworkSceneDescriptor scene,
            FrameworkProjectSettings projectSettings,
            CancellationToken cancellationToken)
        {
            if (m_shutdownRequested || State == FrameworkState.ShuttingDown || State == FrameworkState.Shutdown)
            {
                return UniTask.FromException(new InvalidOperationException(
                    "Framework 正在关停或已经关停，不能再挂载场景作用域。"));
            }

            if (State == FrameworkState.Failed && m_globalScope == null)
            {
                return UniTask.FromException(LastException ?? new InvalidOperationException(
                    "Framework 的全局初始化已经失败，请先调用 ShutdownAsync 后再重新启动。"));
            }

            PrepareReadinessBatch();
            return m_operationQueue.Enqueue(
                () => AttachSceneCoreAsync(scene, projectSettings, cancellationToken));
        }

        internal UniTask DetachSceneAsync(ulong sceneHandle)
        {
            if (m_shutdownRequested || State == FrameworkState.Shutdown)
            {
                return UniTask.CompletedTask;
            }

            return m_operationQueue.Enqueue(() => DetachSceneCoreAsync(sceneHandle));
        }

        internal UniTask ShutdownAsync()
        {
            if (State == FrameworkState.Shutdown)
            {
                return m_shutdownCompletion.Task;
            }

            if (m_shutdownRequested)
            {
                return m_shutdownCompletion.Task;
            }

            m_shutdownRequested = true;
            // 取消正在加载的 Scope，使关停请求不必等待一个已经失去意义的长时间加载自然结束。
            m_shutdownCancellation.Cancel();
            ObserveShutdownAsync(m_operationQueue.Enqueue(ShutdownCoreAsync)).Forget();
            return m_shutdownCompletion.Task;
        }

        private async UniTask AttachSceneCoreAsync(
            FrameworkSceneDescriptor scene,
            FrameworkProjectSettings projectSettings,
            CancellationToken sceneCancellationToken)
        {
            var settingsResult = FrameworkProjectSettingsResolver.Resolve(projectSettings, scene.Path);
            if (!settingsResult.IsValid)
            {
                var messages = CollectProjectSettingsErrors(settingsResult);
                RejectConfiguration(messages);
                throw LastException;
            }

            var globalConfig = settingsResult.GlobalConfig;
            var sceneConfig = settingsResult.SceneConfig;
            if (m_globalConfigTemplate != null && m_globalConfigTemplate != globalConfig)
            {
                var exception = new InvalidOperationException(
                    $"中央项目设置的 GlobalConfig 已发生变化。当前为 {m_globalConfigTemplate.name}，新配置为 {globalConfig.name}。" );
                LastException = exception;
                throw exception;
            }

            var graph = ModuleGraphResolver.Resolve(globalConfig, sceneConfig);
            if (!graph.IsValid || globalConfig?.DriverHandler == null)
            {
                var messages = CollectConfigurationErrors(graph);
                if (globalConfig != null && globalConfig.DriverHandler == null)
                {
                    messages.Add("[Error] Global[-1] FrameworkGlobalConfig: 没有配置 FrameworkDriverHandler。" );
                }

                RejectConfiguration(messages);
                throw LastException;
            }

            if (m_globalScope == null)
            {
                await InitializeGlobalAsync(globalConfig, graph.OrderedGlobalNodes, sceneCancellationToken);
            }

            if (m_sceneScope != null)
            {
                State = FrameworkState.UnloadingScene;
                var unloadErrors = await UnloadSceneScopeAsync();
                LogCleanupErrors("替换 SceneScope", unloadErrors);
            }

            await LoadSceneAsync(scene.Handle, graph.OrderedSceneNodes, sceneCancellationToken);
        }

        private async UniTask DetachSceneCoreAsync(ulong sceneHandle)
        {
            // Unity 的旧场景卸载事件可能晚于新活动场景的挂载；Scene Handle 防止它误卸载新 Scope。
            if (m_sceneScope == null || m_sceneOwnerId != sceneHandle)
            {
                return;
            }

            PrepareReadinessBatch();
            State = FrameworkState.UnloadingScene;
            var errors = await UnloadSceneScopeAsync();
            State = FrameworkState.GlobalReady;

            if (errors.Count > 0)
            {
                var exception = new AggregateException("SceneScope 已完成清理，但一个或多个卸载步骤失败。", errors);
                LastException = exception;
                LogCleanupErrors("卸载 SceneScope", errors);
                throw exception;
            }
        }

        private async UniTask ShutdownCoreAsync()
        {
            State = FrameworkState.ShuttingDown;
            var errors = new List<Exception>();

            if (m_sceneScope != null)
            {
                AppendErrors(errors, await UnloadSceneScopeAsync());
            }

            if (m_globalScope != null && m_driverHandler != null)
            {
                AppendErrors(errors, await m_globalScope.UnloadAndDestroyAsync(m_driverHandler));
                m_globalScope = null;
            }

            m_driverHandler?.ReleaseRuntime();
            m_driverHandler = null;
            m_driverContext = null;

            RuntimeObjectUtility.Destroy(m_globalConfigRuntime);
            m_globalConfigRuntime = null;
            m_globalConfigTemplate = null;
            m_sceneOwnerId = 0;

            if (!m_readyCompletionFinished)
            {
                FailReadiness(new InvalidOperationException("Framework 已关停，当前就绪等待不会再完成。"));
            }

            State = FrameworkState.Shutdown;
            m_shutdownCancellation.Dispose();

            if (errors.Count > 0)
            {
                var exception = new AggregateException("Framework 已完成关停，但一个或多个卸载步骤失败。", errors);
                LastException = exception;
                LogCleanupErrors("Framework Shutdown", errors);
                throw exception;
            }

            LastException = null;
        }

        private async UniTaskVoid ObserveShutdownAsync(UniTask shutdownTask)
        {
            try
            {
                await shutdownTask;
                m_shutdownCompletion.TrySetResult();
            }
            catch (Exception exception)
            {
                m_shutdownCompletion.TrySetException(exception);
            }
        }

        #endregion

        #region Scope 装配

        private async UniTask InitializeGlobalAsync(
            FrameworkGlobalConfig globalConfig,
            IReadOnlyList<ModuleGraphNode> orderedNodes,
            CancellationToken sceneCancellationToken)
        {
            State = FrameworkState.InitializingGlobal;
            m_globalConfigTemplate = globalConfig;
            m_globalConfigRuntime = RuntimeObjectUtility.CloneGlobalConfig(globalConfig);
            m_driverHandler = m_globalConfigRuntime.DriverHandler;
            m_driverContext = new FrameworkDriverContext(this);
            m_driverHandler.BindRuntime(m_driverContext);
            m_globalScope = new ModuleScopeRuntime(this, ModuleScopeKind.Global, orderedNodes);

            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                       sceneCancellationToken,
                       m_shutdownCancellation.Token))
            {
                try
                {
                    await m_globalScope.LoadAsync(m_driverHandler, linkedCancellation.Token);
                    State = FrameworkState.GlobalReady;
                }
                catch (Exception exception)
                {
                    m_globalScope = null;
                    m_driverHandler.ReleaseRuntime();
                    m_driverHandler = null;
                    m_driverContext = null;
                    RuntimeObjectUtility.Destroy(m_globalConfigRuntime);
                    m_globalConfigRuntime = null;
                    State = FrameworkState.Failed;
                    LastException = exception;
                    FailReadiness(exception);
                    Debug.LogError($"[FrameWork_Ranger] GlobalScope 初始化失败：{exception.Message}" );
                    throw;
                }
            }
        }

        private async UniTask LoadSceneAsync(
            ulong sceneHandle,
            IReadOnlyList<ModuleGraphNode> orderedNodes,
            CancellationToken sceneCancellationToken)
        {
            State = FrameworkState.LoadingScene;
            m_sceneOwnerId = sceneHandle;
            m_sceneScope = new ModuleScopeRuntime(this, ModuleScopeKind.Scene, orderedNodes);

            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                       sceneCancellationToken,
                       m_shutdownCancellation.Token))
            {
                try
                {
                    await m_sceneScope.LoadAsync(m_driverHandler, linkedCancellation.Token);
                    State = FrameworkState.Ready;
                    LastException = null;
                    CompleteReadiness();
                }
                catch (Exception exception)
                {
                    m_sceneScope = null;
                    m_sceneOwnerId = 0;
                    State = FrameworkState.GlobalReady;
                    LastException = exception;
                    FailReadiness(exception);
                    Debug.LogError($"[FrameWork_Ranger] SceneScope 加载失败：{exception.Message}" );
                    throw;
                }
            }
        }

        private async UniTask<IReadOnlyList<Exception>> UnloadSceneScopeAsync()
        {
            var scope = m_sceneScope;
            m_sceneScope = null;
            m_sceneOwnerId = 0;
            if (scope == null)
            {
                return Array.Empty<Exception>();
            }

            return await scope.UnloadAndDestroyAsync(m_driverHandler);
        }

        #endregion

        #region 就绪等待

        internal UniTask WhenReadyAsync(CancellationToken cancellationToken)
        {
            if (State == FrameworkState.Ready)
            {
                return UniTask.CompletedTask;
            }

            if (State == FrameworkState.Shutdown || State == FrameworkState.ShuttingDown)
            {
                return UniTask.FromException(new InvalidOperationException("Framework 已经关停。"));
            }

            var task = m_readyCompletion.Task;
            return cancellationToken.CanBeCanceled
                ? task.AttachExternalCancellation(cancellationToken)
                : task;
        }

        private void PrepareReadinessBatch()
        {
            if (!m_readyCompletionFinished)
            {
                return;
            }

            m_readyCompletion = new UniTaskCompletionSource();
            m_readyCompletionFinished = false;
        }

        private void CompleteReadiness()
        {
            if (m_readyCompletionFinished)
            {
                return;
            }

            m_readyCompletionFinished = true;
            m_readyCompletion.TrySetResult();
        }

        private void FailReadiness(Exception exception)
        {
            if (m_readyCompletionFinished)
            {
                return;
            }

            m_readyCompletionFinished = true;
            if (exception is OperationCanceledException canceled)
            {
                m_readyCompletion.TrySetCanceled(canceled.CancellationToken);
            }
            else
            {
                m_readyCompletion.TrySetException(exception);
                // 就绪批次可能在没有外部等待者时失败。UniTask 的异常完成源若始终无人读取，
                // 会在稍后 GC 时再次发布“未观察异常”，把已经处理过的加载失败污染到后续场景。
                // 内部观察者只负责消费完成源的观察标记；同一原始异常仍可由未来的 WhenReadyAsync 读取。
                ObserveReadinessFailureAsync(m_readyCompletion.Task).Forget();
            }
        }

        private static async UniTaskVoid ObserveReadinessFailureAsync(UniTask readinessTask)
        {
            try
            {
                await readinessTask;
            }
            catch
            {
                // Runtime 已在失败发生处记录 LastException 与结构化日志，这里不能重复记录。
            }
        }

        #endregion

        #region 模块查询

        internal bool TryGetModule(Type moduleType, out ModuleBase module)
        {
            if (m_sceneScope != null && m_sceneScope.TryGetLoadedModule(moduleType, out module))
            {
                return true;
            }

            if (m_globalScope != null && m_globalScope.TryGetLoadedModule(moduleType, out module))
            {
                return true;
            }

            module = null;
            return false;
        }

        internal bool TryGetModuleForContext(
            Type moduleType,
            ModuleScopeRuntime ownerScope,
            out ModuleBase module)
        {
            if (ownerScope.TryGetLoadedModule(moduleType, out module))
            {
                return true;
            }

            if (ownerScope.ScopeKind == ModuleScopeKind.Scene &&
                m_globalScope != null &&
                m_globalScope.TryGetLoadedModule(moduleType, out module))
            {
                return true;
            }

            module = null;
            return false;
        }

        #endregion

        #region Tick 驱动

        internal void TickUpdate(float deltaTime)
        {
            m_globalScope?.TickUpdate(deltaTime);
            m_sceneScope?.TickUpdate(deltaTime);
        }

        internal void TickFixedUpdate(float fixedDeltaTime)
        {
            m_globalScope?.TickFixedUpdate(fixedDeltaTime);
            m_sceneScope?.TickFixedUpdate(fixedDeltaTime);
        }

        internal void TickLateUpdate(float deltaTime)
        {
            m_globalScope?.TickLateUpdate(deltaTime);
            m_sceneScope?.TickLateUpdate(deltaTime);
        }

        #endregion

        #region 内部实现

        private static List<string> CollectConfigurationErrors(ModuleGraphResult graph)
        {
            var result = new List<string>();
            for (var i = 0; i < graph.Diagnostics.Count; i++)
            {
                if (graph.Diagnostics[i].Severity == ModuleGraphDiagnosticSeverity.Error)
                {
                    result.Add(graph.Diagnostics[i].ToString());
                }
            }

            return result;
        }

        private static List<string> CollectProjectSettingsErrors(FrameworkProjectSettingsResult result)
        {
            var messages = new List<string>();
            for (var i = 0; i < result.Diagnostics.Count; i++)
            {
                if (result.Diagnostics[i].Severity == FrameworkProjectSettingsDiagnosticSeverity.Error)
                {
                    messages.Add(result.Diagnostics[i].ToString());
                }
            }

            return messages;
        }

        private void RejectConfiguration(IReadOnlyList<string> messages)
        {
            var exception = new FrameworkConfigurationException(messages);
            LastException = exception;

            // 首次装配尚无 Global 时，配置错误是终止性失败；已有 Global 时保留仍然有效的旧 Scope。
            State = m_globalScope == null
                ? FrameworkState.Failed
                : m_sceneScope == null ? FrameworkState.GlobalReady : FrameworkState.Ready;
            FailReadiness(exception);
            Debug.LogError(exception.Message);
        }

        private static void AppendErrors(List<Exception> target, IReadOnlyList<Exception> source)
        {
            for (var i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }

        private static void LogCleanupErrors(string phase, IReadOnlyList<Exception> errors)
        {
            for (var i = 0; i < errors.Count; i++)
            {
                Debug.LogError($"[FrameWork_Ranger] {phase}时发生异常，清理流程继续执行。" );
                Debug.LogException(errors[i]);
            }
        }

        #endregion
    }
}
