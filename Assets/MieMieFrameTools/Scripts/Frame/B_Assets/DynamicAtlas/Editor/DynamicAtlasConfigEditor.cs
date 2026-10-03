using System;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas.Editor
{
    /// <summary>
    /// 提供显式 Shader 绑定入口 不在运行时查找或兜底
    /// </summary>
    [CustomEditor(typeof(DynamicAtlasConfig))]
    public sealed class DynamicAtlasConfigEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 绘制配置与显式绑定按钮
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (((DynamicAtlasConfig)target).PaddingShader != null)
                return;
            EditorGUILayout.HelpBox("运行前需要显式绑定模块内置边缘扩展 Shader", MessageType.Warning);
            if (!GUILayout.Button("绑定内置 Shader"))
                return;
            var Shader = UnityEngine.Shader.Find("Hidden/MieMieFrameWork/DynamicAtlasPadding");
            if (Shader == null)
                throw new InvalidOperationException("未找到 DynamicAtlasPadding Shader 请检查模块 Shader 是否已导入");
            serializedObject.Update();
            serializedObject.FindProperty("paddingShader").objectReferenceValue = Shader;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
