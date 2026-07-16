using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.Session;
using MarbleSort.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MarbleSort.Tests.EditMode
{
    public sealed class FoundationTests
    {
        [Test]
        public void ProductionCatalog_HasNoValidationErrors()
        {
            TextAsset levelJson = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/MarbleSort/Resources/Levels/levels.json");

            Assert.That(levelJson, Is.Not.Null, "The production level catalog is missing.");

            LevelCatalogData catalog = LevelCatalogLoader.Parse(levelJson.text);
            ValidationReport report = LevelCatalogValidator.Validate(catalog);

            Assert.That(
                report.HasErrors,
                Is.False,
                BuildIssueMessage(report));
        }

        [Test]
        public void Catalog_WithRequiredRatio_HasNoErrors()
        {
            LevelCatalogData catalog = CreateValidCatalog();

            ValidationReport report = LevelCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Catalog_WithInvalidColorRatio_ReportsRatioError()
        {
            LevelCatalogData catalog = CreateValidCatalog();
            catalog.levels[0].receiverLanes[2].boxes = new BottomBoxData[0];

            ValidationReport report = LevelCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(ContainsIssue(report, "LEVEL_COLOR_RATIO"), Is.True);
        }

        [Test]
        public void Catalog_WithUnsupportedColor_ReportsClearError()
        {
            LevelCatalogData catalog = CreateValidCatalog();
            catalog.levels[0].topGrid.boxes[0].color = "purple";

            ValidationReport report = LevelCatalogValidator.Validate(catalog);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(ContainsIssue(report, "COLOR_UNSUPPORTED"), Is.True);
        }

        [Test]
        public void Session_AfterLastLevel_WrapsToFirstLevel()
        {
            GameSession session = new GameSession(5, 4);

            int nextLevel = session.AdvanceToNextLevel();

            Assert.That(nextLevel, Is.EqualTo(0));
            Assert.That(session.CurrentLevelIndex, Is.EqualTo(0));
        }

        [Test]
        public void Deadlock_WhenFullAndNoReceiverHeadMatches_ReturnsTrue()
        {
            ConveyorSlotSnapshot[] slots =
            {
                new ConveyorSlotSnapshot(true, "orange"),
                new ConveyorSlotSnapshot(true, "yellow")
            };
            ReceiverSnapshot[] receivers =
            {
                new ReceiverSnapshot(true, "green", 3),
                new ReceiverSnapshot(true, "blue", 1)
            };

            Assert.That(DeadlockDetector.IsDeadlocked(slots, receivers), Is.True);
        }

        [Test]
        public void Deadlock_WhenAReceiverHeadMatches_ReturnsFalse()
        {
            ConveyorSlotSnapshot[] slots =
            {
                new ConveyorSlotSnapshot(true, "orange"),
                new ConveyorSlotSnapshot(true, "yellow")
            };
            ReceiverSnapshot[] receivers =
            {
                new ReceiverSnapshot(true, "green", 3),
                new ReceiverSnapshot(true, "yellow", 1)
            };

            Assert.That(DeadlockDetector.IsDeadlocked(slots, receivers), Is.False);
        }

        [Test]
        public void StadiumPath_TopCenter_IsTheEntranceAndMovesLeft()
        {
            const float straightLength = 7f;
            const float turnRadius = 0.75f;
            float entry = StadiumPath.GetTopCenterNormalizedDistance(straightLength, turnRadius);

            StadiumPose pose = StadiumPath.Evaluate(entry, straightLength, turnRadius);

            Assert.That(pose.Position.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(pose.Position.y, Is.EqualTo(turnRadius).Within(0.0001f));
            Assert.That(Vector3.Dot(pose.Tangent, Vector3.left), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void TopGrid_OnlyTheLowestBoxInEachColumnIsExposed()
        {
            TopGridState grid = new TopGridState(CreateStackedTopGrid());

            Assert.That(grid.IsExposed("lower_green"), Is.True);
            Assert.That(grid.IsExposed("upper_blue"), Is.False);
            Assert.That(grid.IsExposed("single_yellow"), Is.True);
            Assert.That(grid.ActiveCount, Is.EqualTo(3));
        }

        [Test]
        public void TopGrid_RemovingAnExposedBoxCollapsesAndExposesTheNextBox()
        {
            TopGridState grid = new TopGridState(CreateStackedTopGrid());

            bool coveredRemoval = grid.TryRemoveExposed("upper_blue", out TopBoxRemovalResult coveredResult);
            bool exposedRemoval = grid.TryRemoveExposed("lower_green", out TopBoxRemovalResult result);

            Assert.That(coveredRemoval, Is.False);
            Assert.That(coveredResult, Is.Null);
            Assert.That(exposedRemoval, Is.True);
            Assert.That(result.Moves.Count, Is.EqualTo(1));
            Assert.That(result.Moves[0].BoxId, Is.EqualTo("upper_blue"));
            Assert.That(result.Moves[0].FromRow, Is.EqualTo(1));
            Assert.That(result.Moves[0].ToRow, Is.EqualTo(0));
            Assert.That(grid.GetBox("upper_blue").CurrentRow, Is.EqualTo(0));
            Assert.That(grid.IsExposed("upper_blue"), Is.True);
            Assert.That(grid.ActiveCount, Is.EqualTo(2));
        }

        [Test]
        public void MarbleReleasePattern_ContainsExactlyNineUniquePositions()
        {
            HashSet<Vector3> positions = new HashSet<Vector3>();
            for (int index = 0; index < MarbleReleasePattern.MarbleCount; index++)
            {
                positions.Add(MarbleReleasePattern.GetLocalPosition(index));
            }

            Assert.That(MarbleReleasePattern.MarbleCount, Is.EqualTo(9));
            Assert.That(TopGridController.MarblesPerBox, Is.EqualTo(9));
            Assert.That(positions.Count, Is.EqualTo(9));
        }

        [Test]
        public void MarblePool_ReturnedMarbleIsReusedAndConstrainedToGameplayPlane()
        {
            GameObject poolObject = new GameObject("Marble Pool Test");
            poolObject.SetActive(false);
            MarblePool pool = poolObject.AddComponent<MarblePool>();
            pool.Configure(null, 1, 0.2f, -5f);
            poolObject.SetActive(true);

            try
            {
                MarbleActor first = pool.Rent("green", Vector3.zero, Vector3.down);
                int createdAfterFirstRent = pool.CreatedCount;

                Assert.That(
                    (first.Body.constraints & RigidbodyConstraints.FreezePositionZ) != 0,
                    Is.True);
                Assert.That(pool.Return(first), Is.True);

                MarbleActor second = pool.Rent("blue", Vector3.one, Vector3.zero);

                Assert.That(second, Is.SameAs(first));
                Assert.That(pool.CreatedCount, Is.EqualTo(createdAfterFirstRent));
                Assert.That(pool.ActiveCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(poolObject);
            }
        }

        private static bool ContainsIssue(ValidationReport report, string code)
        {
            for (int index = 0; index < report.Issues.Count; index++)
            {
                if (report.Issues[index].Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildIssueMessage(ValidationReport report)
        {
            if (report.Issues.Count == 0)
            {
                return "No validation issues.";
            }

            List<string> messages = new List<string>(report.Issues.Count);
            for (int index = 0; index < report.Issues.Count; index++)
            {
                ValidationIssue issue = report.Issues[index];
                messages.Add($"{issue.Severity} {issue.Code}: {issue.Message}");
            }

            return string.Join("\n", messages);
        }

        private static LevelCatalogData CreateValidCatalog()
        {
            LevelCatalogData catalog = new LevelCatalogData
            {
                conveyor = new ConveyorSettingsData
                {
                    slotCount = 24,
                    unitsPerSecond = 4f,
                    straightLength = 7f,
                    turnRadius = 0.75f
                },
                levels = new LevelData[5]
            };

            for (int levelIndex = 0; levelIndex < catalog.levels.Length; levelIndex++)
            {
                string prefix = $"test_{levelIndex}";
                catalog.levels[levelIndex] = new LevelData
                {
                    id = prefix,
                    displayName = $"Test {levelIndex}",
                    topGrid = new TopGridData
                    {
                        columns = 4,
                        rows = 4,
                        cellSpacing = 1f,
                        boxes = new[]
                        {
                            new TopBoxData
                            {
                                id = $"{prefix}_top_green",
                                color = "green",
                                column = 0,
                                row = 0
                            }
                        }
                    },
                    receiverLanes = new[]
                    {
                        CreateLane(prefix, 0, "green"),
                        CreateLane(prefix, 1, "green"),
                        CreateLane(prefix, 2, "green"),
                        CreateLane(prefix, 3)
                    }
                };
            }

            return catalog;
        }

        private static TopGridData CreateStackedTopGrid()
        {
            return new TopGridData
            {
                columns = 2,
                rows = 3,
                cellSpacing = 1f,
                boxes = new[]
                {
                    new TopBoxData
                    {
                        id = "lower_green",
                        color = "green",
                        column = 0,
                        row = 0
                    },
                    new TopBoxData
                    {
                        id = "upper_blue",
                        color = "blue",
                        column = 0,
                        row = 1
                    },
                    new TopBoxData
                    {
                        id = "single_yellow",
                        color = "yellow",
                        column = 1,
                        row = 0
                    }
                }
            };
        }

        private static ReceiverLaneData CreateLane(string prefix, int index, params string[] colors)
        {
            List<BottomBoxData> boxes = new List<BottomBoxData>(colors.Length);
            for (int colorIndex = 0; colorIndex < colors.Length; colorIndex++)
            {
                boxes.Add(new BottomBoxData
                {
                    id = $"{prefix}_receiver_{index}_{colorIndex}",
                    color = colors[colorIndex]
                });
            }

            return new ReceiverLaneData
            {
                id = $"{prefix}_lane_{index}",
                position = new SerializableVector3 { x = index, y = -5f, z = 0f },
                verticalSpacing = 0.7f,
                boxes = boxes.ToArray()
            };
        }
    }
}
