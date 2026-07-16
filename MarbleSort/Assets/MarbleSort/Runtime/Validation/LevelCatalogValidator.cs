using System;
using System.Collections.Generic;
using MarbleSort.Data;

namespace MarbleSort.Validation
{
    public static class LevelCatalogValidator
    {
        private static readonly HashSet<string> SupportedColors = new HashSet<string>(
            new[] { "green", "blue", "orange", "yellow" },
            StringComparer.OrdinalIgnoreCase);

        public static ValidationReport Validate(LevelCatalogData catalog)
        {
            ValidationReport report = new ValidationReport();
            if (catalog == null)
            {
                report.Add(ValidationSeverity.Error, "CATALOG_NULL", "catalog", "The catalog is null.");
                return report;
            }

            if (catalog.version < 1)
            {
                report.Add(ValidationSeverity.Error, "CATALOG_VERSION", "catalog", "Version must be at least 1.");
            }

            ValidateConveyor(catalog.conveyor, report);

            LevelData[] levels = catalog.levels ?? new LevelData[0];
            if (levels.Length < 5)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "CATALOG_MIN_LEVELS",
                    "catalog",
                    $"The case study requires at least 5 levels, but the catalog contains {levels.Length}.");
            }

            HashSet<string> levelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                LevelData level = levels[levelIndex];
                string context = level == null ? $"level[{levelIndex}]" : GetContext(level.id, $"level[{levelIndex}]");
                if (level == null)
                {
                    report.Add(ValidationSeverity.Error, "LEVEL_NULL", context, "The level entry is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(level.id))
                {
                    report.Add(ValidationSeverity.Error, "LEVEL_ID_EMPTY", context, "Every level needs a stable ID.");
                }
                else if (!levelIds.Add(level.id.Trim()))
                {
                    report.Add(ValidationSeverity.Error, "LEVEL_ID_DUPLICATE", context, $"Duplicate level ID '{level.id}'.");
                }

                ValidateLevel(level, context, report);
            }

            return report;
        }

        private static void ValidateConveyor(ConveyorSettingsData conveyor, ValidationReport report)
        {
            if (conveyor == null)
            {
                report.Add(ValidationSeverity.Error, "CONVEYOR_NULL", "catalog.conveyor", "Conveyor settings are required.");
                return;
            }

            if (conveyor.slotCount < 1)
            {
                report.Add(ValidationSeverity.Error, "CONVEYOR_SLOT_COUNT", "catalog.conveyor", "Slot count must be positive.");
            }
            else if (conveyor.slotCount != 24)
            {
                report.Add(
                    ValidationSeverity.Warning,
                    "CONVEYOR_REFERENCE_SLOT_COUNT",
                    "catalog.conveyor",
                    $"The supplied game reference uses 24 slots; this catalog uses {conveyor.slotCount}.");
            }

            if (conveyor.unitsPerSecond <= 0f)
            {
                report.Add(ValidationSeverity.Error, "CONVEYOR_SPEED", "catalog.conveyor", "Speed must be greater than zero.");
            }

            if (conveyor.straightLength <= 0f || conveyor.turnRadius <= 0f)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "CONVEYOR_GEOMETRY",
                    "catalog.conveyor",
                    "Straight length and turn radius must both be greater than zero.");
            }
        }

        private static void ValidateLevel(LevelData level, string context, ValidationReport report)
        {
            Dictionary<string, int> topCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> receiverCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> objectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ValidateTopGrid(level.topGrid, context, objectIds, topCounts, report);
            ValidateReceiverLanes(level.receiverLanes, context, objectIds, receiverCounts, report);
            ValidateColorRatio(context, topCounts, receiverCounts, report);
        }

        private static void ValidateTopGrid(
            TopGridData grid,
            string context,
            HashSet<string> objectIds,
            Dictionary<string, int> topCounts,
            ValidationReport report)
        {
            if (grid == null)
            {
                report.Add(ValidationSeverity.Error, "TOP_GRID_NULL", context, "Top-grid data is required.");
                return;
            }

            if (grid.columns < 1 || grid.rows < 1 || grid.cellSpacing <= 0f)
            {
                report.Add(ValidationSeverity.Error, "TOP_GRID_DIMENSIONS", context, "Grid dimensions and spacing must be positive.");
            }

            TopBoxData[] boxes = grid.boxes ?? new TopBoxData[0];
            if (boxes.Length == 0)
            {
                report.Add(ValidationSeverity.Error, "TOP_BOXES_EMPTY", context, "A playable level needs at least one top box.");
            }

            HashSet<string> occupiedCells = new HashSet<string>();
            for (int index = 0; index < boxes.Length; index++)
            {
                TopBoxData box = boxes[index];
                string boxContext = $"{context}.topGrid.boxes[{index}]";
                if (box == null)
                {
                    report.Add(ValidationSeverity.Error, "TOP_BOX_NULL", boxContext, "The top-box entry is null.");
                    continue;
                }

                ValidateObjectId(box.id, boxContext, objectIds, report);
                string color = ValidateColor(box.color, boxContext, report);
                if (color.Length > 0)
                {
                    AddCount(topCounts, color);
                }

                if (box.column < 0 || box.column >= grid.columns || box.row < 0 || box.row >= grid.rows)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "TOP_BOX_OUT_OF_BOUNDS",
                        boxContext,
                        $"Cell ({box.column}, {box.row}) is outside a {grid.columns}x{grid.rows} grid.");
                }

                string cellKey = $"{box.column}:{box.row}";
                if (!occupiedCells.Add(cellKey))
                {
                    report.Add(ValidationSeverity.Error, "TOP_BOX_CELL_DUPLICATE", boxContext, $"Cell {cellKey} contains more than one box.");
                }
            }
        }

        private static void ValidateReceiverLanes(
            ReceiverLaneData[] lanes,
            string context,
            HashSet<string> objectIds,
            Dictionary<string, int> receiverCounts,
            ValidationReport report)
        {
            lanes = lanes ?? new ReceiverLaneData[0];
            if (lanes.Length == 0)
            {
                report.Add(ValidationSeverity.Error, "RECEIVER_LANES_EMPTY", context, "A playable level needs receiver lanes.");
                return;
            }

            if (lanes.Length != 4)
            {
                report.Add(
                    ValidationSeverity.Warning,
                    "RECEIVER_REFERENCE_LANE_COUNT",
                    context,
                    $"The supplied game reference uses 4 receiver lanes; this level uses {lanes.Length}.");
            }

            for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
            {
                ReceiverLaneData lane = lanes[laneIndex];
                string laneContext = $"{context}.receiverLanes[{laneIndex}]";
                if (lane == null)
                {
                    report.Add(ValidationSeverity.Error, "RECEIVER_LANE_NULL", laneContext, "The receiver-lane entry is null.");
                    continue;
                }

                ValidateObjectId(lane.id, laneContext, objectIds, report);
                if (lane.verticalSpacing <= 0f)
                {
                    report.Add(ValidationSeverity.Error, "RECEIVER_LANE_SPACING", laneContext, "Vertical spacing must be positive.");
                }

                BottomBoxData[] boxes = lane.boxes ?? new BottomBoxData[0];
                if (boxes.Length == 0)
                {
                    report.Add(ValidationSeverity.Warning, "RECEIVER_LANE_EMPTY", laneContext, "This lane contains no receiver boxes.");
                }

                for (int boxIndex = 0; boxIndex < boxes.Length; boxIndex++)
                {
                    BottomBoxData box = boxes[boxIndex];
                    string boxContext = $"{laneContext}.boxes[{boxIndex}]";
                    if (box == null)
                    {
                        report.Add(ValidationSeverity.Error, "RECEIVER_BOX_NULL", boxContext, "The receiver-box entry is null.");
                        continue;
                    }

                    ValidateObjectId(box.id, boxContext, objectIds, report);
                    string color = ValidateColor(box.color, boxContext, report);
                    if (color.Length > 0)
                    {
                        AddCount(receiverCounts, color);
                    }
                }
            }
        }

        private static void ValidateColorRatio(
            string context,
            Dictionary<string, int> topCounts,
            Dictionary<string, int> receiverCounts,
            ValidationReport report)
        {
            HashSet<string> colors = new HashSet<string>(topCounts.Keys, StringComparer.OrdinalIgnoreCase);
            colors.UnionWith(receiverCounts.Keys);

            foreach (string color in colors)
            {
                int topCount = topCounts.TryGetValue(color, out int topValue) ? topValue : 0;
                int receiverCount = receiverCounts.TryGetValue(color, out int receiverValue) ? receiverValue : 0;
                int requiredReceiverCount = topCount * 3;
                if (receiverCount != requiredReceiverCount)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "LEVEL_COLOR_RATIO",
                        context,
                        $"Color '{color}' has {topCount} top boxes and {receiverCount} receiver boxes; exactly {requiredReceiverCount} receivers are required.");
                }
            }
        }

        private static void ValidateObjectId(
            string id,
            string context,
            HashSet<string> objectIds,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                report.Add(ValidationSeverity.Error, "OBJECT_ID_EMPTY", context, "Every configured object needs a stable ID.");
                return;
            }

            if (!objectIds.Add(id.Trim()))
            {
                report.Add(ValidationSeverity.Error, "OBJECT_ID_DUPLICATE", context, $"Duplicate object ID '{id}'.");
            }
        }

        private static string ValidateColor(string color, string context, ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                report.Add(ValidationSeverity.Error, "COLOR_EMPTY", context, "A marble color is required.");
                return string.Empty;
            }

            string normalized = color.Trim().ToLowerInvariant();
            if (!SupportedColors.Contains(normalized))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COLOR_UNSUPPORTED",
                    context,
                    $"Color '{color}' is not supported. Use green, blue, orange, or yellow.");
            }

            return normalized;
        }

        private static void AddCount(Dictionary<string, int> counts, string key)
        {
            counts[key] = counts.TryGetValue(key, out int current) ? current + 1 : 1;
        }

        private static string GetContext(string preferred, string fallback)
        {
            return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred.Trim();
        }
    }
}
