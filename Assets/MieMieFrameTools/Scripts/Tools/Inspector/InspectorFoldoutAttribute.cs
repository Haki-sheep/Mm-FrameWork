namespace MieMieFrameWork.Tools.Inspector
{
    using UnityEngine;

    public sealed class InspectorFoldoutAttribute : PropertyAttribute
    {
        public string Title { get; }

        public InspectorFoldoutAttribute(string title)
        {
            Title = title;
        }
    }
}
