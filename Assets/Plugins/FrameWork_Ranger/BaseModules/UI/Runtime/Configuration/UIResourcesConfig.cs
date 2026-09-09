using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [Serializable]
    public sealed class UISceneReference
    {
        public string Guid;
        public string Path;
    }
    [Serializable]
    public sealed class UIPanelEntry
    {
        public bool Enabled = true;
        public UIPanelConfig Panel;
        public bool AllScenes = true;
        public List<UISceneReference> Scenes = new List<UISceneReference>();
        public bool AutoOpen;
        public bool Required;
        public bool Allows(string path) => AllScenes || (Scenes != null && Scenes.Exists(s =>
            s != null && !string.IsNullOrEmpty(s.Path) && string.Equals(s.Path.Replace('\\', '/'), (path ?? "").Replace('\\', '/'), StringComparison.Ordinal)));
    }
    [Serializable]
    public sealed class UIResourceDirectory
    {
        public List<UIDomainDefinition> Domains = new List<UIDomainDefinition>();
        public List<UIPanelEntry> Panels = new List<UIPanelEntry>();
        internal UIConfiguration Snapshot(string scenePath, bool filter)
        {
            var result = new UIConfiguration();
            foreach (var domain in Domains)
                result.Domains.Add(domain == null ? null : (UIDomainDefinition)SerializationUtility.CreateCopy(domain));
            foreach (var entry in Panels)
                if (entry != null && entry.Enabled && entry.Panel && (!filter || entry.Allows(scenePath)))
                    result.Panels.Add((UIPanelDefinition)SerializationUtility.CreateCopy(entry.Panel.Definition));
            return result;
        }
    }
    [CreateAssetMenu(fileName = "UIResources", menuName = "FrameWork_Ranger/UI/资源总配置")]
    [FrameworkArchitecture("UI 资源总配置", "唯一模块指定的域、面板与场景规则目录。", FrameworkArchitectureLayer.Configuration, 130)]
    public sealed class UIResourcesConfig : ScriptableObject
    {
        public bool IsSample;
        public UIResourceDirectory Global = new UIResourceDirectory();
        public UIResourceDirectory Scene = new UIResourceDirectory();
        public List<string> Validate()
        {
            var errors = new List<string>();
            var orders = new HashSet<int>();
            foreach (var directory in new[] { Global, Scene })
            {
                if (directory?.Domains == null || directory.Panels == null)
                { errors.Add("UI 目录、域和面板列表不能为空。"); continue; }
                foreach (var entry in directory.Panels)
                {
                    if (entry == null) { errors.Add("面板目录中有空条目。"); continue; }
                    if (!entry.Enabled) continue;
                    if (!entry.Panel || entry.Panel.Definition == null) errors.Add("已启用条目缺少面板配置。");
                    if (directory == Scene && !entry.AllScenes && entry.Scenes != null)
                        foreach (var scene in entry.Scenes)
                            if (scene == null || string.IsNullOrWhiteSpace(scene.Path))
                                errors.Add("适用场景引用失效。");
                }
                errors.AddRange(directory.Snapshot(null, false).Validate());
                foreach (var domain in directory.Domains)
                    if (domain != null && domain.RenderMode == RenderMode.ScreenSpaceOverlay && !orders.Add(domain.SortingOrder))
                        errors.Add("全局与场景屏幕叠加域的显示顺序冲突：" + domain.SortingOrder);
            }
            return errors;
        }
    }
}
