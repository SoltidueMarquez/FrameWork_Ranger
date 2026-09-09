using System;
using System.IO;
using System.Linq;
using FrameWork_Ranger.UI.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Editor
{
    /// <summary>只新增扩展演示资产；不覆盖现有面板 Prefab 和场景。</summary>
    [FrameworkArchitecture("UI 扩展样例创建", "创建缺失组件演示与场景自动打开成功/失败样例。", FrameworkArchitectureLayer.EditorIntegration, 147)]
    public static class UIEvolutionSampleBuilder
    {
        public const string Root = UISampleAssetBuilder.Root;
        public const string PrefabPath = Root + "/Components.prefab";
        [MenuItem("Tools/FrameWork_Ranger/UI/创建扩展组件演示")]
        public static void Build()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("请先保存当前无标题场景，再创建扩展演示。");
            var module = AssetDatabase.LoadAssetAtPath<GlobalUIModule>(UISampleAssetBuilder.ModulePath);
            if (!module) throw new InvalidOperationException("请先创建基础 UI 演示资产。");
            var resources = UIResourceAssetTools.Extract(module);
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) CreatePrefab();
            foreach (var name in new[] { "UI_Components", "UI_OptionalFailure", "UI_RequiredFailure" }) CreateScene(name);
            AddPanel(resources, "OptionalFailure", "Camera", "UI_OptionalFailure", false);
            AddPanel(resources, "RequiredFailure", "Camera", "UI_RequiredFailure", true);
            if (!resources.Scene.Panels.Any(e => e.Panel && e.Panel.Definition.Key == "Components"))
            {
                var panel = ScriptableObject.CreateInstance<UIPanelConfig>();
                panel.Definition = new UIPanelDefinition { Key = "Components", UseDirectPrefab = true,
                    DirectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), Logic = new UIEvolutionLogic(),
                    EnterAnimation = new UIPanelAnimation { Fade = true, Scale = true, Slide = true, Duration = .25f },
                    ExitAnimation = new UIPanelAnimation { Fade = true, Slide = true, Duration = .2f } };
                AssetDatabase.CreateAsset(panel, AssetDatabase.GenerateUniqueAssetPath(Root + "/Scene_Components.asset"));
                var entry = new UIPanelEntry { Panel = panel, AutoOpen = true, Required = true, AllScenes = false };
                foreach (string name in new[] { "UI_Components", "UI_OptionalFailure" }) entry.Scenes.Add(SceneReference(name));
                resources.Scene.Panels.Add(entry);
            }
            resources.IsSample = true; EditorUtility.SetDirty(resources); AssetDatabase.SaveAssets();
            var errors = resources.Validate(); if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var name in new[] { "UI_Components", "UI_OptionalFailure", "UI_RequiredFailure" })
            { var path = Root + "/" + name + ".unity"; if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true)); }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Ranger UI] 已提取旧配置并创建扩展演示；现有面板 GUID 与业务场景保持原样。");
        }
        // CLI has no user-owned unsaved scene. Give additive generation a saved host scene.
        public static void BuildForValidation()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("此入口仅供隔离批处理验证。");
            EditorSceneManager.OpenScene(Root + "/UI_A.unity", OpenSceneMode.Single);
            var module = AssetDatabase.LoadAssetAtPath<GlobalUIModule>(UISampleAssetBuilder.ModulePath);
            string stablePath = Root + "/Demo_UIResources.asset";
            var stable = AssetDatabase.LoadAssetAtPath<UIResourcesConfig>(stablePath);
            if (stable) module.UIResources = stable;
            Build();
            if (!stable)
            {
                string error = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(module.UIResources), stablePath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            foreach (var directory in new[] { module.UIResources.Global, module.UIResources.Scene })
                foreach (var entry in directory.Panels)
                {
                    string oldPath = AssetDatabase.GetAssetPath(entry.Panel);
                    string newPath = Root + "/Demo_" + (directory == module.UIResources.Global ? "Global_" : "Scene_") + entry.Panel.Definition.Key + ".asset";
                    if (oldPath == newPath) continue;
                    string error = AssetDatabase.MoveAsset(oldPath, newPath);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                }
            EditorUtility.SetDirty(module); AssetDatabase.SaveAssets();
            // Explicit manifest excludes artifacts left by earlier isolated test runs.
            var paths = new System.Collections.Generic.HashSet<string> { UISampleAssetBuilder.ModulePath, stablePath, PrefabPath, Root + "/OutlineCircle.png" };
            foreach (var directory in new[] { module.UIResources.Global, module.UIResources.Scene })
                foreach (var entry in directory.Panels) paths.Add(AssetDatabase.GetAssetPath(entry.Panel));
            foreach (var name in new[] { "UI_Components", "UI_OptionalFailure", "UI_RequiredFailure" }) paths.Add(Root + "/" + name + ".unity");
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/UIEvolutionAssets.txt", paths.OrderBy(p => p));
        }
        private static UISceneReference SceneReference(string name)
        { string path = Root + "/" + name + ".unity"; return new UISceneReference { Path = path, Guid = AssetDatabase.AssetPathToGUID(path) }; }
        private static void AddPanel(UIResourcesConfig resources, string key, string domain, string scene, bool required)
        {
            if (resources.Scene.Panels.Any(e => e.Panel && e.Panel.Definition.Key == key)) return;
            var panel = ScriptableObject.CreateInstance<UIPanelConfig>();
            panel.Definition = new UIPanelDefinition { Key = key, DomainKey = domain, UseDirectPrefab = true, DirectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) };
            AssetDatabase.CreateAsset(panel, AssetDatabase.GenerateUniqueAssetPath(Root + "/Scene_" + key + ".asset"));
            var entry = new UIPanelEntry { Panel = panel, AllScenes = false, AutoOpen = true, Required = required }; entry.Scenes.Add(SceneReference(scene));
            resources.Scene.Panels.Add(entry);
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static Text Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize = 20)
        {
            var text = Rect(parent, value, position, size).gameObject.AddComponent<Text>(); text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize;
            text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
        }
        private static UIButton Button(Transform parent, string title, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, title, position, size); var image = rect.gameObject.AddComponent<UIImage>(); image.color = new Color(.13f, .4f, .52f);
            var button = rect.gameObject.AddComponent<UIButton>(); button.targetGraphic = image;
            Label(rect, title, Vector2.zero, size - new Vector2(12, 0)); return button;
        }
        private static void CreatePrefab()
        {
            var root = new GameObject("Components demonstration", typeof(RectTransform)); root.SetActive(false);
            try
            {
                var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(1000, 700);
                var view = root.AddComponent<UIEvolutionView>(); root.AddComponent<UIImage>().color = new Color(.06f, .10f, .16f);
                var header = Rect(rect, "Drag header", new Vector2(0, 305), new Vector2(1000, 90));
                header.gameObject.AddComponent<UIImage>().color = new Color(.12f, .28f, .36f);
                header.gameObject.AddComponent<UIWindowDrag>().Target = rect;
                Label(header, "RANGER UI / COMPONENTS     -     DRAG THIS HEADER", Vector2.zero, new Vector2(950, 60), 27);
                view.Gesture = Button(rect, "Click / double / hold / right", new Vector2(-245, 205), new Vector2(420, 48)); view.Gesture.EnableDoubleClick = true;
                view.Status = Label(rect, "Try the button", new Vector2(-245, 155), new Vector2(440, 40), 18);
                var toggleRect = Rect(rect, "Quiet capable toggle", new Vector2(-245, 100), new Vector2(420, 45));
                var toggleImage = toggleRect.gameObject.AddComponent<UIImage>(); var toggle = toggleRect.gameObject.AddComponent<UIToggle>(); toggle.transition = Selectable.Transition.None;
                toggle.targetGraphic = toggleImage; var visual = toggleRect.gameObject.AddComponent<UIControlVisual>(); visual.Control = toggle;
                foreach (var style in new[] { visual.Off.Normal, visual.Off.Hover, visual.Off.Pressed, visual.Off.Focused }) style.Colors.Add(new UIColorTarget { Target = toggleImage, Color = new Color(.22f,.26f,.34f) });
                foreach (var style in new[] { visual.On.Normal, visual.On.Hover, visual.On.Pressed, visual.On.Focused }) style.Colors.Add(new UIColorTarget { Target = toggleImage, Color = new Color(.1f,.6f,.4f) });
                Label(toggleRect, "Toggle / multi-target visual", Vector2.zero, new Vector2(400, 40));
                var shapes = Rect(rect, "Union outline", new Vector2(-245, -30), new Vector2(400, 150));
                var outline = shapes.gameObject.AddComponent<UIOutlineGroup>(); outline.Width = 5; outline.OutlineColor = new Color(.45f, .95f, .9f);
                var sprite = CreateCircle();
                for (int i = 0; i < 3; i++)
                {
                    var shape = Rect(shapes, "Image " + i, new Vector2(-60 + i * 60, i == 1 ? 15 : -10), new Vector2(110, 110));
                    var image = shape.gameObject.AddComponent<UIImage>(); image.sprite = sprite; image.raycastTarget = false;
                    var gradient = shape.gameObject.AddComponent<UIGradient>(); gradient.Angle = 90;
                }
                Label(rect, "Gradient + merged transparent outline", new Vector2(-245, -140), new Vector2(450, 42), 18);
                var viewport = Rect(rect, "Foldout viewport", new Vector2(250, -10), new Vector2(430, 480));
                viewport.gameObject.AddComponent<UIImage>().color = new Color(.035f,.065f,.10f); viewport.gameObject.AddComponent<RectMask2D>();
                var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.horizontal = false;
                var content = Rect(viewport, "Content", Vector2.zero, Vector2.zero); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1);
                var layout = content.gameObject.AddComponent<UIVerticalLayout>(); layout.ExpandWidth = true; layout.Spacing = 12; layout.padding = new RectOffset(12,12,12,12);
                content.gameObject.AddComponent<UIContentFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var accordion = content.gameObject.AddComponent<UIAccordionGroup>(); accordion.AllowMultiple = true; scroll.content = content; view.Scroll = scroll;
                for (int i = 0; i < 4; i++)
                {
                    var fold = Rect(content, "Foldout " + (i+1), Vector2.zero, new Vector2(400, 180));
                    var foldLayout = fold.gameObject.AddComponent<UIVerticalLayout>(); foldLayout.ExpandWidth = true;
                    var item = fold.gameObject.AddComponent<UIFoldout>(); item.Group = accordion;
                    var title = Button(fold, "Section " + (i+1) + " / click to fold", Vector2.zero, new Vector2(400, 44)); title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44; item.Header = title;
                    var body = Label(fold, "Reusable content\nLayout refresh keeps the list aligned", Vector2.zero, new Vector2(400, 120), 18);
                    body.gameObject.AddComponent<LayoutElement>().preferredHeight = 120; item.Content = body.gameObject;
                }
                view.ScrollBottom = Button(rect, "Smooth scroll to bottom", new Vector2(-245, -220), new Vector2(420, 48));
                view.CloseButton = Button(rect, "Close with animation", new Vector2(0, -300), new Vector2(420, 48));
                root.SetActive(true); PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static Sprite CreateCircle()
        {
            string path = Root + "/OutlineCircle.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64,64,TextureFormat.RGBA32,false);
                try
                {
                    var pixels = new Color[4096];
                    for (int y=0;y<64;y++) for (int x=0;x<64;x++) pixels[y*64+x] = new Color(1,1,1,Mathf.Clamp01(30.5f - Vector2.Distance(new Vector2(x,y), new Vector2(31.5f,31.5f))));
                    texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.isReadable = false; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void CreateScene(string name)
        {
            string path = Root + "/" + name + ".unity"; if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var camera = new GameObject("Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(camera, scene); camera.transform.position = new Vector3(0,0,-10);
                camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = new Color(.025f,.04f,.06f);
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); SceneManager.MoveGameObjectToScene(events, scene);
                var runner = new GameObject("UI evolution demo", typeof(UIEvolutionRunner)); SceneManager.MoveGameObjectToScene(runner, scene);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("无法保存：" + path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
