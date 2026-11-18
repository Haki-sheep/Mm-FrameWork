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
using MmUIFrameWork.Core;

/// <summary>
/// UI模版生成核心工具类 - 分部类方案
/// 生成 {className}Gen.cs 序列化引用字段 + 挂载时烤引用
/// {className}GenPartial.cs 用户扩展 + {className}.cs 用户手写模板
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
    [Obsolete]

    public static void GenerateUITemplates(string className, GameObject uiPrefab, string folderPath)
    {
        if (!ValidateInputs(className, uiPrefab)) return;

        try
        {
            string genScriptPath = Path.Combine(folderPath, $"{className}Gen.cs");
            string genExtScriptPath = Path.Combine(folderPath, $"{className}GenPartial.cs");
            string mainScriptPath = Path.Combine(folderPath, $"{className}.cs");

            // 扫描预制体绑定配置
            if (!TryScanUIPrefabComponents(uiPrefab, out var UiComponents, out var InvalidBindingList))
            {
                ShowInvalidBindings(uiPrefab, InvalidBindingList);
                return;
            }

            if (UiComponents.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有找到任何勾选绑定项，请先在Hierarchy中勾选UIContent下的组件", "确定");
                return;
            }

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

            // 确保文件夹存在
            Directory.CreateDirectory(folderPath);

            // 覆盖Gen文件，保留用户的.cs文件
            GenerateGenScript(genScriptPath, UiComponents, className);

            if (!genExtExists)
            {
                GenerateGenExtScript(genExtScriptPath, className);
            }

            if (!mainExists)
            {
                GenerateMainScriptTemplate(mainScriptPath, UiComponents, className);
            }

            // 1 先写入待挂载 必须在任何 Refresh 之前
            RegisterPendingAttachOnly(className, uiPrefab);

            // 2 Import 新脚本让 Project 可见 并异步触发编译
            // 不用 ForceSynchronousImport 避免中途域重载把后续逻辑打断
            ImportGeneratedScripts(folderPath, className, genExtExists, mainExists);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            EditorUtility.DisplayDialog("成功", $"UI模版生成完成！\n\nGen脚本(自动生成): {genScriptPath}\nGen扩展脚本(用户编写): {genExtScriptPath}\n主脚本(用户编写): {mainScriptPath}\n\n编译完成后会自动挂载到 UIContent", "确定");
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
    /// 生成Gen脚本 - 仅序列化引用字段 由挂载流程赋值 无运行时 Find
    /// </summary>
    private static void GenerateGenScript(string filePath, List<UIComponentInfo> uiComponents, string className)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {className} View层 - 自动生成，请勿手动修改");
        sb.AppendLine("/// 字段引用由生成器挂载时写入 运行时无 Find");
        sb.AppendLine("/// </summary>");
        sb.AppendLine();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();
        sb.AppendLine("namespace MieMieUIFrameWork.Runtime");
        sb.AppendLine("{");

        sb.AppendLine($"    public partial class {className}Gen : MonoBehaviour");
        sb.AppendLine("    {");

        foreach (var comp in uiComponents)
        {
            sb.AppendLine("        [SerializeField]");
            sb.AppendLine($"        public {comp.type} {comp.fieldName};");
            sb.AppendLine();
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
        sb.AppendLine("namespace MieMieUIFrameWork.Runtime");
        sb.AppendLine("{");

        // 类声明
        sb.AppendLine($"    public partial class {className}Gen");
        sb.AppendLine("    {");
        sb.AppendLine("        // 在这里添加额外的View逻辑");
        sb.AppendLine("    }");
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
        sb.AppendLine("using MmUIFrameWork.Core;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine();
        sb.AppendLine("namespace MieMieUIFrameWork.Runtime");
        sb.AppendLine("{");

        // 分部类声明
        sb.AppendLine($"    internal class {className} : UIWindowBase");
        sb.AppendLine("    {");

        // View属性
        sb.AppendLine($"        internal {className}Gen View {{ get; private set; }}");
        sb.AppendLine();

        // OnAwake
        sb.AppendLine("        protected override void OnAwake()");
        sb.AppendLine("        {");
        sb.AppendLine("            base.OnAwake();");
        sb.AppendLine($"            View = UIContent.GetComponent<{className}Gen>();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // OnShow
        sb.AppendLine("        protected override void OnShow()");
        sb.AppendLine("        {");
        sb.AppendLine("            base.OnShow();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // OnHide
        sb.AppendLine("        protected override void OnHide()");
        sb.AppendLine("        {");
        sb.AppendLine("            base.OnHide();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // OnDestroy
        sb.AppendLine("        protected override void OnDestroy()");
        sb.AppendLine("        {");
        sb.AppendLine("            base.OnDestroy();");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("    }");
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
        /// <summary> 编辑器挂载时用于烤引用 </summary>
        public Component componentRef;
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

    #region 预制体绑定扫描

    // 扫描UI预制体组件 - 支持多前缀解析规则
    [Obsolete]

    private static bool TryScanUIPrefabComponents(GameObject uiPrefab,
        out List<UIComponentInfo> components, out List<string> invalidBindingList)
    {
        components = new List<UIComponentInfo>();
        invalidBindingList = new List<string>();

        Transform uiContent = uiPrefab.transform.Find("UIContent");
        if (uiContent == null)
        {
            invalidBindingList.Add("预制体中未找到 UIContent");
            return false;
        }

        var bindConfig = uiPrefab.GetComponent<UIBindConfig>();
        if (bindConfig == null)
        {
            Debug.LogWarning("[GenerateUITool] 预制体未找到UIBindConfig，请先在Hierarchy中勾选绑定组件");
            return true;
        }

        foreach (var bindItem in bindConfig.BindItemList)
        {
            if (bindItem == null)
            {
                invalidBindingList.Add("勾选项为空");
                continue;
            }

            Transform target = string.IsNullOrEmpty(bindItem.nodePath)
                ? uiContent
                : uiContent.Find(bindItem.nodePath);
            if (target == null)
            {
                invalidBindingList.Add($"绑定节点已丢失 {bindItem.nodePath}");
                continue;
            }

            Type componentType = ResolveBindType(bindItem);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            {
                invalidBindingList.Add($"绑定组件类型无效 {bindItem.nodePath} / {bindItem.componentFullTypeName}");
                continue;
            }

            if (componentType == typeof(Transform) && target is RectTransform)
                componentType = typeof(RectTransform);

            Component componentRef = target.GetComponent(componentType);
            if (componentRef == null)
            {
                invalidBindingList.Add($"节点缺少绑定组件 {bindItem.nodePath} / {componentType.Name}");
                continue;
            }

            // 仅把后缀 Transform 升级为 RectTransform 已是 RectTransform 则不改 避免 ContentRectTransform 变成 ContentRectRectTransform
            string fieldName = bindItem.fieldName;
            if (componentType == typeof(RectTransform) &&
                !string.IsNullOrEmpty(fieldName) &&
                fieldName.EndsWith(nameof(Transform), StringComparison.Ordinal) &&
                !fieldName.EndsWith(nameof(RectTransform), StringComparison.Ordinal))
            {
                fieldName = fieldName.Substring(0, fieldName.Length - nameof(Transform).Length) + nameof(RectTransform);
            }

            components.Add(new UIComponentInfo
            {
                name = target.name,
                type = GetCodeTypeName(componentType),
                path = bindItem.nodePath,
                fieldName = EnsureValidFieldName(fieldName),
                instanceId = target.gameObject.GetInstanceID(),
                componentRef = componentRef
            });
        }

        if (invalidBindingList.Count > 0)
            return false;

        EnsureUniqueFieldNames(components);
        Debug.Log($"[GenerateUITool] 共扫描到 {components.Count} 个有效UI组件");
        return true;
    }

    private static void ShowInvalidBindings(GameObject prefab, List<string> invalidBindingList)
    {
        string Message = $"请修正或取消勾选以下绑定项后重新生成\n\n{string.Join("\n", invalidBindingList)}";
        Debug.LogError($"[GenerateUITool] {Message}", prefab);
        EditorUtility.DisplayDialog("UI 绑定无效", Message, "确定");
    }

    #endregion

    private static Type ResolveBindType(UIBindItem bindItem)
    {
        if (string.IsNullOrEmpty(bindItem.componentFullTypeName))
            return null;

        if (bindItem.componentFullTypeName == typeof(Transform).FullName)
            return typeof(Transform);

        if (!string.IsNullOrEmpty(bindItem.componentAssemblyName))
        {
            var type = Type.GetType($"{bindItem.componentFullTypeName}, {bindItem.componentAssemblyName}");
            if (type != null) return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(bindItem.componentFullTypeName);
            if (type != null) return type;
        }

        return null;
    }

    private static string GetCodeTypeName(Type componentType)
    {
        if (componentType == typeof(Transform)) return nameof(Transform);
        if (componentType == typeof(RectTransform)) return nameof(RectTransform);
        if (componentType.Namespace == "UnityEngine.UI") return componentType.Name;
        if (componentType.Namespace == "TMPro") return componentType.Name;
        if (componentType.Namespace == "UnityEngine") return componentType.Name;
        return componentType.FullName.Replace("+", ".");
    }

    private static string EnsureValidFieldName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return "Node";

        var builder = new StringBuilder();
        foreach (char c in fieldName)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                builder.Append(c);
        }

        if (builder.Length == 0) return "Node";
        if (char.IsDigit(builder[0])) builder.Insert(0, "N");
        return char.ToUpper(builder[0]) + builder.ToString(1, builder.Length - 1);
    }

    private static void EnsureUniqueFieldNames(List<UIComponentInfo> components)
    {
        var fieldNameCountDict = new Dictionary<string, int>();
        foreach (var component in components)
        {
            if (!fieldNameCountDict.ContainsKey(component.fieldName))
            {
                fieldNameCountDict.Add(component.fieldName, 0);
                continue;
            }

            fieldNameCountDict[component.fieldName]++;
            component.fieldName = $"{component.fieldName}{fieldNameCountDict[component.fieldName] + 1}";
        }
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

    // ========== 脚本编译完成后自动挂载 {ClassName}Gen.cs 到预制体 UIContent ==========
    //
    //  流程：先 RegisterPendingAttachOnly 写入 SessionState
    //      → Import + Refresh 触发编译
    //      → InitializeOnLoad 的 update 轮询 编译结束后挂载

    [Serializable]
    private class PendingAttach
    {
        public string className;
        public string prefabGuid;
    }

    private const string PendingAttachesSessionKey = "MieMieUIFrameWork.GenerateUITool.PendingAttaches";
    private const string LegacyPendingAttachesFile = "Assets/MieMieFrameTools/Editor/UIForEditor/UIScripts/PendingAttaches.json";

    /// <summary> 本域内是否已挂上 update 轮询 </summary>
    private static bool s_PendingAttachUpdaterHooked;

    [InitializeOnLoadMethod]
    private static void HookPendingAttachUpdater()
    {
        if (s_PendingAttachUpdaterHooked) return;
        s_PendingAttachUpdaterHooked = true;
        EditorApplication.update -= ProcessPendingAttachesOnUpdate;
        EditorApplication.update += ProcessPendingAttachesOnUpdate;
    }

    /// <summary>
    /// 生成时先入队 再 Refresh 等编译完成后挂载
    /// </summary>
    public static void RegisterGenScriptToPrefab(string className, GameObject prefab)
    {
        RegisterPendingAttachOnly(className, prefab);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// 只写入待挂载队列 不触发 Refresh
    /// </summary>
    private static void RegisterPendingAttachOnly(string className, GameObject prefab)
    {
        if (prefab == null) return;

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
        }

        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("[GenerateUITool] 无法解析预制体资产路径 跳过自动挂载", prefab);
            return;
        }

        string guid = AssetDatabase.AssetPathToGUID(prefabPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"[GenerateUITool] 无法解析预制体 GUID path={prefabPath}", prefab);
            return;
        }

        var pending = new PendingAttach { className = className, prefabGuid = guid };
        SavePendingAttach(pending);
        HookPendingAttachUpdater();
        Debug.Log($"[GenerateUITool] 注册待挂载: {className}Gen -> {prefabPath} guid={guid}");
    }

    /// <summary>
    /// 把磁盘上的新脚本立刻导入 AssetDatabase
    /// </summary>
    private static void ImportGeneratedScripts(string folderPath, string className, bool genExtExistedBefore, bool mainExistedBefore)
    {
        string folderAssetPath = ToUnityAssetPath(folderPath);
        if (!string.IsNullOrEmpty(folderAssetPath) && !AssetDatabase.IsValidFolder(folderAssetPath))
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        ImportScriptAsset(Path.Combine(folderPath, $"{className}Gen.cs"));

        if (!genExtExistedBefore)
        {
            ImportScriptAsset(Path.Combine(folderPath, $"{className}GenPartial.cs"));
        }

        if (!mainExistedBefore)
        {
            ImportScriptAsset(Path.Combine(folderPath, $"{className}.cs"));
        }
    }

    /// <summary>
    /// 导入单个脚本资产
    /// </summary>
    private static void ImportScriptAsset(string filePath)
    {
        if (!File.Exists(filePath)) return;

        string assetPath = ToUnityAssetPath(filePath);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[GenerateUITool] 无法转为 Assets 路径: {filePath}");
            return;
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[GenerateUITool] 已导入脚本: {assetPath}");
    }

    /// <summary>
    /// 绝对路径或混用路径转为 Unity Assets 路径
    /// </summary>
    private static string ToUnityAssetPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string normalized = path.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');

        if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        int assetsIndex = normalized.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
        if (assetsIndex >= 0)
        {
            return normalized.Substring(assetsIndex + 1);
        }

        if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return null;
    }

    /// <summary>
    /// update 轮询消费待挂载队列
    /// </summary>
    private static void ProcessPendingAttachesOnUpdate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (string.IsNullOrEmpty(SessionState.GetString(PendingAttachesSessionKey, string.Empty))) return;

        var pending = LoadPendingAttaches();
        if (pending.Count == 0) return;

        for (int i = 0; i < pending.Count; i++)
        {
            PendingAttach p = pending[i];
            TryAttachOnce(p.className, p.prefabGuid);
        }
    }

    /// <summary>
    /// 尝试挂载一次 成功返回 true
    /// </summary>
    private static bool TryAttachOnce(string className, string prefabGuid)
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"[GenerateUITool] 挂载失败 找不到预制体 guid={prefabGuid}");
            RemovePendingAttach(className);
            return false;
        }

        Type genType = GetTypeByName($"{className}Gen");
        if (genType == null)
        {
            // 类型未编译出来 继续等 不移出队列
            return false;
        }

        try
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform uiContent = FindChildRecursive(prefabRoot.transform, "UIContent");
                if (uiContent == null)
                {
                    Debug.LogWarning($"[GenerateUITool] 挂载失败 未找到 UIContent: {prefabPath}", prefabRoot);
                    RemovePendingAttach(className);
                    return false;
                }

                // 旧 Gen 仍在内存时会缺新字段 等重编译后再烤 避免 Pending 被提前清掉
                if (!TryScanUIPrefabComponents(prefabRoot, out var UiComponents, out var InvalidBindingList))
                {
                    Debug.LogError($"[GenerateUITool] 挂载失败 {prefabPath}\n{string.Join("\n", InvalidBindingList)}", prefabRoot);
                    RemovePendingAttach(className);
                    return false;
                }

                if (UiComponents.Count == 0)
                {
                    Debug.LogWarning($"[GenerateUITool] 绑定引用跳过 无有效勾选项: {prefabRoot.name}", prefabRoot);
                    RemovePendingAttach(className);
                    return false;
                }

                for (int i = 0; i < UiComponents.Count; i++)
                {
                    if (genType.GetField(UiComponents[i].fieldName,
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic) == null)
                        return false;
                }

                Component gen = uiContent.GetComponent(genType);
                if (gen == null)
                {
                    gen = uiContent.gameObject.AddComponent(genType);
                }

                if (!BindGenSerializedReferences(gen, UiComponents))
                {
                    RemovePendingAttach(className);
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[GenerateUITool] 已挂载并绑定引用 {className}Gen -> {prefabPath}/UIContent");
                RemovePendingAttach(className);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GenerateUITool] 挂载失败: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 递归查找子节点
    /// </summary>
    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        Transform direct = root.Find(childName);
        if (direct != null) return direct;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>
    /// 按 UIBindConfig 将组件引用写入 Gen 序列化字段 无运行时 Find
    /// </summary>
    private static bool BindGenSerializedReferences(Component gen, List<UIComponentInfo> uiComponents)
    {
        var SerializedGen = new SerializedObject(gen);
        foreach (var ComponentInfo in uiComponents)
        {
            var Property = SerializedGen.FindProperty(ComponentInfo.fieldName);
            if (Property == null || Property.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogError($"[GenerateUITool] Gen 缺少组件引用字段 {ComponentInfo.fieldName} 请重新生成脚本", gen);
                return false;
            }

            Property.objectReferenceValue = ComponentInfo.componentRef;
        }

        SerializedGen.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[GenerateUITool] 已写入 {uiComponents.Count} 个序列化引用 -> {gen.GetType().Name}");
        return true;
    }

    /// <summary>
    /// 按简单类名查找 Type 兼容无命名空间与带命名空间
    /// </summary>
    private static Type GetTypeByName(string typeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            catch
            {
            }
        }

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type[] types = assembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i].Name == typeName)
                    {
                        return types[i];
                    }
                }
            }
            catch
            {
            }
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
            PendingAttachList wrapper = JsonUtility.FromJson<PendingAttachList>(json);
            if (wrapper?.items == null || wrapper.items.Length == 0)
                return new List<PendingAttach>();
            return new List<PendingAttach>(wrapper.items);
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
            JsonUtility.ToJson(new PendingAttachList { items = list.ToArray() }));
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
            if (imported?.items != null && imported.items.Length > 0)
                SavePendingList(new List<PendingAttach>(imported.items));
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
        public PendingAttach[] items = Array.Empty<PendingAttach>();
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

    /// <summary>
    /// 从 UIContent 上的 Gen 序列化引用反向写入 UIBindConfig
    /// Hierarchy 勾选高亮以 Gen 为准 避免手动重勾
    /// </summary>
    /// <param name="uiRoot">带 UIContent 的 UI 根节点</param>
    /// <returns>成功同步的条目数 失败返回 -1</returns>
    public static int SyncBindConfigFromGen(GameObject uiRoot)
    {
        if (uiRoot == null)
        {
            EditorUtility.DisplayDialog("错误", "UI 根节点为空", "确定");
            return -1;
        }

        Transform uiContent = uiRoot.transform.Find("UIContent");
        if (uiContent == null)
        {
            EditorUtility.DisplayDialog("错误", $"未找到 UIContent: {uiRoot.name}", "确定");
            return -1;
        }

        Component gen = FindGenComponent(uiContent);
        if (gen == null)
        {
            EditorUtility.DisplayDialog("错误",
                $"UIContent 上未找到 *Gen 组件\n请先生成并挂载 Gen 脚本: {uiRoot.name}",
                "确定");
            return -1;
        }

        var bindItemList = BuildBindItemsFromGen(uiContent, gen, out int skippedNullCount, out int skippedOutOfContentCount);
        if (bindItemList.Count == 0)
        {
            string tip = skippedNullCount > 0
                ? $"Gen 上没有有效引用（空引用 {skippedNullCount} 个）\n无法同步勾选状态"
                : "Gen 上没有可同步的序列化引用";
            EditorUtility.DisplayDialog("提示", tip, "确定");
            return 0;
        }

        var config = uiRoot.GetComponent<UIBindConfig>();
        if (config == null)
        {
            config = Undo.AddComponent<UIBindConfig>(uiRoot);
        }

        config.hideFlags = HideFlags.None;
        Undo.RecordObject(config, "Sync Bind From Gen");

        config.EditorBindItemList.Clear();
        for (int i = 0; i < bindItemList.Count; i++)
        {
            config.EditorBindItemList.Add(bindItemList[i]);
        }

        EditorUtility.SetDirty(config);
        EditorUtility.SetDirty(uiRoot);
        PrefabUtility.RecordPrefabInstancePropertyModifications(config);
        EditorApplication.RepaintHierarchyWindow();

        string skipMsg = string.Empty;
        if (skippedNullCount > 0 || skippedOutOfContentCount > 0)
        {
            skipMsg = $"\n跳过 空引用={skippedNullCount} 超出UIContent={skippedOutOfContentCount}";
        }

        Debug.Log(
            $"[GenerateUITool] 已从 {gen.GetType().Name} 反向同步 {bindItemList.Count} 条绑定到 UIBindConfig{skipMsg}",
            uiRoot);
        return bindItemList.Count;
    }

    /// <summary>
    /// 在 UIContent 上查找 *Gen 组件
    /// </summary>
    private static Component FindGenComponent(Transform uiContent)
    {
        if (uiContent == null) return null;

        MonoBehaviour[] behaviourList = uiContent.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviourList.Length; i++)
        {
            MonoBehaviour behaviour = behaviourList[i];
            if (behaviour == null) continue;

            string typeName = behaviour.GetType().Name;
            if (typeName.EndsWith("Gen", StringComparison.Ordinal))
                return behaviour;
        }

        return null;
    }

    /// <summary>
    /// 读取 Gen 序列化 Object 引用并生成 UIBindItem 列表
    /// </summary>
    private static List<UIBindItem> BuildBindItemsFromGen(
        Transform uiContent,
        Component gen,
        out int skippedNullCount,
        out int skippedOutOfContentCount)
    {
        skippedNullCount = 0;
        skippedOutOfContentCount = 0;
        var bindItemList = new List<UIBindItem>();

        SerializedObject so = new SerializedObject(gen);
        SerializedProperty iterator = so.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (iterator.name == "m_Script") continue;

            UnityEngine.Object objectRef = iterator.objectReferenceValue;
            if (objectRef == null)
            {
                skippedNullCount++;
                Debug.LogWarning($"[GenerateUITool] 反向同步跳过空引用字段: {iterator.name}", gen);
                continue;
            }

            Component component = objectRef as Component;
            if (component == null)
            {
                Debug.LogWarning(
                    $"[GenerateUITool] 反向同步跳过非组件引用: {iterator.name} -> {objectRef.GetType().Name}",
                    gen);
                continue;
            }

            Transform target = component.transform;
            if (!IsSelfOrChildOfTransform(target, uiContent))
            {
                skippedOutOfContentCount++;
                Debug.LogWarning(
                    $"[GenerateUITool] 反向同步跳过 UIContent 外节点: {iterator.name} -> {target.name}",
                    target);
                continue;
            }

            Type componentType = ResolveComponentTypeForBind(component);
            bindItemList.Add(new UIBindItem
            {
                nodePath = GetRelativeNodePath(uiContent, target),
                nodeName = target.name,
                componentTypeName = componentType.Name,
                componentFullTypeName = componentType.FullName,
                componentAssemblyName = componentType.Assembly.GetName().Name,
                fieldName = iterator.name
            });
        }

        return bindItemList;
    }

    /// <summary>
    /// RectTransform 统一记为 RectTransform 与 Hierarchy 勾选一致
    /// </summary>
    private static Type ResolveComponentTypeForBind(Component component)
    {
        if (component is RectTransform) return typeof(RectTransform);
        if (component is Transform) return typeof(Transform);
        return component.GetType();
    }

    /// <summary>
    /// 计算相对 UIContent 的节点路径
    /// </summary>
    private static string GetRelativeNodePath(Transform uiContent, Transform target)
    {
        if (target == uiContent) return string.Empty;

        var pathStack = new Stack<string>();
        Transform current = target;
        while (current != null && current != uiContent)
        {
            pathStack.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", pathStack);
    }

    /// <summary>
    /// 判断 target 是否为 parent 自身或子节点
    /// </summary>
    private static bool IsSelfOrChildOfTransform(Transform target, Transform parent)
    {
        Transform current = target;
        while (current != null)
        {
            if (current == parent) return true;
            current = current.parent;
        }

        return false;
    }
}
