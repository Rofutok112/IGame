using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace IGame.Core
{
    public class ClearUIBurstPresenter : MonoBehaviour
    {
        [Tooltip("UI root to activate and animate. If empty, this GameObject is used.")]
        public GameObject targetRoot;

        [Tooltip("RectTransform animated for the burst effect. If empty, searched from targetRoot.")]
        public RectTransform targetRect;

        [Tooltip("CanvasGroup used for fade. If empty, searched or added on targetRoot.")]
        public CanvasGroup targetCanvasGroup;

        [Min(0.01f)]
        public float duration = 0.45f;

        [Range(0.1f, 1f)]
        [Tooltip("Starting scale before the burst animation.")]
        public float startScale = 0.7f;

        [Min(0f)]
        [Tooltip("Extra overshoot scale before settling back to 1.")]
        public float overshootScale = 0.18f;

        [Tooltip("Only play the clear animation once.")]
        public bool playOnlyOnce = true;

        [Tooltip("Deactivate the target and initialize the hidden visual state on Awake.")]
        public bool prepareHiddenOnAwake = true;

        private Sequence _sequence;
        private bool _hasPlayed;
        private GameObject _resolvedRoot;
        private RectTransform _resolvedRect;
        private CanvasGroup _resolvedCanvasGroup;

        private void Awake()
        {
            ResolveReferences();

            if (prepareHiddenOnAwake)
            {
                ApplyHiddenVisualState();
                _resolvedRoot.SetActive(false);
            }
        }

        public void Play()
        {
            PlayAsync().Forget();
        }

        public async UniTask PlayAsync()
        {
            ResolveReferences();

            if (playOnlyOnce && _hasPlayed)
                return;

            _hasPlayed = true;
            _sequence?.Kill();

            _resolvedRoot.SetActive(true);
            ApplyHiddenVisualState();

            _sequence = DOTween.Sequence()
                .SetLink(_resolvedRoot)
                .SetUpdate(true)
                .Append(_resolvedRect.DOScale(1f + overshootScale, duration * 0.55f).SetEase(Ease.OutBack))
                .Join(_resolvedCanvasGroup.DOFade(1f, duration * 0.45f).SetEase(Ease.OutQuad))
                .Append(_resolvedRect.DOScale(1f, duration * 0.45f).SetEase(Ease.OutCubic));

            await _sequence.AsyncWaitForCompletion().AsUniTask();
        }

        public void HideInstant()
        {
            ResolveReferences();
            _sequence?.Kill();
            ApplyHiddenVisualState();
            _resolvedRoot.SetActive(false);
            _hasPlayed = false;
        }

        private void ResolveReferences()
        {
            _resolvedRoot = targetRoot != null ? targetRoot : gameObject;

            if (targetRect != null)
            {
                _resolvedRect = targetRect;
            }
            else
            {
                _resolvedRect = _resolvedRoot.GetComponent<RectTransform>();
            }

            if (_resolvedRect == null)
            {
                Debug.LogError("ClearUIBurstPresenter requires a RectTransform target.", this);
            }

            if (targetCanvasGroup != null)
            {
                _resolvedCanvasGroup = targetCanvasGroup;
            }
            else
            {
                _resolvedCanvasGroup = _resolvedRoot.GetComponent<CanvasGroup>();
                if (_resolvedCanvasGroup == null)
                    _resolvedCanvasGroup = _resolvedRoot.AddComponent<CanvasGroup>();
            }
        }

        private void ApplyHiddenVisualState()
        {
            if (_resolvedRect != null)
                _resolvedRect.localScale = Vector3.one * startScale;

            if (_resolvedCanvasGroup != null)
                _resolvedCanvasGroup.alpha = 0f;
        }
    }
}
