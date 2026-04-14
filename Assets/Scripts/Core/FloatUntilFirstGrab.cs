using IGame.IEntity;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace IGame.Core
{
    [RequireComponent(typeof(IController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class FloatUntilFirstGrab : MonoBehaviour
    {
        [Tooltip("If enabled, zeroes velocity while waiting for the first grab.")]
        public bool freezeVelocityWhileFloating = true;
        [Min(0.1f)]
        [Tooltip("Initial local Y scale used while waiting for the first click.")]
        public float initialStretchScaleY = 1f;
        [Tooltip("Optional child transforms whose local X scale should follow the waiting stretch ratio.")]
        public Transform[] widthScaledChildren;
        [Min(0.01f)]
        [Tooltip("Duration of the animation back to the normal scale after the first click.")]
        public float restoreDuration = 0.35f;
        [Tooltip("Ease used while returning to the normal scale.")]
        public Ease restoreEase = Ease.OutCubic;
        [Tooltip("Invoked when the float-until-first-grab state starts.")]
        public UnityEvent onStarted;
        [Tooltip("Invoked immediately when the first valid click activates the object.")]
        public UnityEvent onActivated;
        [Tooltip("Invoked after the restore animation finishes and interaction becomes available.")]
        public UnityEvent onRestoreCompleted;

        private IController _controller;
        private Rigidbody2D _rb;
        private RigidbodyType2D _originalBodyType;
        private Vector3 _originalLocalScale;
        private Vector3[] _originalChildScales = new Vector3[0];
        private bool _activated;
        private bool _isRestoring;

        private void Awake()
        {
            _controller = GetComponent<IController>();
            _rb = GetComponent<Rigidbody2D>();
            _originalBodyType = _rb.bodyType;
            _originalLocalScale = transform.localScale;
            CacheChildScales();

            ApplyStretchVisual(initialStretchScaleY);

            _rb.bodyType = RigidbodyType2D.Kinematic;
            if (freezeVelocityWhileFloating)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            _controller.SetInputEnabled(false);
            onStarted?.Invoke();
        }

        private void Update()
        {
            if (_activated || _isRestoring)
                return;

            if (freezeVelocityWhileFloating)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            if (!_controller.IsMousePressed())
                return;

            Vector2 mousePos = _controller.GetMouseWorldPos();
            if (!_controller.ContainsPointInGrabbableColliders(mousePos))
                return;

            Activate();
        }

        private void Activate()
        {
            _activated = true;
            onActivated?.Invoke();

            if (freezeVelocityWhileFloating)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            if (restoreDuration <= 0.01f)
            {
                ApplyStretchVisual(Mathf.Abs(_originalLocalScale.y));
                FinishActivation();
                return;
            }

            _isRestoring = true;
            transform.DOKill();
            float currentScaleY = Mathf.Abs(transform.localScale.y);
            float targetScaleY = Mathf.Abs(_originalLocalScale.y);
            DOTween.To(() => currentScaleY, value =>
                {
                    currentScaleY = value;
                    ApplyStretchVisual(currentScaleY);
                }, targetScaleY, restoreDuration)
                .SetEase(restoreEase)
                .SetLink(gameObject)
                .OnComplete(FinishActivation);
        }

        private void FinishActivation()
        {
            _isRestoring = false;
            _rb.bodyType = _originalBodyType;
            _controller.SetInputEnabled(true);
            onRestoreCompleted?.Invoke();
            enabled = false;
        }

        private void OnDisable()
        {
            transform.DOKill();
        }

        private void CacheChildScales()
        {
            _originalChildScales = new Vector3[widthScaledChildren != null ? widthScaledChildren.Length : 0];
            for (int i = 0; i < _originalChildScales.Length; i++)
            {
                Transform child = widthScaledChildren[i];
                _originalChildScales[i] = child != null ? child.localScale : Vector3.one;
            }
        }

        private void ApplyStretchVisual(float absoluteScaleY)
        {
            Vector3 stretchedScale = _originalLocalScale;
            stretchedScale.y = Mathf.Sign(_originalLocalScale.y == 0f ? 1f : _originalLocalScale.y) * absoluteScaleY;
            transform.localScale = stretchedScale;

            float baseAbsScaleY = Mathf.Max(0.0001f, Mathf.Abs(_originalLocalScale.y));
            float stretchRatio = absoluteScaleY / baseAbsScaleY;
            for (int i = 0; i < _originalChildScales.Length; i++)
            {
                Transform child = widthScaledChildren[i];
                if (child == null)
                    continue;

                Vector3 childScale = _originalChildScales[i];
                childScale.x = _originalChildScales[i].x * stretchRatio;
                child.localScale = childScale;
            }
        }
    }
}
