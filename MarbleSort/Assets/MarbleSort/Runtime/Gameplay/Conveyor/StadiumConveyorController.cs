using System;
using MarbleSort.Core;
using MarbleSort.Data;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Presentation;
using UnityEngine;

namespace MarbleSort.Gameplay.Conveyor
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    public sealed class StadiumConveyorController : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField, Min(1)] private int slotCount = 24;
        [SerializeField, Min(0.01f)] private float unitsPerSecond = 4f;
        [SerializeField, Min(0.01f)] private float straightLength = 7f;
        [SerializeField, Min(0.01f)] private float turnRadius = 0.75f;
        [SerializeField, Range(0f, 1f)] private float phase;
        [SerializeField, Min(0f)] private float occupantDepth = -0.16f;
        [SerializeField] private Transform[] slotViews = Array.Empty<Transform>();

        private MarbleActor[] occupants = Array.Empty<MarbleActor>();
        private ConveyorState state;

        public event Action<int, string, MarbleActor> SlotOccupied;

        public event Action<int, string, MarbleActor> SlotCleared;

        public int SlotCount => slotCount;

        public float Phase => phase;

        public ConveyorState State => state;

        public int ConfiguredSlotViewCount => slotViews?.Length ?? 0;

        public float EntranceNormalizedDistance =>
            StadiumPath.GetTopCenterNormalizedDistance(straightLength, turnRadius);

        public void Configure(
            GameBootstrap gameBootstrap,
            int newSlotCount,
            float newUnitsPerSecond,
            float newStraightLength,
            float newTurnRadius,
            Transform[] newSlotViews)
        {
            bootstrap = gameBootstrap;
            ApplySettings(
                newSlotCount,
                newUnitsPerSecond,
                newStraightLength,
                newTurnRadius);
            slotViews = newSlotViews ?? Array.Empty<Transform>();
            phase = EntranceNormalizedDistance;
            RefreshPresentation();
        }

        public int GetClosestSlotToEntrance(out float normalizedDistance)
        {
            return StadiumPath.FindClosestSlotIndex(
                phase,
                slotCount,
                EntranceNormalizedDistance,
                out normalizedDistance);
        }

        public Vector3 GetSlotWorldPosition(int index)
        {
            if (index < 0 || index >= slotViews.Length || slotViews[index] == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return slotViews[index].position + new Vector3(0f, 0f, occupantDepth);
        }

        public MarbleActor GetOccupant(int index)
        {
            return index >= 0 && index < occupants.Length ? occupants[index] : null;
        }

        public Transform GetSlotView(int index)
        {
            return index >= 0 && index < (slotViews?.Length ?? 0) ? slotViews[index] : null;
        }

        public bool TryFindClosestOccupiedSlot(
            Vector3 worldPoint,
            string colorId,
            float maximumDistance,
            out int slotIndex)
        {
            slotIndex = -1;
            if (state == null || string.IsNullOrWhiteSpace(colorId) || maximumDistance <= 0f)
            {
                return false;
            }

            float closestSquaredDistance = maximumDistance * maximumDistance;
            int count = Mathf.Min(slotCount, slotViews?.Length ?? 0);
            for (int index = 0; index < count; index++)
            {
                ConveyorSlotState slot = state.GetSlot(index);
                if (slot == null || slot.Status != ConveyorSlotStatus.Occupied ||
                    !string.Equals(slot.ColorId, colorId, StringComparison.OrdinalIgnoreCase) ||
                    occupants[index] == null)
                {
                    continue;
                }

                Vector3 offset = slotViews[index].position - worldPoint;
                offset.z = 0f;
                float squaredDistance = offset.sqrMagnitude;
                if (squaredDistance <= closestSquaredDistance)
                {
                    closestSquaredDistance = squaredDistance;
                    slotIndex = index;
                }
            }

            return slotIndex >= 0;
        }

        public bool TryReserveSlot(int index, MarbleActor marble)
        {
            if (marble == null || !marble.IsRented ||
                marble.MotionMode != MarbleMotionMode.LoosePhysics ||
                state == null || ContainsOccupant(marble))
            {
                return false;
            }

            return state.TryReserve(index, marble.ColorId);
        }

        public bool CommitAdmission(int index, MarbleActor marble)
        {
            ConveyorSlotState slot = state?.GetSlot(index);
            if (slot == null || slot.Status != ConveyorSlotStatus.Reserved ||
                marble == null || slot.ColorId != marble.ColorId ||
                index >= occupants.Length || occupants[index] != null)
            {
                return false;
            }

            if (!marble.AttachToConveyor(GetSlotWorldPosition(index)))
            {
                return false;
            }

            if (!state.TryCommit(index))
            {
                return false;
            }

            occupants[index] = marble;
            SlotOccupied?.Invoke(index, slot.ColorId, marble);
            return true;
        }

        public bool CancelReservation(int index)
        {
            return state != null && state.TryCancelReservation(index);
        }

        public bool TryClearSlot(int index, out MarbleActor marble)
        {
            marble = GetOccupant(index);
            ConveyorSlotState slot = state?.GetSlot(index);
            if (marble == null || slot == null || slot.Status != ConveyorSlotStatus.Occupied)
            {
                marble = null;
                return false;
            }

            string colorId = slot.ColorId;
            if (!state.TryClearOccupied(index))
            {
                marble = null;
                return false;
            }

            occupants[index] = null;
            SlotCleared?.Invoke(index, colorId, marble);
            return true;
        }

        public void ResetConveyor(MarblePool marblePool)
        {
            for (int index = 0; index < occupants.Length; index++)
            {
                MarbleActor marble = occupants[index];
                if (marble != null && marblePool != null && marble.IsRented)
                {
                    marblePool.Return(marble);
                }
            }

            state = new ConveyorState(slotCount);
            occupants = new MarbleActor[slotCount];
            phase = EntranceNormalizedDistance;
            RefreshPresentation();
        }

        public void RefreshPresentation()
        {
            RefreshSlots();
            RefreshOccupants();
        }

        private void Awake()
        {
            if (GetComponent<ConveyorArtworkPresenter>() == null)
            {
                gameObject.AddComponent<ConveyorArtworkPresenter>();
            }

            InitializeRuntimeState();
        }

        private void Start()
        {
            ConveyorSettingsData settings = bootstrap != null && bootstrap.Catalog != null
                ? bootstrap.Catalog.conveyor
                : null;
            if (settings != null)
            {
                ApplySettings(
                    settings.slotCount,
                    settings.unitsPerSecond,
                    settings.straightLength,
                    settings.turnRadius);
            }

            if (slotViews == null || slotViews.Length != slotCount)
            {
                Debug.LogError(
                    $"Conveyor requires exactly {slotCount} configured slot views, but found {slotViews?.Length ?? 0}.",
                    this);
                enabled = false;
                return;
            }

            InitializeRuntimeState();
            RefreshPresentation();
        }

        private void Update()
        {
            float perimeter = StadiumPath.GetPerimeter(straightLength, turnRadius);
            phase = Mathf.Repeat(phase + ((unitsPerSecond / perimeter) * Time.deltaTime), 1f);
            RefreshPresentation();
        }

        private void ApplySettings(
            int newSlotCount,
            float newUnitsPerSecond,
            float newStraightLength,
            float newTurnRadius)
        {
            slotCount = Mathf.Max(1, newSlotCount);
            unitsPerSecond = Mathf.Max(0.01f, newUnitsPerSecond);
            straightLength = Mathf.Max(0.01f, newStraightLength);
            turnRadius = Mathf.Max(0.01f, newTurnRadius);
        }

        private void InitializeRuntimeState()
        {
            if (state != null && state.SlotCount == slotCount)
            {
                return;
            }

            state = new ConveyorState(slotCount);
            occupants = new MarbleActor[slotCount];
        }

        private void RefreshSlots()
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

        private void RefreshOccupants()
        {
            int count = Mathf.Min(occupants.Length, slotViews?.Length ?? 0);
            for (int index = 0; index < count; index++)
            {
                MarbleActor marble = occupants[index];
                if (marble != null)
                {
                    marble.SetConveyorPosition(GetSlotWorldPosition(index));
                }
            }
        }

        private bool ContainsOccupant(MarbleActor marble)
        {
            for (int index = 0; index < occupants.Length; index++)
            {
                if (occupants[index] == marble)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            slotCount = Mathf.Max(1, slotCount);
            unitsPerSecond = Mathf.Max(0.01f, unitsPerSecond);
            straightLength = Mathf.Max(0.01f, straightLength);
            turnRadius = Mathf.Max(0.01f, turnRadius);
            occupantDepth = Mathf.Min(0f, occupantDepth);
            RefreshPresentation();
        }
    }
}
