using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Sprites;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IGame.Core
{
    public class UIImageTypewriterReveal : MonoBehaviour
    {
        [Tooltip("UI root to activate before typing. If empty, this GameObject is used.")]
        public GameObject targetRoot;

        [Tooltip("Image that displays the prepared text sprite.")]
        public Image targetImage;

        [Tooltip("Optional CanvasGroup used to fade the whole image in.")]
        public CanvasGroup targetCanvasGroup;

        [Tooltip("Normalized fill amounts reached on each keystroke.")]
        public float[] stepFillAmounts = { 0.2917f, 0.3699f, 0.5308f, 0.7821f, 0.8383f };

        [Min(0.01f)]
        public float keyInterval = 0.12f;

        [Tooltip("If enabled, each step appears instantly instead of tweening.")]
        public bool snapEachStep = true;

        [Min(0.01f)]
        public float keyRevealDuration = 0.06f;

        [Min(0f)]
        public float startDelay = 0f;

        public bool playOnlyOnce = true;
        public bool prepareHiddenOnAwake = true;

        public UnityEvent onComplete;

        private Sequence sequence;
        private bool hasPlayed;
        private GameObject resolvedRoot;
        private Image resolvedImage;
        private CanvasGroup resolvedCanvasGroup;

        private void Awake()
        {
            ResolveReferences();

            if (prepareHiddenOnAwake)
            {
                ApplyHiddenState();
                resolvedRoot.SetActive(false);
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

            if (startDelay > 0f)
            {
                await UniTask.Delay((int)(startDelay * 1000f), ignoreTimeScale: true, delayTiming: PlayerLoopTiming.Update);
            }

            if (resolvedCanvasGroup != null)
            {
                resolvedCanvasGroup.alpha = 1f;
            }

            foreach (float step in stepFillAmounts)
            {
                float clampedStep = Mathf.Clamp01(step);

                if (snapEachStep || keyRevealDuration <= 0.0001f)
                {
                    resolvedImage.fillAmount = clampedStep;
                }
                else
                {
                    sequence?.Kill();
                    sequence = DOTween.Sequence()
                        .SetLink(resolvedRoot)
                        .SetUpdate(true)
                        .Append(DOTween.To(
                            () => resolvedImage.fillAmount,
                            value => resolvedImage.fillAmount = value,
                            clampedStep,
                            keyRevealDuration).SetEase(Ease.OutQuad));

                    await sequence.AsyncWaitForCompletion().AsUniTask();
                }

                float waitDuration = snapEachStep ? keyInterval : Mathf.Max(0f, keyInterval - keyRevealDuration);
                if (waitDuration > 0f)
                {
                    await UniTask.Delay((int)(waitDuration * 1000f), ignoreTimeScale: true, delayTiming: PlayerLoopTiming.Update);
                }
            }

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

            if (targetImage != null)
            {
                resolvedImage = targetImage;
            }
            else
            {
                resolvedImage = resolvedRoot.GetComponent<Image>();
            }

            if (targetCanvasGroup != null)
            {
                resolvedCanvasGroup = targetCanvasGroup;
            }
            else
            {
                resolvedCanvasGroup = resolvedRoot.GetComponent<CanvasGroup>();
            }

            if (resolvedImage == null)
            {
                Debug.LogError("UIImageTypewriterReveal requires a target Image.", this);
                return;
            }

            resolvedImage.type = Image.Type.Filled;
            resolvedImage.fillMethod = Image.FillMethod.Horizontal;
            resolvedImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            resolvedImage.preserveAspect = true;
        }

        private void ApplyHiddenState()
        {
            if (resolvedImage != null)
            {
                resolvedImage.fillAmount = 0f;
            }

            if (resolvedCanvasGroup != null)
            {
                resolvedCanvasGroup.alpha = 0f;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Image image = targetImage != null ? targetImage : GetComponent<Image>();
            if (image == null)
                return;

            RectTransform rectTransform = image.rectTransform;
            if (rectTransform == null)
                return;

            Rect drawingRect = GetDrawingRect(rectTransform, image);
            Vector3 bottomLeft = rectTransform.TransformPoint(new Vector3(drawingRect.xMin, drawingRect.yMin, 0f));
            Vector3 topLeft = rectTransform.TransformPoint(new Vector3(drawingRect.xMin, drawingRect.yMax, 0f));
            Vector3 topRight = rectTransform.TransformPoint(new Vector3(drawingRect.xMax, drawingRect.yMax, 0f));
            Vector3 bottomRight = rectTransform.TransformPoint(new Vector3(drawingRect.xMax, drawingRect.yMin, 0f));
            Vector3 widthVector = bottomRight - bottomLeft;
            Vector3 heightVector = topLeft - bottomLeft;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);

            if (stepFillAmounts == null)
                return;

            for (int i = 0; i < stepFillAmounts.Length; i++)
            {
                float clampedStep = Mathf.Clamp01(stepFillAmounts[i]);
                Vector3 lineBottom = bottomLeft + widthVector * clampedStep;
                Vector3 lineTop = lineBottom + heightVector;

                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                Gizmos.DrawLine(lineBottom, lineTop);

                Handles.color = new Color(1f, 0.95f, 0.5f, 1f);
                Vector3 labelOffset = Vector3.up * 0.08f;
                if (SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera != null)
                {
                    labelOffset = SceneView.currentDrawingSceneView.camera.transform.up * 0.08f;
                }

                Vector3 labelPosition = lineTop + labelOffset;
                Handles.Label(labelPosition, $"{i + 1}: {clampedStep:0.##}");
            }
        }

        private static Rect GetDrawingRect(RectTransform rectTransform, Image image)
        {
            Rect rect = rectTransform.rect;
            Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
            if (sprite == null)
                return rect;

            Vector4 padding = DataUtility.GetPadding(sprite);
            Rect spriteRect = sprite.rect;
            float spriteWidth = Mathf.Max(1f, spriteRect.width);
            float spriteHeight = Mathf.Max(1f, spriteRect.height);

            if (image.preserveAspect && spriteRect.size.sqrMagnitude > 0f)
            {
                float spriteRatio = spriteRect.width / spriteRect.height;
                float rectRatio = rect.width / rect.height;

                if (spriteRatio > rectRatio)
                {
                    float oldHeight = rect.height;
                    rect.height = rect.width * (1f / spriteRatio);
                    rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
                }
                else
                {
                    float oldWidth = rect.width;
                    rect.width = rect.height * spriteRatio;
                    rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
                }
            }

            return new Rect(
                rect.x + rect.width * (padding.x / spriteWidth),
                rect.y + rect.height * (padding.y / spriteHeight),
                rect.width * ((spriteWidth - padding.x - padding.z) / spriteWidth),
                rect.height * ((spriteHeight - padding.y - padding.w) / spriteHeight));
        }
#endif
    }
}
