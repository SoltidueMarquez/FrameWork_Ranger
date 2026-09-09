using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace FrameWork_Ranger.UI
{
    /// <summary>仅操作 UI 自有的动画节点，不改业务 View 的布局/拖动坐标。</summary>
    internal static class UIPanelPresentation
    {
        internal static async UniTask PlayAsync(UIPanelRecord record, UIPanelAnimation animation, bool enter, CancellationToken token)
        {
            var rect = record.Presentation;
            if (!rect) return;
            var group = rect.GetComponent<CanvasGroup>();
            float duration = animation?.Enabled == true ? animation.Duration : 0;
            float elapsed = 0;
            do
            {
                token.ThrowIfCancellationRequested();
                if (!rect) return;
                float t = duration <= 0 || !record.Domain.IsPresented ? 1 : Mathf.Clamp01(elapsed / duration);
                float eased = t >= 1 ? 1 : animation?.Curve?.Evaluate(t) ?? t;
                float visible = enter ? eased : 1 - eased;
                group.alpha = animation?.Fade == true ? Mathf.Clamp01(visible) : 1;
                rect.localScale = animation?.Scale == true ? Vector3.LerpUnclamped(animation.HiddenScale, Vector3.one, visible) : Vector3.one;
                rect.anchoredPosition = animation?.Slide == true ? Vector2.LerpUnclamped(animation.Offset, Vector2.zero, visible) : Vector2.zero;
                if (t >= 1) break;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.unscaledDeltaTime;
            } while (true);
        }
    }
}
