using UnityEditor;
using UnityEngine;

namespace BouncingPlanet.EditorTools
{
    /// <summary>
    /// 让带 [Tunable] 的字段在 Inspector 里显示中文标签。
    /// </summary>
    [CustomPropertyDrawer(typeof(TunableAttribute))]
    public sealed class TunableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TunableAttribute tunable = (TunableAttribute)attribute;
            string text = string.IsNullOrEmpty(tunable.Label) ? label.text : tunable.Label;
            EditorGUI.PropertyField(position, property, new GUIContent(text, property.tooltip), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }

    /// <summary>
    /// 让带 [LabelText] 的非数值字段在 Inspector 里显示中文标签。
    /// </summary>
    [CustomPropertyDrawer(typeof(LabelTextAttribute))]
    public sealed class LabelTextDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelTextAttribute labelText = (LabelTextAttribute)attribute;
            string text = string.IsNullOrEmpty(labelText.Label) ? label.text : labelText.Label;
            EditorGUI.PropertyField(position, property, new GUIContent(text, property.tooltip), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }
}
