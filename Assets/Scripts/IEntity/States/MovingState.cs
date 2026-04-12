using UnityEngine;

namespace IGame.IEntity.States
{
    public class MovingState : IState
    {
        private Vector2 grabOffset;
        private float originalGravity;
        private RigidbodyType2D originalBodyType;
        private Collider2D[] _colliders;

        // Tiny gap kept between the object surface and colliders to prevent jitter
        private const float SkinWidth = 0.02f;
        private const float HitMergeEpsilon = 0.01f;
        private const int MaxSlideIterations = 3;

        // Reusable buffer for Cast results (avoids GC allocations every frame)
        private static readonly RaycastHit2D[] CastBuffer = new RaycastHit2D[8];

        public override void Enter(IController controller)
        {
            base.Enter(controller);
            originalGravity = controller.Rb.gravityScale;
            originalBodyType = controller.Rb.bodyType;
            // Kinematic body: prevents collision impulses from knocking the object off-course.
            controller.Rb.bodyType = RigidbodyType2D.Kinematic;
            controller.Rb.linearVelocity = Vector2.zero;
            controller.Rb.angularVelocity = 0f;
            _colliders = controller.GetSolidColliders();

            // Rotation can leave the collider in a tiny overlap/contact state.
            // Clear that first so the first movement cast doesn't get stuck at distance 0.
            controller.Depenetrate();

            // Calculate the offset between the object's center and the mouse point
            Vector2 mousePos = controller.GetMouseWorldPos();
            grabOffset = (Vector2)controller.transform.position - mousePos;
        }

        public override void HandleInput()
        {
            if (!controller.IsMouseHeld())
            {
                controller.ChangeState(new FallingState());
            }
        }

        public override void Update() {}

        public override void FixedUpdate()
        {
            if (!controller.IsMouseHeld()) return;

            Vector2 mousePos = controller.GetMouseWorldPos();
            if (controller.ShouldReleaseDrag(mousePos))
            {
                controller.ChangeState(new FallingState());
                return;
            }

            Vector2 targetPos = mousePos + grabOffset;

            // Lerp toward the target for smooth following
            Vector2 desiredPos = Vector2.Lerp(controller.Rb.position, targetPos,
                Time.fixedDeltaTime * controller.moveSpeed);

            desiredPos = ResolveMovement(controller.Rb.position, desiredPos);

            controller.Rb.MovePosition(desiredPos);
        }

        private Vector2 ResolveMovement(Vector2 startPos, Vector2 targetPos)
        {
            if (_colliders == null || _colliders.Length == 0)
                return targetPos;

            Vector2 currentPos = startPos;
            Vector2 remaining = targetPos - startPos;

            for (int iteration = 0; iteration < MaxSlideIterations; iteration++)
            {
                float moveDist = remaining.magnitude;
                if (moveDist <= 0.001f)
                    break;

                Vector2 moveDir = remaining / moveDist;
                if (!TryGetBlockingHit(moveDir, moveDist, out float minDist, out Vector2 hitNormal))
                {
                    currentPos += remaining;
                    break;
                }

                float safeDist = Mathf.Max(0f, minDist - SkinWidth);
                currentPos += moveDir * safeDist;

                Vector2 unresolved = remaining - moveDir * safeDist;
                remaining = unresolved - Vector2.Dot(unresolved, hitNormal) * hitNormal;
            }

            return currentPos;
        }

        private bool TryGetBlockingHit(Vector2 moveDir, float moveDist, out float minDist, out Vector2 hitNormal)
        {
            minDist = float.MaxValue;
            Vector2 accumulatedNormal = Vector2.zero;
            bool foundBlockingHit = false;

            foreach (var col in _colliders)
            {
                if (col == null) continue;

                int hitCount = col.Cast(moveDir, controller.GetContactFilter(), CastBuffer, moveDist + SkinWidth);
                for (int i = 0; i < hitCount; i++)
                {
                    if (CastBuffer[i].collider == null ||
                        CastBuffer[i].collider.gameObject == controller.gameObject)
                    {
                        continue;
                    }

                    if (Vector2.Dot(moveDir, CastBuffer[i].normal) >= -0.001f)
                    {
                        continue;
                    }

                    if (CastBuffer[i].distance < minDist - HitMergeEpsilon)
                    {
                        minDist = CastBuffer[i].distance;
                        accumulatedNormal = CastBuffer[i].normal;
                        foundBlockingHit = true;
                    }
                    else if (Mathf.Abs(CastBuffer[i].distance - minDist) <= HitMergeEpsilon)
                    {
                        accumulatedNormal += CastBuffer[i].normal;
                    }
                }
            }

            hitNormal = accumulatedNormal.sqrMagnitude > 0.0001f
                ? accumulatedNormal.normalized
                : Vector2.up;
            return foundBlockingHit;
        }


        public override void Exit()
        {
            controller.Rb.bodyType = originalBodyType;
            controller.Rb.gravityScale = originalGravity;
            controller.Rb.linearVelocity = Vector2.zero;
        }
    }
}
