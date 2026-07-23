using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Keeps the approved conveyor render crisp, transparent, and color-accurate on mobile.
    /// </summary>
    public sealed class ConveyorArtworkTextureImporter : AssetPostprocessor
    {
        private const string ArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/Conveyor/";
        private const string AtlasPath =
            ArtworkFolder + "ConveyorAnimationAtlas.png";

        [MenuItem("Marble Sort/Setup/Reimport Conveyor Artwork")]
        public static void ReimportAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtworkFolder });
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reimported {assetGuids.Length} conveyor artwork textures.");
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtworkFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 100;
            importer.crunchedCompression = false;
            importer.maxTextureSize = assetPath.Equals(
                AtlasPath,
                System.StringComparison.Ordinal)
                ? 8192
                : 4096;

            if (assetPath.Equals(AtlasPath, System.StringComparison.Ordinal))
            {
                ConfigurePlatform(importer, "iPhone", TextureImporterFormat.ASTC_5x5);
                // Android's Build Settings select ASTC for modern-device builds
                // or ETC2 for the compatibility build from this automatic HQ source.
                importer.ClearPlatformTextureSettings("Android");
            }
        }

        private static void ConfigurePlatform(
            TextureImporter importer,
            string platform,
            TextureImporterFormat format)
        {
            TextureImporterPlatformSettings settings =
                importer.GetPlatformTextureSettings(platform);
            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = 8192;
            settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            settings.format = format;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }

    }
}
