#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawer
{
    [CustomPropertyDrawer(typeof(RectOffset))]
    public class RectOffsetDrawer : PropertyDrawer
    {
        static readonly GUIContent[] labels = new GUIContent[] { new GUIContent("L"), new GUIContent("R"), new GUIContent("T"), new GUIContent("B") };
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            EditorGUI.MultiPropertyField(position, labels, property, label);
        }
    }
}
