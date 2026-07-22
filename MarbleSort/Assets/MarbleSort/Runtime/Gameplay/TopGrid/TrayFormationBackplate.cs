using System;
using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.TopGrid
{
    /// <summary>
    /// Places one immutable, pre-baked premium sheet around the initial tray
    /// formation. The sprite already contains its material, lighting, bevel,
    /// contour and transparent recess, so runtime geometry cannot distort it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrayFormationBackplate : MonoBehaviour
    {
        // Registered against the actual white-board aperture rather than the
        // narrower legacy gameplay panel measurements.
        private const float SurroundLeft = -3.32f;
        private const float SurroundRight = 3.32f;
        private const float SurroundTop = 4.70f;
        private const float MoldedOuterWidth = 0.095f;
        private const float PresentedTrayHalfWidth = 0.4655f;
        private const float PresentedTrayHalfHeight = 0.464f;
        private const float RecessClearance = 0.10f;
        private const float AlignedSheetBottom =
            -PresentedTrayHalfHeight + (MoldedOuterWidth * 0.5f);
        private const int SheetSortingOrder = 20;

        private readonly List<Transform> cutouts = new List<Transform>(16);
        private readonly List<Transform> generatedRoots = new List<Transform>(3);

        private SpriteRenderer bakedSheetRenderer;

        public int CutoutCount => cutouts.Count;

        public int FillSegmentCount => bakedSheetRenderer != null ? 1 : 0;

        public int BoundaryRailCount => 0;

        public int LayerRendererCount { get; private set; }

        public int OpenRecessCount { get; private set; }

        public float TrayClearance => RecessClearance;

        public float VisibleTrayGap =>
            0.5f + RecessClearance - (MoldedOuterWidth * 0.5f) - PresentedTrayHalfWidth;

        public float LowestVisibleSheetEdge =>
            AlignedSheetBottom - (MoldedOuterWidth * 0.5f);

        public float PresentedTrayBottomEdge => -PresentedTrayHalfHeight;

        public float TopExtent => SurroundTop;

        public bool BottomBackboardExposed => true;

        public bool OuterSidesAreSymmetric =>
            Mathf.Approximately(Mathf.Abs(SurroundLeft), SurroundRight);

        public bool ApprovedSurfaceTextureLoaded =>
            bakedSheetRenderer != null &&
            bakedSheetRenderer.sprite != null &&
            bakedSheetRenderer.sprite.texture != null;

        public bool ApprovedRimTextureLoaded => ApprovedSurfaceTextureLoaded;

        public string BakedArtworkName =>
            bakedSheetRenderer == null || bakedSheetRenderer.sprite == null
                ? string.Empty
                : bakedSheetRenderer.sprite.name;

        public Vector3 GetCutoutLocalPosition(int index)
        {
            if (index < 0 || index >= cutouts.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return cutouts[index].localPosition;
        }

        public void Build(TopGridData grid)
        {
            ClearGeneratedGeometry();
            if (grid == null)
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

                GameObject cutout = new GameObject(
                    $"Open Tray Recess {box.column + 1:00}-{box.row + 1:00}");
                cutout.transform.SetParent(transform, false);
                cutout.transform.localPosition = new Vector3(
                    (cell.x - gridCenter) * spacing,
                    cell.y * spacing,
                    0f);
                cutouts.Add(cutout.transform);
            }

            if (occupied.Count == 0)
            {
                return;
            }

            OpenRecessCount = CountOccupiedColumnRuns(grid.columns, occupied);
            if (!PremiumSheetArtworkLibrary.TryGet(grid, out Sprite artwork))
            {
                return;
            }

            Vector2 shadowOffset = PresentationMaterialLibrary.LightCastShadowOffset;
            GameObject sheetShadow = new GameObject("Premium Sheet Light Shadow");
            sheetShadow.transform.SetParent(transform, false);
            sheetShadow.transform.localPosition = new Vector3(
                shadowOffset.x,
                ((SurroundTop + AlignedSheetBottom) * 0.5f) + shadowOffset.y,
                -0.09f);
            SpriteRenderer sheetShadowRenderer = sheetShadow.AddComponent<SpriteRenderer>();
            sheetShadowRenderer.sprite = artwork;
            sheetShadowRenderer.color = PresentationMaterialLibrary.LightCastShadowColor;
            sheetShadowRenderer.sortingOrder = SheetSortingOrder - 1;
            generatedRoots.Add(sheetShadow.transform);

            GameObject sheet = new GameObject("Exact Approved Premium Formation Sheet");
            sheet.transform.SetParent(transform, false);
            sheet.transform.localPosition = new Vector3(
                0f,
                (SurroundTop + AlignedSheetBottom) * 0.5f,
                -0.10f);
            bakedSheetRenderer = sheet.AddComponent<SpriteRenderer>();
            bakedSheetRenderer.sprite = artwork;
            bakedSheetRenderer.color = Color.white;
            bakedSheetRenderer.sortingOrder = SheetSortingOrder;
            generatedRoots.Add(sheet.transform);
            LayerRendererCount = 2;
        }

        private static int CountOccupiedColumnRuns(
            int columnCount,
            HashSet<Vector2Int> occupied)
        {
            int runs = 0;
            bool previousOccupied = false;
            for (int column = 0; column < columnCount; column++)
            {
                bool currentOccupied = false;
                foreach (Vector2Int cell in occupied)
                {
                    if (cell.x == column)
                    {
                        currentOccupied = true;
                        break;
                    }
                }

                if (currentOccupied && !previousOccupied)
                {
                    runs++;
                }

                previousOccupied = currentOccupied;
            }

            return runs;
        }

        private void ClearGeneratedGeometry()
        {
            LayerRendererCount = 0;
            OpenRecessCount = 0;
            bakedSheetRenderer = null;
            DestroyGeneratedRoots(generatedRoots);
            DestroyGeneratedRoots(cutouts);
        }

        private static void DestroyGeneratedRoots(List<Transform> roots)
        {
            for (int index = roots.Count - 1; index >= 0; index--)
            {
                Transform generatedRoot = roots[index];
                if (generatedRoot == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    generatedRoot.gameObject.SetActive(false);
                    Destroy(generatedRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(generatedRoot.gameObject);
                }
            }

            roots.Clear();
        }
    }
}
