using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.Core
{
    /// <summary>
    /// "Ultra" quality: adds a high-priority volume switching reflections, global illumination and ambient
    /// occlusion to hardware ray tracing (DX12 + RTX). Removed automatically on other quality levels.
    /// </summary>
    public static class UltraGraphics
    {
        static GameObject _vol;

        public static void Apply(int quality)
        {
            bool want = quality >= 3 && SystemInfo.supportsRayTracing;
            if (!want) { if (_vol) Object.Destroy(_vol); return; }
            if (_vol) return;
            _vol = new GameObject("[Ultra RT Volume]");
            var v = _vol.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 20;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var ssr = p.Add<ScreenSpaceReflection>(true);
            ssr.enabled.Override(true);
            ssr.tracing.Override(RayCastingMode.RayTracing);
            var gi = p.Add<GlobalIllumination>(true);
            gi.enable.Override(true);
            gi.tracing.Override(RayCastingMode.RayTracing);
            var ao = p.Add<ScreenSpaceAmbientOcclusion>(true);
            ao.rayTracing.Override(true);
            ao.intensity.Override(1.3f);
            var rts = p.Add<RayTracingSettings>(true);
            rts.rayBias.Override(0.001f);
            v.sharedProfile = p;
        }
    }
}
