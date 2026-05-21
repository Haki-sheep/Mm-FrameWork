using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;
using UnityEditor.Callbacks;
using MieMieUITools.Editor;

/// <summary>
/// UI模版生成核心工具类 - 分部类方案
/// 生成 {className}Gen.cs 自动获取组件 + {className}GenPartial.cs 用户扩展 + {className}.cs 用户手写模板
/// </summary>
public class GenerateUITool
{

    private static bool ValidateInputs(string className, GameObject uiPrefab)
    {
        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("错误", "请输入类名", "确定");
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(className, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            EditorUtility.DisplayDialog("错误", "类名格式不正确，只能包含字母、数字和下划线，且不能以数字开头", "确定");
            return false;
        }

        if (uiPrefab == null)
        {
            EditorUtility.DisplayDialog("错误", "请拖入UI预制体", "确定");
            return false;
        }

        return true;
    }


    /// <summary>
    /// 静态方法：生成UI模版脚本
    /// </summary>
    /// <param name="className">类名</param>
    /// <param name="uiPrefab">UI预制体</param>
    /// <param name="folderPath">生成路径</param>
    public static void GenerateUITemplates(string className, GameObject uiPrefab, string folderPath)
    {
        if (!ValidateInputs(className, uiPrefab)) return;

        try
        {
            // 确保文件夹存在
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string genScriptPath = Path.Combine(folderPath, $"{className}Gen.cs");
            string genExtScriptPath = Path.Combine(folderPath, $"{className}GenPartial.cs");
            string mainScriptPath = Path.Combine(folderPath, $"{className}.cs");

            // 扫描预制体组件（使用 PrefixToTypeMap 中所有前缀）
            var uiComponents = ScanUIPrefabComponents(uiPrefab);

            // 检查文件是否已存在
            bool genExists = File.Exists(genScriptPath);
            bool genExtExists = File.Exists(genExtScriptPath);
            bool mainExists = File.Exists(mainScriptPath);

            if (genExists || mainExists)
            {
                int choice = EditorUtility.DisplayDialogComplex("文件已存在",
                    $"以下文件已存在：\n{genScriptPath}\n{mainScriptPath}\n\n请选择操作方式：",
                    "覆盖Gen文件", "取消", "");

                if (choice == 1) return; // 取消
                // choice == 0 覆盖Gen文件，保留用户的.cs文件
            }

            // 覆盖Gen文件，保留用户的.cs文件
            GenerateGenScript(genScriptPath, uiComponents, className);

            if (!genExtExists)
            {
                GenerateGenExtScript(genExtScriptPath, className);
            }

            if (!mainExists)
            {
                GenerateMainScriptTemplate(mainScriptPath, uiComponents, className);
            }

            AssetDatabase.Refresh();

            // 注册映射：编译完成后自动挂载Gen脚本到预制体
            RegisterGenScriptToPrefab(className, uiPrefab);

            EditorUtility.DisplayDialog("成功", $"UI模版生成完成！\n\nGen脚本(自动生成): {genScriptPath}\nGen扩展脚本(用户编写): {genExtScriptPath}\n主脚本(用户编写): {mainScriptPath}", "确定");
            return;
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成失败:\n{e.Message}", "确定");
            Debug.LogError($"UI模版生成错误: {e.StackTrace}");
        }
    }


    // ==================== 生成 {className}Gen.cs ====================
    
    /// <summary>
    /// 生成Gen脚本 - 自动获取组件属性
    /// </summary>
    private static void GenerateGenScript(string filePath, List<UIComponentInfo> uiComponents, string className)
    {
        StringBuilder sb = new StringBuilder();

        // 文件头注释
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {className} View层 - 自动生成，请勿手动修改");
        sb.AppendLine("/// </summary>");
        sb.AppendLine();

        // using语句
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();

        // 类声明
        sb.AppendLine($"public partial class {className}Gen : MonoBehaviour");
        sb.AppendLine("{");

        // 组件属性 - 使用解析后的字段名
        foreach (var comp in uiComponents)
        {
            sb.AppendLine($"    public {comp.type} {comp.fieldName} {{ get; private set; }}");
        }

        sb.AppendLine();

        // Awake方法 - 获取组件
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");

        foreach (var comp in uiComponents)
        {
            string findPath = GetComponentPath(comp);
            
            // 检查是否是直接子级还是更深层级
            if (string.IsNullOrEmpty(findPath) || findPath == comp.name)
            {
                // 组件在当前GameObject上
                sb.AppendLine($"        {comp.fieldName} = GetComponent<{comp.type}>();");
            }
            else
            {
                // 组件在子层级
                sb.AppendLine($"        {comp.fieldName} = transform.Find(\"{findPath}\").GetComponent<{comp.type}>();");
            }
        }
        
        sb.AppendLine("    }");

        sb.AppendLine("}");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[GenerateUITool] Gen脚本已生成: {filePath}");
    }

    // ==================== 生成 {className}GenPartial.cs (用户扩展) ====================

    /// <summary>
    /// 生成Gen扩展脚本 - 用户可编写额外View逻辑
    /// </summary>
    private static void GenerateGenExtScript(string filePath, string className)
    {
        // 如果文件已存在，则不覆盖
        if (File.Exists(filePath))
        {
            Debug.Log($"[GenerateUITool] Gen扩展脚本已存在，跳过生成: {filePath}");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // 文件头注释
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {className} View层扩展 - 用户编写");
        sb.AppendLine("/// </summary>");
        sb.AppendLine();

        // using语句
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();

        // 类声明
        sb.AppendLine($"public partial class {className}Gen");
        sb.AppendLine("{");
        sb.AppendLine("    // 在这里添加额外的View逻辑");
        sb.AppendLine("}");
        sb.AppendLine();

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[GenerateUITool] Gen扩展脚本模板已生成: {filePath}");
    }

    // ==================== 生成用户手写模板 ====================

    /// <summary>
    /// 生成主脚本模板 
    /// </summary>
    private static void GenerateMainScriptTemplate(string filePath, List<UIComponentInfo> uiComponents, string className)
    {
        // 如果文件已存在，则不覆盖
        if (File.Exists(filePath))
        {
            Debug.Log($"[GenerateUITool] 主脚本已存在，跳过生成: {filePath}");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // 文件头注释
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {className} Logic层 - 用户编写");
        sb.AppendLine("/// </summary>");
        sb.AppendLine();

        // using语句
        sb.AppendLine("using MieMieFrameWork;");
        sb.AppendLine("using MieMieFrameWork.UI;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine();

        // 分部类声明
        sb.AppendLine($"internal class {className} : UIWindowBase");
        sb.AppendLine("{");

        // View属性
        sb.AppendLine($"    internal {className}Gen View {{ get; private set; }}");
        sb.AppendLine();

        // OnAwake
        sb.AppendLine("    internal protected override void OnAwake()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnAwake();");
        sb.AppendLine($"        View = UIContent.GetComponent<{className}Gen>();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // OnShow
        sb.AppendLine("    internal protected override void OnShow()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnShow();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // OnHide
        sb.AppendLine("    internal protected override void OnHide()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnHide();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // OnDestroy
        sb.AppendLine("    internal protected override void OnDestroy()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnDestroy();");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("}");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[GenerateUITool] 主脚本模板已生成: {filePath}");
    }


    // ==================== 工具方法 ====================

    // UI组件信息结构
    private class UIComponentInfo
    {
        public string name;       // 原始物体名（如 [Btn][Tmp]DisPlay）
        public string type;
        public string path;
        public string fieldName; // 用于生成的字段名
        public int instanceId;   // 用于去重的GameObject实例ID
    }

    // 多前缀解析分隔符
    private const char MULTI_PREFIX_SEPARATOR = ']';

    /// <summary>内置默认前缀表（重置映射时使用）。</summary>
    public static IReadOnlyDictionary<string, string> DefaultPrefixToTypeMap =>
        UIGenPathSettings.GetBuiltInPrefixDefaults();

    /// <summary>当前前缀映射（来自 UIGenPath/UIGenPathSettings.json）。</summary>
    public static IReadOnlyDictionary<string, string> PrefixToTypeMap { get; private set; }

    static GenerateUITool()
    {
        RefreshPrefixToTypeMap();
    }

    public static void RefreshPrefixToTypeMap()
    {
        PrefixToTypeMap = UIGenPathSettings.BuildPrefixToTypeDictionary();
    }

    // 扫描UI预制体组件 - 支持多前缀解析规则
    private static List<UIComponentInfo> ScanUIPrefabComponents(GameObject uiPrefab)
    {
        var components = new List<UIComponentInfo>();

        // 查找UIContent
        Transform uiContent = uiPrefab.transform.Find("UIContent");
        if (uiContent == null)
        {
            Debug.LogError("预制体中未找到UIContent");
            return components;
        }

        // 直接使用 PrefixToTypeMap 的所有 Key 作为前缀列表
        List<string> effectivePrefixes = new List<string>(PrefixToTypeMap.Keys);

        // 获取所有UIBehaviour组件
        var allComponents = uiContent.GetComponentsInChildren<UnityEngine.EventSystems.UIBehaviour>(true);

        foreach (var comp in allComponents)
        {
            // 排除子预制体
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(comp.gameObject);
            if (prefabRoot != null && prefabRoot != uiPrefab)
                continue;

            string compName = comp.gameObject.name;
            string actualComponentType = comp.GetType().Name;

            // [RT]xxx 物体由 RectTransform 扫描单独处理，避免重复
            if (compName.StartsWith("[RT]", StringComparison.OrdinalIgnoreCase))
                continue;

            // 使用多前缀解析规则处理组件名
            var parsedComponents = ParseMultiPrefix(compName, actualComponentType, effectivePrefixes);

            if (parsedComponents.Count == 0)
                continue;

            // 获取物体上所有组件的类型（用于多前缀解析时的类型过滤）
            var componentsOnObject = comp.gameObject.GetComponents<UnityEngine.EventSystems.UIBehaviour>();
            var componentTypesOnObject = new HashSet<string>();
            foreach (var c in componentsOnObject)
            {
                componentTypesOnObject.Add(c.GetType().Name);
            }

            // 计算相对路径（只需计算一次）
            string path = GetRelativePath(uiContent, comp.transform);

            // 检查是否已处理过此 GameObject - 使用 instanceId 进行精确去重
            // 必须在 parsedComponents 循环外部检查，确保每个 GameObject 只被检查一次，
            // 但允许其所有 parsed 组件都能被添加（多前缀场景：同一物体有多个组件）
            if (components.Any(c => c.instanceId == comp.gameObject.GetInstanceID()))
            {
                continue;
            }

            foreach (var parsed in parsedComponents)
            {
                // 多前缀模式下，需要检查物体上是否有对应类型的组件（否则会生成无法 GetComponent 的字段）
                if (compName.Contains(MULTI_PREFIX_SEPARATOR))
                {
                    if (!componentTypesOnObject.Contains(parsed.type))
                    {
                        Debug.LogWarning(
                            $"[GenerateUITool] 多前缀节点「{compName}」声明了前缀→{parsed.type}（字段 {parsed.fieldName}），" +
                            $"但该物体上未挂载此组件，已跳过。请在同一 GameObject 上同时挂载多前缀所需的全部组件，或拆成多个子节点分别命名。",
                            comp.gameObject);
                        continue;
                    }
                }

                components.Add(new UIComponentInfo
                {
                    name = parsed.originalName,
                    type = parsed.type,
                    path = path,
                    fieldName = parsed.fieldName,
                    instanceId = comp.gameObject.GetInstanceID()
                });
            }
        }

        // RectTransform 扫描：收集 [RT]xxx 等带 RT 前缀的物体，走字典匹配
        foreach (RectTransform rt in uiContent.GetComponentsInChildren<RectTransform>(true))
        {
            // 排除子预制体
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(rt.gameObject);
            if (prefabRoot != null && prefabRoot != uiPrefab)
                continue;

            string compName = rt.gameObject.name;

            // 只处理以 [ 开头的物体（[] 单前缀格式，如 [RT]SettingMain）
            if (!compName.StartsWith("["))
                continue;

            // 非 [RT] 前缀：若已挂 Image/Button/TMP 等 UIBehaviour，由上面的 UIBehaviour 循环处理，避免重复
            // [RT] 前缀：面板节点常同时挂 Image、LayoutGroup 等，仍需生成 RectTransform 引用（UIBehaviour 循环已整体跳过 [RT] 名）
            bool isRtBracketName = compName.StartsWith("[RT]", StringComparison.OrdinalIgnoreCase);
            if (!isRtBracketName && rt.GetComponent<UnityEngine.EventSystems.UIBehaviour>() != null)
                continue;

            // 检查是否已处理过此 GameObject - 使用 instanceId 进行精确去重
            // 必须在 ParseMultiPrefix 之前检查，避免无效解析
            if (components.Any(c => c.instanceId == rt.gameObject.GetInstanceID()))
                continue;

            // 走通用解析：剥去方括号后查字典
            var parsedComponents = ParseMultiPrefix(compName, "RectTransform", effectivePrefixes);
            if (parsedComponents.Count == 0)
                continue;

            // 计算相对路径（只需计算一次）
            string path = GetRelativePath(uiContent, rt);

            foreach (var parsed in parsedComponents)
            {
                components.Add(new UIComponentInfo
                {
                    name = compName,
                    type = parsed.type,
                    path = path,
                    fieldName = parsed.fieldName,
                    instanceId = rt.gameObject.GetInstanceID()
                });
            }
        }

        Debug.Log($"[GenerateUITool] 共扫描到 {components.Count} 个有效UI组件");
        return components;
    }

    // 解析后的单个组件信息
    private struct ParsedComponentInfo
    {
        public string originalName;
        public string type;
        public string fieldName;
    }

    /// <summary>
    /// 多前缀解析规则
    /// 规则：
    /// 1. 如果名字中有 ] 号，表示多层解析
    /// 2. 例如 [Img][Btn][Toggle]Panel，白名单有 Img, Btn, Toggle
    /// 3. 会生成：
    ///    - Image 类型的 ImgPanel
    ///    - Button 类型的 BtnPanel
    ///    - Toggle 类型的 TogglePanel
    /// 4. 如果没有 ] 号，说明只有一层，直接按白名单匹配
    /// </summary>
    private static List<ParsedComponentInfo> ParseMultiPrefix(string compName, string componentType, List<string> effectivePrefixes)
    {
        var result = new List<ParsedComponentInfo>();

        // 检查是否包含多前缀分隔符
        if (compName.Contains(MULTI_PREFIX_SEPARATOR))
        {
            // 多层解析模式
            result = ParseMultiPrefixMode(compName, effectivePrefixes);
        }
        else if (compName.StartsWith("[") && compName.Length > 1)
        {
            // [] 单前缀格式：剥去方括号后按单前缀解析
            // 例如 [RT]SettingMain -> 剥去 [ 和 ] 后得到 "RTSettingMain"，再按单前缀匹配
            int closeBracket = compName.IndexOf(']');
            if (closeBracket > 1)
            {
                string stripped = compName.Substring(closeBracket + 1);
                result = ParseSinglePrefixMode(stripped, componentType, effectivePrefixes);
            }
        }
        else
        {
            // 单层解析模式 - 原始逻辑
            result = ParseSinglePrefixMode(compName, componentType, effectivePrefixes);
        }

        return result;
    }

    /// <summary>
    /// 多前缀解析模式
    /// 格式: [Prefix1][Prefix2][Prefix3]Suffix
    /// 例如：[Img][Btn][Toggle]Panel，白名单有 Img, Btn, Toggle
    /// 解析规则：
    /// - 共同后缀 = 方括号后面的部分（如 Panel）
    /// - 每个方括号内的前缀 + 共同后缀 = 字段名
    ///
    /// 结果：
    /// - Img + Panel = ImgPanel (Image)
    /// - Btn + Panel = BtnPanel (Button)
    /// - Toggle + Panel = TogglePanel (Toggle)
    ///
    /// 注意：多前缀时，该 GameObject 上必须实际挂载声明的每一种 UI 组件，否则对应字段不会生成（避免 Awake 里 GetComponent 失败）。
    /// </summary>
    private static List<ParsedComponentInfo> ParseMultiPrefixMode(string compName, List<string> effectivePrefixes)
    {
        var result = new List<ParsedComponentInfo>();

        // 用 ] 号分割
        string[] parts = compName.Split(MULTI_PREFIX_SEPARATOR);

        if (parts.Length < 2)
        {
            return result; // 没有有效分割，返回空
        }

        // 共同后缀 = 最后一个部分（方括号后面的内容）
        string commonSuffix = parts[parts.Length - 1];

        // 遍历所有方括号内的部分（跳过最后一个共同后缀）
        for (int i = 0; i < parts.Length - 1; i++)
        {
            string prefixContent = parts[i];
            // 去除前导 [ 号（分割后 "[Img" 变成 "Img"）
            if (prefixContent.StartsWith("["))
            {
                prefixContent = prefixContent.Substring(1);
            }

            // 精确匹配优先
            string matchedPrefix = FindExactPrefixMatch(prefixContent, effectivePrefixes);

            // 方括号内为「完整前缀」时：Tmp、Btn 等（prefixContent.StartsWith(白名单前缀)）
            if (string.IsNullOrEmpty(matchedPrefix))
            {
                matchedPrefix = FindPrefixMatch(prefixContent, effectivePrefixes);
            }

            // 方括号内为缩写时：B 匹配 Btn（白名单前缀.StartsWith(prefixContent)，取最短以消歧）
            if (string.IsNullOrEmpty(matchedPrefix))
            {
                matchedPrefix = FindReversePrefixMatch(prefixContent, effectivePrefixes);
            }

            if (string.IsNullOrEmpty(matchedPrefix))
            {
                continue; // 不匹配任何前缀，跳过
            }

            string targetComponentType = GetComponentTypeByPrefix(matchedPrefix);
            if (string.IsNullOrEmpty(targetComponentType))
            {
                continue; // 无法确定组件类型，跳过
            }

            // 字段名 = 前缀 + 共同后缀
            string fieldName = matchedPrefix + commonSuffix;

            result.Add(new ParsedComponentInfo
            {
                originalName = compName,
                type = targetComponentType,
                fieldName = fieldName
            });

            Debug.Log($"[MultiPrefix] 解析: {compName} -> {matchedPrefix} -> 类型={targetComponentType}, 字段名={fieldName}");
        }

        return result;
    }

    /// <summary>
    /// 查找精确匹配的前缀
    /// </summary>
    private static string FindExactPrefixMatch(string input, List<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (input.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return prefix;
            }
        }
        return null;
    }

    /// <summary>
    /// 查找前缀匹配（方括号内文本以白名单前缀开头，如 TmpXxx → Tmp）
    /// </summary>
    private static string FindPrefixMatch(string input, List<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return prefix;
            }
        }
        return null;
    }

    /// <summary>
    /// 缩写匹配：白名单中的前缀以方括号内文本开头（如 B → Btn，Bt → Btn）。
    /// 多个候选时取最短前缀键，避免 B 在 Btn 与 Button 之间歧义时优先 Btn。
    /// </summary>
    private static string FindReversePrefixMatch(string input, List<string> prefixes)
    {
        if (string.IsNullOrEmpty(input)) return null;

        string best = null;
        foreach (var prefix in prefixes)
        {
            if (string.IsNullOrEmpty(prefix)) continue;
            if (!prefix.StartsWith(input, StringComparison.OrdinalIgnoreCase)) continue;
            if (best == null || prefix.Length < best.Length)
                best = prefix;
        }
        return best;
    }

    /// <summary>
    /// 根据前缀获取组件类型
    /// </summary>
    private static string GetComponentTypeByPrefix(string prefix)
    {
        if (PrefixToTypeMap.TryGetValue(prefix, out string type))
        {
            return type;
        }
        return null;
    }

    /// <summary>
    /// 单层解析模式
    /// 规则：
    /// 1. 检查物体名字是否以白名单前缀开头
    /// 2. 如果是，使用前缀映射表确定组件类型
    /// 3. 使用前缀映射表的类型而不是实际组件类型
    /// </summary>
    private static List<ParsedComponentInfo> ParseSinglePrefixMode(string compName, string componentType, List<string> effectivePrefixes)
    {
        var result = new List<ParsedComponentInfo>();

        // 检查是否匹配白名单前缀
        string matchedPrefix = null;

        foreach (var prefix in effectivePrefixes)
        {
            if (!string.IsNullOrEmpty(prefix) && compName.StartsWith(prefix))
            {
                matchedPrefix = prefix;
                break;
            }
        }

        if (string.IsNullOrEmpty(matchedPrefix))
        {
            return result; // 不匹配任何前缀，返回空
        }

        // 生成字段名：首字母大写
        string fieldName = ToPascalCase(compName);

        // 根据前缀确定组件类型（优先使用前缀映射表）
        string type = GetComponentTypeByPrefix(matchedPrefix);
        if (string.IsNullOrEmpty(type))
        {
            type = componentType; // 使用实际组件类型
        }

        result.Add(new ParsedComponentInfo
        {
            originalName = compName,
            type = type,
            fieldName = fieldName
        });

        return result;
    }

    // 获取相对路径
    private static string GetRelativePath(Transform root, Transform target)
    {
        var path = new System.Text.StringBuilder();
        var current = target;

        while (current != null && current != root)
        {
            if (path.Length > 0)
            {
                path.Insert(0, "/");
            }
            path.Insert(0, current.name);
            current = current.parent;
        }

        return path.ToString();
    }

    // 判断是否为交互组件类型
    private static bool IsInteractiveType(string type)
    {
        return type == "Button" || type == "Toggle" || type == "InputField" || 
               type == "TMP_InputField" || type == "Dropdown" || type == "TMP_Dropdown";
    }

    // 驼峰命名转换 - 首字母大写
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 移除前缀（如 @, btn_, img_ 等）
        string result = input;

        // 处理常见的UI前缀
        string[] commonPrefixes = { "@", "btn_", "img_", "txt_", "tg_", "ipt_" };
        foreach (var prefix in commonPrefixes)
        {
            if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length);
                break;
            }
        }

        // 如果结果为空或只有一个字符，直接返回
        if (string.IsNullOrEmpty(result))
            return result;

        // 首字母大写
        return char.ToUpper(result[0]) + result.Substring(1);
    }

    // 获取组件查找路径（用于transform.Find）
    private static string GetComponentPath(UIComponentInfo comp)
    {
        // 使用扫描时计算的实际路径
        return comp.path;
    }

    // ========== 脚本编译完成后自动挂载 {ClassName}Gen.cs 到预制体 UIContent ==========
    //
    //  流程：RegisterGenScriptToPrefab（SessionState 待挂载队列 + 触发编译）
    //      → AssetDatabase.Refresh() 触发编译
    //      → DidReloadScripts（Gen 类型已可用）→ OnScriptsReloaded 挂载

    [Serializable]
    private class PendingAttach
    {
        public string className;
        public string prefabGuid;
    }

    private const string PendingAttachesSessionKey = "MieMieFrameWork.GenerateUITool.PendingAttaches";
    private const string LegacyPendingAttachesFile = "Assets/MieMieFrameTools/Editor/UI/PendingAttaches.json";

    /// <summary>
    /// 生成时记录映射并立即触发挂载流程
    /// </summary>
    public static void RegisterGenScriptToPrefab(string className, GameObject prefab)
    {
        if (prefab == null) return;

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath)) return;

        string guid = AssetDatabase.AssetPathToGUID(prefabPath);
        if (string.IsNullOrEmpty(guid)) return;

        // 写入待挂载队列
        var pending = new PendingAttach { className = className, prefabGuid = guid };
        SavePendingAttach(pending);

        Debug.Log($"[GenerateUITool] 注册待挂载: {className}Gen -> {prefab.name}");

        // 触发编译，DidReloadScripts 会在编译完成后自动执行
        AssetDatabase.Refresh();
    }

    private static void TryAttachOnce(string className, string prefabGuid)
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
        if (string.IsNullOrEmpty(prefabPath)) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        Transform uiContent = prefab.transform.Find("UIContent");
        if (uiContent == null) return;

        var genType = GetTypeByName($"{className}Gen");
        if (genType == null) return; // 类型未编译完成

        if (uiContent.GetComponent(genType) != null) return;

        try
        {
            Undo.AddComponent(uiContent.gameObject, genType);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GenerateUITool] 已挂载 {className}Gen 到 {prefab.name}/UIContent");
            RemovePendingAttach(className);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GenerateUITool] 挂载失败: {e.Message}");
        }
    }

    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        var pending = LoadPendingAttaches();
        foreach (var p in pending)
        {
            TryAttachOnce(p.className, p.prefabGuid);
        }
    }

    private static Type GetTypeByName(string typeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            catch { }
        }
        return null;
    }

    private static List<PendingAttach> LoadPendingAttaches()
    {
        TryImportLegacyPendingAttachesFile();

        string json = SessionState.GetString(PendingAttachesSessionKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new List<PendingAttach>();

        try
        {
            return JsonUtility.FromJson<PendingAttachList>(json)?.items ?? new List<PendingAttach>();
        }
        catch
        {
            return new List<PendingAttach>();
        }
    }

    private static void SavePendingAttach(PendingAttach item)
    {
        var list = LoadPendingAttaches();
        list.RemoveAll(p => p.className == item.className);
        list.Add(item);
        SavePendingList(list);
    }

    private static void RemovePendingAttach(string className)
    {
        var list = LoadPendingAttaches();
        list.RemoveAll(p => p.className == className);
        SavePendingList(list);
    }

    private static void SavePendingList(List<PendingAttach> list)
    {
        if (list == null || list.Count == 0)
        {
            SessionState.EraseString(PendingAttachesSessionKey);
            return;
        }

        SessionState.SetString(PendingAttachesSessionKey,
            JsonUtility.ToJson(new PendingAttachList { items = list }));
    }

    /// <summary>
    /// 一次性：从旧版 Assets 内 PendingAttaches.json 迁入 SessionState 后删除该文件。
    /// </summary>
    private static void TryImportLegacyPendingAttachesFile()
    {
        if (!File.Exists(LegacyPendingAttachesFile)) return;
        if (!string.IsNullOrEmpty(SessionState.GetString(PendingAttachesSessionKey, string.Empty)))
        {
            AssetDatabase.DeleteAsset(LegacyPendingAttachesFile);
            return;
        }

        try
        {
            string json = File.ReadAllText(LegacyPendingAttachesFile);
            var imported = JsonUtility.FromJson<PendingAttachList>(json);
            if (imported?.items != null && imported.items.Count > 0)
                SavePendingList(imported.items);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GenerateUITool] 迁移旧 PendingAttaches.json 失败: {e.Message}");
        }

        AssetDatabase.DeleteAsset(LegacyPendingAttachesFile);
    }

    [Serializable]
    private class PendingAttachList
    {
        public List<PendingAttach> items = new();
    }

    /// <summary>
    /// 根据预制体GUID获取上次生成脚本的路径
    /// </summary>
    public static string GetLastGenScriptPath(string prefabGuid) =>
        UIGenPathSettings.GetLastFolderForPrefab(prefabGuid);

    /// <summary>
    /// 获取默认UI脚本生成路径
    /// </summary>
    public static string GetDefaultUIGenScriptPath() =>
        UIGenPathSettings.GetDefaultFolder();
}
