using System;
using System.Collections.Generic;
using MieMieFrameWork.UI;
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

        static UIHierarchyBindDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }

        private static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;

            if (!TryGetUIRoot(gameObject.transform, out Transform uiRoot, out Transform uiContent)) return;
            if (!IsSelfOrChildOf(gameObject.transform, uiContent)) return;

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
                config.hideFlags = HideFlags.HideInInspector;
            }

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
