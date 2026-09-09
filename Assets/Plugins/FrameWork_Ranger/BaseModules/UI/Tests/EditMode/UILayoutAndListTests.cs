using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Tests
{
    public sealed class UILayoutAndListTests
    {
        [Test]
        public void PerItemSizingAlignmentAndFitterLimits_AreAppliedWithoutOverwritingUncontrolledAxis()
        {
            var root = new GameObject("Layout", typeof(RectTransform), typeof(UIVerticalLayout));
            try
            {
                var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(300, 300);
                root.GetComponent<UIVerticalLayout>().ExpandWidth = true;
                var child = new GameObject("Custom item", typeof(RectTransform), typeof(UILayoutItem), typeof(LayoutElement)); child.transform.SetParent(rect, false);
                var childRect = (RectTransform)child.transform; childRect.sizeDelta = new Vector2(80, 60);
                var item = child.GetComponent<UILayoutItem>(); item.ControlWidth = false; item.OverrideAlignment = true; item.Alignment = TextAnchor.UpperRight; item.Offset = new Vector2(-5, 3);
                child.GetComponent<LayoutElement>().preferredHeight = 50;
                UILayoutScheduler.FlushNow(rect);
                Assert.That(childRect.rect.width, Is.EqualTo(80)); Assert.That(childRect.rect.height, Is.EqualTo(50));
                Assert.That(childRect.anchoredPosition.x, Is.EqualTo(255).Within(.01f));
                var fitter = root.AddComponent<UIContentFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.LimitHeight = true; fitter.HeightRange = new Vector2(90, 140);
                UILayoutScheduler.FlushNow(rect); Assert.That(rect.rect.height, Is.EqualTo(90));
                child.GetComponent<LayoutElement>().preferredHeight = 300;
                UILayoutScheduler.FlushNow(rect); Assert.That(rect.rect.height, Is.EqualTo(140));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void KeyedList_InsertBeforeVisibleItem_PreservesItsViewportPosition()
        {
            var root = new GameObject("Viewport", typeof(RectTransform), typeof(ScrollRect));
            var template = new GameObject("Template", typeof(RectTransform), typeof(UIItemView), typeof(LayoutElement));
            try
            {
                var viewport = (RectTransform)root.transform; viewport.sizeDelta = new Vector2(300, 120);
                var content = new GameObject("Content", typeof(RectTransform), typeof(UIVerticalLayout), typeof(UIContentFitter)); content.transform.SetParent(viewport, false);
                var rect = (RectTransform)content.transform; rect.anchorMin = new Vector2(0,1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,1); rect.sizeDelta = Vector2.zero;
                content.GetComponent<UIContentFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                template.GetComponent<LayoutElement>().preferredHeight = 40;
                var scroll = root.GetComponent<ScrollRect>(); scroll.content = rect; scroll.viewport = viewport; scroll.horizontal = false;
                using (var list = new UIListController<int,int,UIItemView>(rect, template.GetComponent<UIItemView>(), key => key))
                {
                    list.Sync(Enumerable.Range(1, 20).ToArray()); scroll.ScrollToNormalized(new Vector2(0,.5f));
                    var item = list.Items[10]; float before = viewport.InverseTransformPoint(item.transform.position).y;
                    list.Sync(Enumerable.Range(-2, 23).ToArray(), scroll);
                    Assert.That(list.Items[10], Is.SameAs(item));
                    Assert.That(viewport.InverseTransformPoint(item.transform.position).y, Is.EqualTo(before).Within(.1f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(template); }
        }
        [Test]
        public void VerticalLayout_RespectsPreferredHeightSpacingAndIgnoredItems()
        {
            var root = new GameObject("Root", typeof(RectTransform), typeof(UIVerticalLayout));
            try
            {
                var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(300, 300);
                root.GetComponent<UIVerticalLayout>().Spacing = 10;
                for (int i = 0; i < 3; i++)
                {
                    var child = new GameObject("Child", typeof(RectTransform), typeof(LayoutElement));
                    child.transform.SetParent(rect, false);
                    child.GetComponent<LayoutElement>().preferredHeight = 40;
                    child.GetComponent<LayoutElement>().preferredWidth = 120;
                    if (i == 1) child.AddComponent<UIIgnoreLayout>();
                }
                UILayoutScheduler.FlushNow(rect);
                Assert.That(LayoutUtility.GetPreferredHeight(rect), Is.EqualTo(90));
                Assert.That(((RectTransform)rect.GetChild(0)).rect.height, Is.EqualTo(40));
                Assert.That(((RectTransform)rect.GetChild(2)).anchoredPosition.y -
                    ((RectTransform)rect.GetChild(0)).anchoredPosition.y, Is.EqualTo(-50));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void KeyedList_ReusesReordersAndRejectsDuplicatesBeforeMutation()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var template = new GameObject("Template", typeof(RectTransform), typeof(UIItemView));
            var manual = new GameObject("Manual");
            manual.transform.SetParent(root.transform);
            try
            {
                using (var list = new UIListController<int, int, UIItemView>(
                    (RectTransform)root.transform, template.GetComponent<UIItemView>(), value => value))
                {
                    list.Sync(new[] { 1, 2 });
                    var first = list.Items[1];
                    list.Sync(new[] { 2, 1, 3 });
                    Assert.That(list.Items[1], Is.SameAs(first));
                    Assert.Throws<ArgumentException>(() => list.Sync(new[] { 1, 1 }));
                    Assert.That(list.Items.Count, Is.EqualTo(3));
                    list.Sync(new[] { 1 });
                    Assert.That(list.Items[1], Is.SameAs(first));
                }
                Assert.That(manual != null, Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(template); }
        }
        [Test]
        public void ModuleConfig_RejectsOverlayOrderCollisionAcrossOwners()
        {
            var module = ScriptableObject.CreateInstance<GlobalUIModule>();
            try
            {
                module.GlobalConfiguration.Domains.Add(new UIDomainDefinition());
                module.SceneConfiguration.Domains.Add(new UIDomainDefinition());
                Assert.That(module.ValidateConfiguration().Count, Is.EqualTo(1));
                module.SceneConfiguration.Domains[0].SortingOrder = -10;
                CollectionAssert.IsEmpty(module.ValidateConfiguration());
            }
            finally { UnityEngine.Object.DestroyImmediate(module); }
        }
        [Test]
        public void ConfigReportsDuplicateKeysAndMissingDomains()
        {
            var config = new UIConfiguration();
            config.Domains.Add(new UIDomainDefinition { Key = "Main" });
            config.Domains.Add(new UIDomainDefinition { Key = "Main" });
            config.Panels.Add(new UIPanelDefinition { Key = "Panel", DomainKey = "Missing", PrefabAddress = "Panel" });
            Assert.That(config.Validate().Count, Is.EqualTo(2));
        }
    }
}
