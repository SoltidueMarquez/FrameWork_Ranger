using System;
using System.Collections.Generic;
using UnityEngine;
using FrameWork_Ranger.ResourceManagement;
namespace FrameWork_Ranger.UI
{
    /// <summary>只保存模板；运行时实例表属于 UIContext。</summary>
    [Serializable]
    [FrameworkArchitecture("UI 配置目录", "集中保存域与面板模板并校验。", FrameworkArchitectureLayer.Configuration, 130)]
    public sealed class UIConfiguration
    {
        public List<UIDomainDefinition> Domains = new List<UIDomainDefinition>();
        public List<UIPanelDefinition> Panels = new List<UIPanelDefinition>();

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Domains == null || Panels == null)
            { errors.Add("域和面板目录不能为 null。"); return errors; }
            var domains = new HashSet<string>(StringComparer.Ordinal);
            var panels = new HashSet<string>(StringComparer.Ordinal);
            foreach (var domain in Domains)
            {
                if (domain == null || string.IsNullOrWhiteSpace(domain.Key) || !domains.Add(domain.Key))
                { errors.Add("域为空或 key 重复/为空。"); continue; }
                if (!Enum.IsDefined(typeof(RenderMode), domain.RenderMode)) errors.Add(domain.Key + ": 渲染模式无效。");
                if (domain.SortingOrder < short.MinValue || domain.SortingOrder > short.MaxValue)
                    errors.Add(domain.Key + ": Canvas 排序必须在 -32768～32767 之间。");
                if (domain.ReferenceResolution.x <= 0 || domain.ReferenceResolution.y <= 0 ||
                    domain.WorldSize.x <= 0 || domain.WorldSize.y <= 0 || domain.PlaneDistance <= 0 || domain.WorldScale <= 0)
                    errors.Add(domain.Key + ": 尺寸/相机距离必须为正数。");
            }
            foreach (var panel in Panels)
            {
                if (panel == null || string.IsNullOrWhiteSpace(panel.Key) || !panels.Add(panel.Key))
                { errors.Add("面板为空或 key 重复/为空。"); continue; }
                if (panel.DomainKey == null || !domains.Contains(panel.DomainKey)) errors.Add(panel.Key + ": 域不存在。");
                if (panel.UseDirectPrefab ? !panel.DirectPrefab :
                    (!Enum.IsDefined(typeof(ResourceBackendKind), panel.Backend) || !panel.ResourceKey.IsValid))
                    errors.Add(panel.Key + ": Prefab 资源地址无效。");
                if (!Enum.IsDefined(typeof(UIClosePolicy), panel.ClosePolicy) ||
                    !Enum.IsDefined(typeof(UIModalScope), panel.Modal)) errors.Add(panel.Key + ": 行为策略无效。");
            }
            return errors;
        }
    }
}
