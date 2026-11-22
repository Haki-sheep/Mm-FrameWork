namespace MieMieFrameWork.Tools.Inspector
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(InspectorLabelAttribute))]
    internal sealed class InspectorLabelDrawer : PropertyDrawer
    {
        private string Label => ((InspectorLabelAttribute)attribute).Label;

        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new PropertyField(property, Label);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, new GUIContent(Label, label.tooltip), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, new GUIContent(Label, label.tooltip), true);
    }
}
