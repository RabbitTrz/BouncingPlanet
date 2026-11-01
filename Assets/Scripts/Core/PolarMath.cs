using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 极坐标与世界坐标的唯一转换入口。
    /// 为保持与原型一致的左右手性，渲染用的世界角度取 -θ。
    /// </summary>
    public static class PolarMath
    {
        public static Vector2 Polar(float radius, float worldAngle)
        {
            return new Vector2(
                Mathf.Cos(worldAngle) * radius,
                Mathf.Sin(worldAngle) * radius);
        }

        public static Vector2 PlayerWorld(float radius, float theta)
        {
            return Polar(radius, WorldAngleFromTheta(theta));
        }

        public static float WorldAngleFromTheta(float theta)
        {
            return -theta;
        }

        public static float NormalizeAngle(float angle)
        {
            angle = (angle + Mathf.PI) % (Mathf.PI * 2f);
            if (angle < 0f)
                angle += Mathf.PI * 2f;
            return angle - Mathf.PI;
        }
    }
}
