using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Preserves the alpha and color fidelity of the approved 3x3 tray renders.
    /// </summary>
    public sealed class TopTrayArtworkTextureImporter : AssetPostprocessor
    {
        private const string ArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/TopTrays/";

        [MenuItem("Marble Sort/Setup/Reimport Top Tray Artwork")]
        public static void ReimportAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtworkFolder });
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reimported {assetGuids.Length} top-tray artwork textures.");
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
            bool isExposedTray =
                !assetPath.Contains("/Hidden/", System.StringComparison.Ordinal) &&
                !assetPath.Contains("/Mystery/", System.StringComparison.Ordinal);
            importer.mipmapEnabled = isExposedTray;
            importer.mipMapsPreserveCoverage = isExposedTray;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = isExposedTray ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
        }
    }
}
