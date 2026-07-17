using System.Collections;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Marbles;
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

            ConveyorArtworkPresenter artwork = conveyor.GetComponent<ConveyorArtworkPresenter>();
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.IsUsingArtwork, Is.True);
            Assert.That(artwork.ArtworkRenderer.sprite, Is.Not.Null);
            Assert.That(artwork.LegacyRendererCount, Is.EqualTo(28));
            Assert.That(artwork.MovingSocketCount, Is.EqualTo(24));
            Assert.That(artwork.BeltSurfaceRenderer, Is.Not.Null);
            Assert.That(artwork.BeltSurfaceRenderer.enabled, Is.True);
            Assert.That(artwork.CenterRailRenderer, Is.Not.Null);
            Assert.That(artwork.CenterRailRenderer.sprite, Is.Not.Null);
            Assert.That(
                artwork.CenterRailRenderer.sortingOrder,
                Is.GreaterThan(artwork.ArtworkRenderer.sortingOrder),
                "The single approved rail must be the final visible center layer.");
            Assert.That(conveyor.transform.localScale.x, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(conveyor.transform.localScale.y, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(artwork.ArtworkRenderer.bounds.size.x, Is.InRange(8.9f, 9.1f));

            MeshRenderer[] legacyRenderers = conveyor.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(legacyRenderers.Length, Is.EqualTo(28));
            int enabledLegacyRenderers = 0;
            foreach (MeshRenderer legacyRenderer in legacyRenderers)
            {
                if (legacyRenderer.enabled)
                {
                    enabledLegacyRenderers++;
                    Assert.That(legacyRenderer, Is.SameAs(artwork.BeltSurfaceRenderer));
                }
            }

            Assert.That(enabledLegacyRenderers, Is.EqualTo(1));

            float startingPhase = conveyor.Phase;
            Vector3 startingSocketPosition = artwork.GetMovingSocketWorldPosition(0);
            yield return new WaitForSeconds(0.1f);

            Assert.That(conveyor.Phase, Is.GreaterThan(startingPhase));
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
            MarbleActor second = pool.Rent("blue", new Vector3(0f, -1.9f, -0.24f), Vector3.zero);

            Assert.That(admission.TryQueue(first), Is.True);
            Assert.That(admission.TryQueue(second), Is.True);
            Assert.That(admission.QueuedCount, Is.EqualTo(2));

            float timeout = Time.realtimeSinceStartup + 2f;
            while (conveyor.State.OccupiedCount < 2 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(conveyor.State.OccupiedCount, Is.EqualTo(2), "Admission timed out.");
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
            Assert.That(first.transform.localScale, Is.EqualTo(Vector3.one * MarblePool.ActiveMarbleDiameter));
            Assert.That(second.transform.localScale, Is.EqualTo(Vector3.one * MarblePool.ActiveMarbleDiameter));
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
    }
}
