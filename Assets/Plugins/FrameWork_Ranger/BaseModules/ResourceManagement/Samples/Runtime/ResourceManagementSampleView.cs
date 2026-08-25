using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FrameWork_Ranger.ResourceManagement.Samples
{
    /// <summary>
    /// 使用同一 ResourceModule 分别验收 Addressables 与 Unity Resources Prefab。
    /// </summary>
    public sealed class ResourceManagementSampleView : MonoBehaviour
    {
        private const string AddressablesLocation =
            "framework-ranger/samples/resource-management/addressables-prefab";
        private const string ResourcesLocation =
            "FrameWork_Ranger/ResourceManagement/ResourcesSamplePrefab";

        private CancellationTokenSource m_lifetime;
        private ResourceLease<GameObject> m_addressablesLease;
        private ResourceLease<GameObject> m_resourcesLease;
        private GameObject m_addressablesInstance;
        private GameObject m_resourcesInstance;
        private string m_addressablesStatus = "未加载";
        private string m_resourcesStatus = "未加载";

        private void OnEnable()
        {
            m_lifetime = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            m_lifetime?.Cancel();
            m_lifetime?.Dispose();
            m_lifetime = null;
            ReleaseAddressables();
            ReleaseResources();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24f, 24f, 620f, 420f), GUI.skin.box);
            GUILayout.Label("FrameWork_Ranger Resource Management 双后端示例");
            GUILayout.Label($"Framework State: {Framework.State} / Ready: {Framework.IsReady}");
            GUILayout.Space(8f);
            DrawBackend(
                "Addressables",
                AddressablesLocation,
                m_addressablesStatus,
                m_addressablesLease?.IsValid == true,
                AcquireAddressables,
                ReleaseAddressables);
            GUILayout.Space(10f);
            DrawBackend(
                "Unity Resources",
                ResourcesLocation,
                m_resourcesStatus,
                m_resourcesLease?.IsValid == true,
                AcquireResources,
                ReleaseResources);
            GUILayout.Space(12f);
            GUILayout.Label("释放顺序：先销毁场景实例，再归还 Prefab Lease。" );
            GUILayout.Label("Resources 归零只解除框架引用，不触发全局 UnloadUnusedAssets。" );
            GUILayout.EndArea();
        }

        private static void DrawBackend(
            string title,
            string location,
            string status,
            bool loaded,
            Action acquire,
            Action release)
        {
            GUILayout.Label(title, GUI.skin.box);
            GUILayout.Label($"Location: {location}");
            GUILayout.Label($"Status: {status}");
            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = Framework.IsReady && !loaded;
                if (GUILayout.Button("Acquire + Instantiate", GUILayout.Height(32f)))
                {
                    acquire();
                }

                GUI.enabled = loaded;
                if (GUILayout.Button("Destroy + Release", GUILayout.Height(32f)))
                {
                    release();
                }

                GUI.enabled = true;
            }

        }

        private void AcquireAddressables()
        {
            AcquireAddressablesAsync().Forget();
        }

        private async UniTaskVoid AcquireAddressablesAsync()
        {
            if (!Framework.TryGetModule<ResourceModule>(out var module))
            {
                m_addressablesStatus = "ResourceModule 不可用";
                return;
            }

            m_addressablesStatus = "加载中";
            try
            {
                m_addressablesLease = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromAddressables(AddressablesLocation),
                    m_lifetime.Token);
                m_addressablesInstance = Instantiate(
                    m_addressablesLease.Value,
                    new Vector3(-1.5f, 0f, 0f),
                    Quaternion.identity);
                m_addressablesStatus = "已加载，Lease 有效";
            }
            catch (Exception exception)
            {
                m_addressablesStatus = exception.Message;
                Debug.LogException(exception);
            }
        }

        private void AcquireResources()
        {
            AcquireResourcesAsync().Forget();
        }

        private async UniTaskVoid AcquireResourcesAsync()
        {
            if (!Framework.TryGetModule<ResourceModule>(out var module))
            {
                m_resourcesStatus = "ResourceModule 不可用";
                return;
            }

            m_resourcesStatus = "加载中";
            try
            {
                m_resourcesLease = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromResources(ResourcesLocation),
                    m_lifetime.Token);
                m_resourcesInstance = Instantiate(
                    m_resourcesLease.Value,
                    new Vector3(1.5f, 0f, 0f),
                    Quaternion.identity);
                m_resourcesStatus = "已加载，Lease 有效";
            }
            catch (Exception exception)
            {
                m_resourcesStatus = exception.Message;
                Debug.LogException(exception);
            }
        }

        private void ReleaseAddressables()
        {
            if (m_addressablesInstance != null)
            {
                Destroy(m_addressablesInstance);
                m_addressablesInstance = null;
            }

            m_addressablesLease?.Dispose();
            m_addressablesLease = null;
            m_addressablesStatus = "已释放";
        }

        private void ReleaseResources()
        {
            if (m_resourcesInstance != null)
            {
                Destroy(m_resourcesInstance);
                m_resourcesInstance = null;
            }

            m_resourcesLease?.Dispose();
            m_resourcesLease = null;
            m_resourcesStatus = "已释放";
        }
    }
}
