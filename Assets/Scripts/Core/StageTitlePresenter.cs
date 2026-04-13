using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IGame.Core
{
    public class StageTitlePresenter : MonoBehaviour
    {
        [Tooltip("Root GameObject to activate while the title is shown. If empty, this GameObject is used.")]
        public GameObject targetRoot;

        [Tooltip("Text component used for the stage title. If empty, searched from targetRoot.")]
        public TMP_Text targetText;

        [Tooltip("CanvasGroup used for fade animation. If empty, searched or added on targetRoot.")]
        public CanvasGroup targetCanvasGroup;

        [Tooltip("Optional explicit title. If empty, the active scene name is used.")]
        public string overrideTitle;

        [Min(0f)]
        [Tooltip("Delay before showing the title.")]
        public float startDelay = 0.15f;

        [Min(0.01f)]
        [Tooltip("Fade-in duration.")]
        public float fadeInDuration = 0.25f;

        [Min(0f)]
        [Tooltip("How long to keep the title visible after fade-in.")]
        public float visibleDuration = 1.4f;

        [Min(0.01f)]
        [Tooltip("Fade-out duration.")]
        public float fadeOutDuration = 0.35f;

        [Tooltip("Deactivate the root after the animation finishes.")]
        public bool hideAfterPlay = true;

        private Sequence sequence;
        private GameObject resolvedRoot;
        private TMP_Text resolvedText;
        private CanvasGroup resolvedCanvasGroup;

        private void Awake()
        {
            ResolveReferences();
            ApplyHiddenState();

            if (resolvedRoot != null)
            {
                resolvedRoot.SetActive(false);
            }
        }

        private void Start()
        {
            PlayAsync().Forget();
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
            if (resolvedRoot == null || resolvedText == null)
                return;

            sequence?.Kill();

            resolvedRoot.SetActive(true);
            resolvedText.text = GetStageTitle();
            ApplyHiddenState();

            sequence = DOTween.Sequence()
                .SetLink(resolvedRoot)
                .SetUpdate(true)
                .AppendInterval(startDelay)
                .Append(resolvedCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad))
                .AppendInterval(visibleDuration)
                .Append(resolvedCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

            await sequence.AsyncWaitForCompletion().AsUniTask();

            if (hideAfterPlay && resolvedRoot != null)
            {
                resolvedRoot.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            resolvedRoot = targetRoot != null ? targetRoot : gameObject;

            if (targetText != null)
            {
                resolvedText = targetText;
            }
            else if (resolvedRoot != null)
            {
                resolvedText = resolvedRoot.GetComponentInChildren<TMP_Text>(true);
            }

            if (targetCanvasGroup != null)
            {
                resolvedCanvasGroup = targetCanvasGroup;
            }
            else if (resolvedRoot != null)
            {
                resolvedCanvasGroup = resolvedRoot.GetComponent<CanvasGroup>();
                if (resolvedCanvasGroup == null)
                {
                    resolvedCanvasGroup = resolvedRoot.AddComponent<CanvasGroup>();
                }
            }
        }

        private void ApplyHiddenState()
        {
            if (resolvedCanvasGroup != null)
            {
                resolvedCanvasGroup.alpha = 0f;
            }
        }

        private string GetStageTitle()
        {
            if (!string.IsNullOrWhiteSpace(overrideTitle))
            {
                return overrideTitle;
            }

            return SceneManager.GetActiveScene().name;
        }
    }
}
