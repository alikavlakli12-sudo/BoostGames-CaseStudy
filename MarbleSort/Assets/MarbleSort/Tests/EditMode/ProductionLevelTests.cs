using System;
using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Presentation;
using MarbleSort.Session;
using MarbleSort.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class ProductionLevelTests
    {
        private const string CatalogPath = "Assets/MarbleSort/Resources/Levels/levels.json";

        [Test]
        public void ProductionLevels_FollowTheApprovedDifficultyCurve()
        {
            LevelCatalogData catalog = LoadProductionCatalog();
            int[] expectedTopBoxCounts = { 6, 6, 9, 12, 8 };
            int[] expectedVisibleBoxCounts = { 4, 2, 3, 4, 4 };
            int[] expectedColorCounts = { 2, 3, 4, 4, 4 };
            int[] expectedMaximumDepths = { 2, 3, 4, 3, 3 };

            Assert.That(catalog.levels.Length, Is.EqualTo(expectedTopBoxCounts.Length));
            for (int levelIndex = 0; levelIndex < catalog.levels.Length; levelIndex++)
            {
                LevelData level = catalog.levels[levelIndex];
                Assert.That(
                    level.topGrid.boxes.Length,
                    Is.EqualTo(expectedTopBoxCounts[levelIndex]),
                    $"Unexpected top-box count in {level.displayName}.");
                Assert.That(
                    GetVisibleBoxCount(level.topGrid.boxes),
                    Is.EqualTo(expectedVisibleBoxCounts[levelIndex]),
                    $"Unexpected initially visible top-box count in {level.displayName}.");
                Assert.That(
                    GetDistinctColorCount(level.topGrid.boxes),
                    Is.EqualTo(expectedColorCounts[levelIndex]),
                    $"Unexpected color count in {level.displayName}.");
                Assert.That(
                    GetMaximumDepth(level.topGrid.boxes),
                    Is.EqualTo(expectedMaximumDepths[levelIndex]),
                    $"Unexpected stack depth in {level.displayName}.");
            }
        }

        [Test]
        public void EveryProductionLevel_HasAVerifiedSolutionSequence()
        {
            LevelCatalogData catalog = LoadProductionCatalog();

            for (int levelIndex = 0; levelIndex < catalog.levels.Length; levelIndex++)
            {
                LevelData level = catalog.levels[levelIndex];
                LevelSolvabilityResult result = LevelSolvabilityAnalyzer.Analyze(
                    level,
                    catalog.conveyor.slotCount);

                Assert.That(result.IsSolvable, Is.True, $"{level.displayName}: {result.Message}");
                Assert.That(result.SelectionSequence.Count, Is.EqualTo(level.topGrid.boxes.Length));
                Assert.That(result.PeakConveyorOccupancy, Is.InRange(1, catalog.conveyor.slotCount));
                Assert.That(result.ExploredStateCount, Is.GreaterThan(0));
            }
        }

        [Test]
        public void ProductionLevels_EveryOccupiedColumnHasAFirstRowTray()
        {
            LevelCatalogData catalog = LoadProductionCatalog();

            for (int levelIndex = 0; levelIndex < catalog.levels.Length; levelIndex++)
            {
                LevelData level = catalog.levels[levelIndex];
                HashSet<int> occupiedColumns = new HashSet<int>();
                HashSet<int> firstRowColumns = new HashSet<int>();
                for (int boxIndex = 0; boxIndex < level.topGrid.boxes.Length; boxIndex++)
                {
                    TopBoxData box = level.topGrid.boxes[boxIndex];
                    occupiedColumns.Add(box.column);
                    if (box.row == 0)
                    {
                        firstRowColumns.Add(box.column);
                    }
                }

                Assert.That(
                    firstRowColumns.SetEquals(occupiedColumns),
                    Is.True,
                    $"{level.displayName} leaves an empty first-row position below a hidden tray.");
            }
        }

        [Test]
        public void LevelsTwoAndThree_UseTheApprovedCompactFormations()
        {
            LevelCatalogData catalog = LoadProductionCatalog();

            AssertFormation(
                catalog.levels[1],
                "1x0", "2x0", "1x1", "2x1", "1x2", "2x2");
            AssertFormation(
                catalog.levels[2],
                "0x0", "1x0", "2x0",
                "0x1", "1x1",
                "0x2", "1x2",
                "0x3", "1x3");
        }

        [Test]
        public void EveryProductionLevel_LoadsItsExactBakedFormationSheet()
        {
            LevelCatalogData catalog = LoadProductionCatalog();

            for (int levelIndex = 0; levelIndex < catalog.levels.Length; levelIndex++)
            {
                LevelData level = catalog.levels[levelIndex];
                Assert.That(
                    PremiumSheetArtworkLibrary.TryGet(level.topGrid, out Sprite artwork),
                    Is.True,
                    $"Missing baked formation sheet for {level.displayName}.");
                Assert.That(artwork, Is.Not.Null);
                Assert.That(
                    artwork.texture.width,
                    Is.EqualTo(PremiumSheetArtworkLibrary.TextureWidth));
                Assert.That(
                    artwork.texture.height,
                    Is.EqualTo(PremiumSheetArtworkLibrary.TextureHeight));
            }
        }

        [Test]
        public void SolvabilityAnalyzer_WhenReceiverHeadsCanNeverOpen_RejectsLevel()
        {
            LevelData level = CreateBlockedLevel();

            LevelSolvabilityResult result = LevelSolvabilityAnalyzer.Analyze(level, 24);

            Assert.That(result.IsSolvable, Is.False);
            Assert.That(result.SelectionSequence, Is.Empty);
            Assert.That(result.Message, Does.Contain("No exposed-box selection sequence"));
        }

        [Test]
        public void Validator_WhenCatalogDoesNotHaveExactlyFiveLevels_ReportsError()
        {
            LevelCatalogData catalog = LoadProductionCatalog();
            Array.Resize(ref catalog.levels, 4);

            AssertIssue(LevelCatalogValidator.Validate(catalog), "CATALOG_LEVEL_COUNT");
        }

        [Test]
        public void Validator_WhenConveyorDoesNotHaveExactlyTwentyFourSlots_ReportsError()
        {
            LevelCatalogData catalog = LoadProductionCatalog();
            catalog.conveyor.slotCount = 23;

            AssertIssue(LevelCatalogValidator.Validate(catalog), "CONVEYOR_SLOT_COUNT_REQUIRED");
        }

        [Test]
        public void Validator_WhenReceiverLanePositionsOverlap_ReportsError()
        {
            LevelCatalogData catalog = LoadProductionCatalog();
            catalog.levels[0].receiverLanes[1].position.x = catalog.levels[0].receiverLanes[0].position.x;

            AssertIssue(LevelCatalogValidator.Validate(catalog), "RECEIVER_LANE_POSITION_DUPLICATE");
        }

        [Test]
        public void Validator_WhenAProductionLaneIsEmpty_ReportsError()
        {
            LevelCatalogData catalog = LoadProductionCatalog();
            catalog.levels[0].receiverLanes[0].boxes = Array.Empty<BottomBoxData>();

            AssertIssue(LevelCatalogValidator.Validate(catalog), "RECEIVER_LANE_EMPTY");
        }

        [Test]
        public void GameSession_SelectLevelUsesRequestedIndexAndRejectsInvalidIndices()
        {
            GameSession session = new GameSession(5);

            Assert.That(session.SelectLevel(4), Is.EqualTo(4));
            Assert.That(session.CurrentLevelIndex, Is.EqualTo(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.SelectLevel(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.SelectLevel(-1));
            Assert.That(session.CurrentLevelIndex, Is.EqualTo(4));
        }

        private static LevelCatalogData LoadProductionCatalog()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(json, Is.Not.Null, $"Missing production catalog at {CatalogPath}.");
            return LevelCatalogLoader.Parse(json.text);
        }

        private static int GetMaximumDepth(TopBoxData[] boxes)
        {
            int maximumDepth = 0;
            for (int index = 0; index < boxes.Length; index++)
            {
                maximumDepth = Math.Max(maximumDepth, boxes[index].row + 1);
            }

            return maximumDepth;
        }

        private static void AssertFormation(LevelData level, params string[] expectedCells)
        {
            HashSet<string> actualCells = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < level.topGrid.boxes.Length; index++)
            {
                TopBoxData box = level.topGrid.boxes[index];
                actualCells.Add($"{box.column}x{box.row}");
            }

            Assert.That(
                actualCells.SetEquals(expectedCells),
                Is.True,
                $"{level.displayName} does not match its approved compact formation.");
        }

        private static int GetVisibleBoxCount(TopBoxData[] boxes)
        {
            int visibleCount = 0;
            for (int index = 0; index < boxes.Length; index++)
            {
                if (boxes[index].row == 0)
                {
                    visibleCount++;
                }
            }

            return visibleCount;
        }

        private static int GetDistinctColorCount(TopBoxData[] boxes)
        {
            HashSet<string> colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < boxes.Length; index++)
            {
                colors.Add(boxes[index].color);
            }

            return colors.Count;
        }

        private static void AssertIssue(ValidationReport report, string expectedCode)
        {
            List<string> actualCodes = new List<string>(report.Issues.Count);
            for (int index = 0; index < report.Issues.Count; index++)
            {
                actualCodes.Add(report.Issues[index].Code);
                if (report.Issues[index].Code == expectedCode)
                {
                    return;
                }
            }

            Assert.Fail($"Expected validation issue '{expectedCode}', but found: {string.Join(", ", actualCodes)}");
        }

        private static LevelData CreateBlockedLevel()
        {
            TopBoxData[] topBoxes = new TopBoxData[3];
            for (int index = 0; index < topBoxes.Length; index++)
            {
                topBoxes[index] = new TopBoxData
                {
                    id = $"blocked_top_{index}",
                    color = "green",
                    column = index,
                    row = 0
                };
            }

            ReceiverLaneData[] lanes = new ReceiverLaneData[4];
            int greenReceiverIndex = 0;
            for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
            {
                int greenCount = laneIndex == 0 ? 3 : 2;
                BottomBoxData[] boxes = new BottomBoxData[greenCount + 1];
                boxes[0] = new BottomBoxData
                {
                    id = $"blocked_blue_head_{laneIndex}",
                    color = "blue"
                };

                for (int boxIndex = 1; boxIndex < boxes.Length; boxIndex++)
                {
                    boxes[boxIndex] = new BottomBoxData
                    {
                        id = $"blocked_green_{greenReceiverIndex++}",
                        color = "green"
                    };
                }

                lanes[laneIndex] = new ReceiverLaneData
                {
                    id = $"blocked_lane_{laneIndex}",
                    position = new SerializableVector3 { x = laneIndex },
                    verticalSpacing = 0.7f,
                    boxes = boxes
                };
            }

            return new LevelData
            {
                id = "blocked_level",
                displayName = "Blocked Level",
                topGrid = new TopGridData
                {
                    columns = 4,
                    rows = 4,
                    cellSpacing = 1f,
                    boxes = topBoxes
                },
                receiverLanes = lanes
            };
        }
    }
}
