using System;
using System.Collections;
using MarbleSort.Core;
using MarbleSort.Data;
using MarbleSort.Gameplay.Conveyor;
using MarbleSort.Gameplay.Marbles;
using MarbleSort.Gameplay.Receivers;
using MarbleSort.Gameplay.TopGrid;
using MarbleSort.UI;
using UnityEngine;

namespace MarbleSort.Gameplay.Flow
{
    public enum LevelFlowStatus
    {
        Playing,
        Complete,
        Deadlocked
    }

    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class LevelFlowController : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrap;
        [SerializeField] private TopGridController topGrid;
        [SerializeField] private StadiumConveyorController conveyor;
        [SerializeField] private ConveyorAdmissionController admission;
        [SerializeField] private ReceiverQueueController receivers;
        [SerializeField] private MarblePool marblePool;
        [SerializeField] private GameHudView hud;
        [SerializeField, Min(0f)] private float completionAdvanceDelay = 1.2f;

        private ConveyorSlotSnapshot[] conveyorSnapshots = Array.Empty<ConveyorSlotSnapshot>();
        private ReceiverSnapshot[] receiverSnapshots = Array.Empty<ReceiverSnapshot>();
        private Coroutine advanceRoutine;
        private bool suppressEvaluation;

        public event Action<LevelFlowStatus> StatusChanged;

        public event Action<int> LevelStarted;

        public LevelFlowStatus Status { get; private set; } = LevelFlowStatus.Playing;

        public int CurrentLevelIndex => bootstrap?.Session?.CurrentLevelIndex ?? -1;

        public bool IsInitialized { get; private set; }

        public void Configure(
            GameBootstrap gameBootstrap,
            TopGridController topGridController,
            StadiumConveyorController conveyorController,
            ConveyorAdmissionController admissionController,
            ReceiverQueueController receiverController,
            MarblePool pool,
            GameHudView hudView,
            float advanceDelay)
        {
            bootstrap = gameBootstrap;
            topGrid = topGridController;
            conveyor = conveyorController;
            admission = admissionController;
            receivers = receiverController;
            marblePool = pool;
            hud = hudView;
            completionAdvanceDelay = Mathf.Max(0f, advanceDelay);
        }

        public void Reevaluate()
        {
            if (suppressEvaluation || Status != LevelFlowStatus.Playing ||
                conveyor?.State == null || receivers?.State == null)
            {
                return;
            }

            if (receivers.State.IsComplete && receivers.PendingTransferCount == 0)
            {
                SetComplete();
                return;
            }

            EnsureSnapshotCapacity();
            PopulateSnapshots();
            if (DeadlockDetector.IsDeadlocked(conveyorSnapshots, receiverSnapshots))
            {
                SetDeadlocked();
            }
        }

        public void RetryCurrentLevel()
        {
            if (bootstrap?.Session == null)
            {
                return;
            }

            LoadLevel(bootstrap.Session.CurrentLevelIndex);
        }

        public bool TryLoadLevel(int levelIndex)
        {
            if (!IsInitialized || bootstrap?.Catalog?.levels == null || bootstrap.Session == null ||
                levelIndex < 0 || levelIndex >= bootstrap.Catalog.levels.Length)
            {
                return false;
            }

            bootstrap.Session.SelectLevel(levelIndex);
            return LoadLevel(levelIndex);
        }

        public void AdvanceToNextLevel()
        {
            if (bootstrap?.Session == null)
            {
                return;
            }

            int nextLevelIndex = bootstrap.Session.AdvanceToNextLevel();
            LoadLevel(nextLevelIndex);
        }

        private void Start()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("Level flow requires bootstrap and every gameplay subsystem.", this);
                enabled = false;
                return;
            }

            conveyor.SlotOccupied += HandleConveyorChanged;
            conveyor.SlotCleared += HandleConveyorChanged;
            receivers.StateChanged += HandleReceiverChanged;
            hud?.Configure(this);
            ShowPlayingHud();
            EnsureSnapshotCapacity();
            IsInitialized = true;
            Reevaluate();
        }

        private bool HasRequiredReferences()
        {
            return bootstrap != null && bootstrap.Catalog != null && bootstrap.Session != null &&
                   topGrid != null && conveyor != null && admission != null &&
                   receivers != null && marblePool != null;
        }

        private void HandleConveyorChanged(int slotIndex, string colorId, MarbleActor marble)
        {
            Reevaluate();
        }

        private void HandleReceiverChanged()
        {
            Reevaluate();
        }

        private void SetComplete()
        {
            Status = LevelFlowStatus.Complete;
            SetGameplayEnabled(false);
            LevelData level = GetCurrentLevel();
            hud?.ShowComplete(level?.displayName ?? "Level");
            StatusChanged?.Invoke(Status);
            advanceRoutine = StartCoroutine(AdvanceAfterDelay());
        }

        private void SetDeadlocked()
        {
            Status = LevelFlowStatus.Deadlocked;
            SetGameplayEnabled(false);
            LevelData level = GetCurrentLevel();
            hud?.ShowDeadlocked(level?.displayName ?? "Level");
            StatusChanged?.Invoke(Status);
        }

        private IEnumerator AdvanceAfterDelay()
        {
            if (completionAdvanceDelay > 0f)
            {
                yield return new WaitForSeconds(completionAdvanceDelay);
            }

            advanceRoutine = null;
            if (Status == LevelFlowStatus.Complete)
            {
                AdvanceToNextLevel();
            }
        }

        private bool LoadLevel(int levelIndex)
        {
            if (bootstrap?.Catalog?.levels == null ||
                levelIndex < 0 || levelIndex >= bootstrap.Catalog.levels.Length)
            {
                Debug.LogError($"Cannot load Marble Sort level index {levelIndex}.", this);
                return false;
            }

            if (advanceRoutine != null)
            {
                StopCoroutine(advanceRoutine);
                advanceRoutine = null;
            }

            suppressEvaluation = true;
            receivers.SetCollectionEnabled(false);
            admission.ResetAdmission();
            conveyor.ResetConveyor(marblePool);
            marblePool.ReturnAll();

            LevelData level = bootstrap.Catalog.levels[levelIndex];
            topGrid.enabled = true;
            admission.enabled = true;
            bool gridBuilt = topGrid.BuildLevel(level);
            bool receiversBuilt = receivers.BuildLevel(level);
            if (!gridBuilt || !receiversBuilt)
            {
                Debug.LogError($"Failed to build '{level.displayName}'.", this);
                enabled = false;
                suppressEvaluation = false;
                return false;
            }

            receivers.SetCollectionEnabled(true);
            Status = LevelFlowStatus.Playing;
            ShowPlayingHud();
            suppressEvaluation = false;
            StatusChanged?.Invoke(Status);
            LevelStarted?.Invoke(levelIndex);
            Reevaluate();
            return true;
        }

        private void SetGameplayEnabled(bool value)
        {
            if (topGrid != null)
            {
                topGrid.enabled = value;
            }

            if (admission != null)
            {
                admission.enabled = value;
            }

            receivers?.SetCollectionEnabled(value);
        }

        private void EnsureSnapshotCapacity()
        {
            if (conveyorSnapshots.Length != conveyor.State.SlotCount)
            {
                conveyorSnapshots = new ConveyorSlotSnapshot[conveyor.State.SlotCount];
            }

            if (receiverSnapshots.Length != receivers.State.Lanes.Count)
            {
                receiverSnapshots = new ReceiverSnapshot[receivers.State.Lanes.Count];
            }
        }

        private void PopulateSnapshots()
        {
            for (int index = 0; index < conveyorSnapshots.Length; index++)
            {
                ConveyorSlotState slot = conveyor.State.GetSlot(index);
                conveyorSnapshots[index] = new ConveyorSlotSnapshot(
                    slot.Status == ConveyorSlotStatus.Occupied,
                    slot.ColorId);
            }

            for (int index = 0; index < receiverSnapshots.Length; index++)
            {
                ReceiverBoxState active = receivers.State.Lanes[index].ActiveBox;
                receiverSnapshots[index] = active == null
                    ? new ReceiverSnapshot(false, string.Empty, 0)
                    : new ReceiverSnapshot(true, active.ColorId, active.RemainingCapacity);
            }
        }

        private LevelData GetCurrentLevel()
        {
            int index = CurrentLevelIndex;
            return bootstrap?.Catalog?.levels != null && index >= 0 && index < bootstrap.Catalog.levels.Length
                ? bootstrap.Catalog.levels[index]
                : null;
        }

        private void ShowPlayingHud()
        {
            LevelData level = GetCurrentLevel();
            hud?.ShowPlaying(level?.displayName ?? "Level");
        }

        private void OnDestroy()
        {
            if (conveyor != null)
            {
                conveyor.SlotOccupied -= HandleConveyorChanged;
                conveyor.SlotCleared -= HandleConveyorChanged;
            }

            if (receivers != null)
            {
                receivers.StateChanged -= HandleReceiverChanged;
            }
        }

        private void OnValidate()
        {
            completionAdvanceDelay = Mathf.Max(0f, completionAdvanceDelay);
        }
    }
}
