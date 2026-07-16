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
            Assert.That(hud.HintVisible, Is.True);
            Assert.That(hud.CompletedTrayCount, Is.Zero);
            Assert.That(hud.TotalTrayCount, Is.EqualTo(6));
            Assert.That(listener, Is.Not.Null);
            Assert.That(performance, Is.Not.Null);
            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
            Assert.That(performance.FrameSampleCount, Is.GreaterThan(0));
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

            Assert.That(flow.TryLoadLevel(4), Is.True);
            yield return null;
            int createdMarbles = pool.CreatedCount;
            int cachedMeshes = PresentationMeshFactory.CachedMeshCount;
            ParticleSystem particles = feedback.BurstParticles;
            AudioSource audio = Object.FindFirstObjectByType<AudioSource>();

            Assert.That(flow.TryLoadLevel(4), Is.True);
            yield return null;

            Assert.That(hud.HintVisible, Is.False);
            Assert.That(hud.CompletedTrayCount, Is.Zero);
            Assert.That(hud.TotalTrayCount, Is.EqualTo(24));
            Assert.That(pool.CreatedCount, Is.EqualTo(createdMarbles));
            Assert.That(createdMarbles, Is.EqualTo(72));
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
                    $"Level {levelIndex + 1} receiver queues");
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

            flow.RetryCurrentLevel();
            yield return null;

            Assert.That(flow.Status, Is.EqualTo(LevelFlowStatus.Playing));
            Assert.That(hud.OverlayVisible, Is.False);
            Assert.That(hud.RetryVisible, Is.False);
        }

        private static void AssertRenderersInsidePortraitFrame(
            Transform root,
            Camera gameplayCamera,
            string context)
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
                Assert.That(minimum.y, Is.GreaterThanOrEqualTo(0f),
                    $"{context}: '{renderers[index].name}' crosses the bottom edge.");
                Assert.That(maximum.y, Is.LessThanOrEqualTo(1f),
                    $"{context}: '{renderers[index].name}' crosses the top edge.");
            }
        }
    }
}
