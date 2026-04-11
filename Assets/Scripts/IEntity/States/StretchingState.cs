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

        public override void Enter(IController controller)
        {
            base.Enter(controller);
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
            anchorWorldPos = controller.Rb.position - stretchAxis * stretchDirection * currentHalfHeightWorld;
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
            float desiredHeightWorld = Vector2.Dot(mousePos - anchorWorldPos, stretchAxis * stretchDirection);
            float desiredScaleY = desiredHeightWorld / baseLocalHeight;
            desiredScaleY = Mathf.Clamp(desiredScaleY, controller.minStretchScaleY, controller.maxStretchScaleY);

            Vector3 newScale = baseScale;
            newScale.y = Mathf.Sign(baseScale.y) * desiredScaleY;
            controller.transform.localScale = newScale;

            float newHalfHeightWorld = baseLocalHeight * Mathf.Abs(newScale.y) * 0.5f;
            Vector2 centerPos = anchorWorldPos + stretchAxis * stretchDirection * newHalfHeightWorld;
            controller.Rb.MovePosition(centerPos);
            Physics2D.SyncTransforms();
        }

        public override void Exit()
        {
            controller.Rb.bodyType = originalBodyType;
            controller.Rb.gravityScale = originalGravity;
            controller.Rb.linearVelocity = Vector2.zero;
            controller.Rb.angularVelocity = 0f;
        }
    }
}
