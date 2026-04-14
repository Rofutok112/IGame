using UnityEngine;

namespace IGame.IEntity.States
{
    public class StretchingState : IState
    {
        private float originalGravity;
        private RigidbodyType2D originalBodyType;
        private Vector3 baseScale;
        private float baseLocalHeight;
        private Vector2 anchorWorldPos;
        private Vector2 stretchAxis;
        private float stretchDirection;
        private float baseHeightWorld;
        private float initialGrabProjection;

        public override void Enter(IController controller)
        {
            base.Enter(controller);
            controller.EnterPinnedVisualState();
            controller.ResetStretchingSoundState();
            originalGravity = controller.Rb.gravityScale;
            originalBodyType = controller.Rb.bodyType;
            controller.Rb.bodyType = RigidbodyType2D.Kinematic;
            controller.Rb.linearVelocity = Vector2.zero;
            controller.Rb.angularVelocity = 0f;
            controller.Depenetrate();

            baseScale = controller.transform.localScale;
            baseLocalHeight = Mathf.Max(0.001f, controller.GetApproxLocalColliderSize().y);
            stretchAxis = controller.transform.up.normalized;
            stretchDirection = controller.StretchFromTop ? 1f : -1f;

            float currentHalfHeightWorld = baseLocalHeight * Mathf.Abs(baseScale.y) * 0.5f;
            baseHeightWorld = currentHalfHeightWorld * 2f;
            anchorWorldPos = controller.Rb.position - stretchAxis * stretchDirection * currentHalfHeightWorld;

            // Depenetrate can nudge the object before stretching begins, so derive the
            // grab point from the stored local point after the final position is settled.
            Vector2 settledGrabWorldPoint = controller.transform.TransformPoint(controller.GrabLocalPoint);
            initialGrabProjection = Vector2.Dot(settledGrabWorldPoint - anchorWorldPos, stretchAxis * stretchDirection);
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
            float currentGrabProjection = Vector2.Dot(mousePos - anchorWorldPos, stretchAxis * stretchDirection);
            float desiredHeightWorld = baseHeightWorld + (currentGrabProjection - initialGrabProjection);
            desiredHeightWorld = Mathf.Max(0.001f, desiredHeightWorld);
            float unclampedScaleY = desiredHeightWorld / baseLocalHeight;
            float desiredScaleY = Mathf.Clamp(unclampedScaleY, controller.minStretchScaleY, controller.maxStretchScaleY);

            Vector3 newScale = baseScale;
            newScale.y = Mathf.Sign(baseScale.y) * desiredScaleY;
            float currentScaleY = Mathf.Abs(controller.transform.localScale.y);
            controller.transform.localScale = newScale;
            controller.TryPlayStretchingSound(desiredScaleY - currentScaleY);

            bool atMinLimit = desiredScaleY <= controller.minStretchScaleY + 0.0001f && unclampedScaleY <= controller.minStretchScaleY;
            bool atMaxLimit = desiredScaleY >= controller.maxStretchScaleY - 0.0001f && unclampedScaleY >= controller.maxStretchScaleY;
            controller.UpdateStretchLimitSound(atMinLimit, atMaxLimit);

            float newHalfHeightWorld = baseLocalHeight * Mathf.Abs(newScale.y) * 0.5f;
            Vector2 centerPos = anchorWorldPos + stretchAxis * stretchDirection * newHalfHeightWorld;
            controller.Rb.MovePosition(centerPos);
            Physics2D.SyncTransforms();
        }

        public override void Exit()
        {
            controller.ExitPinnedVisualState();
            controller.Rb.bodyType = originalBodyType;
            controller.Rb.gravityScale = originalGravity;
            controller.Rb.linearVelocity = Vector2.zero;
            controller.Rb.angularVelocity = 0f;
        }
    }
}
