using FrameWork_Ranger.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UIEditorCreationTests
    {
        [Test]
        public void SampleAssets_AreCreatedWithValidViewsAndConfig()
        {
            // CLI 创建的无标题测试场景先保存为临时宿主；不替 GUI 用户保存或关闭其场景。
            string hostPath = null;
            var current = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(current.path))
            {
                if (!Application.isBatchMode) Assert.Ignore("请先保存当前无标题场景后运行示例资产检查。");
                System.IO.Directory.CreateDirectory(UISampleAssetBuilder.Root);
                AssetDatabase.Refresh();
                hostPath = AssetDatabase.GenerateUniqueAssetPath(UISampleAssetBuilder.Root + "/TestHost.unity");
                EditorSceneManager.SaveScene(current, hostPath);
            }
            try { UISampleAssetBuilder.Build(); }
            finally
            {
                if (hostPath != null)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    AssetDatabase.DeleteAsset(hostPath);
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UISampleAssetBuilder.PrefabPath);
            CollectionAssert.IsEmpty(UIEditorTools.ValidatePrefab(prefab));
            var module = AssetDatabase.LoadAssetAtPath<GlobalUIModule>(UISampleAssetBuilder.ModulePath);
            CollectionAssert.IsEmpty(module.ValidateConfiguration());
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(UISampleAssetBuilder.Root + "/UI_A.unity"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(UISampleAssetBuilder.Root + "/UI_B.unity"), Is.Not.Null);
        }
    }
}
