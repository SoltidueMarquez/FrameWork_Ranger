using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework_WWJ.Samples
{
    /// <summary>
    /// 第一阶段最小可视化切片，展示状态、模块克隆和场景切换结果。
    /// </summary>
    public sealed class CoreSkeletonSampleView : MonoBehaviour
    {
        private const string SceneAName = "CoreSkeleton_A";
        private const string SceneBName = "CoreSkeleton_B";

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24f, 24f, 520f, 410f), GUI.skin.box);
            GUILayout.Label("Framework_WWJ Phase 1.1 中央启动示例", GUI.skin.label);
            GUILayout.Space(6f);
            GUILayout.Label($"Framework State: {Framework.State}");
            GUILayout.Label($"IsReady: {Framework.IsReady}");
            GUILayout.Label($"LastException: {Framework.LastException?.Message ?? "<none>"}");

            GUILayout.Space(8f);
            if (Framework.TryGetModule<SampleGlobalClockModule>(out var globalClock))
            {
                GUILayout.Label($"Global Clone ID: {globalClock.RuntimeInstanceId}");
                GUILayout.Label($"Global Time / Tick: {globalClock.ElapsedSeconds:F2}s / {globalClock.TickCount}");
            }

            if (Framework.TryGetModule<SampleSceneModule>(out var sceneModule))
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Scene Clone ID: {sceneModule.RuntimeInstanceId}");
                GUILayout.Label($"Scene Handler: {sceneModule.HandlerLabel}");
                GUILayout.Label($"Handler Type: {sceneModule.HandlerTypeName}");
                GUILayout.Label($"Tick / Value: {sceneModule.TickCount} / {sceneModule.AccumulatedValue:F2}");
                GUILayout.Label($"Observed Global Time: {sceneModule.ObservedGlobalTime:F2}s");
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("加载场景 A", GUILayout.Height(34f)))
            {
                SceneManager.LoadScene(SceneAName);
            }

            if (GUILayout.Button("加载场景 B", GUILayout.Height(34f)))
            {
                SceneManager.LoadScene(SceneBName);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Framework.ShutdownAsync", GUILayout.Height(30f)))
            {
                ObserveShutdownAsync().Forget();
            }

            GUILayout.EndArea();
        }

        private static async UniTaskVoid ObserveShutdownAsync()
        {
            try
            {
                await Framework.ShutdownAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
