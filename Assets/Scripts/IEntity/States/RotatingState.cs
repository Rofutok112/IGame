using UnityEngine;

namespace IGame.IEntity.States
{
    /// <summary>
    /// Rotating state: the object's rotation tracks the angle from its centre to
    /// the mouse cursor.
    ///
    /// Collision is handled by advancing rotation in small safe steps.
    /// This mirrors MovingState's "stop before penetration" approach more closely
    /// than applying a full rotation first and correcting afterwards.
    /// </summary>
    public class RotatingState : IState
    {
        private float grabAngleOffset;
        private float originalGravity;
        private RigidbodyType2D originalBodyType;

        /// Maximum angle advanced in one collision-check step.
        private const float RotationStepDegrees = 0.5f;
        private const float ContactPadding = 0.01f;

        // Reusable overlap buffer
        private static readonly Collider2D[] OverlapBuffer = new Collider2D[8];

        private Collider2D[] _colliders;
        private BoxCollider2D[] _boxColliders;

        public override void Enter(IController controller)
        {
            base.Enter(controller);
            controller.EnterPinnedVisualState();
            controller.ShowRotationGuide();
            originalGravity = controller.Rb.gravityScale;
            originalBodyType = controller.Rb.bodyType;
            controller.Rb.bodyType = RigidbodyType2D.Kinematic;
            controller.Rb.linearVelocity = Vector2.zero;
            controller.Rb.angularVelocity = 0f;

            _colliders = controller.GetSolidColliders();
            _boxColliders = controller.GetSolidBoxColliders();
            controller.Depenetrate();

            Vector2 mousePos = controller.GetMouseWorldPos();
            Vector2 dir = mousePos - (Vector2)controller.transform.position;
            float mouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            grabAngleOffset = controller.Rb.rotation - mouseAngle;
        }

        public override void HandleInput()
        {
            if (!controller.IsMouseHeld())
                controller.ChangeState(new FallingState());
        }

        public override void Update() { }

        public override void FixedUpdate()
        {
            if (!controller.IsMouseHeld()) return;

            Vector2 mousePos = controller.GetMouseWorldPos();
            Vector2 dir = mousePos - (Vector2)controller.transform.position;
            if (dir.sqrMagnitude < 0.01f) return;

            float mouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float targetRotation = mouseAngle + grabAngleOffset;

            float currentRotation = controller.Rb.rotation;
            float candidateRotation = Mathf.LerpAngle(currentRotation, targetRotation,
                Time.fixedDeltaTime * controller.rotateSpeed);

            ApplySteppedRotation(currentRotation, candidateRotation);
        }

        /// <summary>
        /// Advances from <paramref name="startAngle"/> toward <paramref name="targetAngle"/>
        /// in small increments, stopping at the last non-penetrating angle.
        /// </summary>
        private void ApplySteppedRotation(float startAngle, float targetAngle)
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                controller.Rb.MoveRotation(targetAngle);
                return;
            }

            float delta = Mathf.DeltaAngle(startAngle, targetAngle);
            float absDelta = Mathf.Abs(delta);
            if (absDelta <= 0.001f)
                return;

            int stepCount = Mathf.Max(1, Mathf.CeilToInt(absDelta / RotationStepDegrees));
            float appliedAngle = startAngle;

            for (int step = 1; step <= stepCount; step++)
            {
                float t = step / (float)stepCount;
                float candidate = Mathf.LerpAngle(startAngle, targetAngle, t);
                if (WouldPenetrateAtAngle(candidate))
                    break;

                appliedAngle = candidate;
            }

            controller.Rb.MoveRotation(appliedAngle);
            Physics2D.SyncTransforms();
        }

        /// Returns true if rotating to the candidate angle would overlap another collider.
        private bool WouldPenetrateAtAngle(float candidateAngle)
        {
            if (_boxColliders == null || _boxColliders.Length == 0)
            {
                float originalRotation = controller.Rb.rotation;
                controller.Rb.MoveRotation(candidateAngle);
                Physics2D.SyncTransforms();
                foreach (var col in _colliders)
                {
                    if (col == null) continue;
                    int overlapCount = Physics2D.OverlapCollider(col, controller.GetContactFilter(), OverlapBuffer);
                    for (int i = 0; i < overlapCount; i++)
                    {
                        if (OverlapBuffer[i] == null || OverlapBuffer[i].gameObject == controller.gameObject)
                            continue;

                        controller.Rb.MoveRotation(originalRotation);
                        Physics2D.SyncTransforms();
                        return true;
                    }
                }

                controller.Rb.MoveRotation(originalRotation);
                Physics2D.SyncTransforms();
                return false;
            }

            Vector3 lossyScale = controller.transform.lossyScale;
            Quaternion rotation = Quaternion.Euler(0f, 0f, candidateAngle);
            foreach (var boxCol in _boxColliders)
            {
                if (boxCol == null)
                    continue;

                Vector2 scaledOffset = new Vector2(boxCol.offset.x * lossyScale.x, boxCol.offset.y * lossyScale.y);
                Vector2 center = controller.Rb.position + (Vector2)(rotation * scaledOffset);

                Vector2 size = new Vector2(
                    Mathf.Abs(boxCol.size.x * lossyScale.x),
                    Mathf.Abs(boxCol.size.y * lossyScale.y));
                size += Vector2.one * ContactPadding;

                int count = Physics2D.OverlapBox(center, size, candidateAngle, controller.GetContactFilter(), OverlapBuffer);
                for (int i = 0; i < count; i++)
                {
                    if (OverlapBuffer[i] == null || OverlapBuffer[i].gameObject == controller.gameObject)
                        continue;
                    return true;
                }
            }

            return false;
        }

        public override void Exit()
        {
            controller.ExitPinnedVisualState();
            controller.HideRotationGuide();
            controller.Rb.bodyType = originalBodyType;
            controller.Rb.gravityScale = originalGravity;
            controller.Rb.linearVelocity = Vector2.zero;
            controller.Rb.angularVelocity = 0f;
        }
    }
}
