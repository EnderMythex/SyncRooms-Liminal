using UnityEditor;
using UnityEngine;

namespace SyncRooms.EditorTools
{
    /// <summary>Import rules for downloaded / generated art: correct color spaces, normal maps, compression, model flags.</summary>
    public class AssetImportRules : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art")) return;
            var ti = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 2048;
            ti.streamingMipmaps = true;

            if (file.EndsWith("normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.sRGBTexture = false;
            }
            else if (file.EndsWith("mask") || file.EndsWith("height") || file.EndsWith("opacity"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
                if (file.EndsWith("height")) ti.textureCompression = TextureImporterCompression.Uncompressed;
            }
            else if (assetPath.Contains("/Icons/"))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 512;
                ti.alphaIsTransparency = true;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art")) return;
            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = true; // needed for MeshColliders built at edit time and runtime lightmap-free GI
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.generateSecondaryUV = false;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            // articulated rigs are exported without baked space transform: let Unity bake the axis conversion
            mi.bakeAxisConversion = assetPath.Contains("/Monsters/") || assetPath.Contains("/Rigs/");
        }
    }
}
