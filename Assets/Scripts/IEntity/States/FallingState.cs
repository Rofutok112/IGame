using UnityEngine;

namespace IGame.IEntity.States
{
    public class FallingState : IState
    {
        private float defaultGravity;

        public override void Enter(IController controller)
        {
            base.Enter(controller);
            // Restore gravity to make it fall
            if (controller.Rb.gravityScale <= 0.01f)
            {
                controller.Rb.gravityScale = 1f;
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
        public override void FixedUpdate() {}
    }
}
