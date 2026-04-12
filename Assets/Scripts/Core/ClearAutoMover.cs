using Cysharp.Threading.Tasks;
using DG.Tweening;
using IGame.IEntity;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    public class ClearAutoMover : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private IController targetController;

        [Header("Destination")]
        [SerializeField] private Transform destination;
        [SerializeField] private Vector3 fallbackWorldPosition;
        [SerializeField] private float fallbackWorldAngle;

        [Header("Animation")]
        [Min(0.01f)]
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private Ease moveEase = Ease.InOutCubic;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool disablePhysicsAfterArrival = true;

        [Header("Final Visuals")]
        [SerializeField] private SpriteRenderer[] targetSpriteRenderers;
        [SerializeField] private bool applyWhiteColorAtEnd = true;
        [SerializeField] private Color finalColor = Color.white;
        [SerializeField] private bool applySortingOrderAtEnd = true;
        [SerializeField] private int finalSortingOrder = 10;

        [Header("Events")]
        [SerializeField] private UnityEvent onComplete;

        private Sequence activeSequence;
        private bool isPlaying;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ResolveReferences();
            }
        }

        private void OnDisable()
        {
            activeSequence?.Kill();
            activeSequence = null;
            isPlaying = false;
        }

        public void Play()
        {
            PlayAsync().Forget();
        }

        public async UniTask PlayAsync()
        {
            if (isPlaying)
                return;

            ResolveReferences();
            if (target == null)
            {
                Debug.LogWarning("ClearAutoMover requires a target Transform.", this);
                return;
            }

            isPlaying = true;

            if (targetController != null)
            {
                targetController.SetInputEnabled(false);
            }

            if (targetRigidbody != null)
            {
                targetRigidbody.linearVelocity = Vector2.zero;
                targetRigidbody.angularVelocity = 0f;
                targetRigidbody.bodyType = RigidbodyType2D.Kinematic;
                targetRigidbody.simulated = false;
            }

            Vector3 destinationPosition = GetDestinationPosition();
            Quaternion destinationRotation = Quaternion.Euler(0f, 0f, GetDestinationAngle());

            activeSequence?.Kill();
            activeSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .Append(target.DOMove(destinationPosition, duration).SetEase(moveEase))
                .Join(target.DORotateQuaternion(destinationRotation, duration).SetEase(moveEase));

            await activeSequence.AsyncWaitForCompletion().AsUniTask();

            target.SetPositionAndRotation(destinationPosition, destinationRotation);
            ApplyFinalVisuals();

            if (targetRigidbody != null && !disablePhysicsAfterArrival)
            {
                targetRigidbody.simulated = true;
            }

            onComplete?.Invoke();
            isPlaying = false;
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                target = transform;
            }

            if (targetRigidbody == null && target != null)
            {
                targetRigidbody = target.GetComponent<Rigidbody2D>();
            }

            if (targetController == null && target != null)
            {
                targetController = target.GetComponent<IController>();
            }

            if ((targetSpriteRenderers == null || targetSpriteRenderers.Length == 0) && target != null)
            {
                targetSpriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private Vector3 GetDestinationPosition()
        {
            return destination != null ? destination.position : fallbackWorldPosition;
        }

        private float GetDestinationAngle()
        {
            return destination != null ? destination.eulerAngles.z : fallbackWorldAngle;
        }

        private void ApplyFinalVisuals()
        {
            if (targetSpriteRenderers == null || targetSpriteRenderers.Length == 0)
                return;

            foreach (SpriteRenderer spriteRenderer in targetSpriteRenderers.Where(r => r != null))
            {
                if (applyWhiteColorAtEnd)
                {
                    spriteRenderer.color = finalColor;
                }

                if (applySortingOrderAtEnd)
                {
                    spriteRenderer.sortingOrder = finalSortingOrder;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 destinationPosition = GetDestinationPosition();
            float angle = GetDestinationAngle();
            Vector3 start = target != null ? target.position : transform.position;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 right = rotation * Vector3.right * 0.35f;
            Vector3 up = rotation * Vector3.up * 0.2f;

            Gizmos.color = new Color(0.2f, 1f, 0.9f, 0.9f);
            Gizmos.DrawLine(start, destinationPosition);

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(destinationPosition, 0.08f);
            Gizmos.DrawLine(destinationPosition - right, destinationPosition + right);
            Gizmos.DrawLine(destinationPosition - up, destinationPosition + up);
        }
#endif
    }
}
