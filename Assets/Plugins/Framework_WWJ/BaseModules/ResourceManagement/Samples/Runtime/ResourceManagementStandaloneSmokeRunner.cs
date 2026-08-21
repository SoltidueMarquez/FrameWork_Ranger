using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework_WWJ.ResourceManagement.Samples
{
    /// <summary>
    /// Standalone 构建门禁入口；只有显式命令行参数存在时才运行，不影响正常游戏启动。
    /// </summary>
    internal static class ResourceManagementStandaloneSmokeRunner
    {
        private const string CommandLineFlag = "-frameworkWwjResourceSmoke";
        private const string AddressablesLocation =
            "framework-wwj/samples/resource-management/addressables-prefab";
        private const string ResourcesLocation =
            "Framework_WWJ/ResourceManagement/ResourcesSamplePrefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), CommandLineFlag) < 0)
            {
                return;
            }

            RunAsync().Forget();
        }

        private static async UniTaskVoid RunAsync()
        {
            ResourceLease<GameObject> resourcesLease = null;
            ResourceLease<GameObject> addressablesLease = null;
            GameObject resourcesInstance = null;
            GameObject addressablesInstance = null;
            var exitCode = 0;
            try
            {
                await Framework.WhenReadyAsync();
                var module = Framework.GetModule<ResourceModule>();
                resourcesLease = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromResources(ResourcesLocation));
                addressablesLease = await module.AcquireAsync<GameObject>(
                    ResourceKey.FromAddressables(AddressablesLocation));
                resourcesInstance = UnityEngine.Object.Instantiate(resourcesLease.Value);
                addressablesInstance = UnityEngine.Object.Instantiate(addressablesLease.Value);
                UnityEngine.Object.Destroy(resourcesInstance);
                UnityEngine.Object.Destroy(addressablesInstance);
                resourcesInstance = null;
                addressablesInstance = null;
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                resourcesLease.Dispose();
                resourcesLease = null;
                addressablesLease.Dispose();
                addressablesLease = null;
                Debug.Log("[Framework_WWJ][ResourceStandaloneSmoke] PASS 双后端 Acquire/Instantiate/Destroy/Release。");
            }
            catch (Exception exception)
            {
                exitCode = 1;
                Debug.LogError($"[Framework_WWJ][ResourceStandaloneSmoke] FAIL {exception}");
            }
            finally
            {
                if (resourcesInstance != null)
                {
                    UnityEngine.Object.Destroy(resourcesInstance);
                }

                if (addressablesInstance != null)
                {
                    UnityEngine.Object.Destroy(addressablesInstance);
                }

                resourcesLease?.Dispose();
                addressablesLease?.Dispose();
                try
                {
                    await Framework.ShutdownAsync();
                }
                catch (Exception shutdownException)
                {
                    exitCode = 1;
                    Debug.LogError(
                        $"[Framework_WWJ][ResourceStandaloneSmoke] Shutdown FAIL {shutdownException}");
                }

                Application.Quit(exitCode);
            }
        }
    }
}
