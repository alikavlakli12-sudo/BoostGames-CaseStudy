using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the baked receiver box artwork and trims its transparent generation canvas at runtime.
    /// Keeping the crop data here lets the renderer preserve the approved artwork pixel-for-pixel
    /// without making the textures readable in player builds.
    /// </summary>
    public static class ReceiverArtworkLibrary
    {
        private const float PixelsPerUnit = 100f;
        private const string ResourceRoot = "Presentation/Receivers/";

        private static readonly Dictionary<string, ReceiverArtwork> Cache =
            new Dictionary<string, ReceiverArtwork>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, CropData> Crops =
            new Dictionary<string, CropData>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "green",
                    new CropData(
                        Normalize(new Rect(167f, 169f, 1202f, 687f), 1536f, 1024f),
                        Normalize(new Rect(165f, 176f, 922f, 949f), 1254f, 1254f),
                        Normalize(new Rect(154f, 344f, 1236f, 504f), 1536f, 1024f))
                },
                {
                    "blue",
                    new CropData(
                        Normalize(new Rect(167f, 169f, 1202f, 687f), 1536f, 1024f),
                        Normalize(new Rect(174f, 190f, 903f, 912f), 1254f, 1254f),
                        Normalize(new Rect(154f, 344f, 1236f, 504f), 1536f, 1024f))
                },
                {
                    "orange",
                    new CropData(
                        Normalize(new Rect(167f, 169f, 1202f, 687f), 1536f, 1024f),
                        Normalize(new Rect(201f, 218f, 851f, 872f), 1254f, 1254f),
                        Normalize(new Rect(154f, 344f, 1236f, 504f), 1536f, 1024f))
                },
                {
                    "yellow",
                    new CropData(
                        Normalize(new Rect(167f, 169f, 1202f, 687f), 1536f, 1024f),
                        Normalize(new Rect(201f, 206f, 852f, 883f), 1254f, 1254f),
                        Normalize(new Rect(154f, 344f, 1236f, 504f), 1536f, 1024f))
                }
            };

        public static bool TryGet(string colorId, out ReceiverArtwork artwork)
        {
            if (string.IsNullOrWhiteSpace(colorId) || !Crops.TryGetValue(colorId, out CropData crop))
            {
                artwork = default;
                return false;
            }

            if (Cache.TryGetValue(colorId, out artwork))
            {
                return artwork.IsValid;
            }

            string assetSuffix = char.ToUpperInvariant(colorId[0]) + colorId.Substring(1).ToLowerInvariant();
            Texture2D boxTexture = Resources.Load<Texture2D>($"{ResourceRoot}ReceiverBoxV2_{assetSuffix}");
            Texture2D ballTexture = Resources.Load<Texture2D>($"{ResourceRoot}ReceiverBall_{assetSuffix}");
            Texture2D capTexture = Resources.Load<Texture2D>($"{ResourceRoot}ReceiverCapV2_{assetSuffix}");
            if (boxTexture == null || ballTexture == null || capTexture == null)
            {
                Debug.LogError($"Receiver artwork for color '{colorId}' is missing from Resources.");
                artwork = default;
                return false;
            }

            Sprite box = CreateTrimmedSprite(boxTexture, crop.Box, $"Receiver Box V2 {assetSuffix}");
            Sprite ball = CreateTrimmedSprite(ballTexture, crop.Ball, $"Receiver Ball {assetSuffix}");
            Sprite cap = CreateTrimmedSprite(capTexture, crop.Cap, $"Receiver Cap V2 {assetSuffix}");
            artwork = new ReceiverArtwork(box, ball, cap);
            Cache[colorId] = artwork;
            return true;
        }

        internal static Sprite CreateTrimmedSprite(Texture2D texture, Rect normalizedRect, string spriteName)
        {
            Rect requestedRect = new Rect(
                normalizedRect.x * texture.width,
                normalizedRect.y * texture.height,
                normalizedRect.width * texture.width,
                normalizedRect.height * texture.height);
            Rect textureRect = new Rect(0f, 0f, texture.width, texture.height);
            float xMin = Mathf.Clamp(requestedRect.xMin, textureRect.xMin, textureRect.xMax - 1f);
            float yMin = Mathf.Clamp(requestedRect.yMin, textureRect.yMin, textureRect.yMax - 1f);
            float xMax = Mathf.Clamp(requestedRect.xMax, xMin + 1f, textureRect.xMax);
            float yMax = Mathf.Clamp(requestedRect.yMax, yMin + 1f, textureRect.yMax);
            Rect crop = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            Sprite sprite = Sprite.Create(
                texture,
                crop,
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        internal static Rect Normalize(Rect pixelRect, float textureWidth, float textureHeight)
        {
            return new Rect(
                pixelRect.x / textureWidth,
                pixelRect.y / textureHeight,
                pixelRect.width / textureWidth,
                pixelRect.height / textureHeight);
        }

        private readonly struct CropData
        {
            public CropData(Rect box, Rect ball, Rect cap)
            {
                Box = box;
                Ball = ball;
                Cap = cap;
            }

            public Rect Box { get; }

            public Rect Ball { get; }

            public Rect Cap { get; }
        }
    }

    public readonly struct ReceiverArtwork
    {
        public ReceiverArtwork(Sprite box, Sprite ball, Sprite cap)
        {
            Box = box;
            Ball = ball;
            Cap = cap;
        }

        public Sprite Box { get; }

        public Sprite Ball { get; }

        public Sprite Cap { get; }

        public bool IsValid => Box != null && Ball != null && Cap != null;
    }
}
