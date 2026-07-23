using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the corrected conveyor animation from one GPU-friendly atlas.
    /// The texture stays non-readable in player builds and the descriptor is
    /// cached for the lifetime of the process.
    /// </summary>
    public static class ConveyorArtworkLibrary
    {
        public const int ExpectedAnimationFrameCount = 192;
        public const int AnimationFrameWidth = 797;
        public const int AnimationFrameHeight = 207;
        public const float AnimationPhasePeriod = 1f;
        public const int AtlasColumns = 8;
        public const int AtlasRows = 24;
        public const int AtlasPadding = 4;
        public const int AtlasCellWidth = AnimationFrameWidth + (AtlasPadding * 2);
        public const int AtlasCellHeight = AnimationFrameHeight + (AtlasPadding * 2);
        public const int AtlasWidth = AtlasColumns * AtlasCellWidth;
        public const int AtlasHeight = AtlasRows * AtlasCellHeight;

        private const float PixelsPerUnit = 100f;
        private const string AtlasResourcePath =
            "Presentation/Conveyor/ConveyorAnimationAtlas";
        private const string ShaderResourcePath =
            "Presentation/Conveyor/ConveyorAnimationAtlasShader";

        private static ConveyorAnimationAsset cachedAnimation;

        public static bool TryGetAnimation(out ConveyorAnimationAsset animation)
        {
            if (cachedAnimation != null &&
                cachedAnimation.Atlas != null &&
                cachedAnimation.FrameGeometrySprite != null &&
                cachedAnimation.Material != null)
            {
                animation = cachedAnimation;
                return true;
            }

            Texture2D atlas = Resources.Load<Texture2D>(AtlasResourcePath);
            if (atlas == null)
            {
                Debug.LogError(
                    $"Conveyor animation atlas is missing from Resources at '{AtlasResourcePath}'.");
                animation = null;
                return false;
            }

            if (atlas.width != AtlasWidth || atlas.height != AtlasHeight)
            {
                Debug.LogError(
                    $"Conveyor animation atlas must remain {AtlasWidth} x {AtlasHeight}, " +
                    $"but imported as {atlas.width} x {atlas.height}.");
                animation = null;
                return false;
            }

            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                Debug.LogError(
                    $"Conveyor atlas shader is missing from Resources at '{ShaderResourcePath}'.");
                animation = null;
                return false;
            }

            Sprite geometrySprite = BuildFrameGeometrySprite(atlas);

            Material material = new Material(shader)
            {
                name = "Conveyor Animation Atlas Material",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = atlas
            };
            material.SetVector(
                "_AtlasGeometry",
                new Vector4(
                    AnimationFrameWidth,
                    AnimationFrameHeight,
                    AtlasCellWidth,
                    AtlasCellHeight));
            material.SetVector(
                "_AtlasLayout",
                new Vector4(AtlasColumns, AtlasRows, AtlasPadding, ExpectedAnimationFrameCount));

            cachedAnimation = new ConveyorAnimationAsset(
                atlas,
                geometrySprite,
                material,
                ExpectedAnimationFrameCount,
                AtlasColumns,
                AtlasRows,
                AtlasPadding);
            animation = cachedAnimation;
            return true;
        }

        private static Sprite BuildFrameGeometrySprite(Texture2D atlas)
        {
            Rect frameZeroRect = new Rect(
                AtlasPadding,
                AtlasHeight - AtlasCellHeight + AtlasPadding,
                AnimationFrameWidth,
                AnimationFrameHeight);
            Sprite sprite = Sprite.Create(
                atlas,
                frameZeroRect,
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = "Conveyor Atlas Frame Geometry";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
