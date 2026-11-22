namespace MieMieFrameWork.Tools.Inspector
{
    using UnityEngine;

    public sealed class InspectorShowIfAttribute : PropertyAttribute
    {
        public string ConditionField { get; }
        public int? EnumValue { get; }

        public InspectorShowIfAttribute(string objectField)
        {
            ConditionField = objectField;
        }

        public InspectorShowIfAttribute(string enumField, int enumValue)
        {
            ConditionField = enumField;
            EnumValue = enumValue;
        }
    }
}
