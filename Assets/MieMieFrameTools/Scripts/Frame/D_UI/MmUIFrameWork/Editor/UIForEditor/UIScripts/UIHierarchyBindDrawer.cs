using System;
using System.Collections.Generic;
using MmUIFrameWork.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUITools.Editor
{
    /// <summary>
    /// UI 层级绑定按钮绘制
    /// </summary>
    [InitializeOnLoad]
    public static class UIHierarchyBindDrawer
    {
        /// <summary>
        /// 按钮宽度
        /// </summary>
        private const float ButtonWidth = 38f;

        /// <summary>
        /// 按钮间距
        /// </summary>
        private const float ButtonSpace = 2f;

        /// <summary>
        /// 右侧预留宽度
        /// </summary>
        private const float RightPadding = 2f;

        /// <summary>
        /// 最大绘制数量
        /// </summary>
        private const int MaxDrawCount = 5;

        /// <summary>
        /// 组件别名字典
        /// </summary>
        private static readonly Dictionary<string, string> ComponentAliasDict = new Dictionary<string, string>
        {
            { typeof(Transform).FullName, "Trans" },
            { typeof(RectTransform).FullName, "RT" },
            { typeof(Image).FullName, "Img" },
            { typeof(RawImage).FullName, "Raw" },
            { typeof(Button).FullName, "Btn" },
            { typeof(Text).FullName, "Text" },
            { typeof(Toggle).FullName, "Tg" },
            { typeof(Slider).FullName, "Slider" },
            { typeof(ScrollRect).FullName, "Scroll" },
            { typeof(InputField).FullName, "Input" },
            { typeof(Dropdown).FullName, "Drop" },
            { "TMPro.TextMeshProUGUI", "Tmp" },
            { "TMPro.TMP_InputField", "TmpInput" },
            { "TMPro.TMP_Dropdown", "TmpDrop" }
        };

        // 6000.4 起 Hierarchy 回调改为 EntityId 版本 6000.3 只有 instanceID 版本
#if UNITY_6000_4_OR_NEWER
        static UIHierarchyBindDrawer()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyWindowItemGUI;
        }

        private static void OnHierarchyWindowItemGUI(EntityId entityId, Rect selectionRect)
        {
            DrawHierarchyItem(EditorUtility.EntityIdToObject(entityId) as GameObject, selectionRect);
        }
#else
        static UIHierarchyBindDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }

        private static void OnHierarchyWindowItemGUI(int instanceId, Rect selectionRect)
        {
            DrawHierarchyItem(EditorUtility.InstanceIDToObject(instanceId) as GameObject, selectionRect);
        }
#endif

        private static void DrawHierarchyItem(GameObject gameObject, Rect selectionRect)
        {
            if (gameObject == null) return;

            if (!TryGetUIRoot(gameObject.transform, out Transform uiRoot, out Transform uiContent)) return;

            // 预制体根画绑定按钮总开关与反向同步
            if (gameObject.transform == uiRoot)
            {
                DrawRootBindToolbar(uiRoot.gameObject, selectionRect);
                return;
            }

            if (!IsSelfOrChildOf(gameObject.transform, uiContent)) return;

            var config = uiRoot.GetComponent<UIBindConfig>();
            if (config != null && !config.ShowHierarchyBindButtons) return;

            var bindableComponentList = GetBindableComponents(gameObject);
            if (bindableComponentList.Count == 0) return;

            float rightX = selectionRect.xMax - RightPadding;
            int drawCount = Mathf.Min(bindableComponentList.Count, MaxDrawCount);

            for (int i = 0; i < drawCount; i++)
            {
                var component = bindableComponentList[i];
                Type componentType = GetBindType(component);
                bool isBound = IsBound(uiRoot.gameObject, uiContent, gameObject.transform, componentType);
                float buttonX = rightX - (i + 1) * ButtonWidth - i * ButtonSpace;
                var buttonRect = new Rect(buttonX, selectionRect.y + 1f, ButtonWidth, selectionRect.height - 2f);

                Color cacheColor = GUI.backgroundColor;
                GUI.backgroundColor = isBound ? new Color(0.35f, 0.85f, 0.45f, 1f) : cacheColor;

                if (GUI.Button(buttonRect, GetComponentAlias(componentType), EditorStyles.miniButton))
                {
                    ToggleBind(uiRoot.gameObject, uiContent, gameObject.transform, componentType);
                }

                GUI.backgroundColor = cacheColor;
            }
        }

        /// <summary>
        /// 在 UI 根节点 Hierarchy 行绘制反向同步与绑定显隐开关
        /// </summary>
        private static void DrawRootBindToolbar(GameObject uiRoot, Rect selectionRect)
        {
            var config = uiRoot.GetComponent<UIBindConfig>();
            if (config != null && config.hideFlags != HideFlags.None)
            {
                config.hideFlags = HideFlags.None;
                EditorUtility.SetDirty(config);
            }

            bool showButtons = config == null || config.ShowHierarchyBindButtons;

            const float syncWidth = 48f;
            const float toggleWidth = 52f;
            const float buttonGap = 2f;
            float buttonY = selectionRect.y + 1f;
            float buttonHeight = selectionRect.height - 2f;

            var syncRect = new Rect(
                selectionRect.xMax - RightPadding - toggleWidth - buttonGap - syncWidth,
                buttonY,
                syncWidth,
                buttonHeight);
            var toggleRect = new Rect(
                selectionRect.xMax - RightPadding - toggleWidth,
                buttonY,
                toggleWidth,
                buttonHeight);

            Color cacheColor = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.35f, 0.65f, 0.95f, 1f);
            if (GUI.Button(syncRect, "反同步", EditorStyles.miniButton))
            {
                SyncBindFromGen(uiRoot);
            }

            GUI.backgroundColor = showButtons
                ? new Color(0.35f, 0.85f, 0.45f, 1f)
                : new Color(0.55f, 0.55f, 0.55f, 1f);

            if (GUI.Button(toggleRect, showButtons ? "Bind开" : "Bind关", EditorStyles.miniButton))
            {
                ToggleHierarchyBindVisibility(uiRoot, !showButtons);
            }

            GUI.backgroundColor = cacheColor;
        }

        /// <summary>
        /// 从 Gen 序列化引用反向同步 Hierarchy 勾选高亮
        /// </summary>
        private static void SyncBindFromGen(GameObject uiRoot)
        {
            int syncedCount = GenerateUITool.SyncBindConfigFromGen(uiRoot);
            if (syncedCount < 0) return;

            EditorUtility.DisplayDialog(
                "反向同步完成",
                syncedCount > 0
                    ? $"已从 Gen 同步 {syncedCount} 条绑定\nHierarchy 勾选颜色已刷新"
                    : "没有可同步的绑定项",
                "确定");
        }

        /// <summary>
        /// 切换 Hierarchy 绑定按钮显隐
        /// </summary>
        private static void ToggleHierarchyBindVisibility(GameObject uiRoot, bool show)
        {
            var config = uiRoot.GetComponent<UIBindConfig>();
            if (config == null)
            {
                config = Undo.AddComponent<UIBindConfig>(uiRoot);
            }

            config.hideFlags = HideFlags.None;
            Undo.RecordObject(config, "Toggle Hierarchy Bind Buttons");
            config.ShowHierarchyBindButtons = show;
            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(uiRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(config);
            EditorApplication.RepaintHierarchyWindow();
        }

        private static bool TryGetUIRoot(Transform target, out Transform uiRoot, out Transform uiContent)
        {
            uiRoot = null;
            uiContent = null;

            Transform current = target;
            while (current != null)
            {
                Transform content = current.Find("UIContent");
                if (content != null)
                {
                    uiRoot = current;
                    uiContent = content;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsSelfOrChildOf(Transform target, Transform parent)
        {
            Transform current = target;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }

            return false;
        }

        private static List<Component> GetBindableComponents(GameObject gameObject)
        {
            var componentList = new List<Component>();
            componentList.Add(gameObject.transform);

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null) continue;
                if (component is Transform) continue;
                if (component is CanvasRenderer) continue;
                if (component is UIBindConfig) continue;
                componentList.Add(component);
            }

            return componentList;
        }

        private static Type GetBindType(Component component)
        {
            if (component is RectTransform) return typeof(RectTransform);
            if (component is Transform) return typeof(Transform);
            return component.GetType();
        }

        private static bool IsBound(GameObject uiRoot, Transform uiContent, Transform target, Type componentType)
        {
            var config = uiRoot.GetComponent<UIBindConfig>();
            if (config == null) return false;

            string nodePath = GetNodePath(uiContent, target);
            string componentFullTypeName = GetComponentFullTypeName(componentType);

            return config.EditorBindItemList.Exists(item =>
                item.nodePath == nodePath && IsSameComponentType(item.componentFullTypeName, componentFullTypeName));
        }

        private static void ToggleBind(GameObject uiRoot, Transform uiContent, Transform target, Type componentType)
        {
            var config = uiRoot.GetComponent<UIBindConfig>();
            if (config == null)
            {
                config = Undo.AddComponent<UIBindConfig>(uiRoot);
            }

            config.hideFlags = HideFlags.None;

            string nodePath = GetNodePath(uiContent, target);
            string componentFullTypeName = GetComponentFullTypeName(componentType);
            int index = config.EditorBindItemList.FindIndex(item =>
                item.nodePath == nodePath && IsSameComponentType(item.componentFullTypeName, componentFullTypeName));

            Undo.RecordObject(config, "Toggle UI Bind");

            if (index >= 0)
            {
                config.EditorBindItemList.RemoveAt(index);
            }
            else
            {
                config.EditorBindItemList.Add(new UIBindItem
                {
                    nodePath = nodePath,
                    nodeName = target.name,
                    componentTypeName = componentType.Name,
                    componentFullTypeName = componentFullTypeName,
                    componentAssemblyName = componentType.Assembly.GetName().Name,
                    fieldName = CreateFieldName(target.name, componentType)
                });
            }

            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(uiRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(config);
            EditorApplication.RepaintHierarchyWindow();
        }

        private static string GetNodePath(Transform uiContent, Transform target)
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

        private static string GetComponentFullTypeName(Type componentType)
        {
            return componentType.FullName;
        }

        private static bool IsSameComponentType(string recordTypeName, string targetTypeName)
        {
            if (recordTypeName == targetTypeName) return true;
            if (targetTypeName == typeof(RectTransform).FullName && recordTypeName == typeof(Transform).FullName) return true;
            return false;
        }

        private static string GetComponentAlias(Type componentType)
        {
            string componentFullTypeName = GetComponentFullTypeName(componentType);
            if (ComponentAliasDict.TryGetValue(componentFullTypeName, out string alias))
                return alias;

            string typeName = componentType.Name;
            return typeName.Length <= 5 ? typeName : typeName.Substring(0, 5);
        }

        private static string CreateFieldName(string nodeName, Type componentType)
        {
            return SanitizeName(nodeName) + GetFieldSuffix(componentType);
        }

        private static string GetFieldSuffix(Type componentType)
        {
            if (componentType == typeof(RectTransform)) return nameof(RectTransform);
            if (componentType == typeof(Transform)) return nameof(Transform);
            return componentType.Name;
        }

        private static string SanitizeName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Node";

            var builder = new System.Text.StringBuilder();
            foreach (char c in rawName)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    builder.Append(c);
            }

            if (builder.Length == 0) return "Node";
            if (char.IsDigit(builder[0])) builder.Insert(0, "N");
            return char.ToUpper(builder[0]) + builder.ToString(1, builder.Length - 1);
        }
    }
}
