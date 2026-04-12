using System;
using System.Collections.Generic;
using IGame.IEntity;
using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    [Serializable]
    public class IFitSequenceStep
    {
        [Tooltip("Desired world position / rotation / stretch for this step.")]
        public Transform targetPose;

        [Tooltip("Use the values below instead of targetPose.")]
        public bool useManualPose;

        [Tooltip("World position used when useManualPose is enabled.")]
        public Vector3 targetPosition;

        [Tooltip("World rotation in Euler angles used when useManualPose is enabled.")]
        public Vector3 targetEulerAngles;

        [Tooltip("World scale approximation used when useManualPose is enabled.")]
        public Vector3 targetScale = Vector3.one;

        [Tooltip("Optional visual shown while this step is active.")]
        public GameObject stepVisual;

        public UnityEvent onStepCleared;

        [NonSerialized] public GameObject generatedVisual;
    }

    [Serializable]
    public class IFitSequenceStepEvent : UnityEvent<int> { }

    public class IFitSequencePuzzle : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private IController targetI;

        [Header("Thresholds")]
        [Min(0f)]
        [SerializeField] private float positionThreshold = 0.2f;
        [Min(0f)]
        [SerializeField] private float angleThreshold = 8f;
        [Min(0f)]
        [SerializeField] private float stretchThreshold = 0.2f;
        [Min(0f)]
        [SerializeField] private float requiredHoldTime = 0.15f;

        [Header("Flow")]
        [SerializeField] private bool showOnlyCurrentStepVisual = true;
        [SerializeField] private IFitSequenceStep[] steps = Array.Empty<IFitSequenceStep>();

        [Header("Auto Ghost Visuals")]
        [SerializeField] private bool autoCreateGhostVisualsFromTargetI = true;
        [SerializeField] private Transform ghostVisualParent;
        [SerializeField] private Color ghostVisualColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        [SerializeField] private int ghostSortingOrderOffset = -1;

        [Header("Events")]
        [SerializeField] private UnityEvent onProgressReset;
        [SerializeField] private IFitSequenceStepEvent onStepStarted;
        [SerializeField] private IFitSequenceStepEvent onStepCleared;
        [SerializeField] private UnityEvent onAllCleared;

        public int CurrentStepIndex { get; private set; }
        public bool IsAllCleared { get; private set; }

        private float currentHoldTimer;
        private readonly List<GameObject> generatedGhostVisuals = new List<GameObject>();
        private Vector3 initialTargetLocalScale = Vector3.one;
        private Vector3 initialTargetLossyScale = Vector3.one;
        private bool hasCachedInitialTargetScale;

        private void Awake()
        {
            ResolveReferences();
            RebuildGeneratedGhostVisuals();
        }

        private void Start()
        {
            ResetProgress();
        }

        private void FixedUpdate()
        {
            if (IsAllCleared || targetI == null)
                return;

            SkipInvalidSteps();
            IFitSequenceStep currentStep = GetCurrentStep();
            if (currentStep == null)
            {
                CompleteAll();
                return;
            }

            if (IsMatchingStep(currentStep))
            {
                currentHoldTimer += Time.fixedDeltaTime;
                if (currentHoldTimer >= requiredHoldTime)
                {
                    CompleteCurrentStep();
                }
            }
            else
            {
                currentHoldTimer = 0f;
            }
        }

        public void ResetProgress()
        {
            ResolveReferences();
            CurrentStepIndex = 0;
            currentHoldTimer = 0f;
            IsAllCleared = false;
            SkipInvalidSteps();
            UpdateStepVisuals();
            onProgressReset?.Invoke();

            if (GetCurrentStep() != null)
            {
                onStepStarted?.Invoke(CurrentStepIndex);
            }
        }

        public bool IsCurrentStepMatched()
        {
            IFitSequenceStep currentStep = GetCurrentStep();
            return currentStep != null && IsMatchingStep(currentStep);
        }

        private void OnDestroy()
        {
            ClearGeneratedGhostVisuals();
        }

        private void ResolveReferences()
        {
            if (targetI == null)
            {
                targetI = FindFirstObjectByType<IController>();
            }

            CacheInitialTargetScale();
        }

        private void CacheInitialTargetScale()
        {
            if (hasCachedInitialTargetScale || targetI == null)
                return;

            initialTargetLocalScale = targetI.transform.localScale;
            initialTargetLossyScale = targetI.transform.lossyScale;
            hasCachedInitialTargetScale = true;
        }

        private void RebuildGeneratedGhostVisuals()
        {
            ClearGeneratedGhostVisuals();

            if (!autoCreateGhostVisualsFromTargetI || targetI == null || steps == null)
                return;

            for (int i = 0; i < steps.Length; i++)
            {
                IFitSequenceStep step = steps[i];
                if (!HasValidPose(step) || step.stepVisual != null)
                    continue;

                GameObject generatedVisual = CreateGhostVisualForStep(step, i);
                step.generatedVisual = generatedVisual;
                if (generatedVisual != null)
                {
                    generatedGhostVisuals.Add(generatedVisual);
                }
            }
        }

        private GameObject CreateGhostVisualForStep(IFitSequenceStep step, int index)
        {
            Transform parent = ghostVisualParent != null
                ? ghostVisualParent
                : (step.useManualPose || step.targetPose == null ? null : step.targetPose.parent);
            GameObject clone = Instantiate(targetI.gameObject, parent);
            clone.name = $"{targetI.gameObject.name}_GhostStep_{index + 1}";
            ApplyStepPoseToClone(clone.transform, step, parent);

            foreach (Behaviour behaviour in clone.GetComponentsInChildren<Behaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Collider2D collider2D in clone.GetComponentsInChildren<Collider2D>(true))
            {
                collider2D.enabled = false;
            }

            foreach (Rigidbody2D rigidbody2D in clone.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rigidbody2D.simulated = false;
                rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            }

            foreach (SpriteRenderer spriteRenderer in clone.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color color = spriteRenderer.color;
                color.r = ghostVisualColor.r;
                color.g = ghostVisualColor.g;
                color.b = ghostVisualColor.b;
                color.a *= ghostVisualColor.a;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder += ghostSortingOrderOffset;
            }

            clone.SetActive(false);
            return clone;
        }

        private void ApplyStepPoseToClone(Transform cloneTransform, IFitSequenceStep step, Transform parent)
        {
            if (!step.useManualPose && step.targetPose != null && parent == step.targetPose.parent)
            {
                cloneTransform.localPosition = step.targetPose.localPosition;
                cloneTransform.localRotation = step.targetPose.localRotation;
                cloneTransform.localScale = step.targetPose.localScale;
                return;
            }

            Vector3 worldPosition = GetStepPosition(step);
            Quaternion worldRotation = Quaternion.Euler(GetStepEulerAngles(step));
            Vector3 worldScale = GetGhostWorldScale(step);
            cloneTransform.SetPositionAndRotation(worldPosition, worldRotation);

            if (parent == null)
            {
                cloneTransform.localScale = worldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            cloneTransform.localScale = new Vector3(
                parentScale.x != 0f ? worldScale.x / parentScale.x : worldScale.x,
                parentScale.y != 0f ? worldScale.y / parentScale.y : worldScale.y,
                parentScale.z != 0f ? worldScale.z / parentScale.z : worldScale.z);
        }

        private void ClearGeneratedGhostVisuals()
        {
            for (int i = 0; i < generatedGhostVisuals.Count; i++)
            {
                GameObject ghost = generatedGhostVisuals[i];
                if (ghost == null)
                    continue;

                Destroy(ghost);
            }

            generatedGhostVisuals.Clear();

            if (steps == null)
                return;

            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] != null)
                {
                    steps[i].generatedVisual = null;
                }
            }
        }

        private void CompleteCurrentStep()
        {
            IFitSequenceStep step = GetCurrentStep();
            if (step == null)
            {
                CompleteAll();
                return;
            }

            step.onStepCleared?.Invoke();
            onStepCleared?.Invoke(CurrentStepIndex);

            CurrentStepIndex++;
            currentHoldTimer = 0f;
            SkipInvalidSteps();

            if (CurrentStepIndex >= steps.Length)
            {
                CompleteAll();
                return;
            }

            UpdateStepVisuals();
            onStepStarted?.Invoke(CurrentStepIndex);
        }

        private void CompleteAll()
        {
            if (IsAllCleared)
                return;

            IsAllCleared = true;
            currentHoldTimer = 0f;
            UpdateStepVisuals();
            onAllCleared?.Invoke();
        }

        private void SkipInvalidSteps()
        {
            while (CurrentStepIndex < steps.Length)
            {
                IFitSequenceStep step = steps[CurrentStepIndex];
                if (HasValidPose(step))
                    break;

                CurrentStepIndex++;
            }
        }

        private IFitSequenceStep GetCurrentStep()
        {
            if (steps == null || CurrentStepIndex < 0 || CurrentStepIndex >= steps.Length)
                return null;

            return steps[CurrentStepIndex];
        }

        private bool IsMatchingStep(IFitSequenceStep step)
        {
            if (!HasValidPose(step) || targetI == null)
                return false;

            float positionDelta = Vector2.Distance(targetI.transform.position, GetStepPosition(step));
            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(targetI.transform.eulerAngles.z, GetStepEulerAngles(step).z));
            float stretchDelta = Mathf.Abs(GetCurrentStretchValue() - GetTargetStretchValue(step));

            return positionDelta <= positionThreshold &&
                   angleDelta <= angleThreshold &&
                   stretchDelta <= stretchThreshold;
        }

        private float GetCurrentStretchValue()
        {
            return Mathf.Abs(targetI.transform.lossyScale.y);
        }

        private static float GetTargetStretchValue(IFitSequenceStep step)
        {
            return Mathf.Abs(GetStepScale(step).y);
        }

        private static bool HasValidPose(IFitSequenceStep step)
        {
            return step != null && (step.useManualPose || step.targetPose != null);
        }

        private static Vector3 GetStepPosition(IFitSequenceStep step)
        {
            return step.useManualPose || step.targetPose == null
                ? step.targetPosition
                : step.targetPose.position;
        }

        private static Vector3 GetStepEulerAngles(IFitSequenceStep step)
        {
            return step.useManualPose || step.targetPose == null
                ? step.targetEulerAngles
                : step.targetPose.eulerAngles;
        }

        private static Vector3 GetStepScale(IFitSequenceStep step)
        {
            return step.useManualPose || step.targetPose == null
                ? step.targetScale
                : step.targetPose.lossyScale;
        }

        private Vector3 GetGhostWorldScale(IFitSequenceStep step)
        {
            Vector3 targetWorldScale = GetStepScale(step);
            float baseLossyY = Mathf.Abs(initialTargetLossyScale.y) > 0.0001f ? Mathf.Abs(initialTargetLossyScale.y) : 1f;
            float stretchRatioY = Mathf.Abs(targetWorldScale.y) / baseLossyY;

            Vector3 ghostLocalScale = initialTargetLocalScale;
            ghostLocalScale.y = Mathf.Sign(initialTargetLocalScale.y) * Mathf.Abs(initialTargetLocalScale.y) * stretchRatioY;

            Transform targetParent = targetI != null ? targetI.transform.parent : null;
            if (targetParent == null)
            {
                return ghostLocalScale;
            }

            Vector3 parentLossyScale = targetParent.lossyScale;
            return new Vector3(
                parentLossyScale.x * ghostLocalScale.x,
                parentLossyScale.y * ghostLocalScale.y,
                parentLossyScale.z * ghostLocalScale.z);
        }

        private void UpdateStepVisuals()
        {
            if (steps == null)
                return;

            for (int i = 0; i < steps.Length; i++)
            {
                IFitSequenceStep step = steps[i];
                GameObject visual = GetStepVisual(step);
                if (step == null || visual == null)
                    continue;

                bool shouldShow = !IsAllCleared &&
                                  (!showOnlyCurrentStepVisual || i == CurrentStepIndex) &&
                                  i >= CurrentStepIndex;
                visual.SetActive(shouldShow);
            }
        }

        private static GameObject GetStepVisual(IFitSequenceStep step)
        {
            if (step == null)
                return null;

            return step.stepVisual != null ? step.stepVisual : step.generatedVisual;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (steps == null)
                return;

            for (int i = 0; i < steps.Length; i++)
            {
                IFitSequenceStep step = steps[i];
                if (!HasValidPose(step))
                    continue;

                bool isCurrent = i == CurrentStepIndex && !IsAllCleared;
                Color color = isCurrent
                    ? new Color(0.2f, 1f, 0.4f, 0.95f)
                    : new Color(1f, 0.75f, 0.2f, 0.65f);

                Vector3 position = GetStepPosition(step);
                Quaternion rotation = Quaternion.Euler(0f, 0f, GetStepEulerAngles(step).z);
                Vector3 up = rotation * Vector3.up * Mathf.Max(0.2f, GetTargetStretchValue(step) * 0.5f);
                Vector3 right = rotation * Vector3.right * positionThreshold;

                Gizmos.color = color;
                Gizmos.DrawWireSphere(position, positionThreshold);
                Gizmos.DrawLine(position - up, position + up);
                Gizmos.DrawLine(position - right, position + right);
            }
        }
#endif
    }
}
