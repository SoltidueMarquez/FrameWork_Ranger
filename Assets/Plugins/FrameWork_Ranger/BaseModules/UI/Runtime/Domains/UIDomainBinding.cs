using System.Collections.Generic;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    /// <summary>借用场景相机/位姿目标；先于模块启用也可登记。禁用后撤销候选。</summary>
    [FrameworkArchitecture("UI 域绑定", "提供当前场景相机和世界位姿目标。", FrameworkArchitectureLayer.PublicFacade, 130)]
    public sealed class UIDomainBinding : MonoBehaviour
    {
        internal static readonly HashSet<UIDomainBinding> Candidates = new HashSet<UIDomainBinding>();
        public UIOwner Owner;
        public string DomainKey = "Main";
        public Camera Camera;
        public Transform WorldAnchor;
        [Tooltip("只用于持久对象给 Global 域供给绑定。")] public bool Persistent;
        private void OnEnable() { Candidates.Add(this); }
        private void OnDisable() { Candidates.Remove(this); }
        private void OnDestroy() { Candidates.Remove(this); }
        internal bool Matches(UIContext context, string key)
        {
            if (!this || !isActiveAndEnabled || Owner != context.Owner || DomainKey != key) return false;
            if (Persistent) return Owner == UIOwner.Global;
            var scope = context.Module.SceneInfo;
            return scope != null && scope.SceneHandle == gameObject.scene.handle.GetRawData();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCandidates() { Candidates.Clear(); }
    }
}
