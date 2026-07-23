using System;
using System.Collections.Generic;
using System.Text;
using MarbleSort.Data;
using UnityEngine;

namespace MarbleSort.Presentation
{
    /// <summary>
    /// Resolves the finished, formation-specific premium sheet sprites. Every
    /// surface, bevel and contour pixel is baked; runtime only places the art.
    /// </summary>
    public static class PremiumSheetArtworkLibrary
    {
        public const int TextureWidth = 1536;
        public const int TextureHeight = 1184;
        public const float WorldWidth = 6.64f;

        private const string ResourceFolder =
            "Presentation/Surround/Approved/Baked/PremiumSheet_";

        private static readonly Dictionary<string, Sprite> Sprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static bool TryGet(TopGridData grid, out Sprite artwork)
        {
            string key = BuildFormationKey(grid);
            if (string.IsNullOrEmpty(key))
            {
                artwork = null;
                return false;
            }

            if (Sprites.TryGetValue(key, out artwork) && artwork != null)
            {
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(ResourceFolder + key);
            if (texture == null)
            {
                Debug.LogError(
                    $"The baked premium sheet for formation '{key}' is missing. " +
                    "Run Docs/Tools/bake_premium_sheet_sprites.py before building.");
                artwork = null;
                return false;
            }

            if (texture.width != TextureWidth || texture.height != TextureHeight)
            {
                Debug.LogError(
                    $"Premium sheet '{texture.name}' must remain " +
                    $"{TextureWidth} x {TextureHeight}, but imported as " +
                    $"{texture.width} x {texture.height}.");
                artwork = null;
                return false;
            }

            artwork = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width / WorldWidth,
                0u,
                SpriteMeshType.FullRect);
            artwork.name = $"Exact Approved Premium Sheet {key}";
            artwork.hideFlags = HideFlags.HideAndDontSave;
            Sprites[key] = artwork;
            return true;
        }

        public static string BuildFormationKey(TopGridData grid)
        {
            if (grid == null)
            {
                return string.Empty;
            }

            HashSet<Vector2Int> unique = new HashSet<Vector2Int>();
            TopBoxData[] boxes = grid.boxes ?? Array.Empty<TopBoxData>();
            for (int index = 0; index < boxes.Length; index++)
            {
                TopBoxData box = boxes[index];
                if (box != null)
                {
                    unique.Add(new Vector2Int(box.column, box.row));
                }
            }

            if (unique.Count == 0)
            {
                return string.Empty;
            }

            List<Vector2Int> cells = new List<Vector2Int>(unique);
            cells.Sort((left, right) =>
            {
                int column = left.x.CompareTo(right.x);
                return column != 0 ? column : left.y.CompareTo(right.y);
            });

            StringBuilder key = new StringBuilder(64);
            key.Append('c').Append(grid.columns);
            key.Append("_s").Append(Mathf.RoundToInt(grid.cellSpacing * 1000f));
            for (int index = 0; index < cells.Count; index++)
            {
                key.Append('_')
                    .Append(cells[index].x)
                    .Append('x')
                    .Append(cells[index].y);
            }

            return key.ToString();
        }
    }
}
