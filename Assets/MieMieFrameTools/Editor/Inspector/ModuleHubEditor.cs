namespace MieMieFrameWork.Editor
{
    using System;
    using System.Linq;
    using System.Reflection;
    using MieMieFrameWork.Tools.Inspector;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using static MieMieFrameWork.ModuleHub;

    [CustomEditor(typeof(ModuleHub))]
    [CanEditMultipleObjects]
    internal sealed class ModuleHubEditor : UnityEditor.Editor
    {
        private static readonly (EStartupModules Flag, string Label)[] ModuleLabelList = Enum.GetValues(typeof(EStartupModules))
            .Cast<EStartupModules>()
            .Where(Flag => Flag != 0 && ((int)Flag & ((int)Flag - 1)) == 0)
            .Select(Flag => (Flag, typeof(EStartupModules).GetField(Flag.ToString()).GetCustomAttribute<InspectorNameAttribute>().displayName))
            .ToArray();

        public override VisualElement CreateInspectorGUI()
        {
            var Root = InspectorControls.CreateRoot();
            InspectorControls.AddProperty(Root, serializedObject, "m_Script").SetEnabled(false);

            var Startup = InspectorControls.AddFoldout(Root, "startup", "框架启动", true);
            InspectorControls.AddProperty(Startup, serializedObject, "eStartupPreset");
            InspectorControls.AddProperty(Startup, serializedObject, "eSelectedModules");
            var Dependencies = InspectorControls.AddReadOnlyText(Startup, "added-dependencies", "自动补入的依赖");

            var Resources = InspectorControls.AddFoldout(Root, "resources", "资源与热更新", true);
            AddProperties(Resources, "resourcePackageName", "resourceManifestTimeout", "resourceUpdateConfig", "hotUpdateConfig");
            var Modules = InspectorControls.AddFoldout(Root, "modules", "模块配置", true);
            AddProperties(Modules, "archiveSubFolder", "poolManagerConfig", "audioManagerConfig", "effectProfile",
                "effectManagerConfig", "qualityConfig", "uiCoreMgrBehaviour");

            UpdateDependencies(serializedObject);
            Root.TrackSerializedObjectValue(serializedObject, UpdateDependencies);
            return Root;

            void UpdateDependencies(SerializedObject source)
            {
                var Preset = source.FindProperty("eStartupPreset");
                bool Visible = !Preset.hasMultipleDifferentValues && Preset.intValue == (int)EStartupPreset.Custom;
                Dependencies.style.display = Visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!Visible)
                    return;

                var Results = targets.Cast<ModuleHub>().Select(Hub => Hub.AddedDependencies).Distinct().ToArray();
                if (Results.Length != 1)
                {
                    Dependencies.SetValueWithoutNotify("—");
                    return;
                }

                var Mask = Results[0];
                Dependencies.SetValueWithoutNotify(Mask == EStartupModules.None ? "无" :
                    string.Join("、", ModuleLabelList.Where(Item => Mask.HasFlag(Item.Flag)).Select(Item => Item.Label)));
            }
        }

        private void AddProperties(VisualElement parent, params string[] names)
        {
            foreach (string Name in names)
                InspectorControls.AddProperty(parent, serializedObject, Name);
        }
    }
}
