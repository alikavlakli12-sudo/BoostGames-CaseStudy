using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Keeps the approved receiver renders crisp and color-accurate on mobile builds.
    /// </summary>
    public sealed class ReceiverArtworkTextureImporter : AssetPostprocessor
    {
        private const string LegacyArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/Receivers/";
        private const string ApprovedArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/ReceiversV3/";
        private const string LaneArtworkFolder =
            "Assets/MarbleSort/Resources/Presentation/ReceiverLanes/";

        [MenuItem("Marble Sort/Setup/Reimport Receiver Artwork")]
        public static void ReimportAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { LegacyArtworkFolder, ApprovedArtworkFolder, LaneArtworkFolder });
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
            if (!assetPath.StartsWith(LegacyArtworkFolder, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(ApprovedArtworkFolder, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(LaneArtworkFolder, System.StringComparison.Ordinal))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            bool isApprovedReceiver = assetPath.StartsWith(
                ApprovedArtworkFolder,
                System.StringComparison.Ordinal);
            importer.mipmapEnabled = isApprovedReceiver;
            importer.mipMapsPreserveCoverage = isApprovedReceiver;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = isApprovedReceiver
                ? FilterMode.Trilinear
                : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = assetPath.StartsWith(
                LaneArtworkFolder,
                System.StringComparison.Ordinal)
                ? 2048
                : 1024;
        }
    }
}
