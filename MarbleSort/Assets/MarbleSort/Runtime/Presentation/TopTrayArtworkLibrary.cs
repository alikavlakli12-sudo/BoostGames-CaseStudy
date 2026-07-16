using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the baked 3x3 tray surfaces and pairs them with the approved glossy ball artwork.
    /// </summary>
    public static class TopTrayArtworkLibrary
    {
        private const string ResourceRoot = "Presentation/TopTrays/";

        private static readonly Dictionary<string, TopTrayArtwork> Cache =
            new Dictionary<string, TopTrayArtwork>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Rect> TrayCrops =
            new Dictionary<string, Rect>(StringComparer.OrdinalIgnoreCase)
            {
                { "green", ReceiverArtworkLibrary.Normalize(new Rect(133f, 134f, 987f, 983f), 1254f, 1254f) },
                { "blue", ReceiverArtworkLibrary.Normalize(new Rect(133f, 134f, 986f, 982f), 1254f, 1254f) },
                { "orange", ReceiverArtworkLibrary.Normalize(new Rect(133f, 134f, 986f, 982f), 1254f, 1254f) },
                { "yellow", ReceiverArtworkLibrary.Normalize(new Rect(133f, 133f, 987f, 983f), 1254f, 1254f) }
            };

        public static bool TryGet(string colorId, out TopTrayArtwork artwork)
        {
            if (string.IsNullOrWhiteSpace(colorId) || !TrayCrops.TryGetValue(colorId, out Rect crop))
            {
                artwork = default;
                return false;
            }

            if (Cache.TryGetValue(colorId, out artwork))
            {
                return artwork.IsValid;
            }

            if (!ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork receiverArtwork))
            {
                artwork = default;
                return false;
            }

            string assetSuffix = char.ToUpperInvariant(colorId[0]) + colorId.Substring(1).ToLowerInvariant();
            Texture2D trayTexture = Resources.Load<Texture2D>($"{ResourceRoot}TopTray_{assetSuffix}");
            if (trayTexture == null)
            {
                Debug.LogError($"Top-tray artwork for color '{colorId}' is missing from Resources.");
                artwork = default;
                return false;
            }

            Sprite tray = ReceiverArtworkLibrary.CreateTrimmedSprite(
                trayTexture,
                crop,
                $"Top Tray {assetSuffix}");
            artwork = new TopTrayArtwork(tray, receiverArtwork.Ball);
            Cache[colorId] = artwork;
            return true;
        }
    }

    public readonly struct TopTrayArtwork
    {
        public TopTrayArtwork(Sprite tray, Sprite ball)
        {
            Tray = tray;
            Ball = ball;
        }

        public Sprite Tray { get; }

        public Sprite Ball { get; }

        public bool IsValid => Tray != null && Ball != null;
    }
}
