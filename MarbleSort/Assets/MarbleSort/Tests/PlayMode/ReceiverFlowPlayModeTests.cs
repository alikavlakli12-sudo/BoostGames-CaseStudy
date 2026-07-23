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
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(18));
            Assert.That(receivers.GeneratedBoxCount, Is.EqualTo(18));
            Assert.That(receivers.GeneratedLaneArtworkCount, Is.EqualTo(4));
            Assert.That(receivers.PresentationOffsetY, Is.EqualTo(-0.765f).Within(0.001f));
            Assert.That(receivers.State.Lanes[0].ActiveBox.ColorId, Is.EqualTo("yellow"));
            Assert.That(receivers.State.Lanes[1].ActiveBox.ColorId, Is.EqualTo("yellow"));
            Assert.That(receivers.State.Lanes[2].ActiveBox.ColorId, Is.EqualTo("yellow"));
            Assert.That(receivers.State.Lanes[3].ActiveBox.ColorId, Is.EqualTo("blue"));

            SpriteRenderer[] laneArtwork = FindSpriteRenderers(
                receivers.transform,
                "Flat Receiver Queue Lane");
            Assert.That(laneArtwork.Length, Is.EqualTo(4));
            for (int laneIndex = 0; laneIndex < laneArtwork.Length; laneIndex++)
            {
                ReceiverLaneState lane = receivers.State.Lanes[laneIndex];
                Transform activeRoot = FindDescendant(
                    receivers.transform,
                    $"Receiver - {lane.ActiveBox.Id}");
                SpriteRenderer body = FindSpriteRenderer(
                    activeRoot,
                    "Hyper Realistic Receiver Body");

                Assert.That(body, Is.Not.Null);
                Assert.That(laneArtwork[laneIndex].sprite.name,
                    Is.EqualTo("Approved Flat Receiver Queue Lane"));
                Assert.That(laneArtwork[laneIndex].bounds.size.x,
                    Is.LessThanOrEqualTo(body.bounds.size.x + 0.001f));
                Assert.That(laneArtwork[laneIndex].bounds.size.x,
                    Is.GreaterThanOrEqualTo(body.bounds.size.x * 0.97f));
                Assert.That(laneArtwork[laneIndex].sortingOrder, Is.LessThan(body.sortingOrder));
                Assert.That(laneArtwork[laneIndex].GetComponent<Collider>(), Is.Null);

                if (lane.Boxes.Count > lane.ActiveBoxIndex + 1)
                {
                    Transform waitingRoot = FindDescendant(
                        receivers.transform,
                        $"Receiver - {lane.Boxes[lane.ActiveBoxIndex + 1].Id}");
                    Assert.That(waitingRoot, Is.Not.Null);
                    Assert.That(
                        activeRoot.localPosition.y - waitingRoot.localPosition.y,
                        Is.EqualTo(0.63f).Within(0.001f),
                        "Waiting receivers should sit closely behind the box in front.");
                }
            }
        }

        [UnityTest]
        public IEnumerator ReceiverLids_OpenOnlyForTheActiveFrontRow()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();

            Assert.That(receivers, Is.Not.Null);
            Assert.That(receivers.GeneratedLidCount, Is.EqualTo(18));
            yield return WaitForReceiverLids(receivers);

            Assert.That(receivers.OpenLidCount, Is.EqualTo(4));
            Assert.That(receivers.ClosedLidCount, Is.EqualTo(14));
            for (int laneIndex = 0; laneIndex < receivers.State.Lanes.Count; laneIndex++)
            {
                Assert.That(receivers.IsLaneReadyForCollection(laneIndex), Is.True);

                ReceiverLaneState lane = receivers.State.Lanes[laneIndex];
                AssertWaitingBoxesLayerTowardViewer(receivers.transform, lane);
                for (int boxIndex = lane.ActiveBoxIndex + 1; boxIndex < lane.Boxes.Count; boxIndex++)
                {
                    AssertWaitingLidFullyCoversReceiver(receivers.transform, lane.Boxes[boxIndex].Id);
                }
            }
        }

        [UnityTest]
        public IEnumerator ActiveReceiver_OpensByRevealingTheAlignedBakedOpenState()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            Assert.That(receivers, Is.Not.Null);

            string activeBoxId = receivers.State.Lanes[0].ActiveBox.Id;
            Transform activeBox = FindDescendant(receivers.transform, $"Receiver - {activeBoxId}");
            Assert.That(activeBox, Is.Not.Null);

            Transform lidLift = FindDescendant(activeBox, "Receiver Lid Lift");
            Assert.That(lidLift, Is.Not.Null);
            Vector3 firstPosition = lidLift.localPosition;
            SpriteRenderer closedState = FindSpriteRenderer(
                activeBox,
                "Hyper Realistic Receiver Lid");
            Assert.That(closedState, Is.Not.Null);
            float firstAlpha = closedState.color.a;

            yield return new WaitForSeconds(0.06f);

            Assert.That(lidLift.localPosition, Is.EqualTo(firstPosition));
            Assert.That(closedState.color.a, Is.LessThan(firstAlpha));
            Assert.That(lidLift.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(FindDescendant(activeBox, "Receiver Lid Hinge"), Is.Null);
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

            yield return WaitForReceiverLids(receivers);

            for (int count = 0; count < ReceiverBoxState.Capacity; count++)
            {
                yield return AdmitMarble("yellow", conveyor, admission, pool);
                int slotIndex = FindOccupiedSlot(conveyor, "yellow");

                Assert.That(receivers.TryCollectMatchingSlot(0, slotIndex), Is.True);
                yield return WaitForReceiverTransfer(receivers);
            }

            Assert.That(receivers.State.CompletedBoxCount, Is.EqualTo(1));
            Assert.That(hud.CompletedTrayCount, Is.EqualTo(1));
            Assert.That(hud.TotalTrayCount, Is.EqualTo(18));
            Assert.That(receivers.State.Lanes[0].ActiveBox.ColorId, Is.EqualTo("blue"));
            Assert.That(receivers.State.Lanes[0].ActiveBox.FillCount, Is.Zero);
            Assert.That(receivers.GeneratedLidCount, Is.EqualTo(17));
            Assert.That(receivers.OpenLidCount, Is.EqualTo(4));
            Assert.That(receivers.ClosedLidCount, Is.EqualTo(13));
            Assert.That(receivers.IsLaneReadyForCollection(0), Is.True);
            Assert.That(conveyor.State.OccupiedCount, Is.Zero);
            Assert.That(conveyor.State.EmptyCount, Is.EqualTo(24));
        }

        [UnityTest]
        public IEnumerator CompletedReceiver_BurstsFourApprovedStarsAfterItsLidCloses()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor =
                Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission =
                Object.FindFirstObjectByType<ConveyorAdmissionController>();
            ReceiverQueueController receivers =
                Object.FindFirstObjectByType<ReceiverQueueController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            yield return WaitForReceiverLids(receivers);
            string completedBoxId = receivers.State.Lanes[0].ActiveBox.Id;
            int previousBurstCount = receivers.CompletionStarBurstCount;

            for (int count = 0; count < ReceiverBoxState.Capacity; count++)
            {
                yield return AdmitMarble("yellow", conveyor, admission, pool);
                int slotIndex = FindOccupiedSlot(conveyor, "yellow");
                Assert.That(receivers.TryCollectMatchingSlot(0, slotIndex), Is.True);

                if (count < ReceiverBoxState.Capacity - 1)
                {
                    yield return WaitForReceiverTransfer(receivers);
                }
            }

            float timeout = Time.realtimeSinceStartup + 2f;
            while (receivers.ActiveCompletionStarCount == 0 &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(receivers.ActiveCompletionStarCount, Is.EqualTo(4));
            Assert.That(receivers.CompletionStarBurstCount, Is.EqualTo(previousBurstCount + 1));

            // The burst begins at eight percent scale and reaches its authored,
            // visibly different sizes during the pop phase. Sample after that
            // phase rather than on the first frame the burst becomes active.
            yield return new WaitForSecondsRealtime(0.12f);

            Transform completedBox = FindDescendant(
                receivers.transform,
                $"Receiver - {completedBoxId}");
            Assert.That(completedBox, Is.Not.Null);
            SpriteRenderer closedLid = FindSpriteRenderer(
                completedBox,
                "Hyper Realistic Receiver Lid");
            Assert.That(closedLid, Is.Not.Null);
            Assert.That(closedLid.enabled, Is.True);
            Assert.That(closedLid.color.a, Is.GreaterThanOrEqualTo(0.99f));

            Transform burstRoot = FindDescendant(
                receivers.transform,
                "Receiver Completion Star Burst");
            Assert.That(burstRoot, Is.Not.Null);
            SpriteRenderer[] stars = burstRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(stars.Length, Is.EqualTo(4));
            for (int first = 0; first < stars.Length; first++)
            {
                Assert.That(stars[first].sprite.name, Is.EqualTo("Approved Receiver Completion Star"));
                Assert.That(stars[first].color.a, Is.GreaterThan(0f));
                for (int second = first + 1; second < stars.Length; second++)
                {
                    Assert.That(
                        Mathf.Abs(stars[first].bounds.size.x - stars[second].bounds.size.x),
                        Is.GreaterThan(0.005f),
                        "Every completion star must have a visibly different size.");
                }
            }

            yield return WaitForReceiverTransfer(receivers);
            timeout = Time.realtimeSinceStartup + 1f;
            while (receivers.ActiveCompletionStarCount > 0 &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(receivers.ActiveCompletionStarCount, Is.Zero);
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
        public IEnumerator ReceiverTransfer_ShrinksTransitBallToReceiverCupSize()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            yield return WaitForReceiverLids(receivers);
            yield return AdmitMarble("yellow", conveyor, admission, pool);

            int slotIndex = FindOccupiedSlot(conveyor, "yellow");
            MarbleActor marble = conveyor.GetOccupant(slotIndex);
            Assert.That(marble, Is.Not.Null);
            Assert.That(
                marble.VisualDiameter,
                Is.EqualTo(MarblePool.ConveyorMarbleDiameter).Within(0.002f));

            Assert.That(receivers.TryCollectMatchingSlot(0, slotIndex), Is.True);
            yield return new WaitForSeconds(0.07f);

            Assert.That(marble.MotionMode, Is.EqualTo(MarbleMotionMode.ReceiverTransition));
            Assert.That(marble.VisualDiameter, Is.LessThan(MarblePool.ConveyorMarbleDiameter));
            Assert.That(marble.VisualDiameter, Is.GreaterThan(MarblePool.ReceiverMarbleDiameter));

            yield return WaitForReceiverTransfer(receivers);
            Transform activeReceiver = FindDescendant(
                receivers.transform,
                $"Receiver - {receivers.State.Lanes[0].ActiveBox.Id}");
            Transform filledBall = FindDescendant(activeReceiver, "Glossy Receiver Ball 1");
            Assert.That(filledBall, Is.Not.Null);
            Assert.That(filledBall.gameObject.activeInHierarchy, Is.True);

            SpriteRenderer renderer = filledBall.GetComponent<SpriteRenderer>();
            float worldDiameter = renderer.sprite.bounds.size.y * Mathf.Abs(filledBall.lossyScale.y);
            Assert.That(
                worldDiameter,
                Is.EqualTo(MarblePool.ReceiverMarbleDiameter).Within(0.002f));
        }

        [UnityTest]
        public IEnumerator FollowingMatchingMarble_IsAcceptedWhilePreviousReceiverPulseIsPlaying()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            Assert.That(conveyor, Is.Not.Null);
            Assert.That(admission, Is.Not.Null);
            Assert.That(receivers, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);

            yield return WaitForReceiverLids(receivers);

            yield return AdmitMarble("yellow", conveyor, admission, pool);
            yield return AdmitMarble("yellow", conveyor, admission, pool);

            int firstSlot = -1;
            int secondSlot = -1;
            for (int index = 0; index < conveyor.SlotCount; index++)
            {
                ConveyorSlotState slot = conveyor.State.GetSlot(index);
                if (slot.Status != ConveyorSlotStatus.Occupied || slot.ColorId != "yellow")
                {
                    continue;
                }

                if (firstSlot < 0)
                {
                    firstSlot = index;
                }
                else
                {
                    secondSlot = index;
                    break;
                }
            }

            Assert.That(firstSlot, Is.GreaterThanOrEqualTo(0));
            Assert.That(secondSlot, Is.GreaterThanOrEqualTo(0));
            Assert.That(receivers.TryCollectMatchingSlot(0, firstSlot), Is.True);

            // Adjacent slots reach a receiver about 0.195 seconds apart. At this point
            // the first marble has landed, but its decorative pulse is still running.
            yield return new WaitForSeconds(0.22f);

            Assert.That(receivers.State.Lanes[0].ActiveBox.FillCount, Is.EqualTo(1));
            Assert.That(
                receivers.TryCollectMatchingSlot(0, secondSlot),
                Is.True,
                "A valid following marble must not be forced to circle the conveyor during receiver feedback.");
            Assert.That(receivers.State.Lanes[0].ActiveBox.FillCount, Is.EqualTo(2));

            yield return WaitForReceiverTransfer(receivers);
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
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(18));
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
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(6));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(18));
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
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(10));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(30));

            flow.RetryCurrentLevel();

            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(4));
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(10));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(30));

            CompleteEveryReceiver(receivers);
            flow.Reevaluate();
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));

            flow.AdvanceToNextLevel();

            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.Zero);
            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(topGrid.GeneratedBoxCount, Is.EqualTo(6));
            Assert.That(receivers.State.TotalBoxCount, Is.EqualTo(18));
        }

        [UnityTest]
        public IEnumerator LevelOne_PlaysToCompletionAndWaitsForContinueBeforeAdvancing()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();
            StadiumConveyorController conveyor =
                Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission =
                Object.FindFirstObjectByType<ConveyorAdmissionController>();
            ReceiverQueueController receivers =
                Object.FindFirstObjectByType<ReceiverQueueController>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();

            float originalTimeScale = Time.timeScale;
            Time.timeScale = 2f;
            try
            {
                yield return SelectTrayWhenBoardHasCapacity(topGrid, "l01_top_yellow_01");
                yield return WaitForTopGridRelease(topGrid);
                yield return SelectTrayWhenBoardHasCapacity(topGrid, "l01_top_blue_01");
                yield return WaitForTopGridRelease(topGrid);
                yield return SelectTrayWhenBoardHasCapacity(topGrid, "l01_top_blue_02");
                yield return WaitForTopGridRelease(topGrid);
                yield return SelectTrayWhenBoardHasCapacity(topGrid, "l01_top_yellow_02");
                yield return WaitForTopGridRelease(topGrid);
                yield return SelectTrayWhenBoardHasCapacity(topGrid, "l01_top_yellow_03");
                yield return WaitForTopGridRelease(topGrid);
                yield return SelectTrayWhenBoardHasCapacity(topGrid, "l01_top_blue_03");
                yield return WaitForTopGridRelease(topGrid);

                float timeout = Time.realtimeSinceStartup + 30f;
                while (flow.Status == LevelFlowStatus.Playing &&
                       flow.Status != LevelFlowStatus.Deadlocked &&
                       Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(flow.Status, Is.Not.EqualTo(LevelFlowStatus.Deadlocked));
                string marbleDiagnostics = string.Empty;
                MarbleActor[] activeActors = pool.GetComponentsInChildren<MarbleActor>(true);
                for (int actorIndex = 0; actorIndex < activeActors.Length; actorIndex++)
                {
                    MarbleActor actor = activeActors[actorIndex];
                    if (actor.IsRented)
                    {
                        marbleDiagnostics +=
                            $" [{actor.ColorId}:{actor.MotionMode}@{actor.transform.position}]";
                    }
                }

                string receiverDiagnostics = string.Empty;
                for (int laneIndex = 0; laneIndex < receivers.State.Lanes.Count; laneIndex++)
                {
                    ReceiverBoxState activeBox = receivers.State.Lanes[laneIndex].ActiveBox;
                    receiverDiagnostics += activeBox == null
                        ? $" [lane {laneIndex}:complete]"
                        : $" [lane {laneIndex}:{activeBox.ColorId} {activeBox.FillCount}/3]";
                }

                Assert.That(
                    flow.Status,
                    Is.EqualTo(LevelFlowStatus.Complete),
                    "Level 1 did not complete through the real gameplay loop. " +
                    $"Active marbles: {pool.ActiveCount}; conveyor: " +
                    $"{conveyor.State.OccupiedCount}; completed receivers: " +
                    $"{receivers.State.CompletedBoxCount}/{receivers.State.TotalBoxCount}; " +
                    $"queued: {admission.QueuedCount}; ready Y: " +
                    $"{admission.AdmissionReadyWorldY:0.###}; recycled below board: " +
                    $"{pool.ReturnedBelowBoardCount} " +
                    $"({pool.LastReturnedBelowBoardColorId}@" +
                    $"{pool.LastReturnedBelowBoardPosition}); pending transfers: " +
                    $"{receivers.PendingTransferCount}; actors:{marbleDiagnostics}; " +
                    $"receivers:{receiverDiagnostics}");

                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.Zero);
                Assert.That(hud.CompletionVisible, Is.True);
                Assert.That(hud.CompletionArtworkLoaded, Is.True);
                Assert.That(hud.CompletionRewardStage, Is.EqualTo(2));
                Assert.That(hud.CompletionRewardPercent, Is.EqualTo(40));
                Assert.That(hud.CompletionDisplayedPercent, Is.InRange(0f, 5f));
                Assert.That(hud.CompletionCoinReward, Is.EqualTo(25));
                Assert.That(hud.CoinBalance, Is.EqualTo(25));

                float noAutoAdvanceUntil = Time.realtimeSinceStartup + 1.9f;
                while (Time.realtimeSinceStartup < noAutoAdvanceUntil)
                {
                    yield return null;
                }

                Assert.That(
                    bootstrap.Session.CurrentLevelIndex,
                    Is.Zero,
                    "A completed level must remain on screen until Continue is pressed.");
                Assert.That(hud.CompletionDisplayedPercent, Is.InRange(40f, 45f));
                Assert.That(hud.ContinueAfterCompletion(), Is.True);
                yield return null;
                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(1));
                Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));

                CompleteEveryReceiver(receivers);
                flow.Reevaluate();
                Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));
                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(1));
                Assert.That(hud.CompletionRewardStage, Is.EqualTo(3));
                Assert.That(hud.CompletionRewardPercent, Is.EqualTo(60));
                Assert.That(hud.CompletionDisplayedPercent, Is.EqualTo(40f).Within(0.1f));
                Assert.That(hud.CoinBalance, Is.EqualTo(50));

                Assert.That(hud.ContinueAfterCompletion(), Is.True);
                yield return null;
                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(2));

                CompleteEveryReceiver(receivers);
                flow.Reevaluate();
                Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));
                Assert.That(hud.CompletionRewardStage, Is.EqualTo(4));
                Assert.That(hud.CompletionRewardPercent, Is.EqualTo(80));
                Assert.That(hud.CoinBalance, Is.EqualTo(75));

                Assert.That(hud.ContinueAfterCompletion(), Is.True);
                yield return null;
                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(3));

                CompleteEveryReceiver(receivers);
                flow.Reevaluate();
                Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));
                Assert.That(hud.CompletionRewardStage, Is.EqualTo(5));
                Assert.That(hud.CompletionRewardPercent, Is.EqualTo(100));
                Assert.That(hud.CoinBalance, Is.EqualTo(100));
                Assert.That(hud.MysteryBoxVisible, Is.False);
                Assert.That(hud.MysteryBoxArtworkLoaded, Is.True);

                Assert.That(hud.ContinueAfterCompletion(), Is.True);
                yield return null;
                Assert.That(
                    bootstrap.Session.CurrentLevelIndex,
                    Is.EqualTo(3),
                    "Level 4 must remain loaded while the milestone card is visible.");
                Assert.That(hud.CompletionVisible, Is.False);
                Assert.That(hud.MysteryBoxVisible, Is.True);
                Assert.That(hud.OverlayVisible, Is.True);
                Assert.That(hud.CoinBalance, Is.EqualTo(125));

                Assert.That(hud.ContinueAfterCompletion(), Is.True);
                yield return null;
                Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(4));
                Assert.That(hud.MysteryBoxVisible, Is.False);
                Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator EditorSkipToLevelFour_ShowsTheOneHundredPercentCompletionState()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            ReceiverQueueController receivers =
                Object.FindFirstObjectByType<ReceiverQueueController>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();

            Assert.That(flow.TryLoadLevel(3), Is.True);
            Assert.That(bootstrap.Session.CurrentLevelIndex, Is.EqualTo(3));

            CompleteEveryReceiver(receivers);
            flow.Reevaluate();

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Complete));
            Assert.That(flow.CompletionRewardStage, Is.EqualTo(5));
            Assert.That(hud.CompletionRewardPercent, Is.EqualTo(100));
            Assert.That(hud.CompletionVisible, Is.True);
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

        private static IEnumerator WaitForReceiverLids(ReceiverQueueController receivers)
        {
            float timeout = Time.realtimeSinceStartup + 2f;
            while (!AreAllReceiverLanesReady(receivers) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(
                AreAllReceiverLanesReady(receivers),
                Is.True,
                "Active receiver lids did not finish opening and unlock their lanes.");
        }

        private static bool AreAllReceiverLanesReady(ReceiverQueueController receivers)
        {
            for (int laneIndex = 0; laneIndex < receivers.State.Lanes.Count; laneIndex++)
            {
                if (!receivers.IsLaneReadyForCollection(laneIndex))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertWaitingLidFullyCoversReceiver(Transform receiversRoot, string boxId)
        {
            Transform boxRoot = FindDescendant(receiversRoot, $"Receiver - {boxId}");
            Assert.That(boxRoot, Is.Not.Null, $"Waiting receiver '{boxId}' was not rendered.");

            SpriteRenderer body = null;
            SpriteRenderer lid = null;
            SpriteRenderer[] renderers = boxRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].name == "Hyper Realistic Receiver Body")
                {
                    body = renderers[index];
                }
                else if (renderers[index].name == "Hyper Realistic Receiver Lid")
                {
                    lid = renderers[index];
                }
            }

            Assert.That(body, Is.Not.Null);
            Assert.That(lid, Is.Not.Null);

            Assert.That(lid.bounds.size.x, Is.GreaterThanOrEqualTo(body.bounds.size.x * 0.98f));
            Assert.That(lid.bounds.min.y, Is.LessThanOrEqualTo(body.bounds.center.y - 0.05f));
            Assert.That(lid.bounds.max.y, Is.GreaterThanOrEqualTo(body.bounds.max.y - 0.03f));
            Assert.That(body.color.a, Is.LessThanOrEqualTo(0.01f));
            Assert.That(lid.color.a, Is.GreaterThanOrEqualTo(0.99f));

            Transform lidLift = FindDescendant(boxRoot, "Receiver Lid Lift");
            Assert.That(lidLift, Is.Not.Null);
            Assert.That(lidLift.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(FindDescendant(boxRoot, "Receiver Lid Hinge"), Is.Null);
        }

        private static void AssertWaitingBoxesLayerTowardViewer(
            Transform receiversRoot,
            ReceiverLaneState lane)
        {
            if (lane.ActiveBox == null)
            {
                return;
            }

            Transform activeRoot = FindDescendant(receiversRoot, $"Receiver - {lane.ActiveBox.Id}");
            SpriteRenderer activeBody = FindSpriteRenderer(activeRoot, "Hyper Realistic Receiver Body");
            Assert.That(activeBody, Is.Not.Null);
            int previousSortingOrder = activeBody.sortingOrder;

            for (int boxIndex = lane.ActiveBoxIndex + 1; boxIndex < lane.Boxes.Count; boxIndex++)
            {
                Transform waitingRoot = FindDescendant(
                    receiversRoot,
                    $"Receiver - {lane.Boxes[boxIndex].Id}");
                SpriteRenderer waitingLid = FindSpriteRenderer(
                    waitingRoot,
                    "Hyper Realistic Receiver Lid");
                SpriteRenderer waitingBody = FindSpriteRenderer(
                    waitingRoot,
                    "Hyper Realistic Receiver Body");
                Assert.That(waitingLid, Is.Not.Null);
                Assert.That(waitingBody, Is.Not.Null);
                Assert.That(waitingBody.bounds.size.x, Is.EqualTo(activeBody.bounds.size.x).Within(0.01f));
                Assert.That(waitingBody.bounds.size.y, Is.EqualTo(activeBody.bounds.size.y).Within(0.01f));
                Assert.That(
                    waitingLid.sortingOrder,
                    Is.GreaterThan(previousSortingOrder),
                    "Each waiting receiver must render in front of the receiver above it.");
                previousSortingOrder = waitingLid.sortingOrder;
            }
        }

        private static SpriteRenderer FindSpriteRenderer(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].name == objectName)
                {
                    return renderers[index];
                }
            }

            return null;
        }

        private static SpriteRenderer[] FindSpriteRenderers(Transform root, string objectName)
        {
            SpriteRenderer[] allRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            System.Collections.Generic.List<SpriteRenderer> matches =
                new System.Collections.Generic.List<SpriteRenderer>();
            for (int index = 0; index < allRenderers.Length; index++)
            {
                if (allRenderers[index].name == objectName)
                {
                    matches.Add(allRenderers[index]);
                }
            }

            return matches.ToArray();
        }

        private static Transform FindDescendant(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform result = FindDescendant(parent.GetChild(index), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static IEnumerator WaitForTopGridRelease(TopGridController topGrid)
        {
            float timeout = Time.realtimeSinceStartup + 8f;
            while (topGrid.ActiveReleaseCount > 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(topGrid.ActiveReleaseCount, Is.Zero, "Top-box release timed out.");
        }

        private static IEnumerator SelectTrayWhenBoardHasCapacity(
            TopGridController topGrid,
            string boxId)
        {
            float timeout = Time.realtimeSinceStartup + 12f;
            while (!topGrid.HasCapacityForTrayRelease &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(
                topGrid.HasCapacityForTrayRelease,
                Is.True,
                $"The loose board did not make room for tray '{boxId}'.");
            Assert.That(topGrid.TrySelectBox(boxId), Is.True);
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
