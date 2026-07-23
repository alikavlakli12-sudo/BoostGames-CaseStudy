using System.Collections;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.Receivers;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.Presentation;
using MarbleSort.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MarbleSort.Tests.PlayMode
{
    public sealed class PresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainScene_BuildsOneReusableFeedbackSystemAndResponsiveBackdrop()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameFeedbackController feedback = Object.FindFirstObjectByType<GameFeedbackController>();
            ResponsiveCameraController responsive = Object.FindFirstObjectByType<ResponsiveCameraController>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();
            AudioListener listener = Object.FindFirstObjectByType<AudioListener>();
            RuntimePerformanceProbe performance = Object.FindFirstObjectByType<RuntimePerformanceProbe>();
            GameObject background = GameObject.Find("Illustrated Background");

            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.AudioReady, Is.True);
            Assert.That(feedback.BurstParticles, Is.Not.Null);
            Assert.That(feedback.BurstParticles.main.maxParticles, Is.EqualTo(160));
            Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(responsive, Is.Not.Null);
            Assert.That(responsive.CurrentOrthographicSize, Is.GreaterThanOrEqualTo(9.4f));
            Assert.That(background, Is.Not.Null);
            Assert.That(
                background.transform.localScale.y,
                Is.GreaterThanOrEqualTo(responsive.CurrentOrthographicSize * 2f));
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.HintVisible, Is.False);
            Assert.That(hud.CompletedTrayCount, Is.Zero);
            Assert.That(hud.TotalTrayCount, Is.EqualTo(18));
            Assert.That(hud.SettingsButtonVisible, Is.True);
            Assert.That(hud.TrayCounterVisible, Is.False);
            Assert.That(hud.CoinBalance, Is.Zero);
            Assert.That(hud.UnlockCardCount, Is.Zero);
            Assert.That(hud.LevelIndicatorInteractive, Is.False);
            Assert.That(hud.PremiumHudArtworkLoaded, Is.True);
            Assert.That(listener, Is.Not.Null);
            Assert.That(performance, Is.Not.Null);
            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
            Assert.That(performance.FrameSampleCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator PremiumHudAndBoardComposition_MatchApprovedPortraitLayout()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();
            GameObject topGrid = GameObject.Find("Runtime Top Grid");
            GameObject basinRim = GameObject.Find("Basin Rim");
            GameObject conveyor = GameObject.Find("Stadium Conveyor");
            GameObject leftEntranceGuide = GameObject.Find("Left Entrance Guide");
            GameObject rightEntranceGuide = GameObject.Find("Right Entrance Guide");
            ChuteBoundaryRig chuteBoundary = Object.FindFirstObjectByType<ChuteBoundaryRig>();

            Assert.That(hud, Is.Not.Null);
            Rect hudRect = GameHudView.CalculateHudPlateRect(720f, 100f);
            float hudScale = hudRect.width / 853f;
            Assert.That(hudRect.width, Is.EqualTo(720f * 0.88f).Within(0.01f));
            Assert.That(hudRect.x, Is.GreaterThan(40f));
            Assert.That(
                hudRect.y + (48f * hudScale),
                Is.EqualTo(114f).Within(0.01f),
                "The first visible HUD pixel must keep a 14 px gap below the safe-area top.");
            Assert.That(topGrid, Is.Not.Null);
            Assert.That(basinRim, Is.Not.Null);
            Assert.That(conveyor, Is.Not.Null);
            Assert.That(leftEntranceGuide, Is.Not.Null);
            Assert.That(rightEntranceGuide, Is.Not.Null);
            Assert.That(chuteBoundary, Is.Not.Null);
            Assert.That(topGrid.transform.localPosition.y, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(topGrid.transform.localScale.x, Is.EqualTo(1.18f).Within(0.001f));
            Assert.That(topGrid.transform.localScale.z, Is.EqualTo(1f).Within(0.001f));
            Assert.That(basinRim.transform.localPosition.y, Is.EqualTo(1.76f).Within(0.001f));
            Assert.That(conveyor.transform.localPosition.y, Is.EqualTo(-4.015f).Within(0.001f));
            Assert.That(leftEntranceGuide.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(rightEntranceGuide.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(leftEntranceGuide.GetComponent<Collider>().enabled, Is.True);
            Assert.That(rightEntranceGuide.GetComponent<Collider>().enabled, Is.True);
            Assert.That(
                chuteBoundary.SolidBoundaryColliderCount,
                Is.EqualTo(ChuteBoundaryRig.ExpectedSolidColliderCount));
            Assert.That(
                chuteBoundary.ArtMatchedBoundarySegmentCount,
                Is.EqualTo(ChuteBoundaryRig.ExpectedArtMatchedBoundarySegmentCount));
            Assert.That(
                chuteBoundary.BoundaryContourMatchesArtwork,
                Is.True,
                "The invisible collision surface must use the measured contact line of the illustrated backboard.");
            Assert.That(
                chuteBoundary.RenderedBoundaryCount,
                Is.Zero,
                "The illustrated backboard must remain the only visible pinball border.");
            Assert.That(GameObject.Find("Visible 3D Pinball Basin Border"), Is.Null);
            Assert.That(
                GameObject.Find("Solid 3D Chute Foreground Lip"),
                Is.Null,
                "No extracted chute texture may render over the conveyor artwork.");

            AssertSolidBoundary("Left Wall");
            AssertSolidBoundary("Right Wall");
            AssertSolidBoundary("Left Funnel");
            AssertSolidBoundary("Right Funnel");
            AssertSolidBoundary("Left Entrance Guide");
            AssertSolidBoundary("Right Entrance Guide");
            AssertSolidBoundary("Admission Gate");
            BoxCollider admissionGate = GameObject.Find("Admission Gate")
                .GetComponent<BoxCollider>();
            Assert.That(
                admissionGate.bounds.size.y,
                Is.GreaterThanOrEqualTo(ChuteBoundaryRig.AdmissionGateThickness - 0.001f),
                "The hidden admission catch must be deep enough to stop fast balls escaping below the belt.");

            float originalTimeScale = Time.timeScale;
            hud.SetSettingsVisible(true);
            yield return null;

            Assert.That(hud.SettingsVisible, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            hud.SetSettingsVisible(false);
            Assert.That(hud.SettingsVisible, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale).Within(0.001f));
        }

        private static void AssertSolidBoundary(string objectName)
        {
            GameObject boundary = GameObject.Find(objectName);
            Assert.That(boundary, Is.Not.Null, $"Missing solid boundary '{objectName}'.");
            BoxCollider collider = boundary.GetComponent<BoxCollider>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.isTrigger, Is.False);
            Assert.That(
                collider.bounds.size.z,
                Is.GreaterThanOrEqualTo(ChuteBoundaryRig.SolidDepth - 0.001f));
        }

        [UnityTest]
        public IEnumerator SelectingAnExposedBox_EmitsFeedbackWithoutCreatingAnotherSystem()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            GameFeedbackController feedback = Object.FindFirstObjectByType<GameFeedbackController>();
            MarbleSort.Gameplay.TopGrid.TopGridController grid =
                Object.FindFirstObjectByType<MarbleSort.Gameplay.TopGrid.TopGridController>();
            int previousBursts = feedback.BurstEventCount;

            Assert.That(grid.TrySelectBox("l01_top_yellow_01"), Is.True);
            yield return null;

            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();
            Assert.That(feedback.BurstEventCount, Is.GreaterThan(previousBursts));
            Assert.That(feedback.BurstParticles.particleCount, Is.GreaterThan(0));
            Assert.That(hud.HintVisible, Is.False);
            Assert.That(Object.FindObjectsByType<GameFeedbackController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReloadingHighestLoadLevel_ReusesMarbleAudioParticleAndMeshInfrastructure()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            MarblePool pool = Object.FindFirstObjectByType<MarblePool>();
            GameFeedbackController feedback = Object.FindFirstObjectByType<GameFeedbackController>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();

            Assert.That(flow.TryLoadLevel(3), Is.True);
            yield return null;
            int createdMarbles = pool.CreatedCount;
            int cachedMeshes = PresentationMeshFactory.CachedMeshCount;
            ParticleSystem particles = feedback.BurstParticles;
            AudioSource audio = Object.FindFirstObjectByType<AudioSource>();

            Assert.That(flow.TryLoadLevel(3), Is.True);
            yield return null;

            Assert.That(hud.HintVisible, Is.False);
            Assert.That(hud.CompletedTrayCount, Is.Zero);
            Assert.That(hud.TotalTrayCount, Is.EqualTo(36));
            Assert.That(pool.CreatedCount, Is.EqualTo(createdMarbles));
            Assert.That(createdMarbles, Is.EqualTo(MarblePool.DefaultInitialCapacity));
            Assert.That(pool.RuntimeExpansionCount, Is.Zero);
            Assert.That(PresentationMeshFactory.CachedMeshCount, Is.EqualTo(cachedMeshes));
            Assert.That(feedback.BurstParticles, Is.SameAs(particles));
            Assert.That(Object.FindFirstObjectByType<AudioSource>(), Is.SameAs(audio));
            Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EveryProductionLevel_KeepsInteractiveContentInsidePortraitFrame()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            TopGridController topGrid = Object.FindFirstObjectByType<TopGridController>();
            ReceiverQueueController receivers = Object.FindFirstObjectByType<ReceiverQueueController>();
            Camera gameplayCamera = Camera.main;

            for (int levelIndex = 0; levelIndex < 5; levelIndex++)
            {
                Assert.That(flow.TryLoadLevel(levelIndex), Is.True);
                yield return null;

                AssertRenderersInsidePortraitFrame(
                    topGrid.transform,
                    gameplayCamera,
                    $"Level {levelIndex + 1} top grid");
                AssertRenderersInsidePortraitFrame(
                    receivers.transform,
                    gameplayCamera,
                    $"Level {levelIndex + 1} receiver queues",
                    allowBottomOverflow: true);
            }
        }

        [UnityTest]
        public IEnumerator Deadlock_ShowsPolishedRetryStateAndRetryClearsIt()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            StadiumConveyorController conveyor = Object.FindFirstObjectByType<StadiumConveyorController>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();

            for (int index = 0; index < conveyor.State.SlotCount; index++)
            {
                Assert.That(conveyor.State.TryReserve(index, "orange"), Is.True);
                Assert.That(conveyor.State.TryCommit(index), Is.True);
            }

            flow.Reevaluate();
            yield return null;

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Deadlocked));
            Assert.That(hud.OverlayVisible, Is.True);
            Assert.That(hud.RetryVisible, Is.True);
            Assert.That(hud.LossArtworkLoaded, Is.True);
            Assert.That(hud.LossSnapshotAvailable, Is.True);
            Assert.That(conveyor.enabled, Is.False);

            flow.RetryCurrentLevel();
            yield return null;

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(hud.OverlayVisible, Is.False);
            Assert.That(hud.RetryVisible, Is.False);
            Assert.That(hud.LossSnapshotAvailable, Is.False);
            Assert.That(conveyor.enabled, Is.True);
        }

        private static void AssertRenderersInsidePortraitFrame(
            Transform root,
            Camera gameplayCamera,
            string context,
            bool allowBottomOverflow = false)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Assert.That(renderers, Is.Not.Empty, $"{context} has no visible renderers.");

            for (int index = 0; index < renderers.Length; index++)
            {
                Bounds bounds = renderers[index].bounds;
                Vector3 minimum = gameplayCamera.WorldToViewportPoint(
                    new Vector3(bounds.min.x, bounds.min.y, bounds.center.z));
                Vector3 maximum = gameplayCamera.WorldToViewportPoint(
                    new Vector3(bounds.max.x, bounds.max.y, bounds.center.z));

                Assert.That(minimum.x, Is.GreaterThanOrEqualTo(0f),
                    $"{context}: '{renderers[index].name}' crosses the left edge.");
                Assert.That(maximum.x, Is.LessThanOrEqualTo(1f),
                    $"{context}: '{renderers[index].name}' crosses the right edge.");
                if (!allowBottomOverflow)
                {
                    Assert.That(minimum.y, Is.GreaterThanOrEqualTo(0f),
                        $"{context}: '{renderers[index].name}' crosses the bottom edge.");
                }
                Assert.That(maximum.y, Is.LessThanOrEqualTo(1f),
                    $"{context}: '{renderers[index].name}' crosses the top edge.");
            }
        }
    }
}
