using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 一颗行星的运行时数据。Stage 2 用固定测试场创建它，Stage 3 会由生成器创建。
    /// angle 直接是世界角度（不做 θ 镜像），与 PolarMath.Polar 配套。
    /// </summary>
    public sealed class PlanetRuntime
    {
        public PlanetTypeDef def;
        public float radius;
        public float baseAngle;
        public float angle;
        public float worldRadius;
        public bool alive = true;
        public float cooldown;
        public float smashImmunity;
        public float flash;
        public float swayPhase;
        public float swaySpeed;
        public float swayAmplitude;
        public Transform view;
        public PlanetView viewComponent;
        public float deathViewTimer;
        public bool fromBombSkill;
        public bool markerReached;

        public PlanetTypeId Id => def != null ? def.id : PlanetTypeId.Rock;
        public Vector2 WorldPosition => PolarMath.Polar(radius, angle);
    }
}
