using UnityEngine;

namespace IGame.Core
{
    public class MoveTarget : MonoBehaviour
    {
        [Tooltip("Default movement direction used by MoveDefault().")]
        public Vector3 defaultDirection = Vector3.down;

        [Tooltip("Default distance moved when MoveDefault() is called.")]
        public float defaultDistance = 1f;

        [Min(0.01f)]
        [Tooltip("Movement speed in world units per second.")]
        public float moveSpeed = 2f;

        [Tooltip("If enabled, movement is applied in local space.")]
        public bool useLocalSpace = false;

        [Tooltip("If enabled, ignores further move requests after the first completed move until ResetTarget() is called.")]
        public bool lockAfterFirstMove = true;

        private Vector3 _targetPosition;
        private bool _hasPendingMove;
        private bool _isMoveLocked;

        private void Awake()
        {
            _targetPosition = GetCurrentPosition();
        }

        private void Update()
        {
            Vector3 current = GetCurrentPosition();
            Vector3 next = Vector3.MoveTowards(current, _targetPosition, moveSpeed * Time.deltaTime);
            SetCurrentPosition(next);

            if (_hasPendingMove && Vector3.Distance(next, _targetPosition) <= 0.0001f)
            {
                _hasPendingMove = false;
                if (lockAfterFirstMove)
                {
                    _isMoveLocked = true;
                }
            }
        }

        public void MoveDefault()
        {
            MoveByDirection(defaultDirection, defaultDistance);
        }

        public void MoveByDirection(Vector3 direction, float distance)
        {
            if (_isMoveLocked)
                return;

            Vector3 resolvedDirection = GetResolvedDirection(direction);
            if (resolvedDirection.sqrMagnitude <= 0.0001f)
                return;

            _targetPosition += resolvedDirection.normalized * Mathf.Abs(distance);
            _hasPendingMove = true;
        }

        public void MoveToCurrentPlusDirection(Vector3 direction, float distance)
        {
            if (_isMoveLocked)
                return;

            Vector3 resolvedDirection = GetResolvedDirection(direction);
            if (resolvedDirection.sqrMagnitude <= 0.0001f)
                return;

            _targetPosition = GetCurrentPosition() + resolvedDirection.normalized * Mathf.Abs(distance);
            _hasPendingMove = true;
        }

        public void ResetTarget()
        {
            _targetPosition = GetCurrentPosition();
            _hasPendingMove = false;
            _isMoveLocked = false;
        }

        private Vector3 GetCurrentPosition()
        {
            return useLocalSpace ? transform.localPosition : transform.position;
        }

        private Vector3 GetResolvedDirection(Vector3 direction)
        {
            if (useLocalSpace)
                return direction;

            return direction;
        }

        private void SetCurrentPosition(Vector3 position)
        {
            if (useLocalSpace)
                transform.localPosition = position;
            else
                transform.position = position;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 start = useLocalSpace && transform.parent != null
                ? transform.parent.TransformPoint(transform.localPosition)
                : transform.position;
            Vector3 resolvedDirection = defaultDirection.sqrMagnitude > 0.0001f
                ? defaultDirection.normalized
                : Vector3.down;
            if (useLocalSpace && transform.parent != null)
                resolvedDirection = transform.parent.TransformDirection(resolvedDirection).normalized;
            Vector3 end = start + resolvedDirection * Mathf.Abs(defaultDistance);

            Gizmos.color = new Color(0.2f, 1f, 0.8f, 0.9f);
            Gizmos.DrawWireSphere(start, 0.05f);
            Gizmos.DrawLine(start, end);

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireSphere(end, 0.07f);
        }
#endif
    }
}
