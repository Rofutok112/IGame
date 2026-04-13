using UnityEngine;

namespace IGame.IEntity.States
{
    public class FallingState : IState
    {
        private float targetGravityScale;
        private float floatTimer;
        private bool isFloating;

        public override void Enter(IController controller)
        {
            base.Enter(controller);
            controller.PlayFallingVisualCue();

            targetGravityScale = controller.Rb.gravityScale <= 0.01f
                ? controller.defaultFallingGravityScale
                : controller.Rb.gravityScale;

            floatTimer = controller.fallingFloatDuration;
            isFloating = floatTimer > 0f;

            if (isFloating)
            {
                controller.Rb.gravityScale = controller.fallingFloatGravityScale;
            }
            else
            {
                controller.Rb.gravityScale = targetGravityScale;
            }
        }

        public override void HandleInput()
        {
            if (controller.IsMousePressed())
            {
                controller.TryBeginGrab(controller.GetMouseWorldPos());
            }
        }

        public override void Update() {}

        public override void FixedUpdate()
        {
            if (!isFloating)
                return;

            floatTimer -= Time.fixedDeltaTime;
            if (floatTimer > 0f)
                return;

            isFloating = false;
            controller.Rb.gravityScale = targetGravityScale;
        }
    }
}
