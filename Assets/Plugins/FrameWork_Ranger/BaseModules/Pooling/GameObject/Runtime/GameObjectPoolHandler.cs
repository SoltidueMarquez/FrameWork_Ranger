using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.ResourceManagement;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FrameWork_Ranger.Pooling.GameObjects
{
    /// <summary>
    /// GameObjectPoolModule 的 SceneScope 加载、预热、Tick 与逆序回滚编排器。
    /// </summary>
    [Serializable]
    [FrameworkArchitecture(
        "GameObject 池 Handler",
        "无副作用校验定义，串行取得 Prefab Lease，按全模块预算分帧预热并在场景卸载时逆序清理。",
        FrameworkArchitectureLayer.RuntimeDriving,
        134,
        typeof(GameObjectPoolModule),
        typeof(ResourceModule),
        typeof(GameObjectPoolRuntime))]
    public sealed class GameObjectPoolHandler : ModuleHandlerBase, IModuleUpdate
    {
        public const int DefaultPrewarmBudgetPerFrame = 10;

        [OdinSerialize]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        private List<GameObjectPoolDefinition> m_definitions = new List<GameObjectPoolDefinition>();

        [SerializeField]
        [Min(1)]
        private int m_prewarmBudgetPerFrame = DefaultPrewarmBudgetPerFrame;

        [NonSerialized]
        private Dictionary<string, GameObjectPoolRuntime> m_pools;

        [NonSerialized]
        private List<GameObjectPoolRuntime> m_creationOrder;

        [NonSerialized]
        private GameObject m_moduleRoot;

        [NonSerialized]
        private int m_mainThreadId;

        [NonSerialized]
        private bool m_acceptingRequests;

        internal IReadOnlyList<GameObjectPoolDefinition> Definitions => m_definitions;

        internal int PrewarmBudgetPerFrame => m_prewarmBudgetPerFrame;

        protected override async UniTask OnLoadAsync(CancellationToken cancellationToken)
        {
            if (Context.ScopeKind != ModuleScopeKind.Scene)
            {
                throw new InvalidOperationException("GameObjectPoolModule 只能安装在 SceneScope。" );
            }

            m_mainThreadId = Thread.CurrentThread.ManagedThreadId;
            cancellationToken.ThrowIfCancellationRequested();
            ValidateConfiguration();
            var resourceModule = Context.GetModule<ResourceModule>();
            m_pools = new Dictionary<string, GameObjectPoolRuntime>(StringComparer.Ordinal);
            m_creationOrder = new List<GameObjectPoolRuntime>(m_definitions.Count);

            try
            {
                CreateModuleRoot();
                var createdThisFrame = 0;
                for (var i = 0; i < m_definitions.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var definition = m_definitions[i];
                    var lease = await resourceModule.AcquireAsync<GameObject>(
                        definition.ResourceKey,
                        cancellationToken);
                    GameObjectPoolRuntime runtime;
                    try
                    {
                        runtime = new GameObjectPoolRuntime(
                            definition,
                            lease,
                            m_moduleRoot.transform);
                    }
                    catch (Exception creationException)
                    {
                        try
                        {
                            lease.Release();
                        }
                        catch (Exception releaseException)
                        {
                            throw new AggregateException(
                                $"建立 GameObject 池 {definition.PoolName} 失败，释放 Prefab Lease 时又发生异常。",
                                creationException,
                                releaseException);
                        }

                        throw;
                    }

                    m_pools.Add(definition.PoolName, runtime);
                    m_creationOrder.Add(runtime);
                    while (runtime.InactiveCount < runtime.InitialCount)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        runtime.PrewarmOne();
                        createdThisFrame++;
                        if (createdThisFrame < m_prewarmBudgetPerFrame ||
                            IsPrewarmCompleteFrom(i))
                        {
                            continue;
                        }

                        createdThisFrame = 0;
                        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                m_acceptingRequests = true;
            }
            catch (Exception loadException)
            {
                m_acceptingRequests = false;
                var errors = new List<Exception> { loadException };
                ShutdownPoolsAndRoot(errors);
                ResetRuntimeState();
                throw errors.Count == 1
                    ? loadException
                    : new AggregateException("GameObjectPoolModule 加载失败，回滚时又发生异常。", errors);
            }
        }

        protected override UniTask OnUnloadAsync()
        {
            AssertMainThread();
            m_acceptingRequests = false;
            var errors = new List<Exception>();
            if (m_creationOrder != null)
            {
                for (var i = 0; i < m_creationOrder.Count; i++)
                {
                    var runtime = m_creationOrder[i];
                    runtime.PruneExternallyDestroyed();
                    if (runtime.BorrowedCount > 0)
                    {
                        Debug.LogWarning(
                            $"GameObjectPoolModule 卸载时池 {runtime.PoolName} 仍有 " +
                            $"{runtime.BorrowedCount} 个实例未归还；将执行 OnDespawned 并强制销毁。" );
                    }
                }
            }

            ShutdownPoolsAndRoot(errors);
            ResetRuntimeState();
            if (errors.Count > 0)
            {
                throw new AggregateException("GameObjectPoolModule 关闭时发生清理异常。", errors);
            }

            return UniTask.CompletedTask;
        }

        public void OnModuleUpdate(float deltaTime)
        {
            AssertReadyAndMainThread();
            for (var i = 0; i < m_creationOrder.Count; i++)
            {
                m_creationOrder[i].Tick(deltaTime);
            }
        }

        internal GameObject Spawn(
            string poolName,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
        {
            AssertReadyAndMainThread();
            ValidatePoolNameArgument(poolName);
            if (!m_pools.TryGetValue(poolName, out var runtime))
            {
                throw new KeyNotFoundException($"SceneScope 中不存在 GameObject 池 {poolName}。" );
            }

            return runtime.Spawn(position, rotation, parent);
        }

        internal bool TrySpawn(
            string poolName,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            out GameObject instance)
        {
            AssertReadyAndMainThread();
            if (!IsValidPoolNameArgument(poolName) || !m_pools.TryGetValue(poolName, out var runtime))
            {
                instance = null;
                return false;
            }

            instance = runtime.Spawn(position, rotation, parent);
            return true;
        }

        internal void Despawn(string poolName, GameObject instance)
        {
            AssertReadyAndMainThread();
            ValidatePoolNameArgument(poolName);
            if (!m_pools.TryGetValue(poolName, out var runtime))
            {
                throw new KeyNotFoundException($"SceneScope 中不存在 GameObject 池 {poolName}。" );
            }

            runtime.Despawn(instance);
        }

        internal bool TryDespawn(string poolName, GameObject instance)
        {
            AssertReadyAndMainThread();
            if (!IsValidPoolNameArgument(poolName) || !m_pools.TryGetValue(poolName, out var runtime))
            {
                return false;
            }

            return runtime.TryDespawn(instance);
        }

        internal void ClearInactive(string poolName)
        {
            AssertReadyAndMainThread();
            ValidatePoolNameArgument(poolName);
            if (!m_pools.TryGetValue(poolName, out var runtime))
            {
                throw new KeyNotFoundException($"SceneScope 中不存在 GameObject 池 {poolName}。" );
            }

            runtime.ClearInactive();
        }

        internal IReadOnlyList<GameObjectPoolDiagnosticsSnapshot> CreateDiagnosticsSnapshots()
        {
            AssertReadyAndMainThread();
            var snapshots = new List<GameObjectPoolDiagnosticsSnapshot>(m_creationOrder.Count);
            for (var i = 0; i < m_creationOrder.Count; i++)
            {
                snapshots.Add(m_creationOrder[i].CreateDiagnosticsSnapshot());
            }

            return snapshots;
        }

        internal void SetConfiguration(
            IReadOnlyList<GameObjectPoolDefinition> definitions,
            int prewarmBudgetPerFrame = DefaultPrewarmBudgetPerFrame)
        {
            m_definitions.Clear();
            if (definitions != null)
            {
                for (var i = 0; i < definitions.Count; i++)
                {
                    m_definitions.Add(definitions[i]);
                }
            }

            m_prewarmBudgetPerFrame = prewarmBudgetPerFrame;
        }

        private void ValidateConfiguration()
        {
            if (m_definitions == null)
            {
                throw new InvalidOperationException("GameObjectPoolHandler 池定义列表为空引用。" );
            }

            if (m_prewarmBudgetPerFrame <= 0)
            {
                throw new InvalidOperationException("GameObjectPoolHandler 预热预算必须大于 0。" );
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_definitions.Count; i++)
            {
                var definition = m_definitions[i];
                if (definition == null)
                {
                    throw new InvalidOperationException($"GameObjectPoolHandler Definitions[{i}] 为空。" );
                }

                if (!definition.TryValidate(out var error))
                {
                    throw new InvalidOperationException(
                        $"GameObjectPoolHandler Definitions[{i}] 无效：{error}" );
                }

                if (!names.Add(definition.PoolName))
                {
                    throw new InvalidOperationException(
                        $"GameObjectPoolHandler 重复配置了池名 {definition.PoolName}。" );
                }
            }
        }

        private bool IsPrewarmCompleteFrom(int currentDefinitionIndex)
        {
            if (currentDefinitionIndex < m_creationOrder.Count &&
                m_creationOrder[currentDefinitionIndex].InactiveCount <
                m_creationOrder[currentDefinitionIndex].InitialCount)
            {
                return false;
            }

            for (var i = currentDefinitionIndex + 1; i < m_definitions.Count; i++)
            {
                if (m_definitions[i].Capacity.InitialCount > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void CreateModuleRoot()
        {
            m_moduleRoot = new GameObject("[FrameWork_Ranger GameObject Pools]");
            m_moduleRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            m_moduleRoot.SetActive(false);
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(m_moduleRoot, activeScene);
            }
        }

        private void ShutdownPoolsAndRoot(ICollection<Exception> errors)
        {
            if (m_creationOrder != null)
            {
                for (var i = m_creationOrder.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        m_creationOrder[i].Shutdown();
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }
            }

            try
            {
                DestroyUnityObject(m_moduleRoot);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        private void ResetRuntimeState()
        {
            m_acceptingRequests = false;
            m_pools = null;
            m_creationOrder = null;
            m_moduleRoot = null;
            m_mainThreadId = 0;
        }

        private void AssertReadyAndMainThread()
        {
            AssertMainThread();
            if (!m_acceptingRequests || m_pools == null)
            {
                throw new InvalidOperationException("GameObjectPoolModule 尚未完成加载或已经关闭。" );
            }
        }

        private void AssertMainThread()
        {
            if (m_mainThreadId == 0 || Thread.CurrentThread.ManagedThreadId != m_mainThreadId)
            {
                throw new InvalidOperationException("GameObjectPoolModule 只能在加载它的 Unity 主线程调用。" );
            }
        }

        private static bool IsValidPoolNameArgument(string poolName)
        {
            return !string.IsNullOrEmpty(poolName) &&
                   string.Equals(poolName, poolName.Trim(), StringComparison.Ordinal);
        }

        private static void ValidatePoolNameArgument(string poolName)
        {
            if (!IsValidPoolNameArgument(poolName))
            {
                throw new ArgumentException("池名不能为空，也不能包含首尾空白。", nameof(poolName));
            }
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
