using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MieMieFrameWork.DMVC.Editor;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.DMVC
{
    /// <summary>
    /// DMVC 执行顺序生成器
    /// </summary>
    public class DMVCExecutionOrderWindow : EditorWindow
    {
        /// <summary>
        /// World 类型列表
        /// </summary>
        private readonly List<Type> worldTypeList = new List<Type>();

        /// <summary>
        /// Logic 类型列表
        /// </summary>
        private readonly List<Type> logicTypeList = new List<Type>();

        /// <summary>
        /// Data 类型列表
        /// </summary>
        private readonly List<Type> dataTypeList = new List<Type>();

        /// <summary>
        /// Message 类型列表
        /// </summary>
        private readonly List<Type> messageTypeList = new List<Type>();

        /// <summary>
        /// 选中 World 索引
        /// </summary>
        private int selectedWorldIndex;

        /// <summary>
        /// 生成类名
        /// </summary>
        private string generatedClassName = string.Empty;

        /// <summary>
        /// 输出文件夹
        /// </summary>
        private string outputFolder = "Assets/Scene/DMVC/Runtime";

        /// <summary>
        /// 主滚动位置
        /// </summary>
        private Vector2 mainScroll;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MieMieFrameWork/DMVC/Execution Order")]
        public static void Open()
        {
            DMVCExecutionOrderWindow window = GetWindow<DMVCExecutionOrderWindow>("DMVC 执行顺序");
            window.minSize = new Vector2(420f, 520f);
        }

        private void OnEnable()
        {
            minSize = new Vector2(420f, 520f);
            RefreshWorldTypes();
            SetDefaultClassName();
            ScanBehaviourTypes();
        }

        private void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            EditorGUILayout.LabelField("DMVC 执行顺序", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (!DmvcLibraryChecker.DrawInstallGate())
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.HelpBox(
                "为什么需要执行顺序\n" +
                "同层模块默认按类名排序 可能不符合依赖关系\n" +
                "IBehaviourExecution 告诉框架每层先创建谁后创建谁\n\n" +
                "本窗口会生成执行顺序脚本 并自动注册到 WorldManager\n" +
                "生成后无需手写 Register 代码\n\n" +
                "完整启动示例\n" +
                "WorldManager.CreateWorld<你的World>()",
                MessageType.Info);

            EditorGUILayout.Space(6);
            DrawWorldSelector();

            generatedClassName = EditorGUILayout.TextField("生成类名", generatedClassName);
            outputFolder = EditorGUILayout.TextField("输出文件夹", outputFolder);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新 World", GUILayout.Height(24)))
                {
                    RefreshWorldTypes();
                    SetDefaultClassName();
                    ScanBehaviourTypes();
                }

                if (GUILayout.Button("重新扫描模块", GUILayout.Height(24)))
                    ScanBehaviourTypes();
            }

            EditorGUILayout.Space(8);
            DrawTypeList("Logic 控制层  越靠上越先 OnCreate", logicTypeList);
            EditorGUILayout.Space(6);
            DrawTypeList("Data 数据层  越靠上越先 OnCreate", dataTypeList);
            EditorGUILayout.Space(6);
            DrawTypeList("Message 消息层  越靠上越先 OnCreate", messageTypeList);

            EditorGUILayout.Space(10);
            if (GUILayout.Button("生成执行顺序脚本", GUILayout.Height(30)))
                GenerateExecutionOrderScript();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制 World 选择器
        /// </summary>
        private void DrawWorldSelector()
        {
            if (worldTypeList.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到 WorldModule 子类 请先用代码生成器创建 World", MessageType.Warning);
                return;
            }

            string[] options = worldTypeList.Select(t => t.FullName).ToArray();
            int nextIndex = EditorGUILayout.Popup("目标 World", selectedWorldIndex, options);
            if (nextIndex != selectedWorldIndex)
            {
                selectedWorldIndex = nextIndex;
                SetDefaultClassName();
                ScanBehaviourTypes();
            }
        }

        /// <summary>
        /// 绘制可排序类型列表
        /// </summary>
        private void DrawTypeList(string title, List<Type> typeList)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (typeList.Count == 0)
            {
                EditorGUILayout.HelpBox("当前 World 命名空间下没有对应模块", MessageType.Info);
                return;
            }

            for (int i = 0; i < typeList.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{i + 1}. {typeList[i].Name}");

                    GUI.enabled = i > 0;
                    if (GUILayout.Button("上移", GUILayout.Width(48)))
                        Swap(typeList, i, i - 1);

                    GUI.enabled = i < typeList.Count - 1;
                    if (GUILayout.Button("下移", GUILayout.Width(48)))
                        Swap(typeList, i, i + 1);

                    GUI.enabled = true;
                }
            }
        }

        /// <summary>
        /// 交换列表元素
        /// </summary>
        private static void Swap(List<Type> typeList, int a, int b)
        {
            Type temp = typeList[a];
            typeList[a] = typeList[b];
            typeList[b] = temp;
        }

        /// <summary>
        /// 刷新 World 列表
        /// </summary>
        private void RefreshWorldTypes()
        {
            worldTypeList.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in SafeGetTypes(assembly))
                {
                    if (type == null || type.IsAbstract)
                        continue;

                    Type worldModuleType = DmvcLibraryChecker.WorldModuleType;
                    if (worldModuleType == null)
                        continue;

                    if (worldModuleType.IsAssignableFrom(type) && type != worldModuleType)
                        worldTypeList.Add(type);
                }
            }

            worldTypeList.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            selectedWorldIndex = Mathf.Clamp(selectedWorldIndex, 0, Math.Max(0, worldTypeList.Count - 1));
        }

        /// <summary>
        /// 设置默认类名
        /// </summary>
        private void SetDefaultClassName()
        {
            if (worldTypeList.Count == 0)
            {
                generatedClassName = "NewWorldScriptExecutionOrder";
                return;
            }

            string worldName = worldTypeList[selectedWorldIndex].Name;
            generatedClassName = $"{worldName}ScriptExecutionOrder";
        }

        /// <summary>
        /// 扫描三类模块
        /// </summary>
        private void ScanBehaviourTypes()
        {
            logicTypeList.Clear();
            dataTypeList.Clear();
            messageTypeList.Clear();

            if (worldTypeList.Count == 0)
                return;

            Type targetWorldType = worldTypeList[selectedWorldIndex];
            string targetNamespace = targetWorldType.Namespace;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in SafeGetTypes(assembly))
                {
                    if (type == null || type.IsAbstract)
                        continue;

                    if (type.Namespace != targetNamespace)
                        continue;

                    Type logicType = DmvcLibraryChecker.LogicBehaviourType;
                    Type dataType = DmvcLibraryChecker.DataBehaviourType;
                    Type messageType = DmvcLibraryChecker.MessageBehaviourType;

                    if (logicType != null && logicType.IsAssignableFrom(type) && type != logicType)
                        logicTypeList.Add(type);

                    if (dataType != null && dataType.IsAssignableFrom(type) && type != dataType)
                        dataTypeList.Add(type);

                    if (messageType != null && messageType.IsAssignableFrom(type) && type != messageType)
                        messageTypeList.Add(type);
                }
            }

            logicTypeList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            dataTypeList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            messageTypeList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        /// <summary>
        /// 生成执行顺序脚本
        /// </summary>
        private void GenerateExecutionOrderScript()
        {
            if (worldTypeList.Count == 0)
            {
                EditorUtility.DisplayDialog("DMVC", "没有可用的 World 类型", "确定");
                return;
            }

            if (string.IsNullOrWhiteSpace(generatedClassName))
            {
                EditorUtility.DisplayDialog("DMVC", "生成类名不能为空", "确定");
                return;
            }

            if (!outputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("DMVC", "输出路径必须以 Assets 开头", "确定");
                return;
            }

            Type targetWorldType = worldTypeList[selectedWorldIndex];
            string targetNamespace = targetWorldType.Namespace;
            string worldTypeName = targetWorldType.Name;
            string className = generatedClassName.Trim();
            string content = BuildExecutionOrderContent(className, targetNamespace, worldTypeName);

            string absoluteOutputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, outputFolder);
            Directory.CreateDirectory(absoluteOutputFolder);
            string outputFile = Path.Combine(absoluteOutputFolder, $"{className}.cs");

            if (File.Exists(outputFile) &&
                !EditorUtility.DisplayDialog("DMVC", $"{className}.cs 已存在 是否覆盖", "覆盖", "取消"))
                return;

            File.WriteAllText(outputFile, content, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "DMVC",
                $"已生成\n{outputFolder}/{className}.cs\n\n脚本会在运行时自动注册\n直接调用 WorldManager.CreateWorld<{worldTypeName}>() 即可",
                "确定");
        }

        /// <summary>
        /// 构建执行顺序文件内容
        /// </summary>
        private string BuildExecutionOrderContent(string className, string targetNamespace, string worldTypeName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using MiMieDMVC;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(targetNamespace))
                sb.AppendLine($"namespace {targetNamespace}");

            if (!string.IsNullOrEmpty(targetNamespace))
                sb.AppendLine("{");

            string indent = string.IsNullOrEmpty(targetNamespace) ? string.Empty : "    ";

            sb.AppendLine($"{indent}public class {className} : IBehaviourExecution");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
            sb.AppendLine($"{indent}    private static void RegisterExecutionOrder()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        WorldManager.RegisterBehaviourExecution<{worldTypeName}>(() => new {className}());");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    private static readonly Type[] LogicBehaviourExecutionArr = {BuildTypeArray(logicTypeList, indent + "    ")}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    private static readonly Type[] DataBehaviourExecutionArr = {BuildTypeArray(dataTypeList, indent + "    ")}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    private static readonly Type[] MessageBehaviourExecutionArr = {BuildTypeArray(messageTypeList, indent + "    ")}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    public Type[] GetLogicBehaviourExecution()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        return LogicBehaviourExecutionArr;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    public Type[] GetDataBehaviourExecution()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        return DataBehaviourExecutionArr;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    public Type[] GetMessageBehaviourExecution()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        return MessageBehaviourExecutionArr;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(targetNamespace))
                sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 构建 Type 数组字面量
        /// </summary>
        private static string BuildTypeArray(List<Type> typeList, string indent)
        {
            if (typeList.Count == 0)
                return "new Type[] { };";

            var sb = new StringBuilder();
            sb.AppendLine("new Type[]");
            sb.AppendLine(indent + "{");
            foreach (Type type in typeList)
                sb.AppendLine($"{indent}    typeof({type.Name}),");
            sb.Append(indent + "}");
            return sb.ToString();
        }

        /// <summary>
        /// 安全获取程序集类型
        /// </summary>
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
    }
}
