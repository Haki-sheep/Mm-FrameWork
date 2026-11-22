namespace MieMieFrameWork.Tools.Inspector
{
    using UnityEngine;

    public sealed class InspectorLabelAttribute : PropertyAttribute
    {
        public string Label { get; }

        public InspectorLabelAttribute(string label) : base(true)
        {
            Label = label;
            order = int.MinValue;
        }
    }
}
