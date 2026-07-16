using System.Collections;
using MarbleSort.Data;
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

            int releasedCount = 0;
            grid.MarblesReleased += (_, _, count) => releasedCount += count;

            Assert.That(grid.TrySelectBox("l01_top_yellow_01"), Is.True);
            Assert.That(grid.TrySelectBox("l01_top_blue_01"), Is.False, "Input must lock during release.");

            yield return WaitForRelease(grid);

            Assert.That(releasedCount, Is.EqualTo(9));
            Assert.That(pool.ActiveCount, Is.EqualTo(9));
            Assert.That(grid.State.ActiveCount, Is.EqualTo(1));
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(1));
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
            Assert.That(grid.TrySelectBox("stacked_lower"), Is.True);

            yield return WaitForRelease(grid);

            Assert.That(grid.State.ActiveCount, Is.EqualTo(1));
            Assert.That(grid.State.GetBox("stacked_upper").CurrentRow, Is.EqualTo(0));
            Assert.That(grid.State.IsExposed("stacked_upper"), Is.True);
            Assert.That(grid.GeneratedBoxCount, Is.EqualTo(1));
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
    }
}
