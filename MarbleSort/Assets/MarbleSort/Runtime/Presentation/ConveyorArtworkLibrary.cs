using System;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved baked conveyor render as a runtime sprite.
    /// The texture stays non-readable in player builds; Sprite.Create only references it.
    /// </summary>
    public static class ConveyorArtworkLibrary
    {
        public const int ExpectedAnimationFrameCount = 192;
        public const int AnimationFrameWidth = 797;
        public const int AnimationFrameHeight = 207;
        public const float AnimationPhasePeriod = 1f;

        private const float PixelsPerUnit = 100f;
        private const string BaseResourcePath = "Presentation/Conveyor/ConveyorApprovedReference";
        private const string BeltLoopResourcePath = "Presentation/Conveyor/ConveyorApprovedBeltLoop";
        private const string AnimationResourcePath = "Presentation/Conveyor/Animation";

        private static Sprite cachedBaseArtwork;
        private static Texture2D cachedBeltLoopTexture;
        private static Sprite[] cachedAnimationFrames;

        public static bool TryGet(out Sprite artwork)
        {
            if (cachedBaseArtwork != null)
            {
                artwork = cachedBaseArtwork;
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(BaseResourcePath);
            if (texture == null)
            {
                Debug.LogError($"Approved conveyor reference is missing from Resources at '{BaseResourcePath}'.");
                artwork = null;
                return false;
            }

            if (texture.width != 2172 || texture.height != 724)
            {
                Debug.LogError(
                    $"Approved conveyor reference must remain 2172 x 724, but imported as {texture.width} x {texture.height}.");
                artwork = null;
                return false;
            }

            // Exact source crop: measured opaque conveyor bounds are x=113..2056 and
            // y=144..557 in the approved 2172 x 724 render. This symmetric safety crop
            // removes only the preview background while retaining every chassis,
            // rail, highlight, and depth pixel.
            cachedBaseArtwork = Sprite.Create(
                texture,
                new Rect(100f, 154f, 1970f, 445f),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            cachedBaseArtwork.name = "Exact Approved Animated Conveyor";
            cachedBaseArtwork.hideFlags = HideFlags.HideAndDontSave;
            artwork = cachedBaseArtwork;
            return true;
        }

        public static bool TryGetBeltLoop(out Texture2D texture)
        {
            if (cachedBeltLoopTexture != null)
            {
                texture = cachedBeltLoopTexture;
                return true;
            }

            cachedBeltLoopTexture = Resources.Load<Texture2D>(BeltLoopResourcePath);
            if (cachedBeltLoopTexture == null)
            {
                Debug.LogError($"Conveyor belt loop is missing from Resources at '{BeltLoopResourcePath}'.");
                texture = null;
                return false;
            }

            cachedBeltLoopTexture.name = "Approved Continuous Conveyor Belt Loop";
            texture = cachedBeltLoopTexture;
            return true;
        }

        public static bool TryGetAnimation(out Sprite[] frames)
        {
            if (cachedAnimationFrames != null &&
                cachedAnimationFrames.Length == ExpectedAnimationFrameCount)
            {
                frames = cachedAnimationFrames;
                return true;
            }

            Texture2D[] textures = Resources.LoadAll<Texture2D>(AnimationResourcePath);
            Array.Sort(
                textures,
                (left, right) => string.CompareOrdinal(left.name, right.name));
            if (textures.Length != ExpectedAnimationFrameCount)
            {
                Debug.LogError(
                    $"Approved conveyor animation requires {ExpectedAnimationFrameCount} frames, " +
                    $"but found {textures.Length} at '{AnimationResourcePath}'.");
                frames = null;
                return false;
            }

            cachedAnimationFrames = new Sprite[textures.Length];
            for (int index = 0; index < textures.Length; index++)
            {
                Texture2D texture = textures[index];
                if (texture.width != AnimationFrameWidth ||
                    texture.height != AnimationFrameHeight)
                {
                    Debug.LogError(
                        $"Approved conveyor frame '{texture.name}' must remain " +
                        $"{AnimationFrameWidth} x {AnimationFrameHeight}.");
                    cachedAnimationFrames = null;
                    frames = null;
                    return false;
                }

                Sprite frame = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0u,
                    SpriteMeshType.FullRect);
                frame.name = texture.name;
                frame.hideFlags = HideFlags.HideAndDontSave;
                cachedAnimationFrames[index] = frame;
            }

            frames = cachedAnimationFrames;
            return true;
        }

    }
}
