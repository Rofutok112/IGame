using System;
using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    [Serializable]
    public class Rigidbody2DEvent : UnityEvent<Rigidbody2D> { }

    /// <summary>
    /// Detects when a Rigidbody2D enters or exits a circular area in world space.
    /// Attach this to an empty GameObject and tune the radius / offset in the Inspector.
    /// </summary>
    public class BallAreaDetector : MonoBehaviour
    {
        [Min(0.01f)]
        [Tooltip("Detection radius in world units.")]
        public float radius = 0.5f;

        [Tooltip("World-space offset from this transform.")]
        public Vector2 offset = Vector2.zero;

        [Tooltip("Layers that can trigger detection.")]
        public LayerMask detectionMask = ~0;

        [Tooltip("If enabled, only Rigidbody2D objects with a CircleCollider2D are detected.")]
        public bool requireCircleCollider = true;

        public UnityEvent onEnter;
        public UnityEvent onExit;
        public Rigidbody2DEvent onEnterBody;
        public Rigidbody2DEvent onExitBody;

        public Rigidbody2D CurrentBody { get; private set; }

        private readonly Collider2D[] _overlapBuffer = new Collider2D[16];

        private void FixedUpdate()
        {
            Rigidbody2D detectedBody = FindDetectedBody();

            if (detectedBody == CurrentBody)
                return;

            if (CurrentBody != null)
            {
                onExit?.Invoke();
                onExitBody?.Invoke(CurrentBody);
            }

            CurrentBody = detectedBody;

            if (CurrentBody != null)
            {
                onEnter?.Invoke();
                onEnterBody?.Invoke(CurrentBody);
            }
        }

        private Rigidbody2D FindDetectedBody()
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

                Rigidbody2D rb = col.attachedRigidbody;
                if (rb == null)
                    continue;

                if (requireCircleCollider && col.GetComponent<CircleCollider2D>() == null)
                    continue;

                return rb;
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position + (Vector3)offset;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
            Gizmos.DrawSphere(center, radius);
        }
#endif
    }
}
