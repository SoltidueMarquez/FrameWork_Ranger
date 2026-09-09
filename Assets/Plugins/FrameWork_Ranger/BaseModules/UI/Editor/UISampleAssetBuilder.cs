using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrameWork_Ranger.ResourceManagement;
using FrameWork_Ranger.ResourceManagement.UnityResources;
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
    /// <summary>只创建缺失示例资产，向既有 Global 配置追加模块，保留其他模块与场景映射。</summary>
    [FrameworkArchitecture("UI 样例创建", "创建缺失的配置和双场景示例资产。", FrameworkArchitectureLayer.EditorIntegration, 135)]
    public static class UISampleAssetBuilder
    {
        public const string Root = "Assets/Plugins/FrameWork_Ranger/BaseModules/UI/Samples/Generated";
        public const string PrefabPath = Root + "/Resources/RangerUI/SamplePanel.prefab";
        public const string ModulePath = Root + "/GlobalUIModule.asset";
        [MenuItem("Tools/FrameWork_Ranger/UI/Create Sample Assets")]
        public static void Build()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("请先保存当前无标题场景，再创建 UI 示例场景。");
            Directory.CreateDirectory(Root + "/Resources/RangerUI");
            AssetDatabase.Refresh();
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) CreatePrefab();
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Resources/RangerUI/Notice.prefab")) CreateNotice();
            var ui = AssetDatabase.LoadAssetAtPath<GlobalUIModule>(ModulePath);
            if (!ui)
            {
                ui = ScriptableObject.CreateInstance<GlobalUIModule>();
                AddDomain(ui.GlobalConfiguration, "Main", RenderMode.ScreenSpaceOverlay, 100);
                AddDomain(ui.GlobalConfiguration, "World", RenderMode.WorldSpace, 0);
                AddDomain(ui.SceneConfiguration, "Main", RenderMode.ScreenSpaceOverlay, 0);
                AddDomain(ui.SceneConfiguration, "Camera", RenderMode.ScreenSpaceCamera, 0);
                AddDomain(ui.SceneConfiguration, "World", RenderMode.WorldSpace, 1);
                AddPanel(ui.GlobalConfiguration, "GlobalNotice", "Main");
                ui.GlobalConfiguration.Panels[0].PrefabAddress = "RangerUI/Notice";
                ui.GlobalConfiguration.Panels[0].Logic = new UILogic();
                AddPanel(ui.GlobalConfiguration, "GlobalModal", "Main", UIModalScope.Global);
                AddPanel(ui.GlobalConfiguration, "GlobalWorld", "World");
                AddPanel(ui.SceneConfiguration, "Inventory", "Main", group: "Window");
                AddPanel(ui.SceneConfiguration, "Shop", "Main", group: "Window");
                AddPanel(ui.SceneConfiguration, "Confirm", "Main", UIModalScope.Domain);
                AddPanel(ui.SceneConfiguration, "CameraPanel", "Camera");
                AddPanel(ui.SceneConfiguration, "WorldPanel", "World");
                AssetDatabase.CreateAsset(ui, ModulePath);
            }
            EnsurePanel(ui.GlobalConfiguration, "Confirm", "Main");
            EnsurePanel(ui.GlobalConfiguration, "WorldConfirm", "World");
            EnsurePanel(ui.SceneConfiguration, "CameraConfirm", "Camera");
            EnsurePanel(ui.SceneConfiguration, "WorldConfirm", "World");
            EditorUtility.SetDirty(ui);
            if (!ui.UIResources) UIResourceAssetTools.Extract(ui);
            Install(ui);
            CreateScene("UI_A", "UI_B", new Color(0.045f, 0.065f, 0.095f));
            CreateScene("UI_B", "UI_A", new Color(0.06f, 0.10f, 0.08f));
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var name in new[] { "UI_A", "UI_B" })
            {
                var path = Root + "/" + name + ".unity";
                if (!scenes.Any(scene => scene.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        private static void AddDomain(UIConfiguration config, string key, RenderMode mode, int sorting)
        {
            config.Domains.Add(new UIDomainDefinition { Key = key, RenderMode = mode, SortingOrder = sorting,
                PlaneDistance = 5, WorldSize = new Vector2(640, 420), WorldScale = 0.005f });
        }
        private static void AddPanel(UIConfiguration config, string key, string domain,
            UIModalScope modal = UIModalScope.None, string group = null)
        {
            config.Panels.Add(new UIPanelDefinition { Key = key, DomainKey = domain,
                PrefabAddress = "RangerUI/SamplePanel", Logic = new UISampleLogic(),
                Modal = modal, MutexGroup = group, Layer = key == "Confirm" || modal == UIModalScope.Global ? 10 : 0 });
        }
        private static void EnsurePanel(UIConfiguration config, string key, string domain)
        { if (!config.Panels.Any(panel => panel.Key == key)) AddPanel(config, key, domain, UIModalScope.Domain); }
        private static void Install(GlobalUIModule ui)
        {
            var settings = Resources.Load<FrameworkProjectSettings>(FrameworkProjectSettings.ResourcesLoadPath);
            if (!settings || !settings.GlobalConfig)
                throw new InvalidOperationException("请先通过 Framework Center 设置 GlobalConfig，再创建 UI 示例。");
            var global = settings.GlobalConfig;
            var entries = global.Modules.ToList();
            if (!entries.Any(entry => entry.Module is ResourceModule))
            {
                var resource = ScriptableObject.CreateInstance<ResourceModule>();
                var handler = new ResourceHandler();
                handler.SetProviders(new ResourceProviderBase[] { new UnityResourcesProvider() });
                resource.SetHandler(handler);
                AssetDatabase.CreateAsset(resource, AssetDatabase.GenerateUniqueAssetPath(Root + "/ResourceModule.asset"));
                entries.Add(new ModuleConfigEntry(true, resource));
            }
            var existing = entries.Select(entry => entry.Module).OfType<GlobalUIModule>().FirstOrDefault();
            if (existing && existing != ui)
                throw new InvalidOperationException("已有其他 UI 模块配置；示例资产已创建，请将所需示例面板手动合入现有目录。");
            if (!existing) { entries.Add(new ModuleConfigEntry(true, ui)); global.SetModules(entries); EditorUtility.SetDirty(global); }
        }
        private static void CreatePrefab()
        {
            var root = new GameObject("Sample Panel", typeof(RectTransform));
            root.SetActive(false);
            try
            {
                var view = root.AddComponent<UISampleView>();
                ((RectTransform)root.transform).sizeDelta = new Vector2(640, 420);
                root.AddComponent<Image>().color = new Color(0.08f, 0.105f, 0.16f, 0.98f);
                view.Title = Label(root.transform, "Title", "RANGER UI", new Vector2(0, 168), new Vector2(580, 44), 28);
                view.AddItem = Button(root.transform, "Add item", new Vector2(-195, 110));
                view.OpenConfirm = Button(root.transform, "Confirm", new Vector2(0, 110));
                view.ClosePanel = Button(root.transform, "Close", new Vector2(195, 110));
                var viewport = Rect(root.transform, "Viewport", new Vector2(0, -55), new Vector2(580, 260));
                viewport.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f);
                viewport.gameObject.AddComponent<RectMask2D>();
                var content = Rect(viewport, "Content", Vector2.zero, new Vector2(580, 0));
                content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
                content.sizeDelta = Vector2.zero;
                var layout = content.gameObject.AddComponent<UIVerticalLayout>();
                layout.ExpandWidth = true; layout.Spacing = 8;
                content.gameObject.AddComponent<UIContentFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
                view.Scroll = scroll;
                var item = Rect(root.transform, "Item Template", Vector2.zero, new Vector2(580, 48));
                item.gameObject.SetActive(false);
                item.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.19f, 0.27f);
                item.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
                view.ItemTemplate = item.gameObject.AddComponent<UISampleItemView>();
                view.ItemTemplate.Label = Label(item, "Label", "Item", Vector2.zero, new Vector2(540, 40), 18);
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void CreateNotice()
        {
            var root = new GameObject("Global Notice", typeof(RectTransform), typeof(UIView), typeof(Image));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-20, -20); rect.sizeDelta = new Vector2(320, 52);
                root.GetComponent<Image>().color = new Color(0.08f, 0.38f, 0.3f);
                var label = Label(rect, "Label", "GLOBAL / survives scene changes", Vector2.zero, new Vector2(300, 44), 17);
                label.alignment = TextAnchor.MiddleCenter;
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Resources/RangerUI/Notice.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = fontSize; label.color = Color.white; label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false; return label;
        }
        private static Button Button(Transform parent, string text, Vector2 position)
        {
            var rect = Rect(parent, text, position, new Vector2(175, 40));
            rect.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.40f, 0.60f);
            var button = rect.gameObject.AddComponent<UIButton>();
            var label = Label(rect, "Label", text, Vector2.zero, new Vector2(150, 36), 18);
            label.alignment = TextAnchor.MiddleCenter; return button;
        }
        private static void CreateScene(string name, string other, Color background)
        {
            var path = Root + "/" + name + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cameraObject = new GameObject("UI Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 4;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = background;
                var anchor = new GameObject("World Anchor");
                SceneManager.MoveGameObjectToScene(anchor, scene);
                anchor.transform.position = new Vector3(2, 0, 0);
                foreach (var owner in new[] { UIOwner.Global, UIOwner.Scene })
                {
                    var obj = new GameObject(owner + " World Binding");
                    SceneManager.MoveGameObjectToScene(obj, scene);
                    var binding = obj.AddComponent<UIDomainBinding>();
                    binding.Owner = owner; binding.DomainKey = "World"; binding.Camera = camera; binding.WorldAnchor = anchor.transform;
                    if (owner == UIOwner.Global)
                    {
                        var globalAnchor = new GameObject("Global World Anchor");
                        SceneManager.MoveGameObjectToScene(globalAnchor, scene);
                        globalAnchor.transform.position = new Vector3(-2, 0, 0);
                        binding.WorldAnchor = globalAnchor.transform;
                    }
                }
                var cameraBinding = cameraObject.AddComponent<UIDomainBinding>();
                cameraBinding.Owner = UIOwner.Scene; cameraBinding.DomainKey = "Camera"; cameraBinding.Camera = camera;
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
                var runner = new GameObject("UI Sample", typeof(UISampleRunner));
                SceneManager.MoveGameObjectToScene(runner, scene);
                runner.GetComponent<UISampleRunner>().OtherScenePath = Root + "/" + other + ".unity";
                runner.GetComponent<UISampleRunner>().SceneCamera = camera;
                runner.GetComponent<UISampleRunner>().SceneEvents = eventSystem.GetComponent<EventSystem>();
                if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("无法保存 UI 示例场景：" + path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
