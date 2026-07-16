using System;
using System.Collections.Generic;
using MarbleSort.Data;

namespace MarbleSort.Gameplay.Receivers
{
    public sealed class ReceiverBoxState
    {
        public const int Capacity = 3;

        internal ReceiverBoxState(BottomBoxData data)
        {
            Id = data.id.Trim();
            ColorId = data.color.Trim().ToLowerInvariant();
        }

        public string Id { get; }

        public string ColorId { get; }

        public int FillCount { get; internal set; }

        public int RemainingCapacity => Capacity - FillCount;

        public bool IsComplete => FillCount == Capacity;
    }

    public sealed class ReceiverLaneState
    {
        private readonly ReceiverBoxState[] boxes;

        internal ReceiverLaneState(ReceiverLaneData data)
        {
            Id = data.id.Trim();
            BottomBoxData[] source = data.boxes ?? Array.Empty<BottomBoxData>();
            boxes = new ReceiverBoxState[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                BottomBoxData box = source[index];
                if (box == null || string.IsNullOrWhiteSpace(box.id) || string.IsNullOrWhiteSpace(box.color))
                {
                    throw new ArgumentException($"Receiver box {index} in lane '{Id}' is incomplete.", nameof(data));
                }

                boxes[index] = new ReceiverBoxState(box);
            }
        }

        public string Id { get; }

        public IReadOnlyList<ReceiverBoxState> Boxes => boxes;

        public int ActiveBoxIndex { get; internal set; }

        public ReceiverBoxState ActiveBox =>
            ActiveBoxIndex >= 0 && ActiveBoxIndex < boxes.Length ? boxes[ActiveBoxIndex] : null;

        public bool IsComplete => ActiveBox == null;
    }

    public readonly struct ReceiverAcceptanceResult
    {
        internal ReceiverAcceptanceResult(
            int laneIndex,
            string boxId,
            string colorId,
            int fillCount,
            bool boxCompleted,
            bool laneCompleted)
        {
            LaneIndex = laneIndex;
            BoxId = boxId;
            ColorId = colorId;
            FillCount = fillCount;
            BoxCompleted = boxCompleted;
            LaneCompleted = laneCompleted;
        }

        public int LaneIndex { get; }

        public string BoxId { get; }

        public string ColorId { get; }

        public int FillCount { get; }

        public bool BoxCompleted { get; }

        public bool LaneCompleted { get; }
    }

    public sealed class ReceiverQueueState
    {
        private readonly ReceiverLaneState[] lanes;

        public ReceiverQueueState(ReceiverLaneData[] laneData)
        {
            ReceiverLaneData[] source = laneData ?? Array.Empty<ReceiverLaneData>();
            lanes = new ReceiverLaneState[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                ReceiverLaneData data = source[index];
                if (data == null || string.IsNullOrWhiteSpace(data.id))
                {
                    throw new ArgumentException($"Receiver lane {index} is incomplete.", nameof(laneData));
                }

                lanes[index] = new ReceiverLaneState(data);
                TotalBoxCount += lanes[index].Boxes.Count;
            }
        }

        public IReadOnlyList<ReceiverLaneState> Lanes => lanes;

        public int TotalBoxCount { get; }

        public int CompletedBoxCount { get; private set; }

        public int RemainingBoxCount => TotalBoxCount - CompletedBoxCount;

        public bool IsComplete => CompletedBoxCount == TotalBoxCount;

        public bool CanAccept(int laneIndex, string colorId)
        {
            ReceiverBoxState active = GetActiveBox(laneIndex);
            return active != null &&
                   active.RemainingCapacity > 0 &&
                   string.Equals(active.ColorId, colorId, StringComparison.OrdinalIgnoreCase);
        }

        public bool TryAccept(
            int laneIndex,
            string colorId,
            out ReceiverAcceptanceResult result)
        {
            if (!CanAccept(laneIndex, colorId))
            {
                result = default;
                return false;
            }

            ReceiverLaneState lane = lanes[laneIndex];
            ReceiverBoxState active = lane.ActiveBox;
            active.FillCount++;

            bool boxCompleted = active.IsComplete;
            if (boxCompleted)
            {
                CompletedBoxCount++;
                lane.ActiveBoxIndex++;
            }

            result = new ReceiverAcceptanceResult(
                laneIndex,
                active.Id,
                active.ColorId,
                active.FillCount,
                boxCompleted,
                lane.IsComplete);
            return true;
        }

        private ReceiverBoxState GetActiveBox(int laneIndex)
        {
            return laneIndex >= 0 && laneIndex < lanes.Length ? lanes[laneIndex].ActiveBox : null;
        }
    }
}
