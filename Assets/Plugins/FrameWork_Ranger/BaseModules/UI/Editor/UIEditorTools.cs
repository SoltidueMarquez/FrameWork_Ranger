using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI.Editor
{
    /// <summary>创建操作仅写新资产；校验不修改目标。</summary>
    [FrameworkArchitecture("UI 编辑器工具", "创建新 Prefab 并诊断引用和布局。", FrameworkArchitectureLayer.EditorIntegration, 135)]
    public static class UIEditorTools
    {
        public static GameObject CreatePanelPrefab(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.LoadMainAssetAtPath(path))
                throw new ArgumentException("请选择 Assets 下尚不存在的 Prefab 路径。");
            var root = new GameObject("Panel", typeof(RectTransform), typeof(UIView), typeof(Image));
            try
            {
                root.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 0.96f);
                ((RectTransform)root.transform).sizeDelta = new Vector2(640, 420);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        public static List<string> ValidatePrefab(GameObject prefab)
        {
            var errors = new List<string>();
            if (!prefab) { errors.Add("Prefab 为空。"); return errors; }
            var view = prefab.GetComponent<UIView>();
            if (!view) errors.Add(prefab.name + ": 根节点缺少 UIView。");
            else
            {
                try { UIContext.ValidateView(view); } catch (Exception ex) { errors.Add(prefab.name + ": " + ex.Message); }
                foreach (var component in prefab.GetComponentsInChildren<UIView>(true))
                {
                    for (var type = component.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (!field.IsDefined(typeof(UIRequiredAttribute), true)) continue;
                        if (!(field.GetValue(component) is UnityEngine.Object value) || !value)
                            errors.Add(GetPath(component.transform) + "." + field.Name + ": 缺少手动绑定。");
                    }
                }
            }
            foreach (var fitter in prefab.GetComponentsInChildren<ContentSizeFitter>(true))
            {
                if (!fitter.enabled || !fitter.transform.parent) continue;
                var parent = fitter.transform.parent.GetComponent<LayoutGroup>();
                bool x = false, y = false;
                if (parent is UIStackLayout || parent is UIGridLayout || parent is GridLayoutGroup) x = y = true;
                else if (parent is HorizontalOrVerticalLayoutGroup stack) { x = stack.childControlWidth; y = stack.childControlHeight; }
                var item = fitter.GetComponent<UILayoutItem>();
                if (item && item.isActiveAndEnabled && (parent is UIStackLayout || parent is UIGridLayout))
                { x &= item.ControlWidth; y &= item.ControlHeight; }
                if (parent && parent.enabled && ((x && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained) ||
                    (y && fitter.verticalFit != ContentSizeFitter.FitMode.Unconstrained)))
                    errors.Add(GetPath(fitter.transform) + ": 父布局与 ContentFitter 同轴控制尺寸。");
            }
            return errors;
        }
        private static string GetPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }
    }
}
