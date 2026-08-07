using Framework_WWJ.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Framework_WWJ.Tests
{
    internal sealed class FrameworkGraphViewportStateTests
    {
        [Test]
        public void SetZoomAround_KeepsCanvasPointUnderPointer()
        {
            var state = new FrameworkGraphViewportState();
            state.PanBy(new Vector2(70f, -25f));
            var pointer = new Vector2(320f, 180f);
            var canvasPointBefore = state.ViewportToCanvas(pointer);

            state.SetZoomAround(pointer, 1.65f);

            Assert.That(state.ViewportToCanvas(pointer).x, Is.EqualTo(canvasPointBefore.x).Within(0.001f));
            Assert.That(state.ViewportToCanvas(pointer).y, Is.EqualTo(canvasPointBefore.y).Within(0.001f));
        }

        [Test]
        public void SetZoomAround_ClampsZoomRange()
        {
            var state = new FrameworkGraphViewportState();

            state.SetZoomAround(Vector2.zero, 10f);
            Assert.That(state.Zoom, Is.EqualTo(FrameworkGraphViewportState.MaxZoom));

            state.SetZoomAround(Vector2.zero, 0.01f);
            Assert.That(state.Zoom, Is.EqualTo(FrameworkGraphViewportState.MinZoom));
        }

        [Test]
        public void FrameAll_FitsAndCentersContent()
        {
            var state = new FrameworkGraphViewportState();
            var viewportSize = new Vector2(1000f, 600f);
            var content = new Rect(100f, 50f, 1800f, 900f);

            state.FrameAll(viewportSize, content);

            var transformed = state.CanvasToViewport(content);
            Assert.That(transformed.width, Is.LessThanOrEqualTo(
                viewportSize.x - FrameworkGraphViewportState.FramePadding * 2f + 0.01f));
            Assert.That(transformed.height, Is.LessThanOrEqualTo(
                viewportSize.y - FrameworkGraphViewportState.FramePadding * 2f + 0.01f));
            Assert.That(transformed.center.x, Is.EqualTo(viewportSize.x * 0.5f).Within(0.001f));
            Assert.That(transformed.center.y, Is.EqualTo(viewportSize.y * 0.5f).Within(0.001f));
        }

        [Test]
        public void ResetToOne_UsesOneHundredPercentAndCentersContent()
        {
            var state = new FrameworkGraphViewportState();
            state.SetZoomAround(Vector2.zero, 0.5f);
            var viewportSize = new Vector2(800f, 500f);
            var content = new Rect(0f, 0f, 300f, 200f);

            state.ResetToOne(viewportSize, content);

            Assert.That(state.Zoom, Is.EqualTo(1f));
            Assert.That(state.CanvasToViewport(content).center, Is.EqualTo(viewportSize * 0.5f));
        }

        [Test]
        public void PanBy_OffsetsCanvasCoordinates()
        {
            var state = new FrameworkGraphViewportState();
            var before = state.CanvasToViewport(new Vector2(10f, 20f));

            state.PanBy(new Vector2(45f, -12f));

            Assert.That(state.CanvasToViewport(new Vector2(10f, 20f)), Is.EqualTo(before + new Vector2(45f, -12f)));
        }
    }
}
