using System;
using System.Collections.Generic;

namespace MarbleSort.Gameplay.Flow
{
    public readonly struct ConveyorSlotSnapshot
    {
        public ConveyorSlotSnapshot(bool occupied, string color)
        {
            Occupied = occupied;
            Color = color;
        }

        public bool Occupied { get; }

        public string Color { get; }
    }

    public readonly struct ReceiverSnapshot
    {
        public ReceiverSnapshot(bool available, string color, int remainingCapacity)
        {
            Available = available;
            Color = color;
            RemainingCapacity = remainingCapacity;
        }

        public bool Available { get; }

        public string Color { get; }

        public int RemainingCapacity { get; }
    }

    public static class DeadlockDetector
    {
        public static bool IsDeadlocked(
            IReadOnlyList<ConveyorSlotSnapshot> conveyorSlots,
            IReadOnlyList<ReceiverSnapshot> receivers)
        {
            if (conveyorSlots == null || conveyorSlots.Count == 0)
            {
                return false;
            }

            for (int slotIndex = 0; slotIndex < conveyorSlots.Count; slotIndex++)
            {
                if (!conveyorSlots[slotIndex].Occupied)
                {
                    return false;
                }
            }

            for (int slotIndex = 0; slotIndex < conveyorSlots.Count; slotIndex++)
            {
                string slotColor = conveyorSlots[slotIndex].Color;
                if (string.IsNullOrWhiteSpace(slotColor))
                {
                    continue;
                }

                int receiverCount = receivers == null ? 0 : receivers.Count;
                for (int receiverIndex = 0; receiverIndex < receiverCount; receiverIndex++)
                {
                    ReceiverSnapshot receiver = receivers[receiverIndex];
                    if (!receiver.Available || receiver.RemainingCapacity <= 0)
                    {
                        continue;
                    }

                    if (string.Equals(slotColor, receiver.Color, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
