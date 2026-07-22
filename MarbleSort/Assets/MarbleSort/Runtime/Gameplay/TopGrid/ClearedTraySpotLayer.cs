using System;
using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    /// <summary>
    /// Owns the immutable footprints for the level's original tray formation.
    /// The layer is independent from tray views, so removing or revealing a
    /// tray never removes or moves its original footprint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClearedTraySpotLayer : MonoBehaviour
    {
        public const float PresentedSize = 0.77f;

        private const int SpotSortingOrder = 24;
        private const float SpotDepth = -0.18f;

        private readonly List<Transform> spots = new List<Transform>(16);
        private readonly Dictionary<string, GameObject> spotsByBoxId =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private Sprite artwork;

        public int SpotCount => spots.Count;

        public int VisibleSpotCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < spots.Count; index++)
                {
                    if (spots[index] != null && spots[index].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool ApprovedArtworkLoaded => artwork != null && artwork.texture != null;

        public string ArtworkName => artwork == null ? string.Empty : artwork.name;

        public Vector3 GetSpotLocalPosition(int index)
        {
            if (index < 0 || index >= spots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return spots[index].localPosition;
        }

        public void Build(TopGridData grid)
        {
            ClearSpots();
            if (grid == null || !ClearedTraySpotArtworkLibrary.TryGet(out artwork))
            {
                return;
            }

            float spacing = Mathf.Max(0.1f, grid.cellSpacing);
            float gridCenter = (grid.columns - 1) * 0.5f;
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
            TopBoxData[] boxes = grid.boxes ?? Array.Empty<TopBoxData>();
            for (int index = 0; index < boxes.Length; index++)
            {
                TopBoxData box = boxes[index];
                if (box == null)
                {
                    continue;
                }

                Vector2Int cell = new Vector2Int(box.column, box.row);
                if (!occupied.Add(cell))
                {
                    continue;
                }

                GameObject spot = new GameObject(
                    $"Cleared Tray Spot {box.column + 1:00}-{box.row + 1:00}");
                spot.transform.SetParent(transform, false);
                spot.transform.localPosition = new Vector3(
                    (cell.x - gridCenter) * spacing,
                    cell.y * spacing,
                    SpotDepth);

                SpriteRenderer renderer = spot.AddComponent<SpriteRenderer>();
                renderer.sprite = artwork;
                renderer.color = Color.white;
                renderer.sortingOrder = SpotSortingOrder;

                Vector2 bounds = artwork.bounds.size;
                float sourceSize = Mathf.Max(bounds.x, bounds.y);
                float scale = sourceSize <= Mathf.Epsilon
                    ? 1f
                    : PresentedSize / sourceSize;
                spot.transform.localScale = new Vector3(scale, scale, 1f);
                spots.Add(spot.transform);
                spotsByBoxId[box.id] = spot;
                spot.SetActive(false);
            }
        }

        public bool RevealSpot(string boxId)
        {
            if (string.IsNullOrWhiteSpace(boxId) ||
                !spotsByBoxId.TryGetValue(boxId, out GameObject spot) || spot == null)
            {
                return false;
            }

            spot.SetActive(true);
            return true;
        }

        private void ClearSpots()
        {
            artwork = null;
            for (int index = spots.Count - 1; index >= 0; index--)
            {
                Transform spot = spots[index];
                if (spot == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    spot.gameObject.SetActive(false);
                    Destroy(spot.gameObject);
                }
                else
                {
                    DestroyImmediate(spot.gameObject);
                }
            }

            spots.Clear();
            spotsByBoxId.Clear();
        }
    }
}
