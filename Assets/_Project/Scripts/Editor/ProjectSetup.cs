using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace SyncRooms.EditorTools
{
    /// <summary>Configures HDRP assets, quality levels and player settings for realistic rendering.</summary>
    public static class ProjectSetup
    {
        public const string SettingsDir = "Assets/_Project/Settings";

        [MenuItem("SyncRooms/1. Project Setup (HDRP)")]
        public static void Run()
        {
            Directory.CreateDirectory(SettingsDir);
            var high = CreateOrUpdate($"{SettingsDir}/HDRP_High.asset", true);
            var perf = CreateOrUpdate($"{SettingsDir}/HDRP_Performance.asset", false);
            var ultra = CreateOrUpdate($"{SettingsDir}/HDRP_Ultra.asset", true, true);

            GraphicsSettings.defaultRenderPipeline = high;
            ConfigureQuality(perf, high, ultra);

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.companyName = "EnderMythex";
            PlayerSettings.productName = "SyncRooms Liminal";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D12, UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

            AssetDatabase.SaveAssets();
            Debug.Log("[SyncRooms] Project setup done.");
        }

        static HDRenderPipelineAsset CreateOrUpdate(string path, bool highQuality, bool rayTracing = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            var s = asset.currentPlatformRenderPipelineSettings;
            s.supportSSR = true;
            s.supportSSRTransparent = true;
            s.supportSSAO = true;
            s.supportSSGI = highQuality;
            s.supportVolumetrics = true;
            s.supportVolumetricClouds = true;
            s.supportWater = true;
            s.supportWaterExclusion = true;
            s.supportDecals = true;
            s.supportMotionVectors = true;
            s.supportShadowMask = true;
            s.supportedLitShaderMode = RenderPipelineSettings.SupportedLitShaderMode.Both;
            s.supportRayTracing = rayTracing;
            s.supportedRayTracingMode = RenderPipelineSettings.SupportedRayTracingMode.Both;
            s.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            s.supportTransparentBackface = true;
            s.supportDistortion = true;

            // Reflection quality: big planar atlas for pools / marble, crisp cube probes.
            s.planarReflectionResolution = new RenderPipelineSettings.PlanarReflectionAtlasResolutionScalableSetting(new[]
            {
                PlanarReflectionAtlasResolution.Resolution512,
                PlanarReflectionAtlasResolution.Resolution1024,
                highQuality ? PlanarReflectionAtlasResolution.Resolution2048 : PlanarReflectionAtlasResolution.Resolution1024
            }, ScalableSettingSchemaId.With3Levels);
            s.cubeReflectionResolution = new RenderPipelineSettings.ReflectionProbeResolutionScalableSetting(new[]
            {
                CubeReflectionResolution.CubeReflectionResolution256,
                CubeReflectionResolution.CubeReflectionResolution512,
                highQuality ? CubeReflectionResolution.CubeReflectionResolution512 : CubeReflectionResolution.CubeReflectionResolution256
            }, ScalableSettingSchemaId.With3Levels);

            var ll = s.lightLoopSettings;
            ll.reflectionProbeTexCacheSize = ReflectionProbeTextureCacheResolution.Resolution4096x4096;
            ll.maxPlanarReflectionOnScreen = 8;
            ll.maxCubeReflectionOnScreen = 24;
            ll.maxPunctualLightsOnScreen = 256;
            ll.maxAreaLightsOnScreen = 64;
            ll.maxLightsPerClusterCell = 16;
            s.lightLoopSettings = ll;

            var sh = s.hdShadowInitParams;
            sh.maxShadowRequests = 128;
            sh.cachedPunctualLightShadowAtlas = 4096;
            sh.punctualLightShadowAtlas.shadowAtlasResolution = 4096;
            sh.areaLightShadowAtlas.shadowAtlasResolution = 2048;
            sh.supportScreenSpaceShadows = true;
            sh.maxScreenSpaceShadowSlots = 4;
            s.hdShadowInitParams = sh;

            asset.currentPlatformRenderPipelineSettings = s;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void ConfigureQuality(RenderPipelineAsset perf, RenderPipelineAsset high, RenderPipelineAsset ultra)
        {
            var qs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var arr = qs.FindProperty("m_QualitySettings");
            string[] names = { "Low", "Medium", "High", "Ultra" };
            while (arr.arraySize > names.Length) arr.DeleteArrayElementAtIndex(arr.arraySize - 1);
            for (int i = 0; i < names.Length; i++)
            {
                var q = arr.GetArrayElementAtIndex(i);
                q.FindPropertyRelative("name").stringValue = names[i];
                q.FindPropertyRelative("customRenderPipeline").objectReferenceValue = i < 2 ? perf : i == 2 ? high : ultra;
                q.FindPropertyRelative("vSyncCount").intValue = 1;
                q.FindPropertyRelative("lodBias").floatValue = i < 2 ? 1f : 2f;
                q.FindPropertyRelative("maximumLODLevel").intValue = 0;
                q.FindPropertyRelative("anisotropicTextures").intValue = 2;
                q.FindPropertyRelative("globalTextureMipmapLimit").intValue = i == 0 ? 1 : 0;
                q.FindPropertyRelative("realtimeReflectionProbes").boolValue = true;
            }
            qs.FindProperty("m_CurrentQuality").intValue = 2;
            qs.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
