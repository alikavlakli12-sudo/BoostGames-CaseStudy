using System.Collections;
using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MarbleSort.Tests.PlayMode
{
    public sealed class TopGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainScene_LevelOneBuildsAndReleasesExactlyNineMarbles()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();

            Assert.That(grid, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(6));
            Assert.That(grid.State.ActiveCount, Is.EqualTo(6));
            Assert.That(grid.State.IsExposed("l01_top_yellow_01"), Is.True);
            Assert.That(grid.State.IsExposed("l01_top_blue_01"), Is.True);
            Assert.That(grid.State.IsExposed("l01_top_yellow_02"), Is.True);
            Assert.That(grid.State.IsExposed("l01_top_blue_03"), Is.True);
            Assert.That(grid.State.IsExposed("l01_top_blue_02"), Is.False);
            Assert.That(grid.State.IsExposed("l01_top_yellow_03"), Is.False);
            Assert.That(grid.FormationBackplate, Is.Not.Null);
            Assert.That(grid.TraySpotLayer, Is.Not.Null);
            Assert.That(grid.TraySpotLayer.SpotCount, Is.EqualTo(6));
            Assert.That(grid.TraySpotLayer.VisibleSpotCount, Is.Zero);
            Assert.That(grid.TraySpotLayer.ApprovedArtworkLoaded, Is.True);
            Assert.That(grid.TraySpotLayer.ArtworkName, Is.EqualTo("Approved Cleared Tray Spot"));
            Assert.That(
                grid.TraySpotLayer.GetComponentsInChildren<Collider>(true).Length,
                Is.Zero,
                "Static tray footprints must never intercept tray input or marble physics.");
            Assert.That(grid.FormationBackplate.CutoutCount, Is.EqualTo(6));
            Assert.That(grid.FormationBackplate.FillSegmentCount, Is.EqualTo(1));
            Assert.That(grid.FormationBackplate.BoundaryRailCount, Is.Zero);
            Assert.That(grid.FormationBackplate.LayerRendererCount, Is.EqualTo(2));
            Assert.That(grid.FormationBackplate.OpenRecessCount, Is.EqualTo(1));
            Assert.That(grid.FormationBackplate.BottomBackboardExposed, Is.True);
            Assert.That(grid.FormationBackplate.TopExtent, Is.GreaterThanOrEqualTo(4.65f));
            Assert.That(grid.FormationBackplate.OuterSidesAreSymmetric, Is.True);
            Assert.That(grid.FormationBackplate.ApprovedSurfaceTextureLoaded, Is.True);
            Assert.That(grid.FormationBackplate.ApprovedRimTextureLoaded, Is.True);
            StringAssert.StartsWith(
                "Exact Approved Premium Sheet",
                grid.FormationBackplate.BakedArtworkName);
            Assert.That(
                grid.FormationBackplate.TrayClearance,
                Is.EqualTo(0.10f).Within(0.0001f),
                "The sheet must preserve a deliberate recess around the tray formation.");
            Assert.That(
                grid.FormationBackplate.VisibleTrayGap,
                Is.GreaterThanOrEqualTo(0.085f),
                "The visible molded rim must never touch a tray.");
            Assert.That(
                grid.FormationBackplate.LowestVisibleSheetEdge,
                Is.EqualTo(grid.FormationBackplate.PresentedTrayBottomEdge).Within(0.0001f),
                "The sheet silhouette and tray row must finish on the same horizontal line.");
            Assert.That(
                grid.FormationBackplate.GetComponentsInChildren<Collider>(true).Length,
                Is.Zero,
                "The static formation surround must never intercept tray input or physics.");
            Transform sheetShadow = grid.FormationBackplate.transform.Find(
                "Premium Sheet Light Shadow");
            Assert.That(sheetShadow, Is.Not.Null);
            AssertLightCastShadow(
                sheetShadow,
                grid.FormationBackplate.transform.Find(
                    "Exact Approved Premium Formation Sheet"));
            AssertNoSurroundGeometryBehindCutouts(grid.FormationBackplate);

            TopBoxView[] initialTrays = Object.FindObjectsByType<TopBoxView>(FindObjectsSortMode.None);
            Vector3[] restingPositions = new Vector3[initialTrays.Length];
            Vector3[] restingScales = new Vector3[initialTrays.Length];
            List<TopBoxView> exposedTrays = new List<TopBoxView>();
            float trayBallWorldDiameter = -1f;
            Assert.That(initialTrays.Length, Is.EqualTo(6));
            for (int index = 0; index < initialTrays.Length; index++)
            {
                restingPositions[index] = initialTrays[index].transform.position;
                restingScales[index] = initialTrays[index].transform.localScale;
                bool exposed = grid.State.IsExposed(initialTrays[index].BoxId);
                if (!exposed)
                {
                    Assert.That(initialTrays[index].TrayVisible, Is.False);
                    Assert.That(initialTrays[index].HiddenTrayVisible, Is.True);
                    Assert.That(initialTrays[index].HiddenArtworkLoaded, Is.True);
                    Assert.That(initialTrays[index].VisibleMarkerCount, Is.Zero);
                    continue;
                }

                exposedTrays.Add(initialTrays[index]);
                Assert.That(
                    restingScales[index],
                    Is.EqualTo(Vector3.one * TopBoxView.ExposedRestingScale));
                Assert.That(initialTrays[index].TrayVisible, Is.True);
                Assert.That(initialTrays[index].VisibleMarkerCount, Is.EqualTo(9));
                Assert.That(initialTrays[index].BallMaterial.GetFloat("_Glossiness"),
                    Is.EqualTo(0.72f).Within(0.001f));

                Transform trayRoot = initialTrays[index].transform.Find("Exposed Nine-Cup Tray");
                Assert.That(trayRoot, Is.Not.Null);
                Assert.That(trayRoot.localRotation, Is.EqualTo(Quaternion.identity));

                Transform trayArtwork = trayRoot.Find("Hyper Realistic 3x3 Tray");
                Assert.That(trayArtwork, Is.Not.Null);
                AssertLightCastShadow(
                    trayRoot.Find("Exposed Tray Light Shadow"),
                    trayArtwork);
                SpriteRenderer trayRenderer = trayArtwork.GetComponent<SpriteRenderer>();
                Assert.That(trayRenderer, Is.Not.Null);
                Assert.That(trayRenderer.sprite.name, Does.StartWith("Approved Baked Top Tray "));
                Assert.That(trayRenderer.sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(trayRenderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Trilinear));
                Assert.That(trayRenderer.sprite.texture.mipmapCount, Is.GreaterThan(1));
                Assert.That(
                    initialTrays[index].CurrentBakedRemainingCount,
                    Is.EqualTo(MarbleReleasePattern.MarbleCount));

                Transform marker = trayRoot.Find("Nine Marble Markers/Marker 01");
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.localPosition.z, Is.EqualTo(-0.36f).Within(0.001f));
                SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
                Assert.That(markerRenderer, Is.Not.Null);
                Assert.That(
                    markerRenderer.enabled,
                    Is.False,
                    "Mechanical release markers must not duplicate the balls baked into the approved tray frame.");
                Assert.That(markerRenderer.sprite.name, Does.StartWith("Receiver Ball "));
                float markerWorldDiameter =
                    markerRenderer.sprite.bounds.size.y * Mathf.Abs(marker.lossyScale.y);
                Assert.That(
                    markerWorldDiameter,
                    Is.EqualTo(MarblePool.RestingMarbleDiameter).Within(0.002f));
                trayBallWorldDiameter = markerWorldDiameter;
                Assert.That(initialTrays[index].transform.Find("Box Shell")
                    .GetComponent<Renderer>().enabled, Is.False);
                Assert.That(
                    initialTrays[index].transform.Find("Box Shell")
                        .GetComponent<Collider>().isTrigger,
                    Is.True,
                    "A tray's invisible input volume must never block released marbles.");
            }

            Assert.That(exposedTrays.Count, Is.EqualTo(4));
            TopBoxView leftTray = FindView("l01_top_yellow_01");
            TopBoxView rightTray = FindView("l01_top_blue_01");
            SpriteRenderer leftArtwork = leftTray.transform
                .Find("Exposed Nine-Cup Tray/Hyper Realistic 3x3 Tray")
                .GetComponent<SpriteRenderer>();
            SpriteRenderer rightArtwork = rightTray.transform
                .Find("Exposed Nine-Cup Tray/Hyper Realistic 3x3 Tray")
                .GetComponent<SpriteRenderer>();
            float trayGap = rightArtwork.bounds.min.x - leftArtwork.bounds.max.x;
            Assert.That(
                trayGap,
                Is.InRange(0.04f, 0.13f),
                "Adjacent exposed trays need a tiny, consistent visible gap.");

            yield return new WaitForSecondsRealtime(0.4f);
            for (int index = 0; index < initialTrays.Length; index++)
            {
                Assert.That(
                    initialTrays[index].transform.position,
                    Is.EqualTo(restingPositions[index]),
                    "An untouched tray must not bob up and down while idle.");
                Assert.That(
                    initialTrays[index].transform.localScale,
                    Is.EqualTo(restingScales[index]),
                    "An untouched tray must not pulse while idle.");
            }

            Transform receiverRoot = GameObject.Find("Runtime Receiver Queues").transform;
            Transform receiverMarker = FindDescendant(receiverRoot, "Glossy Receiver Ball 1");
            Assert.That(receiverMarker, Is.Not.Null);
            SpriteRenderer receiverMarkerRenderer = receiverMarker.GetComponent<SpriteRenderer>();
            float receiverBallWorldDiameter =
                receiverMarkerRenderer.sprite.bounds.size.y * Mathf.Abs(receiverMarker.lossyScale.y);
            Assert.That(
                receiverBallWorldDiameter,
                Is.EqualTo(MarblePool.ReceiverMarbleDiameter).Within(0.002f),
                "Receiver balls must fill the molded receiver cups at their dedicated size.");

            int releasedCount = 0;
            List<int> markerReleaseOrder = new List<int>();
            grid.MarblesReleased += (_, _, count) => releasedCount += count;
            grid.MarbleReleased += (_, markerIndex) => markerReleaseOrder.Add(markerIndex);

            float releaseStartedAt = Time.time;
            Assert.That(grid.TrySelectBox("l01_top_yellow_01"), Is.True);
            Assert.That(
                grid.InputLocked,
                Is.False,
                "Starting a release must not globally lock the remaining exposed trays.");

            yield return WaitForReleaseWithoutOverlap(grid, pool);
            yield return new WaitForSeconds(0.2f);

            Assert.That(releasedCount, Is.EqualTo(9));
            CollectionAssert.AreEqual(
                new[] { 6, 7, 8, 3, 4, 5, 0, 1, 2 },
                markerReleaseOrder,
                "The chute-facing bottom row must empty before the middle and top rows.");
            Assert.That(
                Time.time - releaseStartedAt,
                Is.LessThan(1.8f),
                "The complete nine-ball release and tray disappearance should feel immediate.");
            Assert.That(
                pool.ActiveCount,
                Is.InRange(1, 9),
                "Released marbles may already be collected while the release animation finishes.");
            Assert.That(grid.State.ActiveCount, Is.EqualTo(5));
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(5));
            Assert.That(
                grid.TraySpotLayer.SpotCount,
                Is.EqualTo(6),
                "Clearing a tray must reveal its permanent original footprint.");
            Assert.That(grid.TraySpotLayer.VisibleSpotCount, Is.EqualTo(1));
            Assert.That(grid.TraySpotLayer.ApprovedArtworkLoaded, Is.True);

            MarbleActor[] pooledMarbles = pool.GetComponentsInChildren<MarbleActor>(true);
            MarbleActor activeMarble = null;
            for (int index = 0; index < pooledMarbles.Length; index++)
            {
                if (pooledMarbles[index].IsRented &&
                    pooledMarbles[index].MotionMode == MarbleMotionMode.LoosePhysics)
                {
                    activeMarble = pooledMarbles[index];
                    break;
                }
            }

            Assert.That(activeMarble, Is.Not.Null);
            Assert.That(pool.MarbleDiameter, Is.EqualTo(MarblePool.TransitMarbleDiameter).Within(0.001f));
            Assert.That(
                activeMarble.transform.localScale,
                Is.EqualTo(Vector3.one * MarblePool.TransitMarbleDiameter));
            Assert.That(
                activeMarble.GetComponent<SphereCollider>().bounds.size.x,
                Is.EqualTo(MarblePool.TransitCollisionDiameter).Within(0.002f),
                "The released ball needs a slightly oversized solid shell so dense piles never visually compress.");
            Assert.That(activeMarble.GetComponent<Renderer>().sharedMaterial.GetFloat("_Glossiness"),
                Is.EqualTo(0.72f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MainScene_BoardCapacityBlocksFifthConcurrentTrayAndRecoversWithoutPoolGrowth()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();
            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            RuntimePerformanceProbe performance =
                Object.FindFirstObjectByType<RuntimePerformanceProbe>();
            MarbleSort.Gameplay.Conveyor.ConveyorAdmissionController admission =
                Object.FindFirstObjectByType<
                    MarbleSort.Gameplay.Conveyor.ConveyorAdmissionController>();
            MarbleSort.Gameplay.Conveyor.StadiumConveyorController conveyor =
                Object.FindFirstObjectByType<MarbleSort.Gameplay.Conveyor.StadiumConveyorController>();

            Assert.That(grid, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);
            Assert.That(flow, Is.Not.Null);
            Assert.That(performance, Is.Not.Null);
            Assert.That(admission, Is.Not.Null);
            Assert.That(conveyor, Is.Not.Null);
            Assert.That(TopGridController.LooseBoardMarbleCapacity, Is.EqualTo(36));
            Assert.That(pool.InitialCapacity, Is.EqualTo(MarblePool.DefaultInitialCapacity));
            Assert.That(pool.CreatedCount, Is.EqualTo(MarblePool.DefaultInitialCapacity));
            Assert.That(pool.RuntimeExpansionCount, Is.Zero);
            Assert.That(conveyor.BoardClearedSpeedActive, Is.False);
            Assert.That(
                conveyor.CurrentUnitsPerSecond,
                Is.EqualTo(conveyor.BaseUnitsPerSecond).Within(0.001f));

            float originalTimeScale = Time.timeScale;
            Time.timeScale = 2f;
            try
            {
                // Hold admission temporarily so the measured stress window has
                // the true maximum of 36 active loose rigidbodies on the board.
                admission.enabled = false;

                // Four immediate reservations account for all 36 permitted loose
                // marbles before any release coroutine has spawned its first ball.
                Assert.That(grid.TrySelectBox("l01_top_yellow_01"), Is.True);
                Assert.That(grid.TrySelectBox("l01_top_blue_01"), Is.True);
                Assert.That(grid.TrySelectBox("l01_top_blue_02"), Is.True);
                Assert.That(grid.TrySelectBox("l01_top_yellow_02"), Is.True);
                Assert.That(grid.ReservedLooseMarbleCount, Is.EqualTo(36));
                Assert.That(grid.ProjectedLooseMarbleCount, Is.EqualTo(36));
                Assert.That(grid.HasCapacityForTrayRelease, Is.False);

                TopBoxView rejectedTray = FindView("l01_top_yellow_03");
                Assert.That(grid.State.IsExposed(rejectedTray.BoxId), Is.True);
                Assert.That(grid.TrySelectBox(rejectedTray.BoxId), Is.False);
                Assert.That(grid.BoardFullRejectionCount, Is.EqualTo(1));
                Assert.That(rejectedTray.BoardFullFeedbackVisible, Is.True);
                Assert.That(rejectedTray.BoardFullFeedbackText, Is.EqualTo("Board Full"));
                Assert.That(grid.State.ActiveCount, Is.EqualTo(2));
                Assert.That(grid.ActiveReleaseCount, Is.EqualTo(4));
                Assert.That(rejectedTray.VisibleMarkerCount, Is.EqualTo(9));
                Assert.That(conveyor.BoardClearedSpeedActive, Is.False);

                yield return null;
                Assert.That(rejectedTray.BoardFullFeedbackVisible, Is.True);

                float releaseTimeout = Time.realtimeSinceStartup + 12f;
                while (pool.LooseMarbleCount < 30 &&
                       Time.realtimeSinceStartup < releaseTimeout)
                {
                    yield return null;
                }

                // The solid board settles a dense pile while any remaining
                // reserved marbles wait safely in their trays for physical
                // release clearance. Together they consume the complete
                // 36-marble capacity budget.
                Assert.That(pool.LooseMarbleCount, Is.InRange(30, 36));
                Assert.That(grid.ProjectedLooseMarbleCount, Is.EqualTo(36));
                Assert.That(
                    grid.ReservedLooseMarbleCount,
                    Is.EqualTo(36 - pool.LooseMarbleCount));
                Assert.That(pool.PeakLooseMarbleCount, Is.InRange(30, 36));
                Assert.That(pool.RuntimeExpansionCount, Is.Zero);

                Time.timeScale = 1f;
                performance.ResetMeasurements();
                int greatestObservedLooseCount = pool.LooseMarbleCount;
                int greatestProjectedLooseCount = grid.ProjectedLooseMarbleCount;
                int greatestRuntimeExpansionCount = pool.RuntimeExpansionCount;
                float lowestRenderedSeparation = pool.LastRenderedMinimumSeparation;
                float stressUntil = Time.realtimeSinceStartup + 4f;
                while (performance.FrameSampleCount < 120 &&
                       Time.realtimeSinceStartup < stressUntil)
                {
                    yield return null;
                    greatestObservedLooseCount = Mathf.Max(
                        greatestObservedLooseCount,
                        pool.LooseMarbleCount);
                    greatestProjectedLooseCount = Mathf.Max(
                        greatestProjectedLooseCount,
                        grid.ProjectedLooseMarbleCount);
                    greatestRuntimeExpansionCount = Mathf.Max(
                        greatestRuntimeExpansionCount,
                        pool.RuntimeExpansionCount);
                    lowestRenderedSeparation = Mathf.Min(
                        lowestRenderedSeparation,
                        pool.LastRenderedMinimumSeparation);
                }

                Debug.Log(
                    $"Dense-board performance: {performance.FrameSampleCount} frames, " +
                    $"{performance.AverageFramesPerSecond:0.0} average FPS, " +
                    $"{performance.WorstFrameMilliseconds:0.00} ms worst frame, " +
                    $"{performance.GenerationZeroCollections} Gen0 collections, " +
                    $"{pool.PeakLooseMarbleCount} peak loose marbles, " +
                    $"{pool.RuntimeExpansionCount} pool expansions.");

                Assert.That(
                    greatestObservedLooseCount,
                    Is.GreaterThanOrEqualTo(8),
                    "The board-capacity stress test did not create a meaningful physical pile.");
                Assert.That(performance.FrameSampleCount, Is.EqualTo(120));
                Assert.That(performance.AverageFramesPerSecond, Is.GreaterThanOrEqualTo(50f));
                Assert.That(performance.WorstFrameMilliseconds, Is.LessThan(33.34f));
                Assert.That(greatestProjectedLooseCount, Is.LessThanOrEqualTo(36));
                Assert.That(greatestRuntimeExpansionCount, Is.Zero);
                Assert.That(
                    lowestRenderedSeparation,
                    Is.GreaterThanOrEqualTo(MarblePool.TransitMarbleDiameter - 0.001f));
                Assert.That(pool.PeakLooseMarbleCount, Is.LessThanOrEqualTo(36));
                Assert.That(pool.CreatedCount, Is.EqualTo(MarblePool.DefaultInitialCapacity));
                Assert.That(pool.RuntimeExpansionCount, Is.Zero);

                Time.timeScale = 2f;
                admission.enabled = true;
                yield return WaitForTrayCapacityWithoutGrowth(grid, pool);
                Assert.That(grid.TrySelectBox(rejectedTray.BoxId), Is.True);
                Assert.That(rejectedTray.BoardFullFeedbackVisible, Is.False);
                Assert.That(grid.ProjectedLooseMarbleCount, Is.LessThanOrEqualTo(36));

                yield return WaitForTrayCapacityWithoutGrowth(grid, pool);
                Assert.That(grid.TrySelectBox("l01_top_blue_03"), Is.True);
                Assert.That(grid.State.ActiveCount, Is.Zero);
                Assert.That(grid.InputLocked, Is.False);
                Assert.That(conveyor.BoardClearedSpeedActive, Is.True);
                Assert.That(
                    conveyor.CurrentUnitsPerSecond,
                    Is.GreaterThan(conveyor.BaseUnitsPerSecond),
                    "The conveyor must accelerate on the final successful tray press.");

                flow.RetryCurrentLevel();
                yield return null;
                Assert.That(grid.ReservedLooseMarbleCount, Is.Zero);
                Assert.That(grid.ProjectedLooseMarbleCount, Is.Zero);
                Assert.That(grid.BoardFullRejectionCount, Is.Zero);
                Assert.That(grid.HasCapacityForTrayRelease, Is.True);
                Assert.That(pool.ActiveCount, Is.Zero);
                Assert.That(pool.CreatedCount, Is.EqualTo(MarblePool.DefaultInitialCapacity));
                Assert.That(pool.RuntimeExpansionCount, Is.Zero);
            }
            finally
            {
                if (admission != null)
                {
                    admission.enabled = true;
                }

                Time.timeScale = originalTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator MainScene_HiddenTrayRevealsAtItsAuthoredGridPosition()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            Assert.That(grid, Is.Not.Null);

            LevelData stackedLevel = new LevelData
            {
                id = "stacked_test",
                displayName = "Stacked Test",
                topGrid = new TopGridData
                {
                    columns = 1,
                    rows = 2,
                    cellSpacing = 1f,
                    boxes = new[]
                    {
                        new TopBoxData
                        {
                            id = "stacked_lower",
                            color = "green",
                            column = 0,
                            row = 0
                        },
                        new TopBoxData
                        {
                            id = "stacked_upper",
                            color = "blue",
                            column = 0,
                            row = 1
                        }
                    }
                }
            };

            Assert.That(grid.BuildLevel(stackedLevel), Is.True);
            TrayFormationBackplate originalBackplate = grid.FormationBackplate;
            ClearedTraySpotLayer originalSpotLayer = grid.TraySpotLayer;
            Assert.That(originalBackplate, Is.Not.Null);
            Assert.That(originalSpotLayer, Is.Not.Null);
            Assert.That(originalSpotLayer.SpotCount, Is.EqualTo(2));
            Assert.That(originalSpotLayer.VisibleSpotCount, Is.Zero);
            Assert.That(originalBackplate.CutoutCount, Is.EqualTo(2));
            Vector3[] originalBackplatePositions =
            {
                originalBackplate.GetCutoutLocalPosition(0),
                originalBackplate.GetCutoutLocalPosition(1)
            };
            Vector3[] originalSpotPositions =
            {
                originalSpotLayer.GetSpotLocalPosition(0),
                originalSpotLayer.GetSpotLocalPosition(1)
            };
            Assert.That(grid.State.IsExposed("stacked_lower"), Is.True);
            Assert.That(grid.State.IsExposed("stacked_upper"), Is.False);
            Assert.That(FindView("stacked_lower").TrayVisible, Is.True);
            Assert.That(FindView("stacked_lower").VisibleMarkerCount, Is.EqualTo(9));
            Assert.That(FindView("stacked_upper").TrayVisible, Is.False);
            Assert.That(FindView("stacked_upper").HiddenTrayVisible, Is.True);
            Assert.That(FindView("stacked_upper").HiddenArtworkLoaded, Is.True);
            Assert.That(
                FindView("stacked_upper").HiddenArtworkName,
                Is.EqualTo("Approved Thin Hidden Tray Blue"));
            Assert.That(FindView("stacked_upper").VisibleMarkerCount, Is.Zero);
            Transform hiddenView = FindView("stacked_upper").transform;
            AssertLightCastShadow(
                hiddenView.Find("Hidden Tray Light Shadow"),
                hiddenView.Find("Approved Thin Hidden Tray"));
            Assert.That(hiddenView.Find("Hidden Tray Light Shadow").gameObject.activeSelf, Is.True);
            Assert.That(FindView("stacked_upper").transform.Find("Box Shell")
                .GetComponent<Renderer>().enabled, Is.False);
            Vector3 upperAuthoredPosition = FindView("stacked_upper").transform.localPosition;
            Assert.That(grid.TrySelectBox("stacked_lower"), Is.True);

            yield return WaitForRelease(grid);

            Assert.That(grid.State.ActiveCount, Is.EqualTo(1));
            Assert.That(grid.State.GetBox("stacked_upper").CurrentRow, Is.EqualTo(1));
            Assert.That(grid.State.IsExposed("stacked_upper"), Is.True);
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(1));
            Assert.That(FindView("stacked_upper").TrayVisible, Is.True);
            Assert.That(FindView("stacked_upper").HiddenTrayVisible, Is.False);
            Assert.That(
                FindView("stacked_upper").transform.Find("Hidden Tray Light Shadow")
                    .gameObject.activeSelf,
                Is.False);
            Assert.That(FindView("stacked_upper").VisibleMarkerCount, Is.EqualTo(9));
            Assert.That(
                FindView("stacked_upper").transform.localPosition,
                Is.EqualTo(upperAuthoredPosition),
                "Revealing a hidden tray must never move or compact its grid position.");
            Assert.That(FindView("stacked_upper").transform.Find("Box Shell")
                .GetComponent<Renderer>().enabled, Is.False);
            Assert.That(
                grid.FormationBackplate,
                Is.SameAs(originalBackplate),
                "Removing a tray must not rebuild or adapt the original formation surround.");
            Assert.That(
                grid.TraySpotLayer,
                Is.SameAs(originalSpotLayer),
                "Removing or revealing trays must not rebuild the original footprint layer.");
            Assert.That(grid.TraySpotLayer.SpotCount, Is.EqualTo(2));
            Assert.That(grid.TraySpotLayer.VisibleSpotCount, Is.EqualTo(1));
            Assert.That(
                grid.TraySpotLayer.GetSpotLocalPosition(0),
                Is.EqualTo(originalSpotPositions[0]));
            Assert.That(
                grid.TraySpotLayer.GetSpotLocalPosition(1),
                Is.EqualTo(originalSpotPositions[1]));
            Assert.That(grid.FormationBackplate.CutoutCount, Is.EqualTo(2));
            Assert.That(
                grid.FormationBackplate.GetCutoutLocalPosition(0),
                Is.EqualTo(originalBackplatePositions[0]));
            Assert.That(
                grid.FormationBackplate.GetCutoutLocalPosition(1),
                Is.EqualTo(originalBackplatePositions[1]));
            AssertNoSurroundGeometryBehindCutouts(grid.FormationBackplate);
        }

        [UnityTest]
        public IEnumerator MysteryTray_RevealsPinkContentsWithoutMovingItsAuthoredCell()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            Assert.That(grid, Is.Not.Null);

            LevelData level = new LevelData
            {
                id = "mystery_test",
                displayName = "Mystery Test",
                topGrid = new TopGridData
                {
                    columns = 1,
                    rows = 2,
                    cellSpacing = 1f,
                    boxes = new[]
                    {
                        new TopBoxData
                        {
                            id = "mystery_lower",
                            color = "green",
                            column = 0,
                            row = 0
                        },
                        new TopBoxData
                        {
                            id = "mystery_upper",
                            color = "pink",
                            column = 0,
                            row = 1,
                            mystery = true
                        }
                    }
                }
            };

            Assert.That(grid.BuildLevel(level), Is.True);
            TopBoxView mystery = FindView("mystery_upper");
            Vector3 authoredPosition = mystery.transform.localPosition;
            Assert.That(mystery.IsMystery, Is.True);
            Assert.That(mystery.ColorId, Is.EqualTo("pink"));
            Assert.That(mystery.TrayVisible, Is.False);
            Assert.That(mystery.HiddenTrayVisible, Is.True);
            Assert.That(mystery.HiddenArtworkLoaded, Is.True);
            Assert.That(mystery.HiddenArtworkName, Is.EqualTo("Approved Mystery Top Tray"));
            Assert.That(mystery.VisibleMarkerCount, Is.Zero);

            Assert.That(grid.TrySelectBox("mystery_lower"), Is.True);
            yield return WaitForRelease(grid);

            Assert.That(grid.State.IsExposed("mystery_upper"), Is.True);
            Assert.That(mystery.TrayVisible, Is.True);
            Assert.That(mystery.HiddenTrayVisible, Is.False);
            Assert.That(mystery.VisibleMarkerCount, Is.EqualTo(9));
            Assert.That(mystery.transform.localPosition, Is.EqualTo(authoredPosition));
            SpriteRenderer exposed = mystery.transform
                .Find("Exposed Nine-Cup Tray/Hyper Realistic 3x3 Tray")
                .GetComponent<SpriteRenderer>();
            Assert.That(exposed.sprite.name, Does.StartWith("Approved Baked Top Tray Pink"));
        }

        [UnityTest]
        public IEnumerator MainScene_LevelFiveBuildsApprovedMysteryAndPinkProductionLayout()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(grid, Is.Not.Null);
            Assert.That(flow.TryLoadLevel(4), Is.True);
            yield return null;

            TopBoxView[] trays = Object.FindObjectsByType<TopBoxView>(FindObjectsSortMode.None);
            int visibleCount = 0;
            int mysteryCount = 0;
            TopBoxView pink = null;
            for (int index = 0; index < trays.Length; index++)
            {
                TopBoxView tray = trays[index];
                if (grid.State.IsExposed(tray.BoxId)) visibleCount++;
                if (tray.IsMystery)
                {
                    mysteryCount++;
                    Assert.That(tray.HiddenArtworkName, Is.EqualTo("Approved Mystery Top Tray"));
                }

                if (tray.ColorId == "pink") pink = tray;
            }

            Assert.That(trays.Length, Is.EqualTo(10));
            Assert.That(visibleCount, Is.EqualTo(4));
            Assert.That(mysteryCount, Is.EqualTo(6));
            Assert.That(pink, Is.Not.Null);
            Assert.That(pink.IsMystery, Is.True);
            Assert.That(grid.State.GetBox(pink.BoxId).InitialRow, Is.EqualTo(2));
            Assert.That(grid.FormationBackplate.CutoutCount, Is.EqualTo(10));
            Assert.That(grid.FormationBackplate.BakedArtworkName, Does.StartWith("Exact Approved Premium Sheet"));
        }

        [UnityTest]
        public IEnumerator ReleasedMarble_FallsThroughTrayInputVolumeWithoutGettingStuck()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(pool, Is.Not.Null);

            TopBoxView tray = FindView("l01_top_yellow_01");
            Assert.That(tray, Is.Not.Null);

            Collider inputVolume = tray.transform.Find("Box Shell").GetComponent<Collider>();
            Assert.That(inputVolume.enabled, Is.True);
            Assert.That(inputVolume.isTrigger, Is.True);

            Vector3 startPosition = tray.transform.position + Vector3.up * 1.1f;
            startPosition.z = MarblePool.TransitDepth;
            MarbleActor marble = pool.Rent("blue", startPosition, Vector3.down * 2f);

            float trayBottom = inputVolume.bounds.min.y;
            float timeout = Time.realtimeSinceStartup + 1.25f;
            while (marble.transform.position.y >= trayBottom &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                marble.transform.position.y,
                Is.LessThan(trayBottom),
                "A released marble must pass through the tray's input-only volume instead of resting on it.");
        }

        [UnityTest]
        public IEnumerator HighestLoadLevel_BuildsHyperRealisticTraysForEveryProductionColor()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();

            Assert.That(flow.TryLoadLevel(3), Is.True);
            yield return null;

            TopBoxView[] trays = Object.FindObjectsByType<TopBoxView>(FindObjectsSortMode.None);
            HashSet<string> colors = new HashSet<string>();
            Dictionary<string, Material> materials = new Dictionary<string, Material>();
            Assert.That(trays.Length, Is.EqualTo(12));
            Assert.That(grid.FormationBackplate, Is.Not.Null);
            Assert.That(grid.TraySpotLayer, Is.Not.Null);
            Assert.That(grid.TraySpotLayer.SpotCount, Is.EqualTo(12));
            Assert.That(grid.TraySpotLayer.VisibleSpotCount, Is.Zero);
            Assert.That(grid.TraySpotLayer.ApprovedArtworkLoaded, Is.True);
            Assert.That(grid.FormationBackplate.CutoutCount, Is.EqualTo(12));
            Assert.That(grid.FormationBackplate.FillSegmentCount, Is.EqualTo(1));
            Assert.That(grid.FormationBackplate.BoundaryRailCount, Is.Zero);
            Assert.That(grid.FormationBackplate.LayerRendererCount, Is.EqualTo(2));
            Assert.That(grid.FormationBackplate.OpenRecessCount, Is.EqualTo(1));
            Assert.That(grid.FormationBackplate.BottomBackboardExposed, Is.True);
            Assert.That(grid.FormationBackplate.TopExtent, Is.GreaterThanOrEqualTo(4.65f));
            Assert.That(grid.FormationBackplate.OuterSidesAreSymmetric, Is.True);
            Assert.That(grid.FormationBackplate.ApprovedSurfaceTextureLoaded, Is.True);
            Assert.That(grid.FormationBackplate.ApprovedRimTextureLoaded, Is.True);
            AssertNoSurroundGeometryBehindCutouts(grid.FormationBackplate);

            for (int index = 0; index < trays.Length; index++)
            {
                TopBoxView tray = trays[index];
                bool exposed = grid.State.IsExposed(tray.BoxId);
                colors.Add(tray.ColorId);
                Assert.That(tray.TrayVisible, Is.EqualTo(exposed));
                Assert.That(tray.HiddenTrayVisible, Is.EqualTo(!exposed));
                Assert.That(tray.HiddenArtworkLoaded, Is.True);
                Assert.That(tray.HiddenArtworkName, Does.StartWith("Approved Thin Hidden Tray "));
                Assert.That(tray.VisibleMarkerCount, Is.EqualTo(exposed ? 9 : 0));
                Assert.That(tray.BallMaterial.GetFloat("_Glossiness"),
                    Is.EqualTo(0.72f).Within(0.001f));

                if (materials.TryGetValue(tray.ColorId, out Material sharedMaterial))
                {
                    Assert.That(tray.BallMaterial, Is.SameAs(sharedMaterial));
                }
                else
                {
                    materials.Add(tray.ColorId, tray.BallMaterial);
                }
            }

            Assert.That(colors, Is.EquivalentTo(new[] { "green", "blue", "orange", "yellow" }));
            Assert.That(materials.Count, Is.EqualTo(4));
        }

        private static void AssertNoSurroundGeometryBehindCutouts(
            TrayFormationBackplate surround)
        {
            MeshRenderer[] renderers = surround.GetComponentsInChildren<MeshRenderer>(true);
            for (int cutoutIndex = 0; cutoutIndex < surround.CutoutCount; cutoutIndex++)
            {
                Vector3 cutoutWorld = surround.transform.TransformPoint(
                    surround.GetCutoutLocalPosition(cutoutIndex));
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Bounds bounds = renderers[rendererIndex].bounds;
                    bool coversCenter =
                        cutoutWorld.x > bounds.min.x && cutoutWorld.x < bounds.max.x &&
                        cutoutWorld.y > bounds.min.y && cutoutWorld.y < bounds.max.y;
                    Assert.That(
                        coversCenter,
                        Is.False,
                        $"{renderers[rendererIndex].name} must surround the tray recess, not sit behind it.");
                }
            }
        }

        private static void AssertLightCastShadow(Transform shadow, Transform subject)
        {
            Assert.That(shadow, Is.Not.Null);
            Assert.That(subject, Is.Not.Null);

            SpriteRenderer shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            SpriteRenderer subjectRenderer = subject.GetComponent<SpriteRenderer>();
            Assert.That(shadowRenderer, Is.Not.Null);
            Assert.That(subjectRenderer, Is.Not.Null);
            Assert.That(shadowRenderer.sprite, Is.SameAs(subjectRenderer.sprite));
            Assert.That(shadowRenderer.sortingOrder, Is.LessThan(subjectRenderer.sortingOrder));
            Assert.That(shadowRenderer.color.a, Is.LessThanOrEqualTo(0.15f));
            Assert.That(shadow.localPosition.x, Is.LessThan(subject.localPosition.x));
            Assert.That(shadow.localPosition.y, Is.LessThan(subject.localPosition.y));
            Assert.That(shadow.GetComponent<Collider>(), Is.Null);
        }

        private static IEnumerator WaitForRelease(TopGridController grid)
        {
            float timeout = Time.realtimeSinceStartup + 6f;
            while (grid.ActiveReleaseCount > 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(grid.ActiveReleaseCount, Is.Zero, "The release flow timed out.");
        }

        private static IEnumerator WaitForReleaseWithoutOverlap(
            TopGridController grid,
            MarblePool pool)
        {
            float timeout = Time.realtimeSinceStartup + 6f;
            while (grid.ActiveReleaseCount > 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                AssertRentedMarblesDoNotOverlap(pool);
            }

            Assert.That(grid.ActiveReleaseCount, Is.Zero, "The collision-safe release flow timed out.");
            AssertRentedMarblesDoNotOverlap(pool);
        }

        private static IEnumerator WaitForTrayCapacityWithoutGrowth(
            TopGridController grid,
            MarblePool pool)
        {
            float timeout = Time.realtimeSinceStartup + 12f;
            while (!grid.HasCapacityForTrayRelease && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                Assert.That(pool.LooseMarbleCount, Is.LessThanOrEqualTo(36));
                Assert.That(grid.ProjectedLooseMarbleCount, Is.LessThanOrEqualTo(36));
                Assert.That(pool.RuntimeExpansionCount, Is.Zero);
            }

            Assert.That(
                grid.HasCapacityForTrayRelease,
                Is.True,
                "The conveyor did not free nine loose-board positions before the timeout.");
        }

        private static void AssertRentedMarblesDoNotOverlap(MarblePool pool)
        {
            float minimumDistance = MarblePool.TransitMarbleDiameter - 0.001f;
            Assert.That(
                pool.LastRenderedMinimumSeparation,
                Is.GreaterThanOrEqualTo(minimumDistance),
                "The final pre-render separation pass allowed visible balls to overlap.");
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < descendants.Length; index++)
            {
                if (descendants[index].name == objectName)
                {
                    return descendants[index];
                }
            }

            return null;
        }

        private static TopBoxView FindView(string boxId)
        {
            TopBoxView[] views = Object.FindObjectsByType<TopBoxView>(FindObjectsSortMode.None);
            for (int index = 0; index < views.Length; index++)
            {
                if (views[index].BoxId == boxId)
                {
                    return views[index];
                }
            }

            return null;
        }
    }
}
