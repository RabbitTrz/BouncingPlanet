using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 玩家在极坐标中的运行时状态。世界坐标不落盘，由 (r, θ) 推导。
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        public float radius;
        public float theta;
        public float radialVelocity;
        public float angularVelocity;
        public float maxRadius;
        public bool dead;
        public int deaths;

        public Vector2 WorldPosition => PolarMath.PlayerWorld(radius, theta);
        public float WorldAngle => PolarMath.WorldAngleFromTheta(theta);

        public void Reset(GameConfig config)
        {
            radius = config != null ? config.startRadius : 1.5f;
            theta = 0f;
            radialVelocity = config != null ? config.startRadialVelocity : 0.6f;
            angularVelocity = 0f;
            maxRadius = radius;
            dead = false;
        }

        public void ResetRun(GameConfig config)
        {
            deaths = 0;
            Reset(config);
        }
    }
}
