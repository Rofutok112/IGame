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
                Vector2 mousePos = controller.GetMouseWorldPos();
                
                // Raycast to check if clicked on this object
                Collider2D[] cols = Physics2D.OverlapPointAll(mousePos);
                bool hitThis = false;
                foreach (var col in cols)
                {
                    if (col.gameObject == controller.gameObject)
                    {
                        hitThis = true;
                        break;
                    }
                }

                if (hitThis)
                {
                    Vector2 localPoint = controller.transform.InverseTransformPoint(mousePos);
                    controller.GrabLocalPoint = localPoint;
                    controller.GrabWorldPoint = mousePos;

                    if (controller.IsInMoveGrabZone(localPoint))
                    {
                        controller.ChangeState(new MovingState());
                    }
                    else if (controller.IsInStretchGrabZone(localPoint))
                    {
                        controller.StretchFromTop = controller.GetTopStretchGrabZoneLocalRect().Contains(localPoint);
                        controller.ChangeState(new StretchingState());
                    }
                    else if (controller.IsInEdgeGrabZone(localPoint))
                    {
                        controller.ChangeState(new RotatingState());
                    }
                    else
                    {
                        if (Mathf.Abs(localPoint.y) >= controller.edgeGrabThreshold)
                            controller.ChangeState(new RotatingState());
                        else
                            controller.ChangeState(new MovingState());
                    }
                }
            }
        }

        public override void Update() {}
        public override void FixedUpdate() {}
    }
}
