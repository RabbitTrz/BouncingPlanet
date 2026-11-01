using System;
using UnityEngine;

namespace BouncingPlanet
{
    /// <summary>
    /// 给不需要运行时滑条、但仍要在 Inspector 显示中文名的字段使用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class LabelTextAttribute : PropertyAttribute
    {
        public string Label { get; }

        public LabelTextAttribute(string label)
        {
            Label = label;
        }
    }
}
