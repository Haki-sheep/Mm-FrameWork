namespace MieMieUITools.Editor
{
    using MieMieFrameWork.Tools.Inspector;
    using MieMieUIFrameWork.UI.FloatingText;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(FloatingTextWorld))]
    [CanEditMultipleObjects]
    internal sealed class FloatingTextWorldEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() => InspectorControls.CreateGroupedInspector(serializedObject);
    }

    [CustomEditor(typeof(FloatingTextManager))]
    [CanEditMultipleObjects]
    internal sealed class FloatingTextManagerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() => InspectorControls.CreateGroupedInspector(serializedObject);
    }
}
