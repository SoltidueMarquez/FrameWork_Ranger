using System;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.Pooling.GameObjects;
using FrameWork_Ranger.Pooling.Reference;
using UnityEngine;

namespace FrameWork_Ranger.Pooling.Samples
{
    /// <summary>
    /// Standalone 双后端 Pooling 门禁；仅在显式命令行参数存在时执行。
    /// </summary>
    internal static class PoolingStandaloneSmokeRunner
    {
        private const string CommandLineFlag = "-frameworkRangerPoolingSmoke";

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
            GameObject resources = null;
            GameObject addressables = null;
            PoolingSampleReferencePayload payload = null;
            var exitCode = 0;
            try
            {
                await Framework.WhenReadyAsync();
                var gameObjects = Framework.GetModule<GameObjectPoolModule>();
                var references = Framework.GetModule<ReferencePoolModule>();
                resources = gameObjects.Spawn("Resources Prefab", new Vector3(-1f, 0f, 0f), Quaternion.identity);
                addressables = gameObjects.Spawn("Addressables Prefab", new Vector3(1f, 0f, 0f), Quaternion.identity);
                payload = references.Rent<PoolingSampleReferencePayload>();
                payload.Sequence = 1;
                payload.Message = "smoke";
                gameObjects.Despawn("Addressables Prefab", addressables);
                addressables = null;
                gameObjects.Despawn("Resources Prefab", resources);
                resources = null;
                references.Return(payload);
                payload = null;
                Debug.Log(
                    "[FrameWork_Ranger][PoolingStandaloneSmoke] PASS Resources/Addressables Spawn/Despawn + Reference Rent/Return。" );
            }
            catch (Exception exception)
            {
                exitCode = 1;
                Debug.LogError($"[FrameWork_Ranger][PoolingStandaloneSmoke] FAIL {exception}");
            }
            finally
            {
                if (Framework.TryGetModule<GameObjectPoolModule>(out var gameObjects))
                {
                    gameObjects.TryDespawn("Resources Prefab", resources);
                    gameObjects.TryDespawn("Addressables Prefab", addressables);
                }

                if (Framework.TryGetModule<ReferencePoolModule>(out var references))
                {
                    references.TryReturn(payload);
                }

                try
                {
                    await Framework.ShutdownAsync();
                }
                catch (Exception shutdownException)
                {
                    exitCode = 1;
                    Debug.LogError(
                        $"[FrameWork_Ranger][PoolingStandaloneSmoke] Shutdown FAIL {shutdownException}");
                }

                Application.Quit(exitCode);
            }
        }
    }
}
