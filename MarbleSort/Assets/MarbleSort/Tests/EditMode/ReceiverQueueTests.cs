using MarbleSort.Data;
using MarbleSort.Gameplay.Receivers;
using NUnit.Framework;

namespace MarbleSort.Tests.EditMode
{
    public sealed class ReceiverQueueTests
    {
        [Test]
        public void NonMatchingMarble_IsRejectedWithoutChangingCapacity()
        {
            ReceiverQueueState state = CreateState("yellow", "blue");

            bool accepted = state.TryAccept(0, "green", out ReceiverAcceptanceResult result);

            Assert.That(accepted, Is.False);
            Assert.That(result.BoxId, Is.Null);
            Assert.That(state.Lanes[0].ActiveBox.FillCount, Is.Zero);
            Assert.That(state.CompletedBoxCount, Is.Zero);
        }

        [Test]
        public void ThirdMatchingMarble_CompletesHeadAndAdvancesFifoQueue()
        {
            ReceiverQueueState state = CreateState("yellow", "blue");

            Assert.That(state.TryAccept(0, "yellow", out ReceiverAcceptanceResult first), Is.True);
            Assert.That(state.TryAccept(0, "yellow", out ReceiverAcceptanceResult second), Is.True);
            Assert.That(state.TryAccept(0, "yellow", out ReceiverAcceptanceResult third), Is.True);

            Assert.That(first.FillCount, Is.EqualTo(1));
            Assert.That(first.BoxCompleted, Is.False);
            Assert.That(second.FillCount, Is.EqualTo(2));
            Assert.That(second.BoxCompleted, Is.False);
            Assert.That(third.FillCount, Is.EqualTo(ReceiverBoxState.Capacity));
            Assert.That(third.BoxCompleted, Is.True);
            Assert.That(third.LaneCompleted, Is.False);
            Assert.That(state.CompletedBoxCount, Is.EqualTo(1));
            Assert.That(state.Lanes[0].ActiveBox.ColorId, Is.EqualTo("blue"));
            Assert.That(state.CanAccept(0, "yellow"), Is.False);
            Assert.That(state.CanAccept(0, "blue"), Is.True);
        }

        [Test]
        public void CompletingEveryConfiguredBox_CompletesTheReceiverState()
        {
            ReceiverQueueState state = CreateState("green", "orange");

            FillActiveBox(state, 0);
            FillActiveBox(state, 0);

            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.TotalBoxCount, Is.EqualTo(2));
            Assert.That(state.CompletedBoxCount, Is.EqualTo(2));
            Assert.That(state.RemainingBoxCount, Is.Zero);
            Assert.That(state.Lanes[0].ActiveBox, Is.Null);
        }

        private static ReceiverQueueState CreateState(params string[] colors)
        {
            BottomBoxData[] boxes = new BottomBoxData[colors.Length];
            for (int index = 0; index < colors.Length; index++)
            {
                boxes[index] = new BottomBoxData
                {
                    id = $"box_{index}",
                    color = colors[index]
                };
            }

            return new ReceiverQueueState(new[]
            {
                new ReceiverLaneData
                {
                    id = "lane_0",
                    boxes = boxes
                }
            });
        }

        private static void FillActiveBox(ReceiverQueueState state, int laneIndex)
        {
            string colorId = state.Lanes[laneIndex].ActiveBox.ColorId;
            for (int count = 0; count < ReceiverBoxState.Capacity; count++)
            {
                Assert.That(state.TryAccept(laneIndex, colorId, out _), Is.True);
            }
        }
    }
}
