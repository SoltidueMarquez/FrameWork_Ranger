using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
namespace FrameWork_Ranger.UI
{
    /// <summary>在组坐标中合并图片透明度；用独立 RT 生成外轮廓，结果仍服从原 Canvas 裁切。</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Ranger UI/效果/多图整体描边")]
    [FrameworkArchitecture("多图整体描边", "合并 Image 透明轮廓，生成无重叠内边的组级描边。", FrameworkArchitectureLayer.PublicFacade, 135)]
    public sealed class UIOutlineGroup : MonoBehaviour
    {
        [LabelText("描边颜色")] public Color OutlineColor = Color.white;
        [LabelText("描边粗细（UI 单位）"), Range(0, 16)] public float Width = 3;
        [LabelText("最大合成尺寸"), Range(64, 2048)] public int MaxResolution = 1024;
        private readonly List<Image> m_sources = new List<Image>();
        private readonly HashSet<Graphic> m_watched = new HashSet<Graphic>();
        private readonly Dictionary<Graphic, Mesh> m_drawMeshes = new Dictionary<Graphic, Mesh>();
        private readonly List<Material> m_stencilMaterials = new List<Material>();
        private readonly List<Mask> m_masks = new List<Mask>();
        private readonly Vector3[] m_corners = new Vector3[4];
        private Material m_dilate;
        private RenderTexture m_mask, m_horizontal, m_result;
        private RawImage m_output;
        private int m_signature;
        private bool m_dirty = true, m_rendering;
        private RectTransform Rect => (RectTransform)transform;
        private void OnEnable() { Canvas.willRenderCanvases += RefreshIfNeeded; m_dirty = true; }
        private void OnDisable() { Canvas.willRenderCanvases -= RefreshIfNeeded; Release(); }
        private void OnDestroy() => Release();
        private void OnValidate() { m_dirty = true; }
        public void Refresh() { m_dirty = true; }
        public void RefreshNow() { Canvas.ForceUpdateCanvases(); m_dirty = true; RefreshIfNeeded(); }
        private void RefreshIfNeeded()
        {
            if (!isActiveAndEnabled || m_rendering) return;
            m_rendering = true;
            var previousTarget = RenderTexture.active;
            try
            {
                GetComponentsInChildren(false, m_sources);
                foreach (var graphic in GetComponentsInChildren<Graphic>(false))
                    if (graphic != m_output && m_watched.Add(graphic))
                    { graphic.RegisterDirtyVerticesCallback(Refresh); graphic.RegisterDirtyMaterialCallback(Refresh); }
                m_watched.RemoveWhere(graphic =>
                {
                    if (graphic && graphic.transform.IsChildOf(transform)) return false;
                    if (graphic) { graphic.UnregisterDirtyVerticesCallback(Refresh); graphic.UnregisterDirtyMaterialCallback(Refresh); }
                    if (m_drawMeshes.TryGetValue(graphic, out var mesh)) { GlobalUIModule.DestroyOwned(mesh); m_drawMeshes.Remove(graphic); }
                    return true;
                });
                int signature = OutlineColor.GetHashCode() ^ Width.GetHashCode() ^ MaxResolution;
                foreach (var image in m_sources)
                {
                    if (!image || !image.isActiveAndEnabled) continue;
                    unchecked
                    {
                        signature = signature * 31 + image.GetEntityId().GetHashCode();
                        signature = signature * 31 + image.transform.localToWorldMatrix.GetHashCode();
                        signature = signature * 31 + image.rectTransform.rect.GetHashCode();
                        signature = signature * 31 + image.color.GetHashCode() + image.canvasRenderer.GetColor().GetHashCode();
                        signature = signature * 31 + (image.overrideSprite ? image.overrideSprite.GetEntityId().GetHashCode() : 0);
                        signature = signature * 31 + image.fillAmount.GetHashCode() + (int)image.type;
                        signature = signature * 31 + RelativeAlpha(image.transform).GetHashCode();
                        for (var p = image.transform.parent; p && p != transform; p = p.parent)
                        {
                            if (p.GetComponent<Mask>() || p.GetComponent<RectMask2D>())
                                signature = signature * 31 + p.localToWorldMatrix.GetHashCode() + ((RectTransform)p).rect.GetHashCode();
                        }
                    }
                }
                if (!m_dirty && signature == m_signature) return;
                m_dirty = false; m_signature = signature;
                Render();
            }
            finally { RenderTexture.active = previousTarget; m_rendering = false; }
        }
        private float RelativeAlpha(Transform source)
        {
            float alpha = 1;
            for (var p = source; p && p != transform; p = p.parent)
                foreach (var group in p.GetComponents<CanvasGroup>()) if (group.enabled) alpha *= group.alpha;
            return alpha;
        }
        private Material StencilMaterial(int level)
        {
            while (m_stencilMaterials.Count <= level)
            {
                var shader = Resources.Load<Shader>("RangerUIShaders/OutlineMask");
                if (!shader) throw new System.InvalidOperationException("缺少 Ranger UI 描边遮罩 Shader。");
                var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
#if UNITY_EDITOR
                UnityEditor.ShaderUtil.CompilePass(material, 0, true);
                UnityEditor.ShaderUtil.CompilePass(material, 1, true);
#endif
                material.SetInt("_Stencil", m_stencilMaterials.Count); m_stencilMaterials.Add(material);
            }
            return m_stencilMaterials[level];
        }
        private void Render()
        {
            if (Width <= 0 || m_sources.Count == 0) { if (m_output) m_output.enabled = false; return; }
            bool any = false; var bounds = new Bounds();
            foreach (var image in m_sources)
            {
                if (!image || !image.isActiveAndEnabled) continue;
                var ownMask = image.GetComponent<Mask>(); if (ownMask && ownMask.enabled && !ownMask.showMaskGraphic) continue;
                image.rectTransform.GetWorldCorners(m_corners);
                foreach (var corner in m_corners)
                { var point = Rect.InverseTransformPoint(corner); if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; } else bounds.Encapsulate(point); }
            }
            if (!any) { if (m_output) m_output.enabled = false; return; }
            bounds.Expand(new Vector3(Width * 2, Width * 2, 0));
            float scale = Mathf.Min(1, (float)MaxResolution / Mathf.Max(1, Mathf.Max(bounds.size.x, bounds.size.y)));
            int width = Mathf.Max(4, Mathf.CeilToInt(bounds.size.x * scale)), height = Mathf.Max(4, Mathf.CeilToInt(bounds.size.y * scale));
            EnsureTextures(width, height);
            if (!m_output)
            {
                var output = new GameObject("整体描边（自动）", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(LayoutElement));
                output.hideFlags = HideFlags.HideAndDontSave; output.transform.SetParent(transform, false);
                output.GetComponent<LayoutElement>().ignoreLayout = true;
                output.transform.SetAsFirstSibling(); m_output = output.GetComponent<RawImage>(); m_output.raycastTarget = false;
            }
            m_output.enabled = true; m_output.color = Color.white;
            var outputRect = m_output.rectTransform; outputRect.anchorMin = outputRect.anchorMax = Rect.pivot;
            outputRect.pivot = new Vector2(0.5f, 0.5f); outputRect.anchoredPosition = bounds.center; outputRect.sizeDelta = bounds.size;
            var ortho = GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, -1000, 1000), true);
            var clip = new Vector4(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
            using (var commands = new CommandBuffer { name = "Ranger UI 合并透明轮廓" })
            {
                commands.SetRenderTarget(m_mask); commands.ClearRenderTarget(true, true, Color.clear);
                commands.SetViewport(new Rect(0, 0, width, height));
                foreach (var image in m_sources)
                {
                    if (!image || !image.isActiveAndEnabled) continue;
                    var ownMask = image.GetComponent<Mask>(); if (ownMask && ownMask.enabled && !ownMask.showMaskGraphic) continue;
                    m_masks.Clear(); var sourceClip = clip;
                    for (var p = image.transform.parent; p && p != transform; p = p.parent)
                    {
                        var mask = p.GetComponent<Mask>();
                        if (mask && mask.IsActive() && mask.graphic) m_masks.Add(mask);
                        var rectMask = p.GetComponent<RectMask2D>();
                        if (rectMask && rectMask.IsActive())
                        {
                            ((RectTransform)p).GetWorldCorners(m_corners);
                            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                            foreach (var corner in m_corners) { Vector2 q = Rect.InverseTransformPoint(corner); min = Vector2.Min(min, q); max = Vector2.Max(max, q); }
                            sourceClip = new Vector4(Mathf.Max(sourceClip.x, min.x), Mathf.Max(sourceClip.y, min.y), Mathf.Min(sourceClip.z, max.x), Mathf.Min(sourceClip.w, max.y));
                        }
                    }
                    commands.ClearRenderTarget(true, false, Color.clear);
                    int level = 0;
                    for (int i = m_masks.Count - 1; i >= 0; i--) Draw(commands, m_masks[i].graphic, ortho, sourceClip, level++, 0);
                    Draw(commands, image, ortho, sourceClip, level, 1);
                }
                Graphics.ExecuteCommandBuffer(commands);
            }
            if (!m_dilate)
            {
                m_dilate = new Material(Resources.Load<Shader>("RangerUIShaders/OutlineDilate")) { hideFlags = HideFlags.HideAndDontSave };
#if UNITY_EDITOR
                UnityEditor.ShaderUtil.CompilePass(m_dilate, 0, true);
                UnityEditor.ShaderUtil.CompilePass(m_dilate, 1, true);
#endif
            }
            m_dilate.SetFloat("_Radius", Mathf.Clamp(Width * scale, 0, 16));
            m_dilate.SetVector("_Direction", new Vector4(1, 0, 0, 0));
            Graphics.Blit(m_mask, m_horizontal, m_dilate, 0);
            m_dilate.SetVector("_Direction", new Vector4(0, 1, 0, 0));
            m_dilate.SetTexture("_Original", m_mask); m_dilate.SetColor("_OutlineColor", OutlineColor);
            Graphics.Blit(m_horizontal, m_result, m_dilate, 1);
            m_output.texture = m_result;
        }
        private void Draw(CommandBuffer commands, Graphic image, Matrix4x4 projection, Vector4 clip, int level, int pass)
        {
            var source = image.canvasRenderer.GetMesh(); if (!source || source.vertexCount == 0) return;
            // CanvasRenderer geometry must be copied into a mesh owned by this draw path.
            // Borrowing its native vertex buffer yields empty draws on Unity 6.5 / D3D12.
            if (!m_drawMeshes.TryGetValue(image, out var mesh))
            { mesh = new Mesh { name = "Ranger outline mesh", hideFlags = HideFlags.HideAndDontSave }; m_drawMeshes.Add(image, mesh); }
            mesh.Clear(); mesh.vertices = source.vertices; mesh.uv = source.uv; mesh.colors32 = source.colors32; mesh.triangles = source.triangles;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", image.mainTexture ? image.mainTexture : Texture2D.whiteTexture);
            properties.SetMatrix("_ToGroup", Rect.worldToLocalMatrix * image.transform.localToWorldMatrix);
            properties.SetMatrix("_Projection", projection);
            properties.SetVector("_ClipRect", clip);
            properties.SetFloat("_Opacity", RelativeAlpha(image.transform) * image.canvasRenderer.GetColor().a);
            commands.DrawMesh(mesh, Matrix4x4.identity, StencilMaterial(level), 0, pass, properties);
        }
        private void EnsureTextures(int width, int height)
        {
            if (m_mask && m_mask.width == width && m_mask.height == height) return;
            ReleaseTexture(ref m_mask); ReleaseTexture(ref m_horizontal); ReleaseTexture(ref m_result);
            m_mask = Texture(width, height, 24); m_horizontal = Texture(width, height, 0); m_result = Texture(width, height, 0);
        }
        private static RenderTexture Texture(int width, int height, int depth)
        {
            var texture = new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                { name = "Ranger UI outline", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create(); return texture;
        }
        private static void ReleaseTexture(ref RenderTexture texture)
        { if (!texture) return; texture.Release(); GlobalUIModule.DestroyOwned(texture); texture = null; }
        private void Release()
        {
            ReleaseTexture(ref m_mask); ReleaseTexture(ref m_horizontal); ReleaseTexture(ref m_result);
            foreach (var material in m_stencilMaterials) GlobalUIModule.DestroyOwned(material); m_stencilMaterials.Clear();
            foreach (var graphic in m_watched)
                if (graphic) { graphic.UnregisterDirtyVerticesCallback(Refresh); graphic.UnregisterDirtyMaterialCallback(Refresh); }
            m_watched.Clear();
            foreach (var mesh in m_drawMeshes.Values) GlobalUIModule.DestroyOwned(mesh); m_drawMeshes.Clear();
            GlobalUIModule.DestroyOwned(m_dilate); m_dilate = null;
            if (m_output) GlobalUIModule.DestroyOwned(m_output.gameObject); m_output = null; m_dirty = true;
        }
    }
}
