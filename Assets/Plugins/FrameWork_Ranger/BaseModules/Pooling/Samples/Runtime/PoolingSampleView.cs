using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Samples
{
    /// <summary>
    /// 独立 Pooling Sample 场景的双后端 GameObject 池与引用池交互面板。
    /// </summary>
    public sealed class PoolingSampleView : MonoBehaviour
    {
        private const string ResourcesPool = "Resources Prefab";
        private const string AddressablesPool = "Addressables Prefab";

        private GameObject m_resourcesInstance;
        private GameObject m_addressablesInstance;
        private PoolingSampleReferencePayload m_payload;
        private int m_sequence;

        private void OnDestroy()
        {
            if (Framework.TryGetModule<GameObjectPoolModule>(out var gameObjectPool))
            {
                gameObjectPool.TryDespawn(ResourcesPool, m_resourcesInstance);
                gameObjectPool.TryDespawn(AddressablesPool, m_addressablesInstance);
            }

            if (Framework.TryGetModule<ReferencePoolModule>(out var referencePool))
            {
                referencePool.TryReturn(m_payload);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24f, 24f, 660f, 480f), GUI.skin.box);
            GUILayout.Label("FrameWork_Ranger Pooling 双 Module 示例");
            GUILayout.Label($"Framework State: {Framework.State} / Ready: {Framework.IsReady}");
            GUILayout.Space(8f);
            DrawGameObjectPool(
                ResourcesPool,
                ref m_resourcesInstance,
                new Vector3(-1.5f, 0f, 0f));
            GUILayout.Space(8f);
            DrawGameObjectPool(
                AddressablesPool,
                ref m_addressablesInstance,
                new Vector3(1.5f, 0f, 0f));
            GUILayout.Space(12f);
            GUILayout.Label("Global Reference Pool", GUI.skin.box);
            GUILayout.Label(m_payload == null
                ? "当前没有借出载荷"
                : $"Sequence={m_payload.Sequence}, Message={m_payload.Message}, RentCount={m_payload.RentCount}");
            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = Framework.IsReady && m_payload == null;
                if (GUILayout.Button("Rent 事件载荷", GUILayout.Height(32f)))
                {
                    var pool = Framework.GetModule<ReferencePoolModule>();
                    m_payload = pool.Rent<PoolingSampleReferencePayload>();
                    m_payload.Sequence = ++m_sequence;
                    m_payload.Message = "未来 EventCenter payload";
                }

                GUI.enabled = m_payload != null;
                if (GUILayout.Button("Return 并重置", GUILayout.Height(32f)))
                {
                    Framework.GetModule<ReferencePoolModule>().Return(m_payload);
                    m_payload = null;
                }

                GUI.enabled = true;
            }

            GUILayout.Space(12f);
            GUILayout.Label("GameObject 池随场景卸载；Reference 池跨场景保持。" );
            GUILayout.Label("Prefab Lease 在整个 SceneScope 生命周期持有，缩容到零也不会释放模板。" );
            GUILayout.EndArea();
        }

        private static void DrawGameObjectPool(
            string poolName,
            ref GameObject instance,
            Vector3 position)
        {
            GUILayout.Label(poolName, GUI.skin.box);
            GUILayout.Label(instance == null ? "空闲" : "已借出并激活");
            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = Framework.IsReady && instance == null;
                if (GUILayout.Button("Spawn", GUILayout.Height(32f)))
                {
                    instance = Framework.GetModule<GameObjectPoolModule>().Spawn(
                        poolName,
                        position,
                        Quaternion.identity);
                }

                GUI.enabled = instance != null;
                if (GUILayout.Button("Despawn", GUILayout.Height(32f)))
                {
                    Framework.GetModule<GameObjectPoolModule>().Despawn(poolName, instance);
                    instance = null;
                }

                GUI.enabled = true;
            }
        }
    }
}
