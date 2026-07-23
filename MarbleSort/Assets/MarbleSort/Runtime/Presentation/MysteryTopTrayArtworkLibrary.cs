using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved baked mystery tray. The grey shell, question mark,
    /// depth, lighting, and outline remain one authored sprite until reveal.
    /// </summary>
    public static class MysteryTopTrayArtworkLibrary
    {
        private const string ResourcePath =
            "Presentation/TopTrays/Mystery/MysteryTopTray";

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
                artwork = null;
                return false;
            }

            cached = ReceiverArtworkLibrary.CreateTrimmedSprite(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                "Approved Mystery Top Tray");
            artwork = cached;
            return true;
        }
    }
}
