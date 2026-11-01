using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 标记一个字段可在运行时调参面板里用滑条编辑，并给 Inspector 提供中文标签。
    /// 仅用于 public 字段；所有数值型参数都应带上这个标记。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TunableAttribute : PropertyAttribute
    {
        public string Group { get; }
        public string Label { get; }
        public float Min { get; }
        public float Max { get; }
        public float Step { get; }

        public TunableAttribute(string group, float min, float max, float step = 0.01f, string label = null)
        {
            Group = group;
            Min = min;
            Max = max;
            Step = step;
            Label = label;
        }
    }
}
