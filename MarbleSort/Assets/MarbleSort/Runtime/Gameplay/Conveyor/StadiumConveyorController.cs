using UnityEngine;

namespace MarbleSort.Gameplay.Conveyor
{
    [DisallowMultipleComponent]
    public sealed class StadiumConveyorController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int slotCount = 24;
        [SerializeField, Min(0.01f)] private float unitsPerSecond = 4f;
        [SerializeField, Min(0.01f)] private float straightLength = 7f;
        [SerializeField, Min(0.01f)] private float turnRadius = 0.75f;
        [SerializeField, Range(0f, 1f)] private float phase;
        [SerializeField] private Transform[] slotViews = new Transform[0];

        public int SlotCount => slotCount;

        public void Configure(
            int newSlotCount,
            float newUnitsPerSecond,
            float newStraightLength,
            float newTurnRadius,
            Transform[] newSlotViews)
        {
            slotCount = Mathf.Max(1, newSlotCount);
            unitsPerSecond = Mathf.Max(0.01f, newUnitsPerSecond);
            straightLength = Mathf.Max(0.01f, newStraightLength);
            turnRadius = Mathf.Max(0.01f, newTurnRadius);
            slotViews = newSlotViews ?? new Transform[0];
            phase = StadiumPath.GetTopCenterNormalizedDistance(straightLength, turnRadius);
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            if (slotViews == null || slotViews.Length == 0)
            {
                return;
            }

            int count = Mathf.Min(slotCount, slotViews.Length);
            for (int index = 0; index < count; index++)
            {
                Transform slot = slotViews[index];
                if (slot == null)
                {
                    continue;
                }

                float normalizedDistance = phase + (index / (float)slotCount);
                StadiumPose pose = StadiumPath.Evaluate(normalizedDistance, straightLength, turnRadius);
                slot.localPosition = pose.Position;
                float angle = Mathf.Atan2(pose.Tangent.y, pose.Tangent.x) * Mathf.Rad2Deg;
                slot.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void Update()
        {
            float perimeter = StadiumPath.GetPerimeter(straightLength, turnRadius);
            phase = Mathf.Repeat(phase + ((unitsPerSecond / perimeter) * Time.deltaTime), 1f);
            RefreshSlots();
        }

        private void OnValidate()
        {
            slotCount = Mathf.Max(1, slotCount);
            unitsPerSecond = Mathf.Max(0.01f, unitsPerSecond);
            straightLength = Mathf.Max(0.01f, straightLength);
            turnRadius = Mathf.Max(0.01f, turnRadius);
            RefreshSlots();
        }
    }
}
