using System;
using IGame.IEntity;
using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    [Serializable]
    public class IControllerEvent : UnityEvent<IController> { }

    /// <summary>
    /// Detects when an IObject (currently represented by IController) stays inside
    /// a circular area for at least the configured duration.
    /// </summary>
    public class IObjectAreaDetector : MonoBehaviour
    {
        [Min(0.01f)]
        [Tooltip("Detection radius in world units.")]
        public float radius = 0.5f;

        [Tooltip("World-space offset from this transform.")]
        public Vector2 offset = Vector2.zero;

        [Min(0f)]
        [Tooltip("How long the IObject must stay inside the area before onQualifiedEnter fires.")]
        public float requiredStayTime = 0f;

        [Tooltip("Layers that can trigger detection.")]
        public LayerMask detectionMask = ~0;

        public UnityEvent onEnter;
        public UnityEvent onExit;
        public UnityEvent onQualifiedEnter;
        public IControllerEvent onEnterObject;
        public IControllerEvent onExitObject;
        public IControllerEvent onQualifiedEnterObject;

        public IController CurrentObject { get; private set; }
        public bool IsQualified { get; private set; }

        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];
        private float _stayTimer;

        private void FixedUpdate()
        {
            IController detectedObject = FindDetectedObject();
            if (detectedObject != CurrentObject)
            {
                HandleObjectChanged(detectedObject);
            }

            if (CurrentObject == null || IsQualified)
                return;

            _stayTimer += Time.fixedDeltaTime;
            if (_stayTimer >= requiredStayTime)
            {
                IsQualified = true;
                onQualifiedEnter?.Invoke();
                onQualifiedEnterObject?.Invoke(CurrentObject);
            }
        }

        private void HandleObjectChanged(IController detectedObject)
        {
            if (CurrentObject != null)
            {
                onExit?.Invoke();
                onExitObject?.Invoke(CurrentObject);
            }

            CurrentObject = detectedObject;
            _stayTimer = 0f;
            IsQualified = false;

            if (CurrentObject != null)
            {
                onEnter?.Invoke();
                onEnterObject?.Invoke(CurrentObject);

                if (requiredStayTime <= 0f)
                {
                    IsQualified = true;
                    onQualifiedEnter?.Invoke();
                    onQualifiedEnterObject?.Invoke(CurrentObject);
                }
            }
        }

        private IController FindDetectedObject()
        {
            Vector2 center = (Vector2)transform.position + offset;
            int count = Physics2D.OverlapCircle(center, radius, new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = detectionMask,
                useTriggers = true
            }, _overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlapBuffer[i];
                if (col == null)
                    continue;

                IController controller = col.GetComponentInParent<IController>();
                if (controller == null)
                    continue;

                return controller;
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position + (Vector3)offset;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.85f);
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawSphere(center, radius);
        }
#endif
    }
}
