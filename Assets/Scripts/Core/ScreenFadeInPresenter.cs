using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IGame.Core
{
    public class ScreenFadeInPresenter : MonoBehaviour
    {
        [Tooltip("Fullscreen UI root to activate and fade. If empty, this GameObject is used.")]
        public GameObject targetRoot;

        [Tooltip("CanvasGroup used for the fade. If empty, searched or added on targetRoot.")]
        public CanvasGroup targetCanvasGroup;

        [Tooltip("Optional Image used as the fade panel. If empty, searched from targetRoot.")]
        public Image targetImage;

        [Min(0.01f)]
        public float duration = 0.6f;

        [Tooltip("Play a fade-out automatically when the scene starts.")]
        public bool playFadeOutOnStart = false;

        [Tooltip("Only play the fade once.")]
        public bool playOnlyOnce = true;

        [Tooltip("Deactivate and hide the fade panel on Awake.")]
        public bool prepareHiddenOnAwake = true;

        public UnityEvent onComplete;

        private Sequence sequence;
        private bool hasPlayed;
        private GameObject resolvedRoot;
        private CanvasGroup resolvedCanvasGroup;
        private Image resolvedImage;
        private Color initialImageColor = Color.black;
        private bool hasInitialImageColor;

        private void Awake()
        {
            ResolveReferences();

            if (prepareHiddenOnAwake)
            {
                ApplyHiddenState();
                resolvedRoot.SetActive(false);
            }
        }

        private void Start()
        {
            if (playFadeOutOnStart)
            {
                PlayFadeOutAsync().Forget();
            }
        }

        private void OnDisable()
        {
            sequence?.Kill();
            sequence = null;
        }

        public void Play()
        {
            PlayAsync().Forget();
        }

        public async UniTask PlayAsync()
        {
            ResolveReferences();

            if (playOnlyOnce && hasPlayed)
                return;

            hasPlayed = true;
            sequence?.Kill();

            resolvedRoot.SetActive(true);
            ApplyHiddenState();

            sequence = DOTween.Sequence()
                .SetLink(resolvedRoot)
                .SetUpdate(true)
                .Append(resolvedCanvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));

            await sequence.AsyncWaitForCompletion().AsUniTask();
            onComplete?.Invoke();
        }

        public void PlayFadeOut()
        {
            PlayFadeOutAsync().Forget();
        }

        public async UniTask PlayFadeOutAsync()
        {
            ResolveReferences();

            if (playOnlyOnce && hasPlayed)
                return;

            hasPlayed = true;
            sequence?.Kill();

            resolvedRoot.SetActive(true);
            ApplyVisibleState();

            sequence = DOTween.Sequence()
                .SetLink(resolvedRoot)
                .SetUpdate(true)
                .Append(resolvedCanvasGroup.DOFade(0f, duration).SetEase(Ease.OutQuad))
                .OnComplete(() => resolvedRoot.SetActive(false));

            await sequence.AsyncWaitForCompletion().AsUniTask();
            onComplete?.Invoke();
        }

        public void HideInstant()
        {
            ResolveReferences();
            sequence?.Kill();
            ApplyHiddenState();
            resolvedRoot.SetActive(false);
            hasPlayed = false;
        }

        private void ResolveReferences()
        {
            resolvedRoot = targetRoot != null ? targetRoot : gameObject;

            if (targetCanvasGroup != null)
            {
                resolvedCanvasGroup = targetCanvasGroup;
            }
            else
            {
                resolvedCanvasGroup = resolvedRoot.GetComponent<CanvasGroup>();
                if (resolvedCanvasGroup == null)
                    resolvedCanvasGroup = resolvedRoot.AddComponent<CanvasGroup>();
            }

            if (targetImage != null)
            {
                resolvedImage = targetImage;
            }
            else
            {
                resolvedImage = resolvedRoot.GetComponent<Image>();
            }

            if (resolvedImage != null)
            {
                if (!hasInitialImageColor || !Application.isPlaying)
                {
                    initialImageColor = resolvedImage.color;
                    hasInitialImageColor = true;
                }
            }
        }

        private void ApplyHiddenState()
        {
            if (resolvedCanvasGroup != null)
            {
                resolvedCanvasGroup.alpha = 0f;
            }

            if (resolvedImage != null)
            {
                resolvedImage.color = initialImageColor;
            }
        }

        private void ApplyVisibleState()
        {
            if (resolvedCanvasGroup != null)
            {
                resolvedCanvasGroup.alpha = 1f;
            }

            if (resolvedImage != null)
            {
                resolvedImage.color = initialImageColor;
            }
        }
    }
}
