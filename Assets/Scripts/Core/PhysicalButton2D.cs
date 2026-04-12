using IGame.IEntity;
using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class PhysicalButton2D : MonoBehaviour
    {
        [Tooltip("Local-space offset applied while the button is pressed.")]
        public Vector3 pressedLocalOffset = new Vector3(0f, -0.1f, 0f);

        [Min(0.01f)]
        [Tooltip("Higher values make the button move faster toward its target position.")]
        public float animationSpeed = 12f;

        [Tooltip("Layers allowed to press the button.")]
        public LayerMask detectionMask = ~0;

        [Tooltip("Only react to objects that have IController attached.")]
        public bool requireIController = true;
        [Tooltip("If enabled, the button stays pressed after the first valid press.")]
        public bool latchWhenPressed = false;
        [Min(0f)]
        [Tooltip("How long to keep the button pressed after contact is temporarily lost.")]
        public float releaseGraceTime = 0.05f;

        public UnityEvent onPress;
        public UnityEvent onRelease;
        public Rigidbody2DEvent onPressBody;
        public Rigidbody2DEvent onReleaseBody;

        public Rigidbody2D CurrentBody { get; private set; }
        public bool IsPressed => _latchedPressed || CurrentBody != null;

        private readonly Collider2D[] _contactBuffer = new Collider2D[16];
        private Collider2D _buttonCollider;
        private Vector3 _releasedLocalPosition;
        private Vector3 _pressedLocalPosition;
        private bool _latchedPressed;
        private float _lastPressTime;

        private void Awake()
        {
            _buttonCollider = GetComponent<Collider2D>();
            _releasedLocalPosition = transform.localPosition;
            _pressedLocalPosition = _releasedLocalPosition + pressedLocalOffset;
        }

        private void FixedUpdate()
        {
            if (_latchedPressed)
                return;

            Rigidbody2D pressingBody = FindPressingBody();

            if (pressingBody != null)
            {
                _lastPressTime = Time.time;
            }
            else if (CurrentBody != null && Time.time - _lastPressTime <= releaseGraceTime)
            {
                pressingBody = CurrentBody;
            }

            if (pressingBody == CurrentBody)
                return;

            if (CurrentBody != null)
            {
                onRelease?.Invoke();
                onReleaseBody?.Invoke(CurrentBody);
            }

            CurrentBody = pressingBody;

            if (CurrentBody != null)
            {
                if (latchWhenPressed)
                    _latchedPressed = true;

                onPress?.Invoke();
                onPressBody?.Invoke(CurrentBody);
            }
        }

        private void Update()
        {
            Vector3 target = IsPressed ? _pressedLocalPosition : _releasedLocalPosition;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                target,
                1f - Mathf.Exp(-animationSpeed * Time.deltaTime));
        }

        private Rigidbody2D FindPressingBody()
        {
            int count = _buttonCollider.GetContacts(_contactBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider2D col = _contactBuffer[i];
                if (col == null)
                    continue;

                Rigidbody2D rb = col.attachedRigidbody;
                if (!CanPress(rb))
                    continue;

                return rb;
            }

            return null;
        }

        private bool CanPress(Rigidbody2D rb)
        {
            if (rb == null || !rb.simulated || rb.bodyType != RigidbodyType2D.Dynamic)
                return false;

            if ((detectionMask.value & (1 << rb.gameObject.layer)) == 0)
                return false;

            if (!requireIController)
                return true;

            return rb.GetComponent<IController>() != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 basePos = Application.isPlaying ? _releasedLocalPosition : transform.localPosition;
            Vector3 pressedPos = basePos + pressedLocalOffset;

            Matrix4x4 original = Gizmos.matrix;
            Gizmos.matrix = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;

            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(basePos, 0.03f);
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(pressedPos, 0.03f);
            Gizmos.DrawLine(basePos, pressedPos);

            Gizmos.matrix = original;
        }
#endif
    }
}
