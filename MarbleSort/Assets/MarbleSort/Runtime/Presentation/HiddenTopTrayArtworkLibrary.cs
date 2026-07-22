using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Loads the approved pre-rendered thin tiles used while a top tray is
    /// hidden. Their complete visual is baked; Unity only positions and swaps
    /// the sprite when the fixed-grid reveal rule is satisfied.
    /// </summary>
    public static class HiddenTopTrayArtworkLibrary
    {
        private const string ResourceRoot = "Presentation/TopTrays/Hidden/";

        private static readonly Dictionary<string, Sprite> Cache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> SupportedColors =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "green", "blue", "orange", "yellow"
            };

        public static bool TryGet(string colorId, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(colorId) || !SupportedColors.Contains(colorId))
            {
                sprite = null;
                return false;
            }

            if (Cache.TryGetValue(colorId, out sprite))
            {
                return sprite != null;
            }

            string suffix = char.ToUpperInvariant(colorId[0]) +
                            colorId.Substring(1).ToLowerInvariant();
            Texture2D texture = Resources.Load<Texture2D>(
                $"{ResourceRoot}HiddenTopTray_{suffix}");
            if (texture == null)
            {
                Debug.LogError($"Approved hidden top-tray artwork for '{colorId}' is missing.");
                sprite = null;
                return false;
            }

            sprite = ReceiverArtworkLibrary.CreateTrimmedSprite(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                $"Approved Thin Hidden Tray {suffix}");
            Cache[colorId] = sprite;
            return true;
        }
    }
}
