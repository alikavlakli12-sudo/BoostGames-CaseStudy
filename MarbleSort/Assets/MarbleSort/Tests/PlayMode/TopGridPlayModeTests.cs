using System.Collections;
using System.Collections.Generic;
using MarbleSort.Data;
using MarbleSort.Gameplay.Flow;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.TopGrid;
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
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(2));
            Assert.That(grid.State.ActiveCount, Is.EqualTo(2));
            Assert.That(grid.State.IsExposed("l01_top_yellow_01"), Is.True);
            Assert.That(grid.State.IsExposed("l01_top_blue_01"), Is.True);

            TopBoxView[] initialTrays = Object.FindObjectsByType<TopBoxView>(FindObjectsSortMode.None);
            Assert.That(initialTrays.Length, Is.EqualTo(2));
            for (int index = 0; index < initialTrays.Length; index++)
            {
                Assert.That(initialTrays[index].TrayVisible, Is.True);
                Assert.That(initialTrays[index].VisibleMarkerCount, Is.EqualTo(9));
                Assert.That(initialTrays[index].BallMaterial.GetFloat("_Glossiness"),
                    Is.EqualTo(0.72f).Within(0.001f));

                Transform trayRoot = initialTrays[index].transform.Find("Exposed Nine-Cup Tray");
                Assert.That(trayRoot, Is.Not.Null);
                Assert.That(trayRoot.localRotation, Is.EqualTo(Quaternion.identity));

                Transform trayArtwork = trayRoot.Find("Hyper Realistic 3x3 Tray");
                Assert.That(trayArtwork, Is.Not.Null);
                SpriteRenderer trayRenderer = trayArtwork.GetComponent<SpriteRenderer>();
                Assert.That(trayRenderer, Is.Not.Null);
                Assert.That(trayRenderer.sprite.name, Does.StartWith("Top Tray "));
                Assert.That(trayRenderer.sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(trayRenderer.sprite.texture.mipmapCount, Is.EqualTo(1));

                Transform marker = trayRoot.Find("Nine Marble Markers/Marker 01");
                Assert.That(marker, Is.Not.Null);
                Assert.That(marker.localPosition.z, Is.EqualTo(-0.36f).Within(0.001f));
                SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
                Assert.That(markerRenderer, Is.Not.Null);
                Assert.That(markerRenderer.sprite.name, Does.StartWith("Receiver Ball "));
                Assert.That(initialTrays[index].transform.Find("Box Shell")
                    .GetComponent<Renderer>().enabled, Is.False);
            }

            int releasedCount = 0;
            grid.MarblesReleased += (_, _, count) => releasedCount += count;

            Assert.That(grid.TrySelectBox("l01_top_yellow_01"), Is.True);
            Assert.That(grid.TrySelectBox("l01_top_blue_01"), Is.False, "Input must lock during release.");

            yield return WaitForRelease(grid);

            Assert.That(releasedCount, Is.EqualTo(9));
            Assert.That(pool.ActiveCount, Is.EqualTo(9));
            Assert.That(grid.State.ActiveCount, Is.EqualTo(1));
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(1));

            MarbleActor[] pooledMarbles = pool.GetComponentsInChildren<MarbleActor>(true);
            MarbleActor activeMarble = null;
            for (int index = 0; index < pooledMarbles.Length; index++)
            {
                if (pooledMarbles[index].IsRented)
                {
                    activeMarble = pooledMarbles[index];
                    break;
                }
            }

            Assert.That(activeMarble, Is.Not.Null);
            Assert.That(activeMarble.GetComponent<Renderer>().sharedMaterial.GetFloat("_Glossiness"),
                Is.EqualTo(0.72f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MainScene_StackedColumnCollapsesAndExposesTheNextBox()
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
            Assert.That(grid.State.IsExposed("stacked_lower"), Is.True);
            Assert.That(grid.State.IsExposed("stacked_upper"), Is.False);
            Assert.That(FindView("stacked_lower").TrayVisible, Is.True);
            Assert.That(FindView("stacked_lower").VisibleMarkerCount, Is.EqualTo(9));
            Assert.That(FindView("stacked_upper").TrayVisible, Is.False);
            Assert.That(FindView("stacked_upper").VisibleMarkerCount, Is.Zero);
            Assert.That(FindView("stacked_upper").transform.Find("Box Shell")
                .GetComponent<Renderer>().enabled, Is.True);
            Assert.That(grid.TrySelectBox("stacked_lower"), Is.True);

            yield return WaitForRelease(grid);

            Assert.That(grid.State.ActiveCount, Is.EqualTo(1));
            Assert.That(grid.State.GetBox("stacked_upper").CurrentRow, Is.EqualTo(0));
            Assert.That(grid.State.IsExposed("stacked_upper"), Is.True);
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(1));
            Assert.That(FindView("stacked_upper").TrayVisible, Is.True);
            Assert.That(FindView("stacked_upper").VisibleMarkerCount, Is.EqualTo(9));
            Assert.That(FindView("stacked_upper").transform.Find("Box Shell")
                .GetComponent<Renderer>().enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator HighestLoadLevel_BuildsHyperRealisticTraysForEveryProductionColor()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;

            LevelFlowController flow = Object.FindFirstObjectByType<LevelFlowController>();
            TopGridController grid = Object.FindFirstObjectByType<TopGridController>();

            Assert.That(flow.TryLoadLevel(4), Is.True);
            yield return null;

            TopBoxView[] trays = Object.FindObjectsByType<TopBoxView>(FindObjectsSortMode.None);
            HashSet<string> colors = new HashSet<string>();
            Dictionary<string, Material> materials = new Dictionary<string, Material>();
            Assert.That(trays.Length, Is.EqualTo(8));

            for (int index = 0; index < trays.Length; index++)
            {
                TopBoxView tray = trays[index];
                bool exposed = grid.State.IsExposed(tray.BoxId);
                colors.Add(tray.ColorId);
                Assert.That(tray.TrayVisible, Is.EqualTo(exposed));
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

        private static IEnumerator WaitForRelease(TopGridController grid)
        {
            float timeout = Time.realtimeSinceStartup + 2f;
            while (grid.InputLocked && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(grid.InputLocked, Is.False, "The release flow timed out.");
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
