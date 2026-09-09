using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    [FrameworkArchitecture("UI 滚动工具", "结算布局后将指定条目移入视口。", FrameworkArchitectureLayer.PublicFacade, 130)]
    public static class UIScrollTools
    {
        /// <summary>先结算布局，再以视口坐标移动最小距离；不做动画或改变不可滚动轴。</summary>
        public static void ScrollToItem(this ScrollRect scroll, RectTransform item)
        {
            scroll.GetComponent<UIScrollMotion>()?.Cancel();
            if (!scroll || !scroll.content || !item) throw new ArgumentNullException("ScrollRect/content/item");
            if (!item.IsChildOf(scroll.content)) throw new ArgumentException("目标不属于此 ScrollRect。");
            UILayoutScheduler.FlushNow(scroll.content);
            var viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, scroll.content);
            var view = viewport.rect;
            Vector3 delta = Vector3.zero;
            for (int axis = 0; axis < 2; axis++)
            {
                if ((axis == 0 && !scroll.horizontal) || (axis == 1 && !scroll.vertical) ||
                    contentBounds.size[axis] <= view.size[axis]) continue;
                if (bounds.min[axis] < view.min[axis]) delta[axis] = view.min[axis] - bounds.min[axis];
                else if (bounds.max[axis] > view.max[axis]) delta[axis] = view.max[axis] - bounds.max[axis];
                delta[axis] = Mathf.Clamp(delta[axis], view.max[axis] - contentBounds.max[axis],
                    view.min[axis] - contentBounds.min[axis]);
            }
            var worldDelta = viewport.TransformVector(delta);
            var parent = scroll.content.parent;
            var localDelta = parent ? parent.InverseTransformVector(worldDelta) : worldDelta;
            scroll.StopMovement();
            scroll.content.anchoredPosition += new Vector2(localDelta.x, localDelta.y);
        }
        public static void ScrollToNormalized(this ScrollRect scroll, Vector2 position)
        {
            scroll.GetComponent<UIScrollMotion>()?.Cancel();
            UILayoutScheduler.FlushNow(scroll.content); scroll.StopMovement();
            scroll.normalizedPosition = new Vector2(scroll.horizontal ? Mathf.Clamp01(position.x) : scroll.normalizedPosition.x,
                scroll.vertical ? Mathf.Clamp01(position.y) : scroll.normalizedPosition.y);
        }
        public static void ScrollToTop(this ScrollRect scroll) => scroll.ScrollToNormalized(new Vector2(scroll.horizontalNormalizedPosition, 1));
        public static void ScrollToBottom(this ScrollRect scroll) => scroll.ScrollToNormalized(new Vector2(scroll.horizontalNormalizedPosition, 0));
        public static void ScrollByDistance(this ScrollRect scroll, Vector2 distance)
        {
            UILayoutScheduler.FlushNow(scroll.content);
            var viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, scroll.content);
            var range = bounds.size - (Vector3)viewport.rect.size;
            var normalized = scroll.normalizedPosition;
            if (range.x > 0) normalized.x += distance.x / range.x;
            if (range.y > 0) normalized.y -= distance.y / range.y;
            scroll.ScrollToNormalized(normalized);
        }
        /// <param name="alignment">0 将条目左/下边对齐，0.5 居中，1 右/上边对齐。</param>
        public static void AlignItem(this ScrollRect scroll, RectTransform item, Vector2 alignment)
        {
            if (!scroll || !scroll.content || !item || !item.IsChildOf(scroll.content)) throw new ArgumentException("条目必须属于滚动内容。");
            UILayoutScheduler.FlushNow(scroll.content);
            var viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            var view = viewport.rect;
            Vector2 itemPoint = (Vector2)bounds.min + Vector2.Scale((Vector2)bounds.size, alignment);
            Vector2 viewPoint = view.min + Vector2.Scale(view.size, alignment);
            // distance 正值向右/向下滚动，内容向相反方向移动。
            scroll.ScrollByDistance(new Vector2(itemPoint.x - viewPoint.x, viewPoint.y - itemPoint.y));
        }
        public static UniTask ScrollToAsync(this ScrollRect scroll, Vector2 normalizedPosition, float duration = 0.2f, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            UILayoutScheduler.FlushNow(scroll.content);
            normalizedPosition = new Vector2(scroll.horizontal ? Mathf.Clamp01(normalizedPosition.x) : scroll.normalizedPosition.x,
                scroll.vertical ? Mathf.Clamp01(normalizedPosition.y) : scroll.normalizedPosition.y);
            if (duration <= 0) { scroll.ScrollToNormalized(normalizedPosition); return UniTask.CompletedTask; }
            var motion = scroll.GetComponent<UIScrollMotion>() ?? scroll.gameObject.AddComponent<UIScrollMotion>();
            return motion.MoveAsync(scroll, normalizedPosition, duration, token);
        }
    }
}
