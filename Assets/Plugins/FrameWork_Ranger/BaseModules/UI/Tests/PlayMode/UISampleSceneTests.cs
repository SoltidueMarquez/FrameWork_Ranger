using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;
using FrameWork_Ranger.UI.Samples;
namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UISampleSceneTests
    {
        private const string Root = "Assets/Plugins/FrameWork_Ranger/BaseModules/UI/Samples/Generated/";
        private FrameworkProjectSettings m_settings;
        private Scene m_original, m_a, m_b;
        [UnitySetUp]
        public IEnumerator Setup() => UniTask.ToCoroutine(async () =>
        {
            await Framework.ShutdownAsync(); FrameworkBootstrap.ResetForTests(); await UniTask.NextFrame();
            m_original = SceneManager.GetActiveScene();
            m_settings = UnityEngine.Object.Instantiate(Resources.Load<FrameworkProjectSettings>(FrameworkProjectSettings.ResourcesLoadPath));
            m_settings.SetDefaultSceneConfig(null);
        });
        [UnityTearDown]
        public IEnumerator Teardown() => UniTask.ToCoroutine(async () =>
        {
            await Framework.ShutdownAsync(); FrameworkBootstrap.ResetForTests();
            if (m_original.IsValid() && m_original.isLoaded) SceneManager.SetActiveScene(m_original);
            if (m_a.IsValid() && m_a.isLoaded) await SceneManager.UnloadSceneAsync(m_a);
            if (m_b.IsValid() && m_b.isLoaded) await SceneManager.UnloadSceneAsync(m_b);
            UnityEngine.Object.Destroy(m_settings); await UniTask.NextFrame();
        });
        [UnityTest]
        public IEnumerator RequiredAutoOpenFailure_RollsBackActualSceneScope_PreservesGlobalModule() => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(Root + "UI_RequiredFailure.unity", LoadSceneMode.Additive);
            m_a = SceneManager.GetSceneByPath(Root + "UI_RequiredFailure.unity"); SceneManager.SetActiveScene(m_a);
            LogAssert.Expect(LogType.Error, new Regex("SceneScope 加载失败：必须成功的场景 UI 打开失败：RequiredFailure"));
            Exception failure = null;
            try { await Framework.StartProjectSceneAsync(m_settings, FrameworkSceneDescriptor.FromScene(m_a)); } catch (Exception ex) { failure = ex; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>()); Assert.That(Framework.IsReady, Is.False);
            var ui = Framework.GetModule<GlobalUIModule>(); Assert.That(ui.TryGetScene(out _), Is.False);
            Assert.That(ui.Global.IsAlive, Is.True); Assert.That(m_a.isLoaded, Is.True, "回滚 Scope 不擅自切换 Unity 场景");
        });
        [UnityTest]
        public IEnumerator OptionalAutoOpenFailure_ActualSceneStillBecomesReady() => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(Root + "UI_OptionalFailure.unity", LoadSceneMode.Additive);
            m_a = SceneManager.GetSceneByPath(Root + "UI_OptionalFailure.unity"); SceneManager.SetActiveScene(m_a);
            LogAssert.Expect(LogType.Error, new Regex("自动打开失败（继续场景启动）：OptionalFailure"));
            await Framework.StartProjectSceneAsync(m_settings, FrameworkSceneDescriptor.FromScene(m_a));
            Assert.That(Framework.IsReady, Is.True);
            Assert.That(Framework.GetModule<GlobalUIModule>().Scene.TryGet("Components", out _), Is.True);
        });
        [UnityTest]
        public IEnumerator ComponentsScene_AutoOpensDirectPrefab_AndRendersExpandedControls() => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(Root + "UI_Components.unity", LoadSceneMode.Additive);
            m_a = SceneManager.GetSceneByPath(Root + "UI_Components.unity"); SceneManager.SetActiveScene(m_a);
            await Framework.StartProjectSceneAsync(m_settings, FrameworkSceneDescriptor.FromScene(m_a));
            var ui = Framework.GetModule<GlobalUIModule>();
            Assert.That(ui.Scene.TryGet("Components", out var panel), Is.True);
            var view = panel.View as UIEvolutionView; Assert.That(view, Is.Not.Null);
            view.Gesture.onClick.Invoke(); Assert.That(view.Status.text, Is.EqualTo("Click 1"));
            UILayoutScheduler.FlushNow(view.Scroll.content);
            Assert.That(view.Scroll.content.rect.height, Is.GreaterThan(view.Scroll.viewport.rect.height));
            var outline = view.GetComponentInChildren<UIOutlineGroup>(); Assert.That(outline, Is.Not.Null);
            outline.RefreshNow(); await Capture("components");
            await ui.Scene.CloseAsync(panel); Assert.That(panel.IsOpen, Is.False);
            var reopened = await ui.Scene.OpenAsync("Components"); Assert.That(reopened.View, Is.SameAs(view));
        });
        [UnityTest]
        public IEnumerator RealScenes_ResourcesDomainsGlobalRebindAndSceneCleanup()
            => UniTask.ToCoroutine(async () =>
        {
            await SceneManager.LoadSceneAsync(Root + "UI_A.unity", LoadSceneMode.Additive);
            m_a = SceneManager.GetSceneByPath(Root + "UI_A.unity");
            SceneManager.SetActiveScene(m_a);
            await Framework.StartProjectSceneAsync(m_settings, FrameworkSceneDescriptor.FromScene(m_a));
            var ui = Framework.GetModule<GlobalUIModule>();
            await UniTask.DelayFrame(4);
            var sceneUI = ui.Scene;
            var panel = await sceneUI.OpenAsync("Inventory", "Inventory");
            Assert.That(panel.IsPresented, Is.True);
            var view = (UISampleView)panel.View;
            for (int i = 0; i < 9; i++) view.AddItem.onClick.Invoke();
            UILayoutScheduler.FlushNow(view.Scroll.content);
            Assert.That(view.Scroll.content.rect.width, Is.EqualTo(view.Scroll.viewport.rect.width).Within(0.1f));
            var last = (RectTransform)view.Scroll.content.GetChild(view.Scroll.content.childCount - 1);
            var visibleBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view.Scroll.viewport, last);
            Assert.That(visibleBounds.min.y, Is.GreaterThanOrEqualTo(view.Scroll.viewport.rect.yMin - 0.1f));
            Assert.That(visibleBounds.max.y, Is.LessThanOrEqualTo(view.Scroll.viewport.rect.yMax + 0.1f));
            await Capture("overlay");
            sceneUI.Close(panel);
            var cameraPanel = await sceneUI.OpenAsync("CameraPanel", "CameraPanel");
            Assert.That(cameraPanel.IsPresented, Is.True);
            await Capture("camera");
            sceneUI.Close(cameraPanel);
            var world = await sceneUI.OpenAsync("WorldPanel", "WorldPanel");
            var global = await ui.Global.OpenAsync("GlobalWorld", "GlobalWorld");
            Assert.That(world.IsPresented && global.IsPresented, Is.True);
            await Capture("world");
            await SceneManager.LoadSceneAsync(Root + "UI_B.unity", LoadSceneMode.Additive);
            m_b = SceneManager.GetSceneByPath(Root + "UI_B.unity");
            SceneManager.SetActiveScene(m_b);
            await Framework.WhenReadyAsync().Timeout(TimeSpan.FromSeconds(15));
            await UniTask.DelayFrame(4);
            Assert.That(m_a.isLoaded, Is.True, "旧 Additive 场景仍存在时也必须清理旧 SceneScope UI。");
            Assert.That(world.IsOpen, Is.False);
            Assert.That(sceneUI.IsAlive, Is.False);
            Assert.That(global.IsOpen && global.IsPresented, Is.True);
            Assert.That(ui.Global.TryGet("GlobalWorld", out var restored), Is.True);
            Assert.That(restored.View, Is.SameAs(global.View));
            await SceneManager.UnloadSceneAsync(m_a);
            await UniTask.NextFrame();
            Assert.That(ui.Scene.IsAlive, Is.True, "迟到卸载不得清理新所有者。");
        });
        private static async UniTask Capture(string name)
        {
            var directory = Path.GetFullPath("Logs/UIVisuals"); Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name + "-preview.png");
            await UniTask.DelayFrame(3);
            Camera camera = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (!camera) camera = root.GetComponentInChildren<Camera>();
            Assert.That(camera, Is.Not.Null);
            var texture = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var overlays = new List<(Canvas canvas, Camera camera, float distance)>();
            try
            {
                camera.targetTexture = texture;
                // batchmode 不提供 GameView 截屏；Overlay 暂借相机生成离屏预览，随后还原。
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        overlays.Add((canvas, canvas.worldCamera, canvas.planeDistance));
                        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                    }
                Canvas.ForceUpdateCanvases();
                if (GraphicsSettings.currentRenderPipeline)
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                else camera.Render();
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                foreach (var item in overlays)
                { item.canvas.renderMode = RenderMode.ScreenSpaceOverlay; item.canvas.worldCamera = item.camera; item.canvas.planeDistance = item.distance; }
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                texture.Release(); UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(pixels);
                Canvas.ForceUpdateCanvases();
            }
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(1000));
        }
    }
}
