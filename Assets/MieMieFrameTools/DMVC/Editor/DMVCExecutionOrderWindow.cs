using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class DMVCExecutionOrderWindow : EditorWindow
{
    #region 状态字段

    private readonly List<Type> worldTypes = new List<Type>();
    private readonly List<Type> logicTypes = new List<Type>();
    private readonly List<Type> dataTypes = new List<Type>();
    private readonly List<Type> messageTypes = new List<Type>();

    private int selectedWorldIndex;
    private string generatedClassName = string.Empty;
    private string outputFolder = "Assets/Scene/DMVC/Runtime";
    private Vector2 scrollPos;

    #endregion

    #region 菜单入口

    [MenuItem("Tools/DMVC/Execution Order")]
    public static void Open()
    {
        GetWindow<DMVCExecutionOrderWindow>("DMVC Execution Order");
    }

    #endregion

    #region Unity生命周期

    private void OnEnable()
    {
        RefreshWorldTypes();
        SetDefaultClassName();
        ScanBehaviourTypes();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("World Execution Order Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawWorldSelector();

        generatedClassName = EditorGUILayout.TextField("Class Name", generatedClassName);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Worlds"))
        {
            RefreshWorldTypes();
            SetDefaultClassName();
            ScanBehaviourTypes();
        }

        if (GUILayout.Button("Scan Behaviours"))
        {
            ScanBehaviourTypes();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawTypeList("Logic Behaviours", logicTypes);
        EditorGUILayout.Space();
        DrawTypeList("Data Behaviours", dataTypes);
        EditorGUILayout.Space();
        DrawTypeList("Message Behaviours", messageTypes);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Execution Order Script", GUILayout.Height(30)))
        {
            GenerateExecutionOrderScript();
        }
    }

    #endregion

    #region 界面绘制

    private void DrawWorldSelector()
    {
        if (worldTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("No WorldModule subclass found.", MessageType.Warning);
            return;
        }

        var options = worldTypes.Select(t => t.FullName).ToArray();
        var nextIndex = EditorGUILayout.Popup("Target World", selectedWorldIndex, options);
        if (nextIndex != selectedWorldIndex)
        {
            selectedWorldIndex = nextIndex;
            SetDefaultClassName();
            ScanBehaviourTypes();
        }
    }

    private void DrawTypeList(string title, List<Type> types)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (types.Count == 0)
        {
            EditorGUILayout.HelpBox("No type found.", MessageType.Info);
            return;
        }

        for (var i = 0; i < types.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i}. {types[i].Name}");

            GUI.enabled = i > 0;
            if (GUILayout.Button("Up", GUILayout.Width(45)))
            {
                Swap(types, i, i - 1);
            }

            GUI.enabled = i < types.Count - 1;
            if (GUILayout.Button("Down", GUILayout.Width(55)))
            {
                Swap(types, i, i + 1);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }

    private static void Swap(List<Type> types, int a, int b)
    {
        var temp = types[a];
        types[a] = types[b];
        types[b] = temp;
    }

    #endregion

    #region 类型扫描

    private void RefreshWorldTypes()
    {
        worldTypes.Clear();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                if (type == null || type.IsAbstract)
                {
                    continue;
                }

                if (typeof(WorldModule).IsAssignableFrom(type) && type != typeof(WorldModule))
                {
                    worldTypes.Add(type);
                }
            }
        }

        worldTypes.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
        selectedWorldIndex = Mathf.Clamp(selectedWorldIndex, 0, Math.Max(0, worldTypes.Count - 1));
    }

    private void SetDefaultClassName()
    {
        if (worldTypes.Count == 0)
        {
            generatedClassName = "NewWorldScriptExecutionOrder";
            return;
        }

        var worldName = worldTypes[selectedWorldIndex].Name;
        generatedClassName = $"{worldName}ScriptExecutionOrder";
    }

    // 根据已选World命名空间，筛出Logic/Data/Message三类行为类型。
    private void ScanBehaviourTypes()
    {
        logicTypes.Clear();
        dataTypes.Clear();
        messageTypes.Clear();

        if (worldTypes.Count == 0)
        {
            return;
        }

        var targetWorldType = worldTypes[selectedWorldIndex];
        var targetNamespace = targetWorldType.Namespace;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                if (type == null || type.IsAbstract)
                {
                    continue;
                }

                if (type.Namespace != targetNamespace)
                {
                    continue;
                }

                if (typeof(ILogicBehaviour).IsAssignableFrom(type))
                {
                    logicTypes.Add(type);
                }

                if (typeof(IDataBehaviour).IsAssignableFrom(type))
                {
                    dataTypes.Add(type);
                }

                if (typeof(IMessageBehaviour).IsAssignableFrom(type))
                {
                    messageTypes.Add(type);
                }
            }
        }

        logicTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        dataTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        messageTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
    }

    #endregion

    #region 文件生成

    private void GenerateExecutionOrderScript()
    {
        if (worldTypes.Count == 0)
        {
            EditorUtility.DisplayDialog("DMVC", "No world type found.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(generatedClassName))
        {
            EditorUtility.DisplayDialog("DMVC", "Class name is required.", "OK");
            return;
        }

        if (!outputFolder.StartsWith("Assets", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("DMVC", "Output folder must start with Assets.", "OK");
            return;
        }

        var targetWorldType = worldTypes[selectedWorldIndex];
        var targetNamespace = targetWorldType.Namespace;

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        if (!string.IsNullOrEmpty(targetNamespace))
        {
            sb.AppendLine($"using {targetNamespace};");
        }
        sb.AppendLine();
        sb.AppendLine($"public class {generatedClassName} : IBehaviourExecution");
        sb.AppendLine("{");
        sb.AppendLine($"    private static readonly Type[] LogicBehaviourExecutionArr = {BuildTypeArray(logicTypes)}");
        sb.AppendLine();
        sb.AppendLine($"    private static readonly Type[] DataBehaviourExecutionArr = {BuildTypeArray(dataTypes)}");
        sb.AppendLine();
        sb.AppendLine($"    private static readonly Type[] MessageBehaviourExecutionArr = {BuildTypeArray(messageTypes)}");
        sb.AppendLine();
        sb.AppendLine("    public Type[] GetLogicBehaviourExecution()");
        sb.AppendLine("    {");
        sb.AppendLine("        return LogicBehaviourExecutionArr;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public Type[] GetDataBehaviourExecution()");
        sb.AppendLine("    {");
        sb.AppendLine("        return DataBehaviourExecutionArr;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public Type[] GetMessageBehaviourExecution()");
        sb.AppendLine("    {");
        sb.AppendLine("        return MessageBehaviourExecutionArr;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var absoluteOutputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, outputFolder);
        Directory.CreateDirectory(absoluteOutputFolder);
        var outputFile = Path.Combine(absoluteOutputFolder, $"{generatedClassName}.cs");

        if (File.Exists(outputFile) &&
            !EditorUtility.DisplayDialog("DMVC", $"{generatedClassName}.cs already exists. Overwrite?", "Overwrite", "Cancel"))
        {
            return;
        }

        File.WriteAllText(outputFile, sb.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("DMVC", $"Generated: {outputFolder}/{generatedClassName}.cs", "OK");
    }

    private static string BuildTypeArray(List<Type> types)
    {
        if (types.Count == 0)
        {
            return "new Type[]{};";
        }

        var sb = new StringBuilder();
        sb.AppendLine("new Type[]{");
        for (var i = 0; i < types.Count; i++)
        {
            sb.AppendLine($"        typeof({types[i].Name}),");
        }
        sb.Append("    };");
        return sb.ToString();
    }

    #endregion

    #region 辅助方法

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null);
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    #endregion
}
