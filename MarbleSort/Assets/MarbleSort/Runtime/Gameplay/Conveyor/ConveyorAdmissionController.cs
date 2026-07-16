using System;
using System.Collections;
using System.Collections.Generic;
using MarbleSort.Gameplay.Marbles;
using UnityEngine;

namespace MarbleSort.Gameplay.Conveyor
{
    [DefaultExecutionOrder(20)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ConveyorAdmissionController : MonoBehaviour
    {
        [SerializeField] private StadiumConveyorController conveyor;
        [SerializeField, Min(0f)] private float transitionDuration = 0.16f;
        [SerializeField, Range(0.001f, 0.04f)] private float admissionWindowNormalized = 0.011f;
        [SerializeField, Min(0f)] private float transitionArcHeight = 0.1f;

        private readonly List<MarbleActor> queuedMarbles = new List<MarbleActor>(72);
        private Coroutine transitionRoutine;
        private MarbleActor transitioningMarble;
        private int transitioningSlot = -1;

        public event Action<int, string, MarbleActor> MarbleAdmitted;

        public int QueuedCount => queuedMarbles.Count;

        public bool IsTransitioning => transitioningMarble != null;

        public void Configure(
            StadiumConveyorController conveyorController,
            float duration,
            float normalizedWindow,
            float arcHeight)
        {
            conveyor = conveyorController;
            transitionDuration = Mathf.Max(0f, duration);
            admissionWindowNormalized = Mathf.Clamp(normalizedWindow, 0.001f, 0.04f);
            transitionArcHeight = Mathf.Max(0f, arcHeight);

            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
        }

        public bool TryQueue(MarbleActor marble)
        {
            if (marble == null || !marble.IsRented ||
                marble.MotionMode != MarbleMotionMode.LoosePhysics ||
                queuedMarbles.Contains(marble))
            {
                return false;
            }

            queuedMarbles.Add(marble);
            return true;
        }

        private void Start()
        {
            if (conveyor == null || conveyor.State == null)
            {
                Debug.LogError("Conveyor admission requires an initialized stadium conveyor.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            PruneQueue();
            if (transitioningMarble != null || queuedMarbles.Count == 0 || conveyor == null)
            {
                return;
            }

            int slotIndex = conveyor.GetClosestSlotToEntrance(out float normalizedDistance);
            if (normalizedDistance > admissionWindowNormalized ||
                !conveyor.State.CanReserve(slotIndex))
            {
                return;
            }

            MarbleActor marble = TakeLowestQueuedMarble();
            if (marble == null || !conveyor.TryReserveSlot(slotIndex, marble))
            {
                return;
            }

            if (!marble.BeginConveyorTransition())
            {
                conveyor.CancelReservation(slotIndex);
                return;
            }

            transitioningMarble = marble;
            transitioningSlot = slotIndex;
            transitionRoutine = StartCoroutine(AnimateAdmission(marble, slotIndex));
        }

        private IEnumerator AnimateAdmission(MarbleActor marble, int slotIndex)
        {
            Vector3 start = marble.transform.position;
            float elapsed = 0f;

            if (transitionDuration > 0f)
            {
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float normalized = Mathf.Clamp01(elapsed / transitionDuration);
                    float eased = normalized * normalized * (3f - (2f * normalized));
                    Vector3 target = conveyor.GetSlotWorldPosition(slotIndex);
                    Vector3 position = Vector3.LerpUnclamped(start, target, eased);
                    position.y += Mathf.Sin(normalized * Mathf.PI) * transitionArcHeight;
                    marble.SetConveyorTransitionPosition(position);
                    yield return null;
                }
            }

            CompleteTransition(marble, slotIndex);
        }

        private void CompleteTransition(MarbleActor marble, int slotIndex)
        {
            if (conveyor.CommitAdmission(slotIndex, marble))
            {
                MarbleAdmitted?.Invoke(slotIndex, marble.ColorId, marble);
            }
            else
            {
                conveyor.CancelReservation(slotIndex);
                marble.ResumeLoosePhysics(Vector3.up * 0.1f);
            }

            transitioningMarble = null;
            transitioningSlot = -1;
            transitionRoutine = null;
        }

        private MarbleActor TakeLowestQueuedMarble()
        {
            int selectedIndex = -1;
            float lowestY = float.MaxValue;
            int lowestInstanceId = int.MaxValue;
            for (int index = 0; index < queuedMarbles.Count; index++)
            {
                MarbleActor candidate = queuedMarbles[index];
                if (candidate == null || candidate.MotionMode != MarbleMotionMode.LoosePhysics)
                {
                    continue;
                }

                float candidateY = candidate.transform.position.y;
                int instanceId = candidate.GetInstanceID();
                if (candidateY < lowestY ||
                    (Mathf.Approximately(candidateY, lowestY) && instanceId < lowestInstanceId))
                {
                    selectedIndex = index;
                    lowestY = candidateY;
                    lowestInstanceId = instanceId;
                }
            }

            if (selectedIndex < 0)
            {
                return null;
            }

            MarbleActor selected = queuedMarbles[selectedIndex];
            int lastIndex = queuedMarbles.Count - 1;
            queuedMarbles[selectedIndex] = queuedMarbles[lastIndex];
            queuedMarbles.RemoveAt(lastIndex);
            return selected;
        }

        private void PruneQueue()
        {
            for (int index = queuedMarbles.Count - 1; index >= 0; index--)
            {
                MarbleActor marble = queuedMarbles[index];
                if (marble == null || !marble.IsRented ||
                    marble.MotionMode != MarbleMotionMode.LoosePhysics)
                {
                    queuedMarbles.RemoveAt(index);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryQueue(other.GetComponent<MarbleActor>());
        }

        private void OnTriggerStay(Collider other)
        {
            TryQueue(other.GetComponent<MarbleActor>());
        }

        private void OnTriggerExit(Collider other)
        {
            MarbleActor marble = other.GetComponent<MarbleActor>();
            if (marble != null && marble.MotionMode == MarbleMotionMode.LoosePhysics)
            {
                queuedMarbles.Remove(marble);
            }
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            if (transitioningSlot >= 0 && conveyor != null)
            {
                conveyor.CancelReservation(transitioningSlot);
            }

            if (transitioningMarble != null)
            {
                transitioningMarble.ResumeLoosePhysics(Vector3.up * 0.1f);
            }

            transitionRoutine = null;
            transitioningMarble = null;
            transitioningSlot = -1;
            queuedMarbles.Clear();
        }

        private void OnValidate()
        {
            transitionDuration = Mathf.Max(0f, transitionDuration);
            admissionWindowNormalized = Mathf.Clamp(admissionWindowNormalized, 0.001f, 0.04f);
            transitionArcHeight = Mathf.Max(0f, transitionArcHeight);

            BoxCollider trigger = GetComponent<BoxCollider>();
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }
    }
}
