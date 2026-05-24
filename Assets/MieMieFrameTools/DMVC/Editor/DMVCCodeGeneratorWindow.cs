using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class DMVCCodeGeneratorWindow : EditorWindow
{
    #region 类型定义

    private enum BehaviourInterface
    {
        ILogicBehaviour,
        IDataBehaviour,
        IMessageBehaviour
    }

    #endregion

    #region World模板状态

    private string worldClassName = "NewWorld";
    private string worldNamespace = string.Empty;
    private string worldOutputFolder = "Assets/Scene/DMVC/Test";

    #endregion

    #region 三模块模板状态

    private readonly List<Type> worldTypes = new List<Type>();
    private int selectedWorldIndex;
    private string moduleClassName = "NewDataMgr";
    private string moduleOutputFolder = "Assets/Scene/DMVC/HailWorld/DataMgr";
    private BehaviourInterface selectedInterface = BehaviourInterface.IDataBehaviour;

    #endregion

    #region 菜单入口

    [MenuItem("Tools/DMVC/Code Generator")]
    public static void Open()
    {
        GetWindow<DMVCCodeGeneratorWindow>("DMVC Code Generator");
    }

    #endregion

    #region Unity生命周期

    private void OnEnable()
    {
        RefreshWorldTypes();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("DMVC Code Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawWorldTemplateSection();
        EditorGUILayout.Space(8);
        DrawModuleTemplateSection();
    }

    #endregion

    #region 界面绘制

    private void DrawWorldTemplateSection()
    {
        EditorGUILayout.LabelField("1) World Template", EditorStyles.boldLabel);

        worldClassName = EditorGUILayout.TextField("World Class Name", worldClassName);
        worldNamespace = EditorGUILayout.TextField("Namespace", worldNamespace);
        worldOutputFolder = EditorGUILayout.TextField("Output Folder", worldOutputFolder);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate World Class", GUILayout.Height(28)))
        {
            GenerateWorldTemplate();
        }
    }

    private void DrawModuleTemplateSection()
    {
        EditorGUILayout.LabelField("2) Logic/Data/Message Template", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("命名空间自动跟随已选 World。", MessageType.None);

        if (worldTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到 WorldModule 子类，请先创建 World。", MessageType.Warning);
            if (GUILayout.Button("Refresh Worlds"))
            {
                RefreshWorldTypes();
            }
            return;
        }

        var worldOptions = worldTypes.Select(t => t.FullName).ToArray();
        selectedWorldIndex = EditorGUILayout.Popup("Target World", selectedWorldIndex, worldOptions);
        selectedWorldIndex = Mathf.Clamp(selectedWorldIndex, 0, worldTypes.Count - 1);

        var targetWorldType = worldTypes[selectedWorldIndex];
        var targetNamespace = targetWorldType.Namespace ?? string.Empty;
        EditorGUILayout.LabelField("Namespace (Auto)", string.IsNullOrEmpty(targetNamespace) ? "<Global>" : targetNamespace);

        moduleClassName = EditorGUILayout.TextField("Module Class Name", moduleClassName);
        selectedInterface = (BehaviourInterface)EditorGUILayout.EnumPopup("Interface", selectedInterface);
        moduleOutputFolder = EditorGUILayout.TextField("Output Folder", moduleOutputFolder);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Worlds", GUILayout.Width(160), GUILayout.Height(28)))
        {
            RefreshWorldTypes();
        }
        if (GUILayout.Button("Generate Module Class", GUILayout.Width(160), GUILayout.Height(28)))
        {
            GenerateModuleTemplate(targetNamespace);
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region 文件生成

    private void GenerateWorldTemplate()
    {
        if (string.IsNullOrWhiteSpace(worldClassName))
        {
            EditorUtility.DisplayDialog("DMVC", "World class name is required.", "OK");
            return;
        }

        if (!worldOutputFolder.StartsWith("Assets", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("DMVC", "Output folder must start with Assets.", "OK");
            return;
        }

        var className = worldClassName.Trim();
        var namespaceName = worldNamespace.Trim();
        var content = BuildWorldContent(className, namespaceName);

        var absoluteOutputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, worldOutputFolder);
        Directory.CreateDirectory(absoluteOutputFolder);
        var outputFile = Path.Combine(absoluteOutputFolder, $"{className}.cs");

        if (File.Exists(outputFile) &&
            !EditorUtility.DisplayDialog("DMVC", $"{className}.cs already exists. Overwrite?", "Overwrite", "Cancel"))
        {
            return;
        }

        File.WriteAllText(outputFile, content, new UTF8Encoding(false));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("DMVC", $"Generated: {worldOutputFolder}/{className}.cs", "OK");
    }

    private void GenerateModuleTemplate(string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(moduleClassName))
        {
            EditorUtility.DisplayDialog("DMVC", "Module class name is required.", "OK");
            return;
        }

        if (!moduleOutputFolder.StartsWith("Assets", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("DMVC", "Output folder must start with Assets.", "OK");
            return;
        }

        var className = moduleClassName.Trim();
        var interfaceName = selectedInterface.ToString();
        var content = BuildModuleContent(className, namespaceName, interfaceName);

        var absoluteOutputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, moduleOutputFolder);
        Directory.CreateDirectory(absoluteOutputFolder);
        var outputFile = Path.Combine(absoluteOutputFolder, $"{className}.cs");

        if (File.Exists(outputFile) &&
            !EditorUtility.DisplayDialog("DMVC", $"{className}.cs already exists. Overwrite?", "Overwrite", "Cancel"))
        {
            return;
        }

        File.WriteAllText(outputFile, content, new UTF8Encoding(false));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("DMVC", $"Generated: {moduleOutputFolder}/{className}.cs", "OK");
    }

    #endregion

    #region 代码模板构建

    private static string BuildWorldContent(string generatedClassName, string generatedNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(generatedNamespace))
        {
            sb.AppendLine($"namespace {generatedNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {generatedClassName} : WorldModule");
            sb.AppendLine("    {");
            sb.AppendLine("        public override void OnCreate()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnCreate();");
            sb.AppendLine($"            Debug.Log(\"{generatedClassName} OnCreate\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnDestroy()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnDestroy();");
            sb.AppendLine($"            Debug.Log(\"{generatedClassName} OnDestroy\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnDestroyPostProcess(object args)");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnDestroyPostProcess(args);");
            sb.AppendLine($"            Debug.Log(\"{generatedClassName} OnDestroyPostProcess\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        sb.AppendLine($"public class {generatedClassName} : WorldModule");
        sb.AppendLine("{");
        sb.AppendLine("    public override void OnCreate()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnCreate();");
        sb.AppendLine($"        Debug.Log(\"{generatedClassName} OnCreate\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void OnDestroy()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnDestroy();");
        sb.AppendLine($"        Debug.Log(\"{generatedClassName} OnDestroy\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void OnDestroyPostProcess(object args)");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnDestroyPostProcess(args);");
        sb.AppendLine($"        Debug.Log(\"{generatedClassName} OnDestroyPostProcess\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildModuleContent(string generatedClassName, string generatedNamespace, string interfaceName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(generatedNamespace))
        {
            sb.AppendLine($"namespace {generatedNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {generatedClassName} : {interfaceName}");
            sb.AppendLine("    {");
            sb.AppendLine("        public void OnCreate()");
            sb.AppendLine("        {");
            sb.AppendLine($"            Debug.Log(\"{generatedClassName} OnCreate\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public void OnDestroy()");
            sb.AppendLine("        {");
            sb.AppendLine($"            Debug.Log(\"{generatedClassName} OnDestroy\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        sb.AppendLine($"public class {generatedClassName} : {interfaceName}");
        sb.AppendLine("{");
        sb.AppendLine("    public void OnCreate()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Debug.Log(\"{generatedClassName} OnCreate\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void OnDestroy()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Debug.Log(\"{generatedClassName} OnDestroy\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    #endregion

    #region 辅助方法

    // 扫描当前域中的 WorldModule 子类，用于模块模板命名空间自动跟随。
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
