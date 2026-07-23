using UnityEditor;
using UnityEngine;

namespace MarbleSort.Editor
{
    /// <summary>
    /// Preserves the approved HUD alpha edges and the sheet's sampled finish
    /// without mobile texture compression or mip bleeding.
    /// </summary>
    public sealed class ApprovedHudSheetTextureImporter : AssetPostprocessor
    {
        private const string HudFolder =
            "Assets/MarbleSort/Resources/Presentation/UI/Approved/";
        private const string SheetFolder =
            "Assets/MarbleSort/Resources/Presentation/Surround/Approved/";
        private const string TopGridFolder =
            "Assets/MarbleSort/Resources/Presentation/TopGrid/";
        private const string CompletionFolder =
            "Assets/MarbleSort/Resources/Presentation/UI/Completion/";

        [MenuItem("Marble Sort/Setup/Reimport Approved HUD And Sheet")]
        public static void ReimportAll()
        {
            string[] assetGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { HudFolder, SheetFolder, TopGridFolder, CompletionFolder });
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Reimported {assetGuids.Length} approved HUD/sheet textures.");
        }

        private void OnPreprocessTexture()
        {
            bool isHud = assetPath.StartsWith(HudFolder, System.StringComparison.Ordinal);
            bool isSheet = assetPath.StartsWith(SheetFolder, System.StringComparison.Ordinal);
            bool isTopGrid = assetPath.StartsWith(
                TopGridFolder,
                System.StringComparison.Ordinal);
            bool isCompletion = assetPath.StartsWith(
                CompletionFolder,
                System.StringComparison.Ordinal);
            bool isBakedSheet = assetPath.StartsWith(
                SheetFolder + "Baked/",
                System.StringComparison.Ordinal);
            if (!isHud && !isSheet && !isTopGrid && !isCompletion)
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            bool expectsAlpha = isHud || isBakedSheet || isTopGrid || isCompletion;
            importer.alphaSource = expectsAlpha
                ? TextureImporterAlphaSource.FromInput
                : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = expectsAlpha;
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
