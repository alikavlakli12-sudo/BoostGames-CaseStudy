using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
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
                                id = $"{prefix}_top_red",
                                color = "red",
                                column = 0,
                                row = 0
                            }
                        }
                    },
                    receiverLanes = new[]
                    {
                        CreateLane(prefix, 0, "red"),
                        CreateLane(prefix, 1, "red"),
                        CreateLane(prefix, 2, "red"),
                        CreateLane(prefix, 3)
                    }
                };
            }

            return catalog;
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
