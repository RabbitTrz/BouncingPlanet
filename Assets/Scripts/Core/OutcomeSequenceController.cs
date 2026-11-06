using System.Collections;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 死亡/胜利结算前的表现：
    /// 死亡被吸入黑洞，胜利以无双状态飞出屏幕，然后黑场进入结算。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OutcomeSequenceController : MonoBehaviour
    {
        public static OutcomeSequenceController Instance { get; private set; }

        public GameBootstrap bootstrap;
        private GameFlow flow;
        private PlayerController player;
        private Stage1WorldView worldView;
        private ProceduralAudioService audioService;
        private Coroutine routine;

        private void Awake()
        {
            Instance = this;
            bootstrap = GameBootstrap.Instance;
        }

        public void Initialize(GameBootstrap gameBootstrap)
        {
            Instance = this;
            bootstrap = gameBootstrap != null
                ? gameBootstrap
                : GameBootstrap.Instance;
            if (bootstrap == null)
                return;
            flow = bootstrap.gameFlow;
            player = bootstrap.player;
            worldView = bootstrap.worldView;
            audioService = bootstrap.audioService;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PlayDeath()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(DeathRoutine());
        }

        public void PlayWin()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(WinRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            if (!Prepare(out Transform root, out Vector3 startPosition,
                    out Quaternion startRotation, out Vector3 startScale, false))
            {
                yield break;
            }

            audioService?.PlayOutcomeDeath();
            Vector3 target = worldView != null && worldView.BlackHole != null
                ? worldView.BlackHole.transform.position
                : Vector3.zero;
            const float duration = 0.72f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                root.position = Vector3.Lerp(startPosition, target, eased);
                root.localScale = startScale * Mathf.Lerp(1f, 0.02f, eased);
                root.localRotation = startRotation *
                    Quaternion.Euler(0f, 0f, 720f * eased);
                yield return null;
            }

            if (root != null)
                root.gameObject.SetActive(false);
            routine = null;
            Finish(false);
        }

        private IEnumerator WinRoutine()
        {
            if (!Prepare(out Transform root, out Vector3 startPosition,
                    out Quaternion startRotation, out Vector3 startScale, true))
            {
                yield break;
            }

            audioService?.PlayOutcomeWin();
            Vector2 direction = player != null
                ? player.State.WorldPosition.normalized
                : Vector2.up;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;
            Vector3 target = startPosition +
                new Vector3(direction.x, direction.y, 0f) * 14f;

            const float duration = 0.78f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                root.position = Vector3.Lerp(startPosition, target, eased);
                root.localScale = startScale;
                root.localRotation = startRotation;
                yield return null;
            }

            routine = null;
            Finish(true);
        }

        private bool Prepare(out Transform root, out Vector3 startPosition,
            out Quaternion startRotation, out Vector3 startScale, bool victory)
        {
            root = null;
            startPosition = Vector3.zero;
            startRotation = Quaternion.identity;
            startScale = Vector3.one;

            if (worldView == null || flow == null)
            {
                flow?.CompleteOutcome(victory);
                return false;
            }

            root = worldView.OutcomePlayerRoot;
            if (root == null)
            {
                flow.CompleteOutcome(victory);
                return false;
            }

            worldView.BeginOutcomeVisual(victory);
            startPosition = root.position;
            startRotation = root.localRotation;
            startScale = root.localScale;
            return true;
        }

        private void Finish(bool victory)
        {
            if (flow == null)
                return;

            if (UiTransition.Instance != null)
                UiTransition.Instance.Play(() => flow.CompleteOutcome(victory));
            else
                flow.CompleteOutcome(victory);
        }
    }
}
