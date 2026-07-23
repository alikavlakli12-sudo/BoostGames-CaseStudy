using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved, pre-rendered footprint revealed beneath cleared
    /// top-grid trays. Its lighting, inset and rounded bevel are baked into one
    /// sprite so runtime geometry cannot alter the approved appearance.
    /// </summary>
    public static class ClearedTraySpotArtworkLibrary
    {
        private const string ResourcePath = "Presentation/TopGrid/ClearedTraySpot";

        // Alpha bounds from the approved 1254 x 1254 production render, with a
        // four-pixel safety margin retained for clean antialiasing.
        private static readonly Rect ApprovedCrop = ReceiverArtworkLibrary.Normalize(
            new Rect(225f, 209f, 804f, 867f),
            1254f,
            1254f);

        private static Sprite cached;

        public static bool TryGet(out Sprite artwork)
        {
            if (cached != null)
            {
                artwork = cached;
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                Debug.LogError("Approved cleared-tray spot artwork is missing from Resources.");
                artwork = null;
                return false;
            }

            cached = ReceiverArtworkLibrary.CreateTrimmedSprite(
                texture,
                ApprovedCrop,
                "Approved Cleared Tray Spot");
            artwork = cached;
            return artwork != null;
        }
    }
}
