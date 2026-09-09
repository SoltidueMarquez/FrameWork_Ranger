using System;
using FrameWork_Ranger.ResourceManagement;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [Serializable]
    [FrameworkArchitecture("面板定义", "配置资源、逻辑、关闭策略及关系。", FrameworkArchitectureLayer.Configuration, 130)]
    public sealed class UIPanelDefinition
    {
        public string Key;
        public string DomainKey = "Main";
        public int Layer;
        public ResourceBackendKind Backend = ResourceBackendKind.UnityResources;
        public string PrefabAddress;
        public bool UseDirectPrefab;
        public GameObject DirectPrefab;
        public UIPanelAnimation EnterAnimation = new UIPanelAnimation();
        public UIPanelAnimation ExitAnimation = new UIPanelAnimation();
        [SerializeReference] public UILogic Logic = new UILogic();
        public UIClosePolicy ClosePolicy;
        [Tooltip("空值表示并存；同域同组面板互斥。")] public string MutexGroup;
        public UIModalScope Modal;
        public ResourceKey ResourceKey => Backend == ResourceBackendKind.UnityResources
            ? ResourceKey.FromResources(PrefabAddress) : ResourceKey.FromAddressables(PrefabAddress);
    }
}
