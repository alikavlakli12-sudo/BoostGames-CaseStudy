using System.Collections;
using MarbleSort.Core;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.Receivers;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MarbleSort.Tests.PlayMode
{
    public sealed class ReceiverFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainScene_LevelOneBuildsFourOrderedReceiverLanes()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();

            Assert.That(receivers, Is.Not.Null);
            Assert.That(receivers.State, Is.Not.Null);
            Assert.That(receivers.State.Lanes.Count, Is.EqualTo(4));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(6));
            Assert.That(receivers.GeneratedBoxCount, Is.EqualTo(6));
            Assert.That(receivers.State.Lanes[0].ActiveBox.ColorId, Is.EqualTo("yellow"));
            Assert.That(receivers.State.Lanes[1].ActiveBox.ColorId, Is.EqualTo("blue"));
            Assert.That(receivers.State.Lanes[2].ActiveBox.ColorId, Is.EqualTo("yellow"));
            Assert.That(receivers.State.Lanes[3].ActiveBox.ColorId, Is.EqualTo("blue"));
        }

        [UnityTest]
        public IEnumerator MatchingConveyorMarbles_FillHeadAndAdvanceTheLane()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();

            for (int count = 0; count < ReceiverBoxState.Capacity; count++)
            {
                yield return AdmitMarble("yellow", conveyor, admission, pool);
                int slotIndex = FindOccupiedSlot(conveyor, "yellow");

                Assert.That(receivers.TryCollectMatchingSlot(0, slotIndex), Is.True);
                yield return WaitForReceiverTransfer(receivers);
            }

            Assert.That(receivers.State.CompletedBoxCount, Is.EqualTo(1));
            Assert.That(hud.CompletedTrayCount, Is.EqualTo(1));
            Assert.That(hud.TotalTrayCount, Is.EqualTo(6));
            Assert.That(receivers.State.Lanes[0].ActiveBox.ColorId, Is.EqualTo("blue"));
            Assert.That(receivers.State.Lanes[0].ActiveBox.FillCount, Is.Zero);
            Assert.That(conveyor.State.OccupiedCount, Is.Zero);
            Assert.That(conveyor.State.EmptyCount, Is.EqualTo(24));
        }

        [UnityTest]
        public IEnumerator MatchingMarble_IsCollectedAutomaticallyAtTheLanePoint()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            yield return AdmitMarble("yellow", conveyor, admission, pool);

            float timeout = Time.realtimeSinceStartup + 3f;
            while (receivers.State.Lanes[0].ActiveBox.FillCount == 0 &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(receivers.State.Lanes[0].ActiveBox.FillCount, Is.EqualTo(1));
            yield return WaitForReceiverTransfer(receivers);
            Assert.That(receivers.PendingTransferCount, Is.Zero);
            Assert.That(conveyor.State.OccupiedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FullIncompatibleConveyor_DeadlocksAndRetryResetsTheLevel()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();

            for (int index = 0; index < conveyor.State.SlotCount; index++)
            {
                Assert.That(conveyor.State.TryReserve(index, "orange"), Is.True);
                Assert.That(conveyor.State.TryCommit(index), Is.True);
            }

            flow.Reevaluate();

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Deadlocked));
            Assert.That(topGrid.enabled, Is.False);
            Assert.That(admission.enabled, Is.False);
            Assert.That(receivers.CollectionEnabled, Is.False);

            flow.RetryCurrentLevel();

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(conveyor.State.EmptyCount, Is.EqualTo(24));
            Assert.That(conveyor.State.OccupiedCount, Is.Zero);
            Assert.That(topGrid.enabled, Is.True);
            Assert.That(admission.enabled, Is.True);
            Assert.That(receivers.CollectionEnabled, Is.True);
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator CompletedReceiverState_AdvancesAndBuildsTheNextLevel()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();

            for (int laneIndex = 0; laneIndex < receivers.State.Lanes.Count; laneIndex++)
            {
                while (receivers.State.Lanes[laneIndex].ActiveBox != null)
                {
                    string colorId = receivers.State.Lanes[laneIndex].ActiveBox.ColorId;
                    for (int count = 0; count < ReceiverBoxState.Capacity; count++)
                    {
                        Assert.That(receivers.State.TryAccept(laneIndex, colorId, out _), Is.True);
                    }
                }
            }

            flow.Reevaluate();
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));

            flow.AdvanceToNextLevel();

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(3));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(9));
            Assert.That(receivers.State.CompletedBoxCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator EditorPreviewSelection_LoadsEveryProductionLevelAndRejectsInvalidIndices()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();

            Assert.That(flow.IsInitialized, Is.True);
            for (int levelIndex = 0; levelIndex < bootstrap.Catalog.levels.Length; levelIndex++)
            {
                int expectedReceiverCount = 0;
                for (int laneIndex = 0;
                     laneIndex < bootstrap.Catalog.levels[levelIndex].receiverLanes.Length;
                     laneIndex++)
                {
                    expectedReceiverCount +=
                        bootstrap.Catalog.levels[levelIndex].receiverLanes[laneIndex].boxes.Length;
                }

                Assert.That(flow.TryLoadLevel(levelIndex), Is.True);
                Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(levelIndex));
                Assert.That(
                    topGrid.GeneratedBoxCount,
                    Is.EqualTo(bootstrap.Catalog.levels[levelIndex].topGrid.boxes.Length));
                Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(expectedReceiverCount));
                Assert.That(receivers.GeneratedBoxCount, Is.EqualTo(expectedReceiverCount));
                Assert.That(receivers.State.CompletedBoxCount, Is.Zero);
            }

            Assert.That(flow.TryLoadLevel(-1), Is.False);
            Assert.That(flow.TryLoadLevel(bootstrap.Catalog.levels.Length), Is.False);
            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator FinalProductionLevel_RetryRestoresItAndCompletionWrapsToLevelOne()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();

            Assert.That(flow.TryLoadLevel(4), Is.True);
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(8));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(24));

            flow.RetryCurrentLevel();

            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(4));
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(8));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(24));

            CompleteEveryReceiver(receivers);
            flow.Reevaluate();
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));

            flow.AdvanceToNextLevel();

            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.Zero);
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(2));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator LevelOne_PlaysFromTopBoxesThroughAutomaticLevelAdvance()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();

            float originalTimeScale = Time.timeScale;
            Time.timeScale = 2f;
            try
            {
                Assert.That(topGrid.TrySelectBox("l01_top_yellow_01"), Is.True);
                yield return WaitForTopGridRelease(topGrid);
                Assert.That(topGrid.TrySelectBox("l01_top_blue_01"), Is.True);
                yield return WaitForTopGridRelease(topGrid);

                float timeout = Time.realtimeSinceStartup + 12f;
                while (bootstrap.Session.CurrentLevelIndex == 0 &&
                       flow.Status != LevelFlowStatus.Deadlocked &&
                       Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(flow.Status, Is.Not.EqualTo(LevelFlowStatus.Deadlocked));
                Assert.That(
                    bootstrap.Session.CurrentLevelIndex,
                    Is.EqualTo(1),
                    "Level 1 did not complete and advance through the real gameplay loop.");
            }
            finally
            {
                Time.timeScale = originalTimeScale;
            }
        }

        private static IEnumerator AdmitMarble(
            string colorId,
            StadiumConveyorController conveyor,
            ConveyorAdmissionController admission,
            MarblePool pool)
        {
            int previousOccupiedCount = conveyor.State.OccupiedCount;
            MarbleActor marble = pool.Rent(colorId, new Vector3(0f, -2.18f, -0.24f), Vector3.zero);
            Assert.That(admission.TryQueue(marble), Is.True);

            float timeout = Time.realtimeSinceStartup + 2f;
            while (conveyor.State.OccupiedCount == previousOccupiedCount &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(conveyor.State.OccupiedCount, Is.EqualTo(previousOccupiedCount + 1));
        }

        private static IEnumerator WaitForReceiverTransfer(ReceiverQueueController receivers)
        {
            float timeout = Time.realtimeSinceStartup + 2f;
            while (receivers.PendingTransferCount > 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(receivers.PendingTransferCount, Is.Zero, "Receiver transfer timed out.");
        }

        private static IEnumerator WaitForTopGridRelease(TopGridController topGrid)
        {
            float timeout = Time.realtimeSinceStartup + 3f;
            while (topGrid.InputLocked && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(topGrid.InputLocked, Is.False, "Top-box release timed out.");
        }

        private static void CompleteEveryReceiver(ReceiverQueueController receivers)
        {
            for (int laneIndex = 0; laneIndex < receivers.State.Lanes.Count; laneIndex++)
            {
                while (receivers.State.Lanes[laneIndex].ActiveBox != null)
                {
                    string colorId = receivers.State.Lanes[laneIndex].ActiveBox.ColorId;
                    for (int count = 0; count < ReceiverBoxState.Capacity; count++)
                    {
                        Assert.That(receivers.State.TryAccept(laneIndex, colorId, out _), Is.True);
                    }
                }
            }
        }

        private static int FindOccupiedSlot(StadiumConveyorController conveyor, string colorId)
        {
            for (int index = 0; index < conveyor.State.SlotCount; index++)
            {
                ConveyorSlotState slot = conveyor.State.GetSlot(index);
                if (slot.Status == ConveyorSlotStatus.Occupied && slot.ColorId == colorId)
                {
                    return index;
                }
            }

            Assert.Fail($"No occupied '{colorId}' conveyor slot was found.");
            return -1;
        }
    }
}
