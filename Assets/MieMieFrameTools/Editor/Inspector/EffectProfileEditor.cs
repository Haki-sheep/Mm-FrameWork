namespace MieMieFrameWork.Editor
{
    using MieMieFrameWork.Effects;
    using MieMieFrameWork.Tools.Inspector;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(EffectProfile))]
    [CanEditMultipleObjects]
    internal sealed class EffectProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var Root = InspectorControls.CreateRoot();
            InspectorControls.AddProperty(Root, serializedObject, "m_Script").SetEnabled(false);
            using var Property = serializedObject.FindProperty("config");
            using var End = Property.GetEndProperty();
            bool EnterChildren = true;
            while (Property.NextVisible(EnterChildren) && !SerializedProperty.EqualContents(Property, End))
            {
                Root.Add(new PropertyField(Property));
                EnterChildren = false;
            }

            return Root;
        }
    }
}
