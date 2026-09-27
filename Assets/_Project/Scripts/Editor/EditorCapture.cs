using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>Renders an HDRP camera to a Texture2D from the editor (icons, QA screenshots).</summary>
    public static class EditorCapture
    {
        public static Texture2D Render(Camera cam, int w, int h, int warmupFrames = 6)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
            rt.Create();
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            // HDRP needs a few frames for exposure / temporal effects to settle.
            for (int i = 0; i < warmupFrames; i++) cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = prev;
            rt.Release();
            Object.DestroyImmediate(rt);
            return tex;
        }

        public static void SavePng(Texture2D linearTex, string path, bool keepAlpha = false)
        {
            // Convert linear float to sRGB 8-bit.
            int w = linearTex.width, h = linearTex.height;
            var src = linearTex.GetPixels();
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var c = src[i];
                px[i] = new Color(Mathf.LinearToGammaSpace(Mathf.Clamp01(c.r)), Mathf.LinearToGammaSpace(Mathf.Clamp01(c.g)),
                    Mathf.LinearToGammaSpace(Mathf.Clamp01(c.b)), keepAlpha ? Mathf.Clamp01(c.a) : 1f);
            }
            dst.SetPixels(px);
            dst.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, dst.EncodeToPNG());
            Object.DestroyImmediate(dst);
        }

        /// <summary>Enables realistic frame settings on a camera (SSR, SSGI, volumetrics, water...).</summary>
        public static void ConfigureCamera(HDAdditionalCameraData hd, bool ssgi = true)
        {
            hd.customRenderingSettings = true;
            var mask = hd.renderingPathCustomFrameSettingsOverrideMask;
            var fs = hd.renderingPathCustomFrameSettings;
            void Set(FrameSettingsField f, bool v)
            {
                mask.mask[(uint)f] = true;
                fs.SetEnabled(f, v);
            }
            Set(FrameSettingsField.SSR, true);
            Set(FrameSettingsField.TransparentSSR, true);
            Set(FrameSettingsField.SSAO, true);
            Set(FrameSettingsField.SSGI, ssgi);
            Set(FrameSettingsField.Volumetrics, true);
            Set(FrameSettingsField.ReprojectionForVolumetrics, true);
            Set(FrameSettingsField.ContactShadows, true);
            Set(FrameSettingsField.ScreenSpaceShadows, true);
            Set(FrameSettingsField.Water, true);
            Set(FrameSettingsField.Refraction, true);
            Set(FrameSettingsField.Distortion, true);
            Set(FrameSettingsField.MotionVectors, true);
            Set(FrameSettingsField.ObjectMotionVectors, true);
            Set(FrameSettingsField.Decals, true);
            Set(FrameSettingsField.PlanarProbe, true);
            Set(FrameSettingsField.ReflectionProbe, true);
            Set(FrameSettingsField.RayTracing, true);
            hd.renderingPathCustomFrameSettings = fs;
            hd.renderingPathCustomFrameSettingsOverrideMask = mask;
            hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            hd.TAAQuality = HDAdditionalCameraData.TAAQualityLevel.High;
            hd.dithering = true;
        }
    }
}
