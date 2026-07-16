using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Keeps the approved receiver renders crisp and color-accurate on mobile builds.
    /// </summary>
    public sealed class ReceiverArtworkTextureImporter : AssetPostprocessor
    {
        private const string ArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/Receivers/";

        [MenuItem("Marble Sort/Setup/Reimport Receiver Artwork")]
        public static void ReimportAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtworkFolder });
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reimported {assetGuids.Length} receiver artwork textures.");
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
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
        }
    }
}
