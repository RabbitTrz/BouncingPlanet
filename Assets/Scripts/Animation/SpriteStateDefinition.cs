using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 一个静态图片状态。frames 是图片序列；简单状态只放 1 张。
    /// 程序化运动全部由归一化 AnimationCurve 控制，可在 Inspector 里直接调。
    /// </summary>
    [Serializable]
    public sealed class SpriteStateDefinition
    {
        [Header("State")]
        public string stateName = "Idle";
        public Sprite[] frames;
        [Min(0f)] public float fps = 8f;
        public bool loop = true;
        public bool autoReturn = true;
        [Min(0f)] public float durationOverride = 0f;

        [Header("Base Motion")]
        public Vector2 baseOffset;
        public Vector2 baseScale = Vector2.one;
        public float baseRotation;

        [Header("Procedural Motion (normalized 0..1)")]
        public AnimationCurve offsetX = AnimationCurve.Constant(0f, 1f, 0f);
        public AnimationCurve offsetY = AnimationCurve.Constant(0f, 1f, 0f);
        public AnimationCurve scaleX = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve scaleY = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve rotation = AnimationCurve.Constant(0f, 1f, 0f);

        public float StateDuration
        {
            get
            {
                if (durationOverride > 0f)
                    return durationOverride;
                if (frames != null && frames.Length > 0 && fps > 0f)
                    return frames.Length / fps;
                return 1f;
            }
        }

        public bool HasFrames => frames != null && frames.Length > 0;
    }
}
