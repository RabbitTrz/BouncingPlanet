using UnityEngine;

namespace BouncingPlanet
{
    public enum GameEventType
    {
        Bounce = 0,
        CloudPass = 1,
        IceCrack = 2,
        BombExplode = 3,
        PlanetRemoved = 4,
        ScoreChanged = 5,
        PlayerDeath = 6,
        Debug = 7,
        Wall = 8
    }

    /// <summary>
    /// 仿真层发给表现层的只读事件。不要放 GameObject / 委托，避免耦合与分配。
    /// </summary>
    public readonly struct GameEvent
    {
        public readonly GameEventType type;
        public readonly Vector2 position;
        public readonly float speed;
        public readonly PlanetTypeId planetType;
        public readonly int scoreDelta;
        public readonly bool heavy;

        public GameEvent(
            GameEventType type,
            Vector2 position = default,
            float speed = 0f,
            PlanetTypeId planetType = PlanetTypeId.Rock,
            int scoreDelta = 0,
            bool heavy = false)
        {
            this.type = type;
            this.position = position;
            this.speed = speed;
            this.planetType = planetType;
            this.scoreDelta = scoreDelta;
            this.heavy = heavy;
        }

        public static GameEvent Score(int delta)
        {
            return new GameEvent(GameEventType.ScoreChanged, scoreDelta: delta);
        }

        public static GameEvent Bounce(Vector2 position, float speed, PlanetTypeId planetType)
        {
            return new GameEvent(GameEventType.Bounce, position, speed, planetType);
        }

        public static GameEvent Death(Vector2 position)
        {
            return new GameEvent(GameEventType.PlayerDeath, position);
        }
    }
}
