using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace FrameWork_Ranger.UI.Samples
{
    /// <summary>示例通过同一个 UI 模块演示两种生命周期。需要将示例模块加入 Global 配置。</summary>
    public sealed class UISampleRunner : MonoBehaviour
    {
        public string OtherScenePath;
        public Camera SceneCamera;
        public EventSystem SceneEvents;
        private GlobalUIModule m_ui;
        private string m_error;
        private void Awake()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (!SceneCamera) SceneCamera = root.GetComponentInChildren<Camera>(true);
                if (!SceneEvents) SceneEvents = root.GetComponentInChildren<EventSystem>(true);
            }
        }
        private async void Start()
        {
            try
            {
                await Framework.WhenReadyAsync(this.GetCancellationTokenOnDestroy());
                m_ui = Framework.GetModule<GlobalUIModule>();
                while (!Framework.IsReady || m_ui.SceneInfo == null ||
                    m_ui.SceneInfo.SceneHandle != gameObject.scene.handle.GetRawData())
                    await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
                await m_ui.Global.OpenAsync("GlobalNotice", "GlobalNotice", this.GetCancellationTokenOnDestroy());
                await m_ui.Scene.OpenAsync("Inventory", "Inventory", this.GetCancellationTokenOnDestroy());
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception ex) { m_error = ex.Message; Debug.LogException(ex); }
        }
        private void OnGUI()
        {
            if (SceneManager.GetActiveScene() != gameObject.scene) return;
            GUILayout.BeginArea(new Rect(12, 12, 230, 300), GUI.skin.box);
            GUILayout.Label("Ranger UI / single Global module");
            if (m_error != null) GUILayout.Label(m_error);
            if (m_ui && m_ui.TryGetScene(out var ui))
            {
                if (GUILayout.Button("Inventory (mutex)")) ui.OpenAsync("Inventory", "Inventory").Forget();
                if (GUILayout.Button("Shop (mutex)")) ui.OpenAsync("Shop", "Shop").Forget();
                if (GUILayout.Button("Camera UI")) { ClearPanels(ui); ui.OpenAsync("CameraPanel", "CameraPanel").Forget(); }
                if (GUILayout.Button("World UI")) { ClearPanels(ui); ui.OpenAsync("WorldPanel", "WorldPanel").Forget(); }
                if (GUILayout.Button("Global modal"))
                    m_ui.Global.OpenAsync("GlobalModal", "GlobalModal").Forget();
                if (GUILayout.Button("Global World / keep across scenes"))
                    m_ui.Global.OpenAsync("GlobalWorld", "GlobalWorld").Forget();
                if (GUILayout.Button("Switch scene")) SceneManager.LoadSceneAsync(OtherScenePath);
            }
            GUILayout.EndArea();
        }
        private void Update()
        {
            bool active = SceneManager.GetActiveScene() == gameObject.scene;
            if (SceneCamera) SceneCamera.enabled = active;
            if (SceneEvents) SceneEvents.gameObject.SetActive(active);
        }
        private static void ClearPanels(UIContext ui)
        { foreach (var key in new[] { "Inventory", "Shop", "CameraPanel", "WorldPanel" }) ui.Close(key); }
    }
}
