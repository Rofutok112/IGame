using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using IGame.IEntity.States;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IGame.IEntity
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class IController : MonoBehaviour
    {
        public Rigidbody2D Rb { get; private set; }
        private IState currentState;
        private bool inputEnabled = true;

        [Header("Settings")]
        [Tooltip("The vertical threshold for deciding if the edge was grabbed (local space).")]
        public float edgeGrabThreshold = 1.0f;
        public float moveSpeed = 15f;
        public float rotateSpeed = 15f;
        [Tooltip("Local-space offset of the move-grab zone. Zero keeps it centered on the object.")]
        public Vector2 moveGrabZoneOffset = Vector2.zero;
        [Tooltip("Local-space size of the move-grab zone. Zero uses collider width and edgeGrabThreshold * 2.")]
        public Vector2 moveGrabZoneSize = Vector2.zero;
        [Tooltip("Local-space size shared by the top and bottom edge-grab zones. Zero uses collider width and the remaining height outside edgeGrabThreshold.")]
        public Vector2 edgeGrabZoneSize = Vector2.zero;
        [Tooltip("Local-space offset added to the top edge-grab zone center.")]
        public Vector2 topEdgeGrabZoneOffset = Vector2.zero;
        [Tooltip("Local-space offset added to the bottom edge-grab zone center.")]
        public Vector2 bottomEdgeGrabZoneOffset = Vector2.zero;
        [Tooltip("Local-space size shared by the top and bottom stretch-grab zones. Zero disables stretch zones.")]
        public Vector2 stretchGrabZoneSize = Vector2.zero;
        [Tooltip("Local-space offset added to the top stretch-grab zone center.")]
        public Vector2 topStretchGrabZoneOffset = Vector2.zero;
        [Tooltip("Local-space offset added to the bottom stretch-grab zone center.")]
        public Vector2 bottomStretchGrabZoneOffset = Vector2.zero;
        [Min(0.1f)]
        [Tooltip("Minimum local Y scale allowed during stretching.")]
        public float minStretchScaleY = 1f;
        [Min(0.1f)]
        [Tooltip("Maximum local Y scale allowed during stretching.")]
        public float maxStretchScaleY = 8f;
        [Min(0f)]
        [Tooltip("Release drag when the mouse gets farther than this from the object's center. 0 disables the check.")]
        public float maxDragDistanceFromCenter = 0f;
        [Tooltip("Local-space offset of the drag-release area. Zero keeps it centered on the object.")]
        public Vector2 dragReleaseZoneOffset = Vector2.zero;
        [Tooltip("Local-space size of the drag-release area. Zero keeps using maxDragDistanceFromCenter.")]
        public Vector2 dragReleaseZoneSize = Vector2.zero;
        [Header("Guides")]
        [Tooltip("Shown only while the object is in rotating state.")]
        public GameObject rotationGuideObject;
        [Tooltip("If enabled, hides the rotation guide automatically on Start.")]
        public bool hideRotationGuideOnStart = true;
        [Tooltip("If enabled, stretches the rotation guide horizontally based on the current Y scale of the I object.")]
        public bool scaleRotationGuideWidthWithStretch = true;

        [Header("Falling")]
        [Min(0f)]
        [Tooltip("Seconds to float before normal gravity is restored after entering FallingState.")]
        public float fallingFloatDuration = 2f;
        [Tooltip("Gravity scale used during the brief floating period.")]
        public float fallingFloatGravityScale = 0f;
        [Tooltip("Gravity scale restored after the floating period when the current gravity is near zero.")]
        public float defaultFallingGravityScale = 1f;

        [Header("State Visuals")]
        [Tooltip("If enabled, plays simple visual feedback for grabbed and falling states.")]
        public bool enableStateVisualFeedback = true;
        [Tooltip("Tint applied while the I is fixed in place by a grab.")]
        public Color pinnedStateColor = new Color(0.75f, 0.92f, 1f, 1f);
        [Min(0.01f)]
        [Tooltip("Blend time when entering or leaving the fixed state visuals.")]
        public float pinnedStateBlendDuration = 0.12f;
        [Tooltip("Flash color used when the I is released into falling.")]
        public Color fallingFlashColor = new Color(1f, 0.88f, 0.7f, 1f);
        [Min(0.01f)]
        [Tooltip("Total duration of the falling flash.")]
        public float fallingFlashDuration = 0.24f;
        [Min(0.01f)]
        [Tooltip("Duration of the small squash/stretch played when falling starts.")]
        public float fallingSquashDuration = 0.2f;
        [Tooltip("Relative scale applied briefly when falling starts. X > 1 and Y < 1 reads like a drop release.")]
        public Vector2 fallingSquashScale = new Vector2(1.08f, 0.92f);

        [Header("Audio")]
        [Tooltip("AudioSource used for interaction sound effects. If empty, searched on this object.")]
        public AudioSource interactionAudioSource;
        [Tooltip("Played once when drag begins.")]
        public AudioClip dragStartClip;
        [Range(0f, 1f)]
        public float dragStartVolume = 1f;
        [Tooltip("Repeated while rotating.")]
        public AudioClip rotatingClip;
        [Range(0f, 1f)]
        public float rotatingVolume = 1f;
        [Min(0f)]
        [Tooltip("Minimum time between repeated rotation sounds.")]
        public float rotatingSoundInterval = 0.06f;
        [Min(0f)]
        [Tooltip("Minimum angle movement required before another rotation sound can play.")]
        public float rotatingSoundAngleStep = 2.5f;
        [Tooltip("Repeated while stretching.")]
        public AudioClip stretchingClip;
        [Range(0f, 1f)]
        public float stretchingVolume = 1f;
        [Min(0f)]
        [Tooltip("Minimum time between repeated stretch sounds.")]
        public float stretchingSoundInterval = 0.06f;
        [Min(0f)]
        [Tooltip("Minimum local Y scale change required before another stretch sound can play.")]
        public float stretchingSoundScaleStep = 0.03f;
        [Tooltip("Played once when the stretch reaches the minimum length.")]
        public AudioClip stretchMinReachedClip;
        [Range(0f, 1f)]
        public float stretchMinReachedVolume = 1f;
        [Tooltip("Played once when the stretch reaches the maximum length.")]
        public AudioClip stretchMaxReachedClip;
        [Range(0f, 1f)]
        public float stretchMaxReachedVolume = 1f;
        [Header("Cursor")]
        [Tooltip("If enabled, swaps the mouse cursor when hovering move/rotate/stretch zones.")]
        public bool enableInteractionCursor = true;
        [Tooltip("If enabled, shows a dedicated cursor while the mouse button is held down.")]
        public bool enableHeldCursor = true;
        [Tooltip("Optional cursor shown when not hovering any interaction zone. Leave empty to use the OS default cursor.")]
        public Texture2D defaultCursorTexture;
        public Vector2 defaultCursorHotspot = Vector2.zero;
        public Texture2D heldCursorTexture;
        public Vector2 heldCursorHotspot = Vector2.zero;
        public Texture2D moveCursorTexture;
        public Vector2 moveCursorHotspot = Vector2.zero;
        public Texture2D rotateCursorTexture;
        public Vector2 rotateCursorHotspot = Vector2.zero;
        public Texture2D stretchCursorTexture;
        public Vector2 stretchCursorHotspot = Vector2.zero;
        public CursorMode cursorMode = CursorMode.Auto;
        [Tooltip("If enabled, custom cursors are drawn with a Screen Space Overlay canvas so they always appear in front.")]
        public bool useOverlayCursor = true;
        [Min(1)]
        [Tooltip("Sorting order used by the generated cursor overlay canvas.")]
        public int overlayCursorSortingOrder = 1000;
        [Tooltip("Display size for the overlay cursor image in pixels.")]
        public Vector2 overlayCursorSize = new Vector2(48f, 48f);

        [Header("Collision")]
        [Tooltip("Layers that block movement and rotation. Default = Everything.")]
        public LayerMask collisionMask = ~0;   // ~0 = all layers

        public Vector2 GrabLocalPoint { get; set; }
        public Vector2 GrabWorldPoint { get; set; }
        public bool StretchFromTop { get; set; }

        private Vector3 initialRotationGuideLocalScale = Vector3.one;
        private float initialAbsLocalScaleY = 1f;
        private bool hasCachedRotationGuideScale;
        private SpriteRenderer[] cachedStateSpriteRenderers = new SpriteRenderer[0];
        private Color[] cachedBaseSpriteColors = new Color[0];
        private bool hasCachedStateVisuals;
        private float lastRotatingSoundTime = float.NegativeInfinity;
        private float lastStretchingSoundTime = float.NegativeInfinity;
        private int lastStretchLimitState;
        private InteractionCursorKind appliedCursorKind = InteractionCursorKind.None;
        private Canvas cursorOverlayCanvas;
        private RectTransform cursorOverlayRect;
        private RawImage cursorOverlayImage;
        private bool isUsingOverlayCursor;

        /// <summary>Returns a ContactFilter2D configured with the collisionMask, excluding triggers.</summary>
        public ContactFilter2D GetContactFilter()
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(collisionMask);
            filter.useTriggers = false;
            filter.useLayerMask = true;
            return filter;
        }

        void Start()
        {
            Rb = GetComponent<Rigidbody2D>();
            if (interactionAudioSource == null)
            {
                interactionAudioSource = GetComponent<AudioSource>();
            }
            CacheRotationGuideScale();
            CacheStateVisuals();
            EnsureCursorOverlay();

            if (hideRotationGuideOnStart)
            {
                HideRotationGuide();
            }

            ChangeState(new FallingState());
        }

        void Update()
        {
            UpdateInteractionCursor();
            UpdateOverlayCursorPosition();

            if (!inputEnabled) return;

            if (currentState != null)
            {
                currentState.HandleInput();
                currentState.Update();
            }
        }

        void FixedUpdate()
        {
            if (!inputEnabled) return;

            if (currentState != null)
            {
                currentState.FixedUpdate();
            }
        }

        void OnDisable()
        {
            ApplyCursor(InteractionCursorKind.None, true);
            SetOverlayCursorVisible(false);
            Cursor.visible = true;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!inputEnabled)
            {
                ApplyCursor(InteractionCursorKind.None);
            }
        }

        public void ShowRotationGuide()
        {
            if (rotationGuideObject != null)
            {
                SyncRotationGuideScale();
                rotationGuideObject.SetActive(true);
            }
        }

        public void HideRotationGuide()
        {
            if (rotationGuideObject != null)
            {
                rotationGuideObject.SetActive(false);
            }
        }

        public void SyncRotationGuideScale()
        {
            if (!scaleRotationGuideWidthWithStretch || rotationGuideObject == null)
                return;

            CacheRotationGuideScale();

            float currentAbsScaleY = Mathf.Abs(transform.localScale.y);
            float stretchRatio = initialAbsLocalScaleY > 0.0001f
                ? currentAbsScaleY / initialAbsLocalScaleY
                : 1f;

            Vector3 scaledGuide = initialRotationGuideLocalScale;
            scaledGuide.x *= stretchRatio;
            rotationGuideObject.transform.localScale = scaledGuide;
        }

        private void CacheRotationGuideScale()
        {
            if (hasCachedRotationGuideScale || rotationGuideObject == null)
                return;

            initialRotationGuideLocalScale = rotationGuideObject.transform.localScale;
            initialAbsLocalScaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.y));
            hasCachedRotationGuideScale = true;
        }

        private void CacheStateVisuals()
        {
            if (hasCachedStateVisuals)
                return;

            cachedStateSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            cachedBaseSpriteColors = new Color[cachedStateSpriteRenderers.Length];
            for (int i = 0; i < cachedStateSpriteRenderers.Length; i++)
            {
                cachedBaseSpriteColors[i] = cachedStateSpriteRenderers[i] != null
                    ? cachedStateSpriteRenderers[i].color
                    : Color.white;
            }

            hasCachedStateVisuals = true;
        }

        public void EnterPinnedVisualState()
        {
            if (!enableStateVisualFeedback)
                return;

            CacheStateVisuals();
            for (int i = 0; i < cachedStateSpriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = cachedStateSpriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                spriteRenderer.DOKill();
                spriteRenderer.DOColor(pinnedStateColor, pinnedStateBlendDuration)
                    .SetLink(spriteRenderer.gameObject);
            }
        }

        public void ExitPinnedVisualState()
        {
            if (!enableStateVisualFeedback)
                return;

            CacheStateVisuals();
            for (int i = 0; i < cachedStateSpriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = cachedStateSpriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                spriteRenderer.DOKill();
                spriteRenderer.DOColor(cachedBaseSpriteColors[i], pinnedStateBlendDuration)
                    .SetLink(spriteRenderer.gameObject);
            }
        }

        public void PlayFallingVisualCue()
        {
            if (!enableStateVisualFeedback)
                return;

            CacheStateVisuals();
            ExitPinnedVisualState();

            float halfFlashDuration = Mathf.Max(0.01f, fallingFlashDuration * 0.5f);
            for (int i = 0; i < cachedStateSpriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = cachedStateSpriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                Color baseColor = cachedBaseSpriteColors[i];
                spriteRenderer.DOKill();
                spriteRenderer.DOColor(fallingFlashColor, halfFlashDuration)
                    .SetLink(spriteRenderer.gameObject)
                    .OnComplete(() =>
                    {
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.DOColor(baseColor, halfFlashDuration)
                                .SetLink(spriteRenderer.gameObject);
                        }
                    });
            }

            transform.DOKill();
            Vector3 baseScale = transform.localScale;
            Vector3 squashScale = new Vector3(
                baseScale.x * fallingSquashScale.x,
                baseScale.y * fallingSquashScale.y,
                baseScale.z);

            Sequence sequence = DOTween.Sequence()
                .SetLink(gameObject)
                .Append(transform.DOScale(squashScale, fallingSquashDuration * 0.45f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(baseScale, fallingSquashDuration * 0.55f).SetEase(Ease.OutCubic));

            sequence.Play();
        }

        public void PlayDragStartSound()
        {
            if (interactionAudioSource == null || dragStartClip == null)
                return;

            interactionAudioSource.PlayOneShot(dragStartClip, dragStartVolume);
        }

        public void ResetRotatingSoundGate()
        {
            lastRotatingSoundTime = float.NegativeInfinity;
        }

        public void TryPlayRotatingSound(float angleMoved)
        {
            if (interactionAudioSource == null || rotatingClip == null)
                return;

            if (Mathf.Abs(angleMoved) < rotatingSoundAngleStep)
                return;

            if (Time.unscaledTime - lastRotatingSoundTime < rotatingSoundInterval)
                return;

            lastRotatingSoundTime = Time.unscaledTime;
            interactionAudioSource.PlayOneShot(rotatingClip, rotatingVolume);
        }

        public void ResetStretchingSoundState()
        {
            lastStretchingSoundTime = float.NegativeInfinity;
            lastStretchLimitState = 0;
        }

        public void TryPlayStretchingSound(float scaleDelta)
        {
            if (interactionAudioSource == null || stretchingClip == null)
                return;

            if (Mathf.Abs(scaleDelta) < stretchingSoundScaleStep)
                return;

            if (Time.unscaledTime - lastStretchingSoundTime < stretchingSoundInterval)
                return;

            lastStretchingSoundTime = Time.unscaledTime;
            interactionAudioSource.PlayOneShot(stretchingClip, stretchingVolume);
        }

        public void UpdateStretchLimitSound(bool atMinLimit, bool atMaxLimit)
        {
            int nextState = atMinLimit ? -1 : atMaxLimit ? 1 : 0;
            if (nextState == lastStretchLimitState)
                return;

            lastStretchLimitState = nextState;
            if (interactionAudioSource == null)
                return;

            if (nextState < 0 && stretchMinReachedClip != null)
            {
                interactionAudioSource.PlayOneShot(stretchMinReachedClip, stretchMinReachedVolume);
            }
            else if (nextState > 0 && stretchMaxReachedClip != null)
            {
                interactionAudioSource.PlayOneShot(stretchMaxReachedClip, stretchMaxReachedVolume);
            }
        }

        public Collider2D[] GetSolidColliders()
        {
            return GetComponents<Collider2D>()
                .Where(c => c != null && c.enabled && !c.isTrigger)
                .ToArray();
        }

        public BoxCollider2D[] GetSolidBoxColliders()
        {
            return GetComponents<BoxCollider2D>()
                .Where(c => c != null && c.enabled && !c.isTrigger)
                .ToArray();
        }

        public Collider2D[] GetGrabbableColliders()
        {
            return GetComponents<Collider2D>()
                .Where(c => c != null && c.enabled)
                .ToArray();
        }

        public bool ContainsPointInSolidColliders(Vector2 worldPoint)
        {
            var colliders = GetSolidColliders();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].OverlapPoint(worldPoint))
                    return true;
            }

            return false;
        }

        public bool ContainsPointInGrabbableColliders(Vector2 worldPoint)
        {
            var colliders = GetGrabbableColliders();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].OverlapPoint(worldPoint))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Pushes the Rigidbody out of any colliders it is genuinely penetrating.
        /// Only reacts to penetrations deeper than <see cref="DepenetrationThreshold"/>
        /// to avoid jitter from floating-point contact results.
        /// </summary>
        public void Depenetrate()
        {
            var colliders = GetSolidColliders();
            if (colliders == null || colliders.Length == 0) return;

            Physics2D.SyncTransforms();
            Vector2 pushout = Vector2.zero;
            foreach (var col in colliders)
            {
                if (col == null) continue;

                int count = Physics2D.OverlapCollider(col, GetContactFilter(), _depenetrateBuffer);
                for (int i = 0; i < count; i++)
                {
                    if (_depenetrateBuffer[i] == null ||
                        _depenetrateBuffer[i].gameObject == gameObject) continue;

                    ColliderDistance2D cd = col.Distance(_depenetrateBuffer[i]);

                    // Only push out for meaningful penetration; ignore mere contact (distance ≈ 0)
                    if (cd.distance < DepenetrationThreshold)
                    {
                        // Push exactly by the penetration depth – no additional epsilon
                        pushout += cd.normal * (-cd.distance);
                    }
                }
            }

            if (pushout.sqrMagnitude > 0.0001f)
            {
                Rb.MovePosition(Rb.position + pushout);
                Physics2D.SyncTransforms();
            }
        }

        /// Minimum penetration depth (negative) that triggers a push-out.
        private const float DepenetrationThreshold = -0.01f;

        private static readonly Collider2D[] _depenetrateBuffer = new Collider2D[8];

        public void ChangeState(IState newState)
        {
            if (currentState != null)
            {
                currentState.Exit();
            }
            currentState = newState;
            if (currentState != null)
            {
                currentState.Enter(this);
            }
        }

        public bool TryBeginGrab(Vector2 mousePos)
        {
            if (!ContainsPointInGrabbableColliders(mousePos))
                return false;

            Vector2 localPoint = transform.InverseTransformPoint(mousePos);
            GrabLocalPoint = localPoint;
            GrabWorldPoint = mousePos;

            if (IsInMoveGrabZone(localPoint))
            {
                ChangeState(new MovingState());
            }
            else if (IsInStretchGrabZone(localPoint))
            {
                StretchFromTop = GetTopStretchGrabZoneLocalRect().Contains(localPoint);
                ChangeState(new StretchingState());
            }
            else if (IsInEdgeGrabZone(localPoint))
            {
                ChangeState(new RotatingState());
            }
            else
            {
                if (Mathf.Abs(localPoint.y) >= edgeGrabThreshold)
                    ChangeState(new RotatingState());
                else
                    ChangeState(new MovingState());
            }

            return true;
        }

        public Vector2 GetMouseWorldPos()
        {
            if (Camera.main == null) return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }
            return Vector2.zero;
#else
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
#endif
        }

        public Vector2 GetMouseScreenPos()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        public bool IsMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public bool IsMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        public bool ShouldReleaseDrag(Vector2 mouseWorldPos)
        {
            if (dragReleaseZoneSize.x > 0f && dragReleaseZoneSize.y > 0f)
            {
                Vector2 localMouse = transform.InverseTransformPoint(mouseWorldPos);
                Vector2 localDelta = localMouse - moveGrabZoneOffset - dragReleaseZoneOffset;
                Vector2 halfSize = dragReleaseZoneSize * 0.5f;
                return Mathf.Abs(localDelta.x) > halfSize.x || Mathf.Abs(localDelta.y) > halfSize.y;
            }

            return maxDragDistanceFromCenter > 0f &&
                   Vector2.Distance(Rb.position, mouseWorldPos) > maxDragDistanceFromCenter;
        }

        public bool IsInMoveGrabZone(Vector2 localPoint)
        {
            Rect moveZone = GetMoveGrabZoneLocalRect();
            return moveZone.Contains(localPoint);
        }

        public bool IsInEdgeGrabZone(Vector2 localPoint)
        {
            return GetTopEdgeGrabZoneLocalRect().Contains(localPoint) ||
                   GetBottomEdgeGrabZoneLocalRect().Contains(localPoint);
        }

        public bool IsInStretchGrabZone(Vector2 localPoint)
        {
            return GetTopStretchGrabZoneLocalRect().Contains(localPoint) ||
                   GetBottomStretchGrabZoneLocalRect().Contains(localPoint);
        }

        public Rect GetMoveGrabZoneLocalRect()
        {
            Vector2 size = moveGrabZoneSize;
            if (size.x <= 0f || size.y <= 0f)
            {
                Vector2 colliderSize = GetApproxLocalColliderSize();
                size = new Vector2(
                    size.x > 0f ? size.x : colliderSize.x,
                    size.y > 0f ? size.y : edgeGrabThreshold * 2f);
            }

            Vector2 min = moveGrabZoneOffset - size * 0.5f;
            return new Rect(min, size);
        }

        public Rect GetTopEdgeGrabZoneLocalRect()
        {
            Rect colliderRect = GetApproxLocalColliderRect();
            Vector2 size = GetEdgeGrabZoneSize(colliderRect.size);
            float topCenterY = colliderRect.center.y + edgeGrabThreshold + size.y * 0.5f;
            Vector2 center = new Vector2(colliderRect.center.x, topCenterY) + topEdgeGrabZoneOffset;
            return new Rect(center - size * 0.5f, size);
        }

        public Rect GetBottomEdgeGrabZoneLocalRect()
        {
            Rect colliderRect = GetApproxLocalColliderRect();
            Vector2 size = GetEdgeGrabZoneSize(colliderRect.size);
            float bottomCenterY = colliderRect.center.y - edgeGrabThreshold - size.y * 0.5f;
            Vector2 center = new Vector2(colliderRect.center.x, bottomCenterY) + bottomEdgeGrabZoneOffset;
            return new Rect(center - size * 0.5f, size);
        }

        public Rect GetTopStretchGrabZoneLocalRect()
        {
            if (stretchGrabZoneSize.x <= 0f || stretchGrabZoneSize.y <= 0f)
                return new Rect(Vector2.zero, Vector2.zero);

            Rect colliderRect = GetApproxLocalColliderRect();
            float topCenterY = colliderRect.yMax - stretchGrabZoneSize.y * 0.5f;
            Vector2 center = new Vector2(colliderRect.center.x, topCenterY) + topStretchGrabZoneOffset;
            return new Rect(center - stretchGrabZoneSize * 0.5f, stretchGrabZoneSize);
        }

        public Rect GetBottomStretchGrabZoneLocalRect()
        {
            if (stretchGrabZoneSize.x <= 0f || stretchGrabZoneSize.y <= 0f)
                return new Rect(Vector2.zero, Vector2.zero);

            Rect colliderRect = GetApproxLocalColliderRect();
            float bottomCenterY = colliderRect.yMin + stretchGrabZoneSize.y * 0.5f;
            Vector2 center = new Vector2(colliderRect.center.x, bottomCenterY) + bottomStretchGrabZoneOffset;
            return new Rect(center - stretchGrabZoneSize * 0.5f, stretchGrabZoneSize);
        }

        private Vector2 GetEdgeGrabZoneSize(Vector2 colliderSize)
        {
            float remainingHeight = Mathf.Max(0f, colliderSize.y * 0.5f - edgeGrabThreshold);
            return new Vector2(
                edgeGrabZoneSize.x > 0f ? edgeGrabZoneSize.x : colliderSize.x,
                edgeGrabZoneSize.y > 0f ? edgeGrabZoneSize.y : remainingHeight);
        }

        public Vector2 GetApproxLocalColliderSize()
        {
            return GetApproxLocalColliderRect().size;
        }

        public Rect GetApproxLocalColliderRect()
        {
            var boxCols = GetSolidBoxColliders();
            if (boxCols != null && boxCols.Length > 0)
            {
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                foreach (var boxCol in boxCols)
                {
                    if (boxCol == null) continue;
                    Vector2 half = boxCol.size * 0.5f;
                    min = Vector2.Min(min, boxCol.offset - half);
                    max = Vector2.Max(max, boxCol.offset + half);
                }

                if (min.x != float.MaxValue && max.x != float.MinValue)
                    return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            var anyCols = GetSolidColliders();
            if (anyCols != null && anyCols.Length > 0)
            {
                Vector3 ls = transform.lossyScale;
                Bounds bounds = anyCols[0].bounds;
                for (int i = 1; i < anyCols.Length; i++)
                {
                    if (anyCols[i] != null)
                        bounds.Encapsulate(anyCols[i].bounds);
                }
                Vector2 center = transform.InverseTransformPoint(bounds.center);
                Vector2 size = new Vector2(
                    ls.x > 0.001f ? bounds.size.x / Mathf.Abs(ls.x) : 1f,
                    ls.y > 0.001f ? bounds.size.y / Mathf.Abs(ls.y) : edgeGrabThreshold * 2f);
                return new Rect(center - size * 0.5f, size);
            }

            return new Rect(new Vector2(-0.5f, -edgeGrabThreshold), new Vector2(1f, edgeGrabThreshold * 2f));
        }

        private void UpdateInteractionCursor()
        {
            if (!enableInteractionCursor && !enableHeldCursor)
            {
                ApplyCursor(InteractionCursorKind.None);
                return;
            }

            if (!inputEnabled)
            {
                ApplyCursor(InteractionCursorKind.None);
                return;
            }

            if (enableHeldCursor && IsMouseHeld())
            {
                ApplyCursor(InteractionCursorKind.Held);
                return;
            }

            if (currentState is MovingState)
            {
                ApplyCursor(InteractionCursorKind.Move);
                return;
            }

            if (currentState is RotatingState)
            {
                ApplyCursor(InteractionCursorKind.Rotate);
                return;
            }

            if (currentState is StretchingState)
            {
                ApplyCursor(InteractionCursorKind.Stretch);
                return;
            }

            Vector2 mouseWorldPos = GetMouseWorldPos();
            if (!ContainsPointInGrabbableColliders(mouseWorldPos))
            {
                ApplyCursor(InteractionCursorKind.None);
                return;
            }

            Vector2 localPoint = transform.InverseTransformPoint(mouseWorldPos);
            if (IsInMoveGrabZone(localPoint))
            {
                ApplyCursor(InteractionCursorKind.Move);
            }
            else if (IsInStretchGrabZone(localPoint))
            {
                ApplyCursor(InteractionCursorKind.Stretch);
            }
            else if (IsInEdgeGrabZone(localPoint) || Mathf.Abs(localPoint.y) >= edgeGrabThreshold)
            {
                ApplyCursor(InteractionCursorKind.Rotate);
            }
            else
            {
                ApplyCursor(InteractionCursorKind.Move);
            }
        }

        private void ApplyCursor(InteractionCursorKind cursorKind, bool force = false)
        {
            if (!force && appliedCursorKind == cursorKind)
                return;

            appliedCursorKind = cursorKind;

            Texture2D texture = defaultCursorTexture;
            Vector2 hotspot = defaultCursorHotspot;
            switch (cursorKind)
            {
                case InteractionCursorKind.Move:
                    texture = moveCursorTexture != null ? moveCursorTexture : defaultCursorTexture;
                    hotspot = moveCursorTexture != null ? moveCursorHotspot : defaultCursorHotspot;
                    break;
                case InteractionCursorKind.Held:
                    texture = heldCursorTexture != null ? heldCursorTexture : defaultCursorTexture;
                    hotspot = heldCursorTexture != null ? heldCursorHotspot : defaultCursorHotspot;
                    break;
                case InteractionCursorKind.Rotate:
                    texture = rotateCursorTexture != null ? rotateCursorTexture : defaultCursorTexture;
                    hotspot = rotateCursorTexture != null ? rotateCursorHotspot : defaultCursorHotspot;
                    break;
                case InteractionCursorKind.Stretch:
                    texture = stretchCursorTexture != null ? stretchCursorTexture : defaultCursorTexture;
                    hotspot = stretchCursorTexture != null ? stretchCursorHotspot : defaultCursorHotspot;
                    break;
            }

            bool shouldUseOverlay = useOverlayCursor && texture != null;
            if (shouldUseOverlay)
            {
                EnsureCursorOverlay();
                if (cursorOverlayImage != null)
                {
                    cursorOverlayImage.texture = texture;
                    cursorOverlayImage.rectTransform.sizeDelta = overlayCursorSize;
                    cursorOverlayImage.SetNativeSize();
                    Vector2 nativeSize = cursorOverlayImage.rectTransform.sizeDelta;
                    if (overlayCursorSize.x > 0f && overlayCursorSize.y > 0f)
                    {
                        nativeSize = overlayCursorSize;
                    }
                    cursorOverlayImage.rectTransform.sizeDelta = nativeSize;
                    SetOverlayCursorPosition(GetMouseScreenPos(), hotspot, nativeSize.y);
                    SetOverlayCursorVisible(true);
                    Cursor.SetCursor(null, Vector2.zero, cursorMode);
                    Cursor.visible = false;
                    isUsingOverlayCursor = true;
                    return;
                }
            }

            SetOverlayCursorVisible(false);
            if (isUsingOverlayCursor)
            {
                Cursor.visible = true;
                isUsingOverlayCursor = false;
            }

            Cursor.SetCursor(texture, hotspot, cursorMode);
            Cursor.visible = true;
        }

        private void EnsureCursorOverlay()
        {
            if (cursorOverlayCanvas != null)
                return;

            GameObject canvasObject = new GameObject($"{name}_CursorOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cursorOverlayCanvas = canvasObject.GetComponent<Canvas>();
            cursorOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cursorOverlayCanvas.sortingOrder = overlayCursorSortingOrder;
            cursorOverlayCanvas.overrideSorting = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            GameObject imageObject = new GameObject("Cursor", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(canvasObject.transform, false);
            cursorOverlayRect = imageObject.GetComponent<RectTransform>();
            cursorOverlayRect.anchorMin = Vector2.zero;
            cursorOverlayRect.anchorMax = Vector2.zero;
            cursorOverlayRect.pivot = Vector2.zero;
            cursorOverlayImage = imageObject.GetComponent<RawImage>();
            cursorOverlayImage.raycastTarget = false;
            SetOverlayCursorVisible(false);
        }

        private void UpdateOverlayCursorPosition()
        {
            if (!isUsingOverlayCursor || cursorOverlayImage == null || !cursorOverlayImage.enabled)
                return;

            SetOverlayCursorPosition(GetMouseScreenPos(), GetActiveCursorHotspot(), cursorOverlayImage.rectTransform.sizeDelta.y);
        }

        private Vector2 GetActiveCursorHotspot()
        {
            switch (appliedCursorKind)
            {
                case InteractionCursorKind.Held:
                    return heldCursorTexture != null ? heldCursorHotspot : defaultCursorHotspot;
                case InteractionCursorKind.Move:
                    return moveCursorTexture != null ? moveCursorHotspot : defaultCursorHotspot;
                case InteractionCursorKind.Rotate:
                    return rotateCursorTexture != null ? rotateCursorHotspot : defaultCursorHotspot;
                case InteractionCursorKind.Stretch:
                    return stretchCursorTexture != null ? stretchCursorHotspot : defaultCursorHotspot;
                default:
                    return defaultCursorHotspot;
            }
        }

        private void SetOverlayCursorPosition(Vector2 screenPos, Vector2 hotspot, float visualHeight)
        {
            if (cursorOverlayRect == null)
                return;

            float convertedHotspotY = Mathf.Max(0f, visualHeight - hotspot.y);
            cursorOverlayRect.anchoredPosition = new Vector2(
                screenPos.x - hotspot.x,
                screenPos.y - convertedHotspotY);
        }

        private void SetOverlayCursorVisible(bool visible)
        {
            if (cursorOverlayImage != null)
            {
                cursorOverlayImage.enabled = visible;
            }
        }

        private enum InteractionCursorKind
        {
            None,
            Held,
            Move,
            Rotate,
            Stretch,
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws grab-zone indicators when the object is selected in the Editor.
        ///  GREEN (centre strip) = move zone  → MovingState
        ///  RED   (top &amp; bottom) = edge zone  → RotatingState
        ///  YELLOW lines        = edgeGrabThreshold boundary
        /// Width is derived from BoxCollider2D.size if present, otherwise defaults to 0.5.
        /// All positions are converted to world space via TransformPoint so scale/rotation
        /// are handled correctly without double-applying through Gizmos.matrix.
        /// </summary>
        void OnDrawGizmosSelected()
        {
            // Prefer BoxCollider2D for precise local size; fall back to bounds as last resort
            float localHalfW = 0.5f;
            float localHalfH = edgeGrabThreshold + 0.5f;

            var boxCols = GetSolidBoxColliders();
            if (boxCols.Length > 0)
            {
                Vector2 colliderSize = GetApproxLocalColliderSize();
                localHalfW = colliderSize.x * 0.5f;
                localHalfH = colliderSize.y * 0.5f;
            }
            else
            {
                var anyCols = GetSolidColliders();
                if (anyCols != null && anyCols.Length > 0)
                {
                    // bounds is world-space; divide by lossy scale to approximate local size
                    Vector3 ls = transform.lossyScale;
                    Bounds bounds = anyCols[0].bounds;
                    for (int i = 1; i < anyCols.Length; i++)
                    {
                        if (anyCols[i] != null)
                            bounds.Encapsulate(anyCols[i].bounds);
                    }
                    localHalfW = ls.x > 0.001f ? bounds.extents.x / Mathf.Abs(ls.x) : 0.5f;
                    localHalfH = ls.y > 0.001f ? bounds.extents.y / Mathf.Abs(ls.y) : edgeGrabThreshold + 0.5f;
                }
            }

            // Helper: convert a local 2D point to world space
            Vector3 W(float lx, float ly) => transform.TransformPoint(new Vector3(lx, ly, 0f));

            Rect moveZone = GetMoveGrabZoneLocalRect();
            float moveMinX = moveZone.xMin;
            float moveMaxX = moveZone.xMax;
            float moveMinY = moveZone.yMin;
            float moveMaxY = moveZone.yMax;
            Rect topEdgeZone = GetTopEdgeGrabZoneLocalRect();
            Rect bottomEdgeZone = GetBottomEdgeGrabZoneLocalRect();
            Rect topStretchZone = GetTopStretchGrabZoneLocalRect();
            Rect bottomStretchZone = GetBottomStretchGrabZoneLocalRect();

            // ── Centre / move zone (green) ──────────────────────────
            DrawGizmoQuad(
                W(moveMinX, moveMinY), W(moveMaxX, moveMinY),
                W(moveMaxX, moveMaxY), W(moveMinX, moveMaxY),
                new Color(0.15f, 0.9f, 0.3f, 0.30f),
                new Color(0.15f, 0.9f, 0.3f, 0.90f));

            // ── Top edge / rotate zone (red) ────────────────────────
            DrawGizmoQuad(
                W(topEdgeZone.xMin, topEdgeZone.yMin), W(topEdgeZone.xMax, topEdgeZone.yMin),
                W(topEdgeZone.xMax, topEdgeZone.yMax), W(topEdgeZone.xMin, topEdgeZone.yMax),
                new Color(0.95f, 0.2f, 0.2f, 0.30f),
                new Color(0.95f, 0.2f, 0.2f, 0.90f));

            // ── Bottom edge / rotate zone (red) ─────────────────────
            DrawGizmoQuad(
                W(bottomEdgeZone.xMin, bottomEdgeZone.yMin), W(bottomEdgeZone.xMax, bottomEdgeZone.yMin),
                W(bottomEdgeZone.xMax, bottomEdgeZone.yMax), W(bottomEdgeZone.xMin, bottomEdgeZone.yMax),
                new Color(0.95f, 0.2f, 0.2f, 0.30f),
                new Color(0.95f, 0.2f, 0.2f, 0.90f));

            if (stretchGrabZoneSize.x > 0f && stretchGrabZoneSize.y > 0f)
            {
                DrawGizmoQuad(
                    W(topStretchZone.xMin, topStretchZone.yMin), W(topStretchZone.xMax, topStretchZone.yMin),
                    W(topStretchZone.xMax, topStretchZone.yMax), W(topStretchZone.xMin, topStretchZone.yMax),
                    new Color(0.2f, 0.6f, 1f, 0.20f),
                    new Color(0.2f, 0.6f, 1f, 0.90f));

                DrawGizmoQuad(
                    W(bottomStretchZone.xMin, bottomStretchZone.yMin), W(bottomStretchZone.xMax, bottomStretchZone.yMin),
                    W(bottomStretchZone.xMax, bottomStretchZone.yMax), W(bottomStretchZone.xMin, bottomStretchZone.yMax),
                    new Color(0.2f, 0.6f, 1f, 0.20f),
                    new Color(0.2f, 0.6f, 1f, 0.90f));
            }

            // ── Threshold boundary lines (yellow) ───────────────────
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(W(-localHalfW, edgeGrabThreshold), W(localHalfW, edgeGrabThreshold));
            Gizmos.DrawLine(W(-localHalfW, -edgeGrabThreshold), W(localHalfW, -edgeGrabThreshold));

            if (dragReleaseZoneSize.x > 0f && dragReleaseZoneSize.y > 0f)
            {
                Vector2 dragCenter = moveGrabZoneOffset + dragReleaseZoneOffset;
                Vector2 dragHalf = dragReleaseZoneSize * 0.5f;
                DrawGizmoQuad(
                    W(dragCenter.x - dragHalf.x, dragCenter.y - dragHalf.y),
                    W(dragCenter.x + dragHalf.x, dragCenter.y - dragHalf.y),
                    W(dragCenter.x + dragHalf.x, dragCenter.y + dragHalf.y),
                    W(dragCenter.x - dragHalf.x, dragCenter.y + dragHalf.y),
                    new Color(0.2f, 0.7f, 1f, 0.08f),
                    new Color(0.2f, 0.7f, 1f, 0.9f));
            }
        }

        /// Draws a filled quad then its wire outline using world-space corner points.
        static void DrawGizmoQuad(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl,
                                  Color fill, Color wire)
        {
            // Filled (via two triangles drawn as overlapping thin lines – Gizmos has no DrawQuad,
            // so we approximate fill with a DrawMesh-free approach using a helper)
            UnityEditor.Handles.DrawSolidRectangleWithOutline(
                new Vector3[] { bl, br, tr, tl }, fill, wire);
        }
#endif
    }
}
