using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads complete approved receiver renders. Every occupancy state and the
    /// closed box are pre-rendered as one sprite, so the wall, rim, wells, balls,
    /// lid, outline, lighting, and spacing are never reconstructed in Unity.
    /// </summary>
    public static class ReceiverArtworkLibrary
    {
        private const float PixelsPerUnit = 100f;
        private const string ResourceRoot = "Presentation/ReceiversV3/";
        private const string LegacyBallResourceRoot = "Presentation/Receivers/";

        public const int OpenFrameCount = 4;

        private static readonly Dictionary<string, ReceiverArtwork> Cache =
            new Dictionary<string, ReceiverArtwork>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> SupportedColors =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "green", "blue", "orange", "yellow", "pink"
            };

        private static readonly Dictionary<string, Rect> BallCrops =
            new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase)
            {
                { "green", Normalize(new Rect(165f, 176f, 922f, 949f), 1254f, 1254f) },
                { "blue", Normalize(new Rect(174f, 190f, 903f, 912f), 1254f, 1254f) },
                { "orange", Normalize(new Rect(201f, 218f, 851f, 872f), 1254f, 1254f) },
                { "yellow", Normalize(new Rect(201f, 206f, 852f, 883f), 1254f, 1254f) },
                { "pink", Normalize(new Rect(174f, 190f, 903f, 912f), 1254f, 1254f) }
            };

        public static bool TryGet(string colorId, out ReceiverArtwork artwork)
        {
            if (string.IsNullOrWhiteSpace(colorId) || !SupportedColors.Contains(colorId))
            {
                artwork = default;
                return false;
            }

            if (Cache.TryGetValue(colorId, out artwork))
            {
                return artwork.IsValid;
            }

            string suffix = char.ToUpperInvariant(colorId[0]) +
                colorId.Substring(1).ToLowerInvariant();
            Sprite[] openFrames = new Sprite[OpenFrameCount];
            for (int count = 0; count < OpenFrameCount; count++)
            {
                Texture2D texture = Resources.Load<Texture2D>(
                    $"{ResourceRoot}Receiver_{suffix}_Open_{count:00}");
                if (texture == null)
                {
                    Debug.LogError(
                        $"Approved receiver open frame {count:00} for '{colorId}' is missing.");
                    artwork = default;
                    return false;
                }

                openFrames[count] = CreateTrimmedSprite(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    $"Approved Baked Receiver {suffix} Open {count:00}");
            }

            Texture2D closedTexture = Resources.Load<Texture2D>(
                $"{ResourceRoot}Receiver_{suffix}_Closed");
            Texture2D ballTexture = Resources.Load<Texture2D>(
                $"{LegacyBallResourceRoot}ReceiverBall_{suffix}");
            if (closedTexture == null || ballTexture == null ||
                !BallCrops.TryGetValue(colorId, out Rect ballCrop))
            {
                Debug.LogError($"Approved receiver artwork for '{colorId}' is incomplete.");
                artwork = default;
                return false;
            }

            Sprite closed = CreateTrimmedSprite(
                closedTexture,
                new Rect(0f, 0f, 1f, 1f),
                $"Approved Baked Receiver {suffix} Closed");
            Sprite ball = CreateTrimmedSprite(
                ballTexture,
                ballCrop,
                $"Receiver Ball {suffix}");
            artwork = new ReceiverArtwork(openFrames, closed, ball);
            Cache[colorId] = artwork;
            return true;
        }

        internal static Sprite CreateTrimmedSprite(
            Texture2D texture,
            Rect normalizedRect,
            string spriteName)
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
    }

    public readonly struct ReceiverArtwork
    {
        public ReceiverArtwork(Sprite[] openFrames, Sprite closed, Sprite ball)
        {
            OpenFrames = openFrames;
            Closed = closed;
            Ball = ball;
        }

        public Sprite[] OpenFrames { get; }

        public Sprite Closed { get; }

        public Sprite Ball { get; }

        // Compatibility names retained for callers that need the empty state or
        // an independently sized transit-ball sprite. Cap now means the complete
        // closed receiver, not a separately reconstructed lid.
        public Sprite Box => GetOpenFrame(0);

        public Sprite Cap => Closed;

        public Sprite GetOpenFrame(int fillCount)
        {
            if (OpenFrames == null || OpenFrames.Length == 0)
            {
                return null;
            }

            return OpenFrames[Mathf.Clamp(fillCount, 0, OpenFrames.Length - 1)];
        }

        public bool IsValid
        {
            get
            {
                if (Closed == null || Ball == null || OpenFrames == null ||
                    OpenFrames.Length != ReceiverArtworkLibrary.OpenFrameCount)
                {
                    return false;
                }

                for (int index = 0; index < OpenFrames.Length; index++)
                {
                    if (OpenFrames[index] == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
