using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using IGame.IEntity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IGame.Core
{
    public class PathReplaySceneReloader : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Rigidbody2D targetRigidbody;
        [SerializeField] private IController targetController;

        [Header("Recording")]
        [Min(0.01f)]
        [SerializeField] private float sampleInterval = 0.05f;
        [Min(0f)]
        [SerializeField] private float positionThreshold = 0.02f;
        [Min(0f)]
        [SerializeField] private float rotationThreshold = 1f;
        [Min(0f)]
        [SerializeField] private float scaleThreshold = 0.02f;

        [Header("Rewind")]
        [Min(0.01f)]
        [SerializeField] private float rewindMoveSpeed = 12f;
        [Min(1f)]
        [SerializeField] private float rewindRotateSpeed = 540f;
        [Min(0.01f)]
        [SerializeField] private float rewindScaleSpeed = 8f;
        [Min(0.01f)]
        [SerializeField] private float minimumSegmentDuration = 0.02f;
        [SerializeField] private Ease rewindEase = Ease.Linear;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool reloadSceneAfterRewind = true;

        private readonly List<RecordedPose> recordedPath = new List<RecordedPose>();
        private Tween activeTween;
        private float lastRecordTime = float.NegativeInfinity;
        private bool isRewinding;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            CapturePose(force: true);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ResolveReferences();
            }
        }

        private void FixedUpdate()
        {
            if (isRewinding)
                return;

            CapturePose(force: false);
        }

        private void OnDisable()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        public void RewindThenReload()
        {
            _ = RewindThenReloadAsync();
        }

        public async UniTask RewindThenReloadAsync()
        {
            if (isRewinding)
                return;

            ResolveReferences();
            CapturePose(force: true);

            if (recordedPath.Count <= 1)
            {
                ReloadActiveScene();
                return;
            }

            isRewinding = true;
            CancellationToken destroyToken = this.GetCancellationTokenOnDestroy();
            PhysicsState physicsState = CapturePhysicsState();
            bool reloadTriggered = false;

            try
            {
                PrepareForRewind();

                for (int i = recordedPath.Count - 1; i > 0; i--)
                {
                    RecordedPose from = recordedPath[i];
                    RecordedPose to = recordedPath[i - 1];
                    float duration = GetSegmentDuration(from, to);
                    await PlaySegmentAsync(to, duration, destroyToken);
                }

                ApplyPose(recordedPath[0]);

                if (reloadSceneAfterRewind)
                {
                    reloadTriggered = true;
                    ReloadActiveScene();
                }
            }
            finally
            {
                activeTween = null;

                if (!reloadTriggered)
                {
                    RestoreAfterRewind(physicsState);
                    isRewinding = false;
                }
            }
        }

        public void RestartRecordingFromCurrentPose()
        {
            ResolveReferences();
            recordedPath.Clear();
            lastRecordTime = float.NegativeInfinity;
            CapturePose(force: true);
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                target = transform;
            }

            if (targetRigidbody == null && target != null)
            {
                targetRigidbody = target.GetComponent<Rigidbody2D>();
            }

            if (targetController == null && target != null)
            {
                targetController = target.GetComponent<IController>();
            }
        }

        private void CapturePose(bool force)
        {
            if (target == null)
                return;

            float now = useUnscaledTime ? Time.unscaledTime : Time.time;
            if (!force && now - lastRecordTime < sampleInterval)
                return;

            RecordedPose pose = new RecordedPose(target.position, target.rotation, target.localScale);
            if (!force && recordedPath.Count > 0 && !HasMeaningfulDifference(recordedPath[recordedPath.Count - 1], pose))
                return;

            recordedPath.Add(pose);
            lastRecordTime = now;
        }

        private bool HasMeaningfulDifference(RecordedPose previous, RecordedPose current)
        {
            return Vector2.Distance(previous.Position, current.Position) >= positionThreshold ||
                   Quaternion.Angle(previous.Rotation, current.Rotation) >= rotationThreshold ||
                   Vector3.Distance(previous.LocalScale, current.LocalScale) >= scaleThreshold;
        }

        private float GetSegmentDuration(RecordedPose from, RecordedPose to)
        {
            float moveDuration = Vector2.Distance(from.Position, to.Position) / rewindMoveSpeed;
            float rotateDuration = Quaternion.Angle(from.Rotation, to.Rotation) / rewindRotateSpeed;
            float scaleDuration = Vector3.Distance(from.LocalScale, to.LocalScale) / rewindScaleSpeed;
            return Mathf.Max(minimumSegmentDuration, moveDuration, rotateDuration, scaleDuration);
        }

        private async UniTask PlaySegmentAsync(RecordedPose targetPose, float duration, CancellationToken cancellationToken)
        {
            activeTween?.Kill();

            Sequence sequence = DOTween.Sequence().SetEase(rewindEase).SetUpdate(useUnscaledTime);
            sequence.Join(target.DOMove(targetPose.Position, duration));
            sequence.Join(target.DORotateQuaternion(targetPose.Rotation, duration));
            sequence.Join(target.DOScale(targetPose.LocalScale, duration));
            activeTween = sequence;

            while (sequence.IsActive() && sequence.IsPlaying())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private void ApplyPose(RecordedPose pose)
        {
            target.SetPositionAndRotation(pose.Position, pose.Rotation);
            target.localScale = pose.LocalScale;
        }

        private PhysicsState CapturePhysicsState()
        {
            if (targetRigidbody == null)
                return default;

            return new PhysicsState(
                targetRigidbody.bodyType,
                targetRigidbody.simulated,
                targetRigidbody.linearVelocity,
                targetRigidbody.angularVelocity,
                targetRigidbody.gravityScale);
        }

        private void PrepareForRewind()
        {
            if (targetController != null)
            {
                targetController.SetInputEnabled(false);
            }

            if (targetRigidbody == null)
                return;

            targetRigidbody.linearVelocity = Vector2.zero;
            targetRigidbody.angularVelocity = 0f;
            targetRigidbody.bodyType = RigidbodyType2D.Kinematic;
            targetRigidbody.simulated = false;
        }

        private void RestoreAfterRewind(PhysicsState physicsState)
        {
            if (targetRigidbody != null)
            {
                targetRigidbody.simulated = physicsState.Simulated;
                targetRigidbody.bodyType = physicsState.BodyType;
                targetRigidbody.linearVelocity = physicsState.LinearVelocity;
                targetRigidbody.angularVelocity = physicsState.AngularVelocity;
                targetRigidbody.gravityScale = physicsState.GravityScale;
            }

            if (targetController != null)
            {
                targetController.SetInputEnabled(true);
            }
        }

        private void ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private readonly struct RecordedPose
        {
            public RecordedPose(Vector3 position, Quaternion rotation, Vector3 localScale)
            {
                Position = position;
                Rotation = rotation;
                LocalScale = localScale;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct PhysicsState
        {
            public PhysicsState(
                RigidbodyType2D bodyType,
                bool simulated,
                Vector2 linearVelocity,
                float angularVelocity,
                float gravityScale)
            {
                BodyType = bodyType;
                Simulated = simulated;
                LinearVelocity = linearVelocity;
                AngularVelocity = angularVelocity;
                GravityScale = gravityScale;
            }

            public RigidbodyType2D BodyType { get; }
            public bool Simulated { get; }
            public Vector2 LinearVelocity { get; }
            public float AngularVelocity { get; }
            public float GravityScale { get; }
        }
    }
}
