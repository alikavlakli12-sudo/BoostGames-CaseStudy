using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the baked receiver artwork and trims its transparent generation canvas at runtime.
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
                        Normalize(new Rect(187f, 284f, 1183f, 500f), 1536f, 1024f),
                        Normalize(new Rect(165f, 176f, 922f, 949f), 1254f, 1254f))
                },
                {
                    "blue",
                    new CropData(
                        Normalize(new Rect(139f, 269f, 1261f, 521f), 1536f, 1024f),
                        Normalize(new Rect(174f, 190f, 903f, 912f), 1254f, 1254f))
                },
                {
                    "orange",
                    new CropData(
                        Normalize(new Rect(133f, 253f, 1270f, 537f), 1536f, 1024f),
                        Normalize(new Rect(201f, 218f, 851f, 872f), 1254f, 1254f))
                },
                {
                    "yellow",
                    new CropData(
                        Normalize(new Rect(117f, 242f, 1304f, 553f), 1536f, 1024f),
                        Normalize(new Rect(201f, 206f, 852f, 883f), 1254f, 1254f))
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
            Texture2D trayTexture = Resources.Load<Texture2D>($"{ResourceRoot}ReceiverTray_{assetSuffix}");
            Texture2D ballTexture = Resources.Load<Texture2D>($"{ResourceRoot}ReceiverBall_{assetSuffix}");
            if (trayTexture == null || ballTexture == null)
            {
                Debug.LogError($"Receiver artwork for color '{colorId}' is missing from Resources.");
                artwork = default;
                return false;
            }

            Sprite tray = CreateSprite(trayTexture, crop.Tray, $"Receiver Tray {assetSuffix}");
            Sprite ball = CreateSprite(ballTexture, crop.Ball, $"Receiver Ball {assetSuffix}");
            artwork = new ReceiverArtwork(tray, ball);
            Cache[colorId] = artwork;
            return true;
        }

        private static Sprite CreateSprite(Texture2D texture, Rect normalizedRect, string spriteName)
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

        private static Rect Normalize(Rect pixelRect, float textureWidth, float textureHeight)
        {
            return new Rect(
                pixelRect.x / textureWidth,
                pixelRect.y / textureHeight,
                pixelRect.width / textureWidth,
                pixelRect.height / textureHeight);
        }

        private readonly struct CropData
        {
            public CropData(Rect tray, Rect ball)
            {
                Tray = tray;
                Ball = ball;
            }

            public Rect Tray { get; }

            public Rect Ball { get; }
        }
    }

    public readonly struct ReceiverArtwork
    {
        public ReceiverArtwork(Sprite tray, Sprite ball)
        {
            Tray = tray;
            Ball = ball;
        }

        public Sprite Tray { get; }

        public Sprite Ball { get; }

        public bool IsValid => Tray != null && Ball != null;
    }
}
