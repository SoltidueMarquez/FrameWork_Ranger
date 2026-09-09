using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
namespace FrameWork_Ranger.UI.Samples
{
    public sealed class UIEvolutionRunner : MonoBehaviour
    {
        private const string Root = "Assets/Plugins/FrameWork_Ranger/BaseModules/UI/Samples/Generated/";
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 330, 215), GUI.skin.box);
            GUILayout.Label("UI v2 demonstration / " + gameObject.scene.name);
            GUILayout.Label("Scene ready: " + Framework.IsReady);
            if (GUILayout.Button("Components / automatic open")) SceneManager.LoadSceneAsync(Root + "UI_Components.unity");
            if (GUILayout.Button("Optional failure / scene continues")) SceneManager.LoadSceneAsync(Root + "UI_OptionalFailure.unity");
            if (GUILayout.Button("Required failure / SceneScope rolls back")) SceneManager.LoadSceneAsync(Root + "UI_RequiredFailure.unity");
            if (Framework.IsReady && GUILayout.Button("Reopen components (animation)"))
                Framework.GetModule<GlobalUIModule>().Scene.OpenAsync("Components").Forget();
            GUILayout.Label("Drag panel header. Check Console for failure samples.");
            GUILayout.EndArea();
        }
    }
}
