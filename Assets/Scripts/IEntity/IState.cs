using UnityEngine;

namespace IGame.IEntity
{
    public abstract class IState
    {
        protected IController controller;

        public virtual void Enter(IController controller)
        {
            this.controller = controller;
        }

        public abstract void HandleInput();
        public abstract void Update();
        public abstract void FixedUpdate();
        public virtual void Exit() {}
    }
}
