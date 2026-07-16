using System;
using System.Collections.Generic;

namespace MarbleSort.Gameplay.Conveyor
{
    public enum ConveyorSlotStatus
    {
        Empty,
        Reserved,
        Occupied
    }

    public sealed class ConveyorSlotState
    {
        internal ConveyorSlotState(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public ConveyorSlotStatus Status { get; internal set; }

        public string ColorId { get; internal set; } = string.Empty;

        public bool IsAvailable => Status == ConveyorSlotStatus.Empty;
    }

    public sealed class ConveyorState
    {
        private readonly ConveyorSlotState[] slots;

        public ConveyorState(int slotCount)
        {
            if (slotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            slots = new ConveyorSlotState[slotCount];
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index] = new ConveyorSlotState(index);
            }

            EmptyCount = slotCount;
        }

        public IReadOnlyList<ConveyorSlotState> Slots => slots;

        public int SlotCount => slots.Length;

        public int EmptyCount { get; private set; }

        public int ReservedCount { get; private set; }

        public int OccupiedCount { get; private set; }

        public bool IsFull => OccupiedCount == slots.Length;

        public ConveyorSlotState GetSlot(int index)
        {
            return index >= 0 && index < slots.Length ? slots[index] : null;
        }

        public bool CanReserve(int index)
        {
            ConveyorSlotState slot = GetSlot(index);
            return slot != null && slot.Status == ConveyorSlotStatus.Empty;
        }

        public bool TryReserve(int index, string colorId)
        {
            ConveyorSlotState slot = GetSlot(index);
            if (slot == null || slot.Status != ConveyorSlotStatus.Empty || string.IsNullOrWhiteSpace(colorId))
            {
                return false;
            }

            slot.Status = ConveyorSlotStatus.Reserved;
            slot.ColorId = colorId.Trim().ToLowerInvariant();
            EmptyCount--;
            ReservedCount++;
            return true;
        }

        public bool TryCommit(int index)
        {
            ConveyorSlotState slot = GetSlot(index);
            if (slot == null || slot.Status != ConveyorSlotStatus.Reserved)
            {
                return false;
            }

            slot.Status = ConveyorSlotStatus.Occupied;
            ReservedCount--;
            OccupiedCount++;
            return true;
        }

        public bool TryCancelReservation(int index)
        {
            ConveyorSlotState slot = GetSlot(index);
            if (slot == null || slot.Status != ConveyorSlotStatus.Reserved)
            {
                return false;
            }

            ResetSlot(slot);
            ReservedCount--;
            EmptyCount++;
            return true;
        }

        public bool TryClearOccupied(int index)
        {
            ConveyorSlotState slot = GetSlot(index);
            if (slot == null || slot.Status != ConveyorSlotStatus.Occupied)
            {
                return false;
            }

            ResetSlot(slot);
            OccupiedCount--;
            EmptyCount++;
            return true;
        }

        private static void ResetSlot(ConveyorSlotState slot)
        {
            slot.Status = ConveyorSlotStatus.Empty;
            slot.ColorId = string.Empty;
        }
    }
}
