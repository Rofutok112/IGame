using UnityEngine;

namespace StageObjects
{
    public class RotationalFanForce2D : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("World-space layers affected by the fan force.")]
        public LayerMask targetMask = ~0;

        [Tooltip("Local-space offset of the fan area center.")]
        public Vector2 areaOffset = new Vector2(1.5f, 0f);

        [Tooltip("Local-space size of the box area affected by the fan force.")]
        public Vector2 areaSize = new Vector2(2.5f, 1.5f);

        [Header("Force")]
        [Min(0f)]
        [Tooltip("Minimum rotation speed in degrees/second before the fan starts pushing.")]
        public float minRotationSpeed = 180f;

        [Min(0f)]
        [Tooltip("Force applied when the rotation speed reaches minRotationSpeed.")]
        public float baseForce = 2f;

        [Min(0f)]
        [Tooltip("Extra force multiplier based on how much faster than the threshold the rotation is.")]
        public float forcePerExtraDegree = 0.01f;

        [Tooltip("If enabled, uses the current spin direction to decide whether to push left or right.")]
        public bool useSpinDirection = true;

        [Tooltip("Fallback local push direction when useSpinDirection is disabled.")]
        public Vector2 localPushDirection = Vector2.right;

        [Tooltip("Apply force continuously each FixedUpdate. ForceMode2D.Force feels like wind.")]
        public ForceMode2D forceMode = ForceMode2D.Force;

        private readonly Collider2D[] overlapBuffer = new Collider2D[16];
        private float previousAngle;
        private bool hasPreviousAngle;

        private void FixedUpdate()
        {
            float currentAngle = transform.eulerAngles.z;
            if (!hasPreviousAngle)
            {
                previousAngle = currentAngle;
                hasPreviousAngle = true;
                return;
            }

            float deltaAngle = Mathf.DeltaAngle(previousAngle, currentAngle);
            previousAngle = currentAngle;

            float rotationSpeed = Mathf.Abs(deltaAngle) / Time.fixedDeltaTime;
            if (rotationSpeed < minRotationSpeed)
                return;

            Vector2 center = transform.TransformPoint(areaOffset);
            int hitCount = Physics2D.OverlapBox(center, areaSize, transform.eulerAngles.z, new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetMask,
                useTriggers = false
            }, overlapBuffer);

            Vector2 pushDirection = GetPushDirection(deltaAngle);
            float forceStrength = baseForce + (rotationSpeed - minRotationSpeed) * forcePerExtraDegree;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = overlapBuffer[i];
                if (hit == null || hit.transform == transform)
                    continue;

                Rigidbody2D targetBody = hit.attachedRigidbody;
                if (targetBody == null || targetBody.bodyType != RigidbodyType2D.Dynamic)
                    continue;

                targetBody.AddForce(pushDirection * forceStrength, forceMode);
            }
        }

        private Vector2 GetPushDirection(float deltaAngle)
        {
            if (useSpinDirection)
            {
                float sign = Mathf.Sign(deltaAngle);
                if (Mathf.Approximately(sign, 0f))
                    sign = 1f;

                return (Vector2)(transform.right * sign);
            }

            Vector2 direction = localPushDirection.sqrMagnitude > 0.0001f
                ? localPushDirection.normalized
                : Vector2.right;
            return (Vector2)transform.TransformDirection(direction);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.2f);
            Gizmos.DrawCube(areaOffset, areaSize);
            Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.95f);
            Gizmos.DrawWireCube(areaOffset, areaSize);

            Vector2 previewDirection = useSpinDirection ? Vector2.right : (localPushDirection.sqrMagnitude > 0.0001f ? localPushDirection.normalized : Vector2.right);
            Vector3 arrowStart = areaOffset;
            Vector3 arrowEnd = arrowStart + (Vector3)previewDirection * (areaSize.x * 0.4f);
            Gizmos.DrawLine(arrowStart, arrowEnd);

            Gizmos.matrix = previousMatrix;
        }
#endif
    }
}
