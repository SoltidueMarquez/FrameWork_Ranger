using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FrameWork_Ranger.ResourceManagement;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    [FrameworkArchitecture("UI 实例记录", "持有实例、关系、输入和打开轮次状态。", FrameworkArchitectureLayer.RuntimeDriving, 135)]
    internal sealed class UIPanelRecord
    {
        internal UIContext Context;
        internal UIDomain Domain;
        internal UIPanelDefinition Definition;
        internal GameObject Host;
        internal UIView View;
        internal UILogic Logic;
        internal CanvasGroup Gate;
        internal RectTransform Presentation;
        internal CancellationTokenSource AnimationCancellation;
        internal UniTaskCompletionSource CloseCompletion;
        internal UniTask Retirement;
        internal object OpeningOperation;
        internal bool Entering, CloseFinished, ForceClose;
        internal int CloseVersion;
        internal UIPrefabLease Lease;
        internal UIPanelRecord Parent;
        internal readonly List<UIPanelRecord> Children = new List<UIPanelRecord>();
        internal bool Open, Closing, Disposed, InputAllowed, Focused;
        internal long Generation, Sequence;
        internal GameObject LastSelection;
        internal UIPanelHandle Handle => new UIPanelHandle(this);
    }
}
