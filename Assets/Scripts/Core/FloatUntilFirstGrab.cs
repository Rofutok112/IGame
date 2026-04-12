using IGame.IEntity;
using UnityEngine;

namespace IGame.Core
{
    [RequireComponent(typeof(IController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class FloatUntilFirstGrab : MonoBehaviour
    {
        [Tooltip("If enabled, zeroes velocity while waiting for the first grab.")]
        public bool freezeVelocityWhileFloating = true;

        private IController _controller;
        private Rigidbody2D _rb;
        private RigidbodyType2D _originalBodyType;
        private bool _activated;

        private void Awake()
        {
            _controller = GetComponent<IController>();
            _rb = GetComponent<Rigidbody2D>();
            _originalBodyType = _rb.bodyType;

            _rb.bodyType = RigidbodyType2D.Kinematic;
            if (freezeVelocityWhileFloating)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            _controller.SetInputEnabled(false);
        }

        private void Update()
        {
            if (_activated)
                return;

            if (freezeVelocityWhileFloating)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            if (!_controller.IsMousePressed())
                return;

            Vector2 mousePos = _controller.GetMouseWorldPos();
            if (!_controller.TryBeginGrab(mousePos))
                return;

            Activate();
        }

        private void Activate()
        {
            _activated = true;
            _rb.bodyType = _originalBodyType;
            _controller.SetInputEnabled(true);
            enabled = false;
        }
    }
}
