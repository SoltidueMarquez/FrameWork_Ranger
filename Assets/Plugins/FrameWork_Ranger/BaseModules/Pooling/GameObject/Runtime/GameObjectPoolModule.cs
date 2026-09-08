using System;
using System.Collections.Generic;
using FrameWork_Ranger.ResourceManagement;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.GameObjects
{
    /// <summary>
    /// SceneScope GameObject 池唯一业务门面。
    /// </summary>
    [CreateAssetMenu(fileName = "GameObjectPoolModule", menuName = "FrameWork_Ranger/Modules/GameObject Pool")]
    [FrameworkArchitecture(
        "GameObject 池模块",
        "以显式池名提供同步 Spawn/Despawn，并声明对 Global ResourceModule 的精确依赖。",
        FrameworkArchitectureLayer.PublicFacade,
        135,
        typeof(GameObjectPoolHandler),
        typeof(ResourceModule),
        typeof(IGameObjectPoolCallbacks))]
    public sealed class GameObjectPoolModule : HandlerModuleBase<GameObjectPoolHandler>
    {
        private static readonly IReadOnlyList<Type> Dependencies = new[]
        {
            typeof(ResourceModule),
        };

        protected override IReadOnlyList<Type> RequiredModuleTypes => Dependencies;

        public GameObject Spawn(string poolName)
        {
            return Handler.Spawn(poolName, Vector3.zero, Quaternion.identity, null);
        }

        public GameObject Spawn(
            string poolName,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            return Handler.Spawn(poolName, position, rotation, parent);
        }

        public bool TrySpawn(string poolName, out GameObject instance)
        {
            return Handler.TrySpawn(
                poolName,
                Vector3.zero,
                Quaternion.identity,
                null,
                out instance);
        }

        public bool TrySpawn(
            string poolName,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            out GameObject instance)
        {
            return Handler.TrySpawn(poolName, position, rotation, parent, out instance);
        }

        public void Despawn(string poolName, GameObject instance)
        {
            Handler.Despawn(poolName, instance);
        }

        public bool TryDespawn(string poolName, GameObject instance)
        {
            return Handler.TryDespawn(poolName, instance);
        }

        public void ClearInactive(string poolName)
        {
            Handler.ClearInactive(poolName);
        }

        internal IReadOnlyList<GameObjectPoolDiagnosticsSnapshot> CreateDiagnosticsSnapshots()
        {
            return Handler.CreateDiagnosticsSnapshots();
        }

        internal IReadOnlyList<GameObjectPoolDefinition> GetDefinitions()
        {
            return Handler?.Definitions;
        }

        internal int GetPrewarmBudgetPerFrame()
        {
            return Handler == null ? 0 : Handler.PrewarmBudgetPerFrame;
        }

        internal bool HasConfiguredHandler => Handler != null;
    }
}
