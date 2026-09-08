using FrameWork_Ranger.Pooling.GameObjects;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Samples
{
    /// <summary>
    /// Sample Prefab 的池回调组件；用颜色和计数直观展示复用而非重复实例化。
    /// </summary>
    public sealed class PoolingSampleCallbacks : MonoBehaviour, IGameObjectPoolCallbacks
    {
        [SerializeField]
        private string m_label = "Pool Item";

        [SerializeField]
        private Color m_spawnedColor = new Color(0.2f, 0.8f, 0.4f, 1f);

        private Renderer m_renderer;

        public int SpawnCount { get; private set; }

        public int DespawnCount { get; private set; }

        public string Label => m_label;

        public void ConfigureSample(string label, Color spawnedColor)
        {
            m_label = label;
            m_spawnedColor = spawnedColor;
        }

        private void Awake()
        {
            m_renderer = GetComponent<Renderer>();
        }

        public void OnSpawned()
        {
            SpawnCount++;
            if (m_renderer != null)
            {
                m_renderer.material.color = m_spawnedColor;
            }
        }

        public void OnDespawned()
        {
            DespawnCount++;
        }
    }
}
