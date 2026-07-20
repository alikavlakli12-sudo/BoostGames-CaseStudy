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
        public const float ChuteGateLocalY = -0.34f;
        public const float ChuteTriggerCenterY = 0.15f;
        public const float ChuteTriggerWidth = 0.76f;
        public const float ChuteTriggerHeight = 1.7f;
        public const float ChuteTriggerDepth = 0.6f;
        // The gate surface sits at -0.26 local Y and the ball radius is 0.26. A small
        // crowding allowance keeps the lowest ball eligible even while another ball
        // presses on it, without starting guided motion high inside the funnel.
        public const float AdmissionReadyLocalY = 0.18f;
        public const float AntiBridgeAssistLocalY = 0.9f;
        private const float AntiBridgeDownAcceleration = 45f;
        private const float AntiBridgeCenterAcceleration = 90f;
        private const float AntiBridgePartnerLiftAcceleration = 55f;

        [SerializeField] private StadiumConveyorController conveyor;
        [SerializeField, Min(0f)] private float transitionDuration = 0.16f;
        [SerializeField, Range(0.001f, 0.04f)] private float admissionWindowNormalized = 0.011f;
        [SerializeField, Min(0f)] private float transitionArcHeight = 0.1f;

        private readonly List<MarbleActor> queuedMarbles = new List<MarbleActor>(72);
        private Coroutine transitionRoutine;
        private MarbleActor transitioningMarble;
        private MarbleActor lastAdmittedMarble;
        private int transitioningSlot = -1;

        public event Action<int, string, MarbleActor> MarbleAdmitted;

        public int QueuedCount => queuedMarbles.Count;

        public bool IsTransitioning => transitioningMarble != null;

        public float RequiredChuteClearance =>
            MarblePool.TransitMarbleDiameter + MarblePool.MinimumMarbleSeparation;

        public Vector3 ChuteExitWorldPosition =>
            transform.TransformPoint(new Vector3(0f, ChuteGateLocalY, 0f));

        public float AdmissionReadyWorldY =>
            transform.TransformPoint(new Vector3(0f, AdmissionReadyLocalY, 0f)).y;

        public Vector3 LastTransitionStartPosition { get; private set; }

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
            trigger.center = new Vector3(0f, ChuteTriggerCenterY, 0f);
            trigger.size = new Vector3(
                ChuteTriggerWidth,
                ChuteTriggerHeight,
                ChuteTriggerDepth);
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

        public void ResetAdmission()
        {
            CancelPendingAdmission();
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

            if (!IsChuteClear())
            {
                return;
            }

            MarbleActor marble = FindLowestQueuedMarble();
            if (marble == null || marble.transform.position.y > AdmissionReadyWorldY)
            {
                return;
            }

            int slotIndex = conveyor.GetClosestSlotToEntrance(out float normalizedDistance);
            if (normalizedDistance > admissionWindowNormalized ||
                !conveyor.State.CanReserve(slotIndex))
            {
                return;
            }

            if (!conveyor.TryReserveSlot(slotIndex, marble))
            {
                return;
            }

            LastTransitionStartPosition = marble.transform.position;
            if (!marble.BeginConveyorTransition())
            {
                conveyor.CancelReservation(slotIndex);
                return;
            }

            queuedMarbles.Remove(marble);
            transitioningMarble = marble;
            transitioningSlot = slotIndex;
            transitionRoutine = StartCoroutine(AnimateAdmission(marble, slotIndex));
        }

        private void FixedUpdate()
        {
            if (transitioningMarble != null || queuedMarbles.Count == 0)
            {
                return;
            }

            MarbleActor lowest = FindLowestQueuedMarble();
            if (lowest == null || lowest.Body == null ||
                lowest.MotionMode != MarbleMotionMode.LoosePhysics)
            {
                return;
            }

            Vector3 localPosition = transform.InverseTransformPoint(
                lowest.transform.position);
            if (localPosition.y <= AdmissionReadyLocalY ||
                localPosition.y > AntiBridgeAssistLocalY)
            {
                return;
            }

            float centerAcceleration = Mathf.Clamp(
                -localPosition.x * AntiBridgeCenterAcceleration,
                -28f,
                28f);
            lowest.Body.AddForce(
                new Vector3(centerAcceleration, -AntiBridgeDownAcceleration, 0f),
                ForceMode.Acceleration);

            MarbleActor bridgePartner = FindBridgePartner(lowest, localPosition);
            if (bridgePartner != null && bridgePartner.Body != null)
            {
                bridgePartner.Body.AddForce(
                    new Vector3(0f, AntiBridgePartnerLiftAcceleration, 0f),
                    ForceMode.Acceleration);
            }
        }

        private MarbleActor FindBridgePartner(
            MarbleActor selected,
            Vector3 selectedLocalPosition)
        {
            for (int index = 0; index < queuedMarbles.Count; index++)
            {
                MarbleActor candidate = queuedMarbles[index];
                if (candidate == null || candidate == selected ||
                    candidate.MotionMode != MarbleMotionMode.LoosePhysics)
                {
                    continue;
                }

                Vector3 candidateLocalPosition = transform.InverseTransformPoint(
                    candidate.transform.position);
                if (candidateLocalPosition.y <= AdmissionReadyLocalY ||
                    candidateLocalPosition.y > AntiBridgeAssistLocalY ||
                    Mathf.Abs(candidateLocalPosition.y - selectedLocalPosition.y) > 0.2f ||
                    Mathf.Sign(candidateLocalPosition.x) ==
                    Mathf.Sign(selectedLocalPosition.x))
                {
                    continue;
                }

                return candidate;
            }

            return null;
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
                    // The ball has already fallen through the full chute under gravity.
                    // Finish with a short accelerating downward settle, never an upward arc.
                    float eased = normalized * normalized;
                    Vector3 target = conveyor.GetSlotWorldPosition(slotIndex);
                    Vector3 position = Vector3.LerpUnclamped(start, target, eased);
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
                lastAdmittedMarble = marble;
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

        private MarbleActor FindLowestQueuedMarble()
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

            return queuedMarbles[selectedIndex];
        }

        private bool IsChuteClear()
        {
            if (lastAdmittedMarble == null || !lastAdmittedMarble.IsRented ||
                lastAdmittedMarble.MotionMode != MarbleMotionMode.Conveyor)
            {
                lastAdmittedMarble = null;
                return true;
            }

            Vector3 difference = lastAdmittedMarble.transform.position - ChuteExitWorldPosition;
            float planarDistanceSquared =
                (difference.x * difference.x) + (difference.y * difference.y);
            float requiredDistance = RequiredChuteClearance;
            if (planarDistanceSquared < requiredDistance * requiredDistance)
            {
                return false;
            }

            lastAdmittedMarble = null;
            return true;
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
            CancelPendingAdmission();
        }

        private void CancelPendingAdmission()
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
            lastAdmittedMarble = null;
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
                trigger.center = new Vector3(0f, ChuteTriggerCenterY, 0f);
                trigger.size = new Vector3(
                    ChuteTriggerWidth,
                    ChuteTriggerHeight,
                    ChuteTriggerDepth);
            }
        }
    }
}
