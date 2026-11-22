namespace MieMieFrameWork.Tools.Inspector
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public static class InspectorControls
    {
        private const string StylePath = "Assets/MieMieFrameTools/Editor/Inspector/Inspector.uss";

        internal static VisualElement CreateRoot()
        {
            var Root = new VisualElement();
            Root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath));
            return Root;
        }

        internal static Foldout AddFoldout(VisualElement parent, string name, string title, bool open = false)
        {
            var Group = new Foldout { text = title, viewDataKey = name, value = open };
            Group.AddToClassList("mm-inspector-section");
            parent.Add(Group);
            return Group;
        }

        internal static TextField AddReadOnlyText(VisualElement parent, string name, string label)
        {
            var Field = new TextField(label) { name = name, isReadOnly = true };
            Field.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            Field.AddToClassList("mm-inspector-readonly");
            parent.Add(Field);
            return Field;
        }

        internal static PropertyField AddProperty(VisualElement parent, SerializedObject source, string name, string label = null)
        {
            var Property = source.FindProperty(name)
                ?? throw new InvalidOperationException($"{source.targetObject.GetType().Name}.{name} 未序列化");
            var Field = new PropertyField(Property, label) { name = name };
            parent.Add(Field);

            var Info = source.targetObject.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var Rule = Info?.GetCustomAttribute<InspectorShowIfAttribute>();
            if (Rule != null)
            {
                var Condition = source.FindProperty(Rule.ConditionField)
                    ?? throw new InvalidOperationException($"{name} 的显隐字段 {Rule.ConditionField} 未序列化");
                var eExpectedType = Rule.EnumValue.HasValue ? SerializedPropertyType.Enum : SerializedPropertyType.ObjectReference;
                if (Condition.propertyType != eExpectedType)
                    throw new InvalidOperationException($"{name} 的显隐字段 {Rule.ConditionField} 必须为 {eExpectedType}");
                UpdateVisibility(Condition);
                Field.TrackPropertyValue(Condition, UpdateVisibility);

                void UpdateVisibility(SerializedProperty property)
                {
                    bool Visible = !property.hasMultipleDifferentValues && (Rule.EnumValue is int Expected
                        ? property.intValue == Expected
                        : property.objectReferenceValue == null && property.objectReferenceInstanceIDValue == 0);
                    Field.style.display = Visible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            return Field;
        }

        public static VisualElement CreateGroupedInspector(SerializedObject source)
        {
            var Root = CreateRoot();
            var GroupDict = new Dictionary<string, Foldout>();
            using var Property = source.GetIterator();
            bool EnterChildren = true;
            while (Property.NextVisible(EnterChildren))
            {
                EnterChildren = false;
                var Field = source.targetObject.GetType().GetField(Property.name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var Group = Field?.GetCustomAttribute<InspectorFoldoutAttribute>();
                VisualElement Parent = Root;
                if (Group != null)
                {
                    if (!GroupDict.TryGetValue(Group.Title, out var Foldout))
                    {
                        Foldout = AddFoldout(Root, Group.Title, Group.Title, GroupDict.Count == 0);
                        GroupDict.Add(Group.Title, Foldout);
                    }

                    Parent = Foldout;
                }

                AddProperty(Parent, source, Property.name).SetEnabled(Property.name != "m_Script");
            }

            return Root;
        }
    }
}
