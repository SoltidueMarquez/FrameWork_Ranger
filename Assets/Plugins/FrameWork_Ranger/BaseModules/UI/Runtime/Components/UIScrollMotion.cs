using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>只在调用平滑定位时附加；负责新请求、拖动与禁用时的取消。</summary>
    [AddComponentMenu("")]
    public sealed class UIScrollMotion : MonoBehaviour, IBeginDragHandler
    {
        private CancellationTokenSource m_cancel;
        internal async UniTask MoveAsync(ScrollRect scroll, Vector2 destination, float duration, CancellationToken token)
        {
            Cancel();
            var source = CancellationTokenSource.CreateLinkedTokenSource(token);
            m_cancel = source;
            try
            {
                var origin = scroll.normalizedPosition; float elapsed = 0;
                scroll.StopMovement();
                while (elapsed < duration)
                {
                    source.Token.ThrowIfCancellationRequested();
                    if (!scroll || !UIInputUtility.IsAllowed(scroll)) throw new System.OperationCanceledException();
                    float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                    scroll.normalizedPosition = Vector2.LerpUnclamped(origin, destination, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, source.Token);
                    elapsed += Time.unscaledDeltaTime;
                }
                source.Token.ThrowIfCancellationRequested();
                scroll.normalizedPosition = destination;
            }
            finally { if (m_cancel == source) m_cancel = null; source.Dispose(); }
        }
        internal void Cancel() { var source = m_cancel; m_cancel = null; source?.Cancel(); }
        public void OnBeginDrag(PointerEventData eventData) => Cancel();
        private void OnDisable() => Cancel();
    }
}
