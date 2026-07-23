using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Keeps approved presentation effects crisp, transparent, and free from mip bleeding.
    /// </summary>
    public sealed class PresentationEffectsTextureImporter : AssetPostprocessor
    {
        private const string ArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/Effects/";

        [MenuItem("Marble Sort/Setup/Reimport Presentation Effects")]
        public static void ReimportAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtworkFolder });
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reimported {assetGuids.Length} presentation-effect textures.");
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
            importer.maxTextureSize = 2048;
        }
    }
}
