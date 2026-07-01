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
    /// DMVC 代码生成器
    /// </summary>
    public class DMVCCodeGeneratorWindow : EditorWindow
    {
        private enum BehaviourInterface
        {
            IDataBehaviour,
            IMessageBehaviour,
            ILogicBehaviour
        }

        /// <summary>
        /// World 类名
        /// </summary>
        private string worldClassName = "NewWorld";

        /// <summary>
        /// World 命名空间
        /// </summary>
        private string worldNamespace = "Game.DMVC";

        /// <summary>
        /// World 输出目录
        /// </summary>
        private string worldOutputFolder = "Assets/Scene/DMVC/World";

        /// <summary>
        /// 已扫描 World 类型
        /// </summary>
        private readonly List<Type> worldTypeList = new List<Type>();

        /// <summary>
        /// 选中 World 索引
        /// </summary>
        private int selectedWorldIndex;

        /// <summary>
        /// 模块类名
        /// </summary>
        private string moduleClassName = "NewDataMgr";

        /// <summary>
        /// 模块输出目录
        /// </summary>
        private string moduleOutputFolder = "Assets/Scene/DMVC/Modules";

        /// <summary>
        /// 选中接口类型
        /// </summary>
        private BehaviourInterface selectedInterface = BehaviourInterface.IDataBehaviour;

        /// <summary>
        /// 主滚动位置
        /// </summary>
        private Vector2 mainScroll;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MieMieFrameWork/DMVC/Code Generator")]
        public static void Open()
        {
            DMVCCodeGeneratorWindow window = GetWindow<DMVCCodeGeneratorWindow>("DMVC 代码生成器");
            window.minSize = new Vector2(420f, 480f);
        }

        private void OnEnable()
        {
            minSize = new Vector2(420f, 480f);
            RefreshWorldTypes();
        }

        private void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            EditorGUILayout.LabelField("DMVC 代码生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (!DmvcLibraryChecker.DrawInstallGate())
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.HelpBox(
                "DMVC 是什么\n" +
                "D = Data 数据层 存状态配置\n" +
                "M = Message 消息层 收发事件通知\n" +
                "Logic = 控制层 写玩法流程\n" +
                "World = 容器 统一管理三层模块生命周期\n\n" +
                "推荐流程\n" +
                "1 生成 World 类\n" +
                "2 生成 Data Message Logic 模块类\n" +
                "3 用执行顺序窗口排序并生成 IBehaviourExecution\n" +
                "4 游戏启动时调用 WorldManager.CreateWorld<你的World>()",
                MessageType.Info);

            EditorGUILayout.Space(8);
            DrawWorldTemplateSection();

            EditorGUILayout.Space(12);
            DrawModuleTemplateSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制 World 模板区
        /// </summary>
        private void DrawWorldTemplateSection()
        {
            EditorGUILayout.LabelField("步骤 1  生成 World 容器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "World 是一个逻辑世界入口\n" +
                "同命名空间下的 Data Message Logic 会在 CreateWorld 时被自动扫描并实例化\n" +
                "命名空间建议按功能域划分 例如 Game.Battle Game.Lobby",
                MessageType.None);

            worldClassName = EditorGUILayout.TextField("World 类名", worldClassName);
            worldNamespace = EditorGUILayout.TextField("命名空间", worldNamespace);
            worldOutputFolder = EditorGUILayout.TextField("输出文件夹", worldOutputFolder);

            if (GUILayout.Button("生成 World 类", GUILayout.Height(28)))
                GenerateWorldTemplate();
        }

        /// <summary>
        /// 绘制模块模板区
        /// </summary>
        private void DrawModuleTemplateSection()
        {
            EditorGUILayout.LabelField("步骤 2  生成 Data Message Logic 模块", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "模块必须和 World 处于同一命名空间 才会被自动扫描\n" +
                "Data 适合 PlayerDataMgr ConfigMgr\n" +
                "Message 适合 BattleMessageHub\n" +
                "Logic 适合 BattleFlowCtrl\n" +
                "初始化顺序固定为 Data -> Message -> Logic",
                MessageType.None);

            if (worldTypeList.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有 World 子类 请先生成 World 或手动编写后点刷新", MessageType.Warning);
                if (GUILayout.Button("刷新 World 列表", GUILayout.Height(24)))
                    RefreshWorldTypes();
                return;
            }

            string[] worldOptions = worldTypeList.Select(t => t.FullName).ToArray();
            selectedWorldIndex = EditorGUILayout.Popup("目标 World", selectedWorldIndex, worldOptions);
            selectedWorldIndex = Mathf.Clamp(selectedWorldIndex, 0, worldTypeList.Count - 1);

            Type targetWorldType = worldTypeList[selectedWorldIndex];
            string targetNamespace = targetWorldType.Namespace ?? string.Empty;
            EditorGUILayout.LabelField("自动命名空间", string.IsNullOrEmpty(targetNamespace) ? "<全局>" : targetNamespace);

            moduleClassName = EditorGUILayout.TextField("模块类名", moduleClassName);
            selectedInterface = (BehaviourInterface)EditorGUILayout.EnumPopup("模块层级", selectedInterface);
            moduleOutputFolder = EditorGUILayout.TextField("输出文件夹", moduleOutputFolder);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新 World 列表", GUILayout.Height(28)))
                    RefreshWorldTypes();

                if (GUILayout.Button("生成模块类", GUILayout.Height(28)))
                    GenerateModuleTemplate(targetNamespace);
            }
        }

        /// <summary>
        /// 生成 World 文件
        /// </summary>
        private void GenerateWorldTemplate()
        {
            if (string.IsNullOrWhiteSpace(worldClassName))
            {
                EditorUtility.DisplayDialog("DMVC", "World 类名不能为空", "确定");
                return;
            }

            if (!worldOutputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("DMVC", "输出路径必须以 Assets 开头", "确定");
                return;
            }

            string className = worldClassName.Trim();
            string namespaceName = worldNamespace.Trim();
            string content = BuildWorldContent(className, namespaceName);
            string absoluteOutputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, worldOutputFolder);
            Directory.CreateDirectory(absoluteOutputFolder);
            string outputFile = Path.Combine(absoluteOutputFolder, $"{className}.cs");

            if (File.Exists(outputFile) &&
                !EditorUtility.DisplayDialog("DMVC", $"{className}.cs 已存在 是否覆盖", "覆盖", "取消"))
                return;

            File.WriteAllText(outputFile, content, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            RefreshWorldTypes();
            EditorUtility.DisplayDialog("DMVC", $"已生成\n{worldOutputFolder}/{className}.cs\n\n下一步请打开执行顺序窗口", "确定");
        }

        /// <summary>
        /// 生成模块文件
        /// </summary>
        private void GenerateModuleTemplate(string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(moduleClassName))
            {
                EditorUtility.DisplayDialog("DMVC", "模块类名不能为空", "确定");
                return;
            }

            if (!moduleOutputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("DMVC", "输出路径必须以 Assets 开头", "确定");
                return;
            }

            string className = moduleClassName.Trim();
            string interfaceName = selectedInterface.ToString();
            string content = BuildModuleContent(className, namespaceName, interfaceName);
            string absoluteOutputFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, moduleOutputFolder);
            Directory.CreateDirectory(absoluteOutputFolder);
            string outputFile = Path.Combine(absoluteOutputFolder, $"{className}.cs");

            if (File.Exists(outputFile) &&
                !EditorUtility.DisplayDialog("DMVC", $"{className}.cs 已存在 是否覆盖", "覆盖", "取消"))
                return;

            File.WriteAllText(outputFile, content, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("DMVC", $"已生成\n{moduleOutputFolder}/{className}.cs", "确定");
        }

        /// <summary>
        /// 构建 World 模板
        /// </summary>
        private static string BuildWorldContent(string generatedClassName, string generatedNamespace)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using MiMieDMVC;");
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

        /// <summary>
        /// 构建模块模板
        /// </summary>
        private static string BuildModuleContent(string generatedClassName, string generatedNamespace, string interfaceName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using MiMieDMVC;");
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

        /// <summary>
        /// 刷新 World 类型列表
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
