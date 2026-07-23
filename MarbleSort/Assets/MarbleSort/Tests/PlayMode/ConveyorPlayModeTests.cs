using System.Collections;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MarbleSort.Tests.PlayMode
{
    public sealed class ConveyorPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainScene_ConveyorBuildsExactlyTwentyFourCounterclockwiseSlots()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();

            Assert.That(conveyor, Is.Not.Null);
            Assert.That(admission, Is.Not.Null);
            Assert.That(conveyor.SlotCount, Is.EqualTo(24));
            Assert.That(conveyor.State.SlotCount, Is.EqualTo(24));
            Assert.That(conveyor.State.EmptyCount, Is.EqualTo(24));

            BoxCollider chuteTrigger = admission.GetComponent<BoxCollider>();
            Assert.That(chuteTrigger.isTrigger, Is.True);
            Assert.That(
                chuteTrigger.size.y,
                Is.EqualTo(ConveyorAdmissionController.ChuteTriggerHeight).Within(0.001f));
            Assert.That(
                chuteTrigger.center.y,
                Is.EqualTo(ConveyorAdmissionController.ChuteTriggerCenterY).Within(0.001f));

            ConveyorArtworkPresenter artwork = conveyor.GetComponent<ConveyorArtworkPresenter>();
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.IsUsingArtwork, Is.True);
            Assert.That(artwork.ArtworkRenderer.sprite, Is.Not.Null);
            Assert.That(
                artwork.AnimationFrameCount,
                Is.EqualTo(ConveyorArtworkLibrary.ExpectedAnimationFrameCount));
            Assert.That(artwork.MovingSocketCount, Is.EqualTo(24));
            Assert.That(artwork.DarkSocketCount, Is.EqualTo(12));
            Assert.That(artwork.LightSocketCount, Is.EqualTo(12));
            Assert.That(artwork.AnimationAtlas, Is.Not.Null);
            Assert.That(artwork.AnimationMaterial, Is.Not.Null);
            Assert.That(
                artwork.ArtworkRenderer.sprite.texture,
                Is.SameAs(artwork.AnimationAtlas));
            Assert.That(
                artwork.ArtworkRenderer.sharedMaterial,
                Is.SameAs(artwork.AnimationMaterial));
            int[] expectedLightSockets = { 0, 4, 5, 6, 10, 11, 12, 16, 17, 18, 22, 23 };
            for (int index = 0; index < conveyor.SlotCount; index++)
            {
                Assert.That(
                    artwork.IsMovingSocketLight(index),
                    Is.EqualTo(System.Array.IndexOf(expectedLightSockets, index) >= 0),
                    $"Socket {index} breaks the continuous 3-dark/3-light sequence.");
            }
            Assert.That(conveyor.transform.Find("Exact Approved Pre-Rendered Conveyor"), Is.Not.Null);
            Assert.That(conveyor.transform.Find("Exact Approved Animated Conveyor"), Is.Null);
            Assert.That(conveyor.transform.Find("Approved Continuous Moving Conveyor Belt"), Is.Null);
            Assert.That(conveyor.transform.Find("Approved Stationary Conveyor Frame"), Is.Null);
            Assert.That(conveyor.transform.Find("Raised Pearlescent Center Rail"), Is.Null);
            Assert.That(conveyor.transform.Find("Premium Front Chassis Rim"), Is.Null);
            Assert.That(conveyor.transform.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(conveyor.transform.localScale.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(conveyor.StraightLength, Is.EqualTo(5.125664f).Within(0.001f));
            Assert.That(conveyor.TurnRadius, Is.EqualTo(0.51f).Within(0.001f));
            Assert.That(artwork.ArtworkRenderer.bounds.size.x, Is.InRange(7.08f, 7.10f));
            Assert.That(artwork.ArtworkRenderer.bounds.size.y, Is.InRange(1.83f, 1.85f));
            Assert.That(
                artwork.ArtworkRenderer.transform.localScale.x,
                Is.EqualTo(artwork.ArtworkRenderer.transform.localScale.y).Within(0.0001f),
                "The approved conveyor animation must be scaled uniformly without aspect distortion.");

            float greatestTurnAmount = 0f;
            float smallestTurnAmount = 1f;

            for (int index = 0; index < conveyor.SlotCount; index++)
            {
                int nextIndex = (index + 1) % conveyor.SlotCount;
                StadiumPose approvedPose = ApprovedConveyorPath.Evaluate(
                    conveyor.Phase + (index / (float)conveyor.SlotCount));
                Assert.That(
                    Vector2.Distance(
                        conveyor.GetSlotView(index).localPosition,
                        approvedPose.Position),
                    Is.LessThan(0.001f),
                    $"Mechanical slot {index} drifted away from the approved artwork path.");
                Assert.That(
                    Vector2.Distance(
                        conveyor.GetSlotView(index).position,
                        conveyor.GetSlotView(nextIndex).position),
                    Is.GreaterThanOrEqualTo(
                        MarblePool.ConveyorMarbleDiameter + MarblePool.MinimumMarbleSeparation),
                    $"Conveyor slots {index} and {nextIndex} must keep conveyor marbles separated.");
                Assert.That(
                    Vector2.Distance(
                        conveyor.GetSlotView(index).position,
                        conveyor.GetSlotView(nextIndex).position),
                    Is.LessThanOrEqualTo(0.62f),
                    $"Conveyor slots {index} and {nextIndex} must remain visually close without adding sockets.");

                float turnAmount = artwork.GetMovingSocketTurnAmount(index);
                greatestTurnAmount = Mathf.Max(greatestTurnAmount, turnAmount);
                smallestTurnAmount = Mathf.Min(smallestTurnAmount, turnAmount);
            }

            Assert.That(smallestTurnAmount, Is.LessThan(0.05f));
            Assert.That(greatestTurnAmount, Is.GreaterThan(0.8f));

            Assert.That(
                conveyor.GetComponentsInChildren<MeshRenderer>(true).Length,
                Is.EqualTo(0),
                "The obsolete procedural conveyor must be physically absent, not merely disabled.");
            Assert.That(
                conveyor.GetComponentsInChildren<MeshFilter>(true).Length,
                Is.EqualTo(0),
                "No obsolete conveyor mesh data may remain behind the approved sprite.");

            Renderer[] allRenderers = conveyor.GetComponentsInChildren<Renderer>(true);
            int enabledRendererCount = 0;
            for (int index = 0; index < allRenderers.Length; index++)
            {
                if (allRenderers[index].enabled)
                {
                    enabledRendererCount++;
                    Assert.That(
                        allRenderers[index],
                        Is.SameAs(artwork.ArtworkRenderer),
                        "The conveyor must have exactly one visible renderer.");
                }
            }
            Assert.That(enabledRendererCount, Is.EqualTo(1));

            float startingPhase = conveyor.Phase;
            float startingTextureOffset = artwork.BeltTextureOffset.x;
            int startingAnimationFrame = artwork.CurrentAnimationFrameIndex;
            Sprite startingSprite = artwork.ArtworkRenderer.sprite;
            Material startingMaterial = artwork.ArtworkRenderer.sharedMaterial;
            Vector3 startingSocketPosition = artwork.GetMovingSocketWorldPosition(0);
            yield return new WaitForSeconds(0.1f);

            Assert.That(conveyor.Phase, Is.GreaterThan(startingPhase));
            Assert.That(artwork.BeltTextureOffset.x, Is.LessThan(startingTextureOffset));
            Assert.That(
                artwork.BeltTextureOffset.x,
                Is.EqualTo(-conveyor.Phase + (0.5f / conveyor.SlotCount)).Within(0.005f),
                "The exact approved belt texture must stay phase-locked to the physical conveyor slots.");
            Assert.That(
                artwork.CurrentAnimationFrameIndex,
                Is.Not.EqualTo(startingAnimationFrame),
                "The complete pre-rendered conveyor frames must advance with the mechanical phase.");
            Assert.That(
                artwork.ArtworkRenderer.sprite,
                Is.SameAs(startingSprite),
                "Atlas playback must not allocate or swap runtime sprites.");
            Assert.That(
                artwork.ArtworkRenderer.sharedMaterial,
                Is.SameAs(startingMaterial),
                "Atlas playback must reuse one shared material.");
            MaterialPropertyBlock frameProperties = new MaterialPropertyBlock();
            artwork.ArtworkRenderer.GetPropertyBlock(frameProperties);
            Assert.That(
                frameProperties.GetFloat(Shader.PropertyToID("_FrameIndex")),
                Is.EqualTo(artwork.CurrentAnimationFrameIndex).Within(0.001f));
            Assert.That(
                Vector3.Distance(startingSocketPosition, artwork.GetMovingSocketWorldPosition(0)),
                Is.GreaterThan(0.01f));
        }

        [UnityTest]
        public IEnumerator MainScene_QueuedMarblesEnterDistinctSlotsOneAtATime()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission = Object.FindFirstObjectByType<ConveyorAdmissionController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            Assert.That(conveyor, Is.Not.Null);
            Assert.That(admission, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);

            ConveyorArtworkPresenter artwork = conveyor.GetComponent<ConveyorArtworkPresenter>();
            Assert.That(artwork, Is.Not.Null);

            int prewarmedCount = pool.CreatedCount;
            MarbleActor first = pool.Rent("green", new Vector3(0f, -2.18f, -0.24f), Vector3.zero);
            MarbleActor second = pool.Rent("blue", new Vector3(0f, -1.55f, -0.24f), Vector3.zero);
            MarbleActor previouslyAdmitted = null;
            int admissionCount = 0;
            admission.MarbleAdmitted += (_, _, admitted) =>
            {
                if (previouslyAdmitted != null)
                {
                    Vector3 difference = previouslyAdmitted.transform.position -
                                         admission.ChuteExitWorldPosition;
                    float distance = new Vector2(difference.x, difference.y).magnitude;
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(admission.RequiredChuteClearance - 0.015f),
                        "The next ball entered before the previous ball cleared the chute.");
                }

                previouslyAdmitted = admitted;
                admissionCount++;
            };

            Assert.That(admission.TryQueue(first), Is.True);
            Assert.That(admission.TryQueue(second), Is.True);
            Assert.That(admission.QueuedCount, Is.EqualTo(2));

            float timeout = Time.realtimeSinceStartup + 3f;
            while (conveyor.State.OccupiedCount < 2 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(conveyor.State.OccupiedCount, Is.EqualTo(2), "Admission timed out.");
            Assert.That(admissionCount, Is.EqualTo(2));
            Assert.That(conveyor.State.ReservedCount, Is.Zero);
            Assert.That(first.MotionMode, Is.EqualTo(MarbleMotionMode.Conveyor));
            Assert.That(second.MotionMode, Is.EqualTo(MarbleMotionMode.Conveyor));
            Assert.That(pool.CreatedCount, Is.EqualTo(prewarmedCount));

            int assignedActors = 0;
            int firstSlot = -1;
            int secondSlot = -1;
            for (int index = 0; index < conveyor.SlotCount; index++)
            {
                MarbleActor occupant = conveyor.GetOccupant(index);
                if (occupant != null)
                {
                    assignedActors++;
                    Assert.That(occupant == first || occupant == second, Is.True);
                    if (occupant == first)
                    {
                        firstSlot = index;
                    }
                    else if (occupant == second)
                    {
                        secondSlot = index;
                    }
                }
            }

            Assert.That(assignedActors, Is.EqualTo(2));
            Assert.That(firstSlot, Is.Not.EqualTo(secondSlot));

            yield return new WaitForSeconds(0.5f);

            Assert.That(conveyor.GetOccupant(firstSlot), Is.SameAs(first));
            Assert.That(conveyor.GetOccupant(secondSlot), Is.SameAs(second));
            Assert.That(first.transform.localScale, Is.EqualTo(Vector3.one * MarblePool.ConveyorMarbleDiameter));
            Assert.That(second.transform.localScale, Is.EqualTo(Vector3.one * MarblePool.ConveyorMarbleDiameter));
            Assert.That(
                Vector2.Distance(first.transform.position, second.transform.position),
                Is.GreaterThanOrEqualTo(MarblePool.ConveyorMarbleDiameter),
                "Conveyor occupants must remain physically separated.");
            Assert.That(
                Vector3.Distance(first.transform.position, conveyor.GetSlotWorldPosition(firstSlot)),
                Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(second.transform.position, conveyor.GetSlotWorldPosition(secondSlot)),
                Is.LessThan(0.001f));

            Vector3 firstSocketPosition = artwork.GetMovingSocketWorldPosition(firstSlot);
            Vector3 secondSocketPosition = artwork.GetMovingSocketWorldPosition(secondSlot);
            Assert.That(
                Vector2.Distance(first.transform.position, firstSocketPosition),
                Is.LessThan(0.001f),
                "The first marble must remain centered inside its moving socket.");
            Assert.That(
                Vector2.Distance(second.transform.position, secondSocketPosition),
                Is.LessThan(0.001f),
                "The second marble must remain centered inside its moving socket.");
        }

        [UnityTest]
        public IEnumerator FastTrayRelease_CannotPenetrateOrEscapeTheSolidChute()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();
            ChuteBoundaryRig boundaryRig = Object.FindFirstObjectByType<ChuteBoundaryRig>();

            Assert.That(grid, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            Assert.That(boundaryRig, Is.Not.Null);

            System.Collections.Generic.IReadOnlyList<BoxCollider> solidBoundaries =
                boundaryRig.SolidBoundaries;
            Assert.That(
                solidBoundaries.Count,
                Is.EqualTo(ChuteBoundaryRig.ExpectedSolidColliderCount));

            Assert.That(grid.TrySelectBox("l01_top_yellow_01"), Is.True);
            float timeout = Time.realtimeSinceStartup + 2.5f;
            int inspectedLooseMarbles = 0;
            while (Time.realtimeSinceStartup < timeout)
            {
                // UnityTest coroutines resume before LateUpdate in batch mode.
                // Advance a frame, then apply the same final projection that the
                // production MarblePool performs in LateUpdate before measuring.
                yield return null;

                MarbleActor[] marbles = pool.GetComponentsInChildren<MarbleActor>(true);
                for (int marbleIndex = 0; marbleIndex < marbles.Length; marbleIndex++)
                {
                    MarbleActor marble = marbles[marbleIndex];
                    if (!marble.IsRented ||
                        marble.MotionMode != MarbleMotionMode.LoosePhysics)
                    {
                        continue;
                    }

                    boundaryRig.ProjectLooseMarbleInsideSolidChute(marble);
                    inspectedLooseMarbles++;
                    Vector3 physicsPosition = marble.Body == null
                        ? marble.transform.position
                        : marble.Body.position;
                    Assert.That(
                        physicsPosition.z,
                        Is.EqualTo(MarblePool.TransitDepth).Within(0.002f),
                        "A marble escaped the solid chute's depth plane.");

                    for (int boundaryIndex = 0;
                         boundaryIndex < solidBoundaries.Count;
                         boundaryIndex++)
                    {
                        BoxCollider boundary = solidBoundaries[boundaryIndex];
                        Vector3 nearest = boundary.ClosestPoint(physicsPosition);
                        float separation = Vector3.Distance(
                            nearest,
                            physicsPosition);
                        Assert.That(
                            separation,
                            Is.GreaterThanOrEqualTo(
                                (MarblePool.TransitMarbleDiameter * 0.5f) - 0.035f),
                            $"A marble penetrated solid boundary '{boundary.name}'.");
                    }
                }

            }

            Assert.That(
                inspectedLooseMarbles,
                Is.GreaterThan(0),
                "The stress test did not observe the released balls inside the chute.");
            Assert.That(
                pool.ReturnedBelowBoardCount,
                Is.Zero,
                "A naturally falling marble escaped below the solid admission gate.");
        }

        [UnityTest]
        public IEnumerator QueuedMarble_FallsNaturallyToGateBeforeAdmissionSettle()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            StadiumConveyorController conveyor =
                Object.FindFirstObjectByType<StadiumConveyorController>();
            ConveyorAdmissionController admission =
                Object.FindFirstObjectByType<ConveyorAdmissionController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            float startY = admission.AdmissionReadyWorldY + 0.95f;
            MarbleActor marble = pool.Rent(
                "green",
                new Vector3(0f, startY, MarblePool.TransitDepth),
                Vector3.zero);
            Assert.That(admission.TryQueue(marble), Is.True);

            yield return new WaitForSeconds(0.08f);

            Assert.That(marble.MotionMode, Is.EqualTo(MarbleMotionMode.LoosePhysics));
            Assert.That(admission.IsTransitioning, Is.False);
            Assert.That(
                marble.transform.position.y,
                Is.LessThan(startY - 0.015f),
                "The queued marble should visibly fall under gravity before admission starts.");

            float transitionTimeout = Time.realtimeSinceStartup + 2f;
            while (!admission.IsTransitioning &&
                   marble.MotionMode != MarbleMotionMode.Conveyor &&
                   Time.realtimeSinceStartup < transitionTimeout)
            {
                yield return null;
            }

            Assert.That(
                admission.LastTransitionStartPosition.y,
                Is.LessThanOrEqualTo(admission.AdmissionReadyWorldY + 0.01f),
                "Guided motion must not start high inside the chute.");

            if (admission.IsTransitioning)
            {
                float transitionStartY = marble.transform.position.y;
                yield return null;
                Assert.That(
                    marble.transform.position.y,
                    Is.LessThanOrEqualTo(transitionStartY + 0.001f),
                    "The final settle must accelerate downward without a magnetic upward arc.");
            }

            float admissionTimeout = Time.realtimeSinceStartup + 2f;
            while (marble.MotionMode != MarbleMotionMode.Conveyor &&
                   Time.realtimeSinceStartup < admissionTimeout)
            {
                yield return null;
            }

            Assert.That(marble.MotionMode, Is.EqualTo(MarbleMotionMode.Conveyor));
            Assert.That(conveyor.State.OccupiedCount, Is.EqualTo(1));
        }
    }
}
