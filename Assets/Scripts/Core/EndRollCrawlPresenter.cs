using TMPro;
using UnityEngine;

namespace IGame.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class EndRollCrawlPresenter : MonoBehaviour
    {
        [TextArea(5, 12)]
        [Tooltip("Displayed crawl text. Uses TMP rich text.")]
        public string crawlText = "Iゲーム\n\n企画：本村\n制作：田尻 \n効果音：ノタの森さま　音人さま　効果音ラボさま　ポケットサウンドさま – https://pocket-se.info/";
        [Tooltip("TMP text used for the crawl. If empty, searched on this object.")]
        public TMP_Text targetText;
        [Min(1f)]
        [Tooltip("Seconds for the full crawl movement.")]
        public float duration = 14f;
        [Tooltip("If enabled, starts automatically on Awake.")]
        public bool playOnAwake = true;
        [Tooltip("Start anchored position in local UI space.")]
        public Vector2 startAnchoredPosition = new Vector2(0f, -520f);
        [Tooltip("End anchored position in local UI space.")]
        public Vector2 endAnchoredPosition = new Vector2(0f, 820f);
        [Tooltip("Rotation used to create the Star Wars style perspective.")]
        public Vector3 crawlEulerAngles = new Vector3(62f, 0f, 0f);
        [Min(0f)]
        [Tooltip("Optional fade-in duration at the beginning.")]
        public float fadeInDuration = 0.45f;
        [Min(0f)]
        [Tooltip("Optional fade-out duration near the end.")]
        public float fadeOutDuration = 0.9f;

        private RectTransform resolvedRect;
        private TMP_Text resolvedText;
        private Color baseColor = Color.white;
        private bool isPlaying;
        private float elapsed;

        private void Awake()
        {
            ResolveReferences();
            PrepareVisual();

            if (playOnAwake)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!isPlaying || resolvedRect == null || resolvedText == null)
                return;

            elapsed += Time.deltaTime;
            float normalized = duration > 0.001f ? Mathf.Clamp01(elapsed / duration) : 1f;
            resolvedRect.anchoredPosition = Vector2.Lerp(startAnchoredPosition, endAnchoredPosition, normalized);

            float alpha = 1f;
            if (fadeInDuration > 0.001f)
            {
                alpha *= Mathf.Clamp01(elapsed / fadeInDuration);
            }

            if (fadeOutDuration > 0.001f && duration - elapsed < fadeOutDuration)
            {
                alpha *= Mathf.Clamp01((duration - elapsed) / fadeOutDuration);
            }

            Color color = baseColor;
            color.a = baseColor.a * Mathf.Clamp01(alpha);
            resolvedText.color = color;

            if (normalized >= 1f)
            {
                isPlaying = false;
            }
        }

        public void Play()
        {
            ResolveReferences();
            PrepareVisual();
            elapsed = 0f;
            isPlaying = true;
        }

        public void Stop()
        {
            isPlaying = false;
        }

        private void ResolveReferences()
        {
            if (resolvedRect == null)
            {
                resolvedRect = GetComponent<RectTransform>();
            }

            if (targetText != null)
            {
                resolvedText = targetText;
            }
            else if (resolvedText == null)
            {
                resolvedText = GetComponent<TMP_Text>();
            }

            if (resolvedText != null)
            {
                baseColor = resolvedText.color;
            }
        }

        private void PrepareVisual()
        {
            if (resolvedRect == null || resolvedText == null)
                return;

            resolvedText.text = crawlText;
            resolvedText.alignment = TextAlignmentOptions.Center;
            resolvedText.textWrappingMode = TextWrappingModes.Normal;

            resolvedRect.anchoredPosition = startAnchoredPosition;
            resolvedRect.localRotation = Quaternion.Euler(crawlEulerAngles);

            Color color = baseColor;
            color.a = fadeInDuration > 0.001f ? 0f : baseColor.a;
            resolvedText.color = color;
        }
    }
}
