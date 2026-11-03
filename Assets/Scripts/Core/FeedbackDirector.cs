using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 事件驱动的撞击反馈：顿帧、震屏、镜头前推、压扁、屏闪。
    /// 通过 GameEventBus 接收碰撞事件，不反向修改仿真。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class FeedbackDirector : MonoBehaviour
    {
        public GameBootstrap bootstrap;

        public float Shake { get; private set; }
        public float Punch { get; private set; }
        public float Squash { get; private set; }
        public float FlashAlpha { get; private set; }
        public Color FlashColor { get; private set; } = Color.white;

        private FeedbackConfig feedback;
        private SimulationClock clock;
        private GameEventBus events;
        private ImpactVfxService vfx;
        private ProceduralAudioService audioService;

        public void Initialize(GameBootstrap gameBootstrap)
        {
            bootstrap = gameBootstrap != null ? gameBootstrap : GameBootstrap.Instance;
            if (bootstrap == null)
                return;

            feedback = bootstrap.feedbackConfig;
            clock = bootstrap.clock;
            events = bootstrap.Events;
            vfx = bootstrap.vfx;
            audioService = bootstrap.audioService;

            Shake = 0f;
            Punch = 0f;
            Squash = 0f;
            FlashAlpha = 0f;
            FlashColor = Color.white;
        }

        private void Update()
        {
            if (bootstrap == null)
                return;

            if (events != null)
                events.Flush(HandleEvent);

            float dt = Time.unscaledDeltaTime;
            if (feedback != null)
            {
                Shake = Mathf.MoveTowards(Shake, 0f, feedback.shakeDecay * dt);
                Punch = Mathf.MoveTowards(Punch, 0f, feedback.punchDecay * dt);
                Squash = Mathf.MoveTowards(Squash, 0f, feedback.squashDecay * dt);
                FlashAlpha = Mathf.MoveTowards(FlashAlpha, 0f, feedback.screenFlashDecay * dt);
            }
        }

        private void HandleEvent(GameEvent gameEvent)
        {
            if (feedback == null)
                return;

            switch (gameEvent.type)
            {
                case GameEventType.Bounce:
                    float speed = gameEvent.speed;
                    if (gameEvent.heavy)
                    {
                        clock?.RequestHitstop(feedback.hookHeavyHitstop);
                        Shake = Mathf.Max(Shake, feedback.hookHeavyShake);
                        Punch = Mathf.Max(Punch, feedback.ComputeLandingPunch(speed));
                        Squash = Mathf.Max(Squash, feedback.ComputeLandingSquash(speed));
                        Flash(GetPlanetColor(gameEvent.planetType), 0.08f);
                        vfx?.PlayHookHeavy(gameEvent.position, gameEvent.planetType);
                        audioService?.PlayHookHeavy();
                    }
                    else
                    {
                        clock?.RequestHitstop(feedback.ComputeLandingHitstop(speed));
                        Shake = Mathf.Max(Shake, feedback.ComputeLandingShake(speed));
                        Punch = Mathf.Max(Punch, feedback.ComputeLandingPunch(speed));
                        Squash = Mathf.Max(Squash, feedback.ComputeLandingSquash(speed));
                        Flash(GetPlanetColor(gameEvent.planetType), 0.05f);
                        vfx?.PlayBounce(gameEvent.position, gameEvent.planetType, speed);
                        audioService?.PlayBounce(speed, gameEvent.planetType);
                    }
                    break;

                case GameEventType.Wall:
                    if (gameEvent.heavy)
                    {
                        clock?.RequestHitstop(feedback.hookHeavyHitstop);
                        Shake = Mathf.Max(Shake, feedback.hookHeavyShake);
                        vfx?.PlayHookHeavy(gameEvent.position, gameEvent.planetType);
                        audioService?.PlayHookHeavy();
                    }
                    else
                    {
                        Shake = Mathf.Max(Shake, feedback.ascendBlockShake);
                    }
                    break;

                case GameEventType.CloudPass:
                    Shake = Mathf.Max(Shake, feedback.cloudShake);
                    Flash(GetPlanetColor(gameEvent.planetType), feedback.cloudScreenFlash);
                    vfx?.PlayCloud(gameEvent.position, gameEvent.planetType);
                    audioService?.PlayCloud();
                    break;

                case GameEventType.IceCrack:
                    clock?.RequestHitstop(feedback.iceHitstop);
                    Shake = Mathf.Max(Shake, feedback.iceShake);
                    Squash = Mathf.Max(Squash, 0.75f);
                    Flash(GetPlanetColor(gameEvent.planetType), feedback.iceScreenFlash);
                    vfx?.PlayIce(gameEvent.position, gameEvent.planetType);
                    audioService?.PlayIce();
                    break;

                case GameEventType.BombExplode:
                    clock?.RequestHitstop(feedback.bombHitstop);
                    Shake = Mathf.Max(Shake, feedback.bombShake);
                    Punch = Mathf.Max(Punch, feedback.bombPunch);
                    Squash = Mathf.Max(Squash, feedback.bombSquash);
                    Flash(GetPlanetColor(gameEvent.planetType), feedback.bombScreenFlash);
                    vfx?.PlayBomb(gameEvent.position, gameEvent.planetType);
                    audioService?.PlayBomb();
                    break;

                case GameEventType.Smash:
                    clock?.RequestHitstop(0.03f);
                    Shake = Mathf.Max(Shake, 0.55f);
                    Squash = Mathf.Max(Squash, 0.7f);
                    Flash(GetPlanetColor(gameEvent.planetType), 0.06f);
                    vfx?.PlayBounce(gameEvent.position, gameEvent.planetType, gameEvent.speed);
                    audioService?.PlayBounce(gameEvent.speed, gameEvent.planetType);
                    break;

                case GameEventType.MusouStart:
                    Shake = Mathf.Max(Shake, 0.6f);
                    Flash(new Color(1f, 0.55f, 0.2f, 1f), 0.15f);
                    break;

                case GameEventType.PlayerDeath:
                    Shake = Mathf.Max(Shake, 1f);
                    Flash(new Color(1f, 0.25f, 0.2f, 1f), 0.25f);
                    vfx?.PlayDeath(gameEvent.position);
                    audioService?.PlayDeath();
                    break;
            }
        }

        private Color GetPlanetColor(PlanetTypeId type)
        {
            PlanetTypeDef def = bootstrap != null ? bootstrap.GetPlanetType(type) : null;
            return def != null ? def.color : Color.white;
        }

        private void Flash(Color color, float alpha)
        {
            if (alpha <= FlashAlpha)
                return;
            FlashAlpha = Mathf.Clamp01(alpha);
            FlashColor = color;
        }

        private void OnGUI()
        {
            if (FlashAlpha <= 0f)
                return;

            Color old = GUI.color;
            GUI.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, FlashAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
