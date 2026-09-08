using System;
using FrameWork_Ranger.Pooling;
using FrameWork_Ranger.ResourceManagement;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.GameObjects
{
    /// <summary>
    /// 一个 SceneScope GameObject 池的静态名称、Prefab ResourceKey 与容量定义。
    /// </summary>
    [Serializable]
    [FrameworkArchitecture(
        "GameObject 池定义",
        "把显式池名、资源后端、位置和容量组合为可验证且不含运行状态的配置。",
        FrameworkArchitectureLayer.Configuration,
        131,
        typeof(ResourceKey),
        typeof(PoolCapacitySettings))]
    public sealed class GameObjectPoolDefinition
    {
        [SerializeField]
        private string m_poolName;

        [SerializeField]
        private ResourceBackendKind m_backend;

        [SerializeField]
        private string m_location;

        [SerializeField]
        private PoolCapacitySettings m_capacity = PoolCapacitySettings.CreateGameObjectDefaults();

        public string PoolName => m_poolName;

        public ResourceBackendKind Backend => m_backend;

        public string Location => m_location;

        public PoolCapacitySettings Capacity => m_capacity;

        public ResourceKey ResourceKey => CreateResourceKey(m_backend, m_location);

        public GameObjectPoolDefinition()
        {
        }

        public GameObjectPoolDefinition(
            string poolName,
            ResourceBackendKind backend,
            string location,
            PoolCapacitySettings capacity)
        {
            m_poolName = poolName;
            m_backend = backend;
            m_location = location;
            m_capacity = capacity;
        }

        internal bool TryValidate(out string error)
        {
            if (string.IsNullOrEmpty(m_poolName))
            {
                error = "池名不能为空。";
                return false;
            }

            if (!string.Equals(m_poolName, m_poolName.Trim(), StringComparison.Ordinal))
            {
                error = $"池名“{m_poolName}”不能包含首尾空白。";
                return false;
            }

            if (!Enum.IsDefined(typeof(ResourceBackendKind), m_backend))
            {
                error = $"池 {m_poolName} 的资源后端值无效：{m_backend}。";
                return false;
            }

            var key = ResourceKey;
            if (!key.IsValid)
            {
                error = $"池 {m_poolName} 的 ResourceKey 无效：{key}。";
                return false;
            }

            if (m_capacity == null)
            {
                error = $"池 {m_poolName} 的容量配置为空。";
                return false;
            }

            if (!m_capacity.TryValidate(out var capacityError))
            {
                error = $"池 {m_poolName} 的容量无效：{capacityError}";
                return false;
            }

            error = null;
            return true;
        }

        private static ResourceKey CreateResourceKey(ResourceBackendKind backend, string location)
        {
            return backend == ResourceBackendKind.Addressables
                ? ResourceKey.FromAddressables(location)
                : ResourceKey.FromResources(location);
        }
    }
}
