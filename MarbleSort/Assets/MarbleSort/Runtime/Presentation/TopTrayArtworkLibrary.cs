using System;
using System.Collections.Generic;
using MarbleSort.Gameplay.TopGrid;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved pre-rendered 3x3 tray occupancy frames. The complete
    /// tray, tightly packed balls, outline, lighting, and front wall are baked
    /// into one sprite per state; Unity never reconstructs their appearance.
    /// </summary>
    public static class TopTrayArtworkLibrary
    {
        private const string ResourceRoot = "Presentation/TopTrays/";
        public const int OccupancyFrameCount = 10;

        private static readonly Dictionary<string, TopTrayArtwork> Cache =
            new Dictionary<string, TopTrayArtwork>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> SupportedColors =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "green", "blue", "orange", "yellow"
            };

        public static bool TryGet(string colorId, out TopTrayArtwork artwork)
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

            if (!ReceiverArtworkLibrary.TryGet(colorId, out ReceiverArtwork receiverArtwork))
            {
                artwork = default;
                return false;
            }

            string assetSuffix = char.ToUpperInvariant(colorId[0]) + colorId.Substring(1).ToLowerInvariant();
            Sprite[] occupancyFrames = new Sprite[OccupancyFrameCount];
            for (int remainingCount = 0; remainingCount < OccupancyFrameCount; remainingCount++)
            {
                Texture2D frameTexture = Resources.Load<Texture2D>(
                    $"{ResourceRoot}TopTray_{assetSuffix}_{remainingCount:00}");
                if (frameTexture == null)
                {
                    Debug.LogError(
                        $"Top-tray occupancy frame {remainingCount:00} for color " +
                        $"'{colorId}' is missing from Resources.");
                    artwork = default;
                    return false;
                }

                occupancyFrames[remainingCount] = ReceiverArtworkLibrary.CreateTrimmedSprite(
                    frameTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    $"Approved Baked Top Tray {assetSuffix} {remainingCount:00}");
            }

            artwork = new TopTrayArtwork(occupancyFrames, receiverArtwork.Ball);
            Cache[colorId] = artwork;
            return true;
        }
    }

    public readonly struct TopTrayArtwork
    {
        public TopTrayArtwork(Sprite[] occupancyFrames, Sprite ball)
        {
            OccupancyFrames = occupancyFrames;
            Ball = ball;
        }

        public Sprite[] OccupancyFrames { get; }

        public Sprite Tray => GetFrame(MarbleReleasePattern.MarbleCount);

        public Sprite Ball { get; }

        public Sprite GetFrame(int remainingCount)
        {
            if (OccupancyFrames == null || OccupancyFrames.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(remainingCount, 0, OccupancyFrames.Length - 1);
            return OccupancyFrames[index];
        }

        public bool IsValid
        {
            get
            {
                if (Ball == null || OccupancyFrames == null ||
                    OccupancyFrames.Length != TopTrayArtworkLibrary.OccupancyFrameCount)
                {
                    return false;
                }

                for (int index = 0; index < OccupancyFrames.Length; index++)
                {
                    if (OccupancyFrames[index] == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
