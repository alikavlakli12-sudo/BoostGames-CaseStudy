using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved baked conveyor render as a runtime sprite.
    /// The texture stays non-readable in player builds; Sprite.Create only references it.
    /// </summary>
    public static class ConveyorArtworkLibrary
    {
        private const float PixelsPerUnit = 100f;
        private const string BaseResourcePath = "Presentation/Conveyor/ConveyorChassisRailFree";
        private const string SlotResourcePath = "Presentation/Conveyor/ConveyorSlot";
        private const string RailResourcePath = "Presentation/Conveyor/ConveyorCenterRail";

        private static Sprite cachedBaseArtwork;
        private static Sprite cachedSlotArtwork;
        private static Sprite cachedRailArtwork;

        public static bool TryGet(out Sprite artwork)
        {
            return TryLoad(BaseResourcePath, "Hyper Realistic Conveyor", ref cachedBaseArtwork, out artwork);
        }

        public static bool TryGetSlot(out Sprite artwork)
        {
            return TryLoad(SlotResourcePath, "Hyper Realistic Conveyor Socket", ref cachedSlotArtwork, out artwork);
        }

        public static bool TryGetRail(out Sprite artwork)
        {
            return TryLoad(RailResourcePath, "Hyper Realistic Conveyor Center Rail", ref cachedRailArtwork, out artwork);
        }

        private static bool TryLoad(
            string resourcePath,
            string spriteName,
            ref Sprite cached,
            out Sprite artwork)
        {
            if (cached != null)
            {
                artwork = cached;
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogError($"Conveyor artwork is missing from Resources at '{resourcePath}'.");
                artwork = null;
                return false;
            }

            cached = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            cached.name = spriteName;
            cached.hideFlags = HideFlags.HideAndDontSave;
            artwork = cached;
            return true;
        }
    }
}
