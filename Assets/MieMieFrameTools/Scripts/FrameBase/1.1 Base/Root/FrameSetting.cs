using UnityEditor;


namespace MieMieFrameWork
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using MieMieFrameWork.Pool;

    /// <summary>
    /// 游戏全局配置 - 框架级别的设置和配置管理
    /// 包含对象池配置、性能设置等
    /// </summary>
    [CreateAssetMenu(fileName = "FrameSetting", menuName = "MieMieFrameTools/Config/FrameSetting")]
    public class FrameSetting : SerializedScriptableObject
    {
        #region 对象池配置

        [TabGroup("对象池配置"), LabelText("最后更新时间")]
        [SerializeField, ReadOnly]
        private string lastUpdateTime = "";

        [TabGroup("对象池配置"), LabelText("进入Play时自动刷新")]
        [SerializeField]
        private bool autoRefreshOnPlay = true;

        [TabGroup("对象池配置"), LabelText("对象池类型列表")]
        [SerializeField, ReadOnly, Tooltip("运行时自动生成，包含所有带有[Pool]特性的类型")]
        private List<string> poolTypeNames = new();
        //运行时对象池类型集合
        public HashSet<Type> PoolCacheSet { get; private set; } = new();

        #endregion

        #region 性能设置

        [field: SerializeField, TabGroup("性能设置"), LabelText("目标帧率"), Range(30, 120)]
        public int TargetFrameRate { get; private set; }

        [field: SerializeField, TabGroup("性能设置"), LabelText("垂直同步")]
        public bool EnableVSync { get; private set; }

        #endregion

        #region 存档设置

        [field: SerializeField, TabGroup("存档设置"), LabelText("启用自动存档")]
        public bool EnableAutoSave { get; private set; } = true;

        [field: SerializeField, TabGroup("存档设置"), LabelText("自动存档间隔(分钟)"), Range(1, 60)]
        public float AutoSaveIntervalMinutes { get; private set; } = 20f;

        [field: SerializeField, TabGroup("存档设置"), LabelText("启用存档加密")]
        public bool EnableSaveEncryption { get; private set; } = true;

        [field: SerializeField, TabGroup("存档设置"), LabelText("最大存档槽数量"), Range(1, 20)]
        public int MaxSaveSlots { get; private set; } = 10;

        [field: SerializeField, TabGroup("存档设置"), LabelText("存档压缩")]
        public bool EnableSaveCompression { get; private set; } = true;

        // 转换为秒的便捷属性
        public float AutoSaveIntervalSeconds => AutoSaveIntervalMinutes * 60f;

        /// <summary>
        /// 运行时设置自动保存间隔（分钟）
        /// </summary>
        /// <param name="minutes">间隔时间（分钟）</param>
        public void SetAutoSaveInterval(float minutes)
        {
            //钳制最大为两小时
            AutoSaveIntervalMinutes = Mathf.Clamp(minutes, 1f, 120f);
            Debug.Log($"[GameSetting] 自动保存间隔已设置为 {AutoSaveIntervalMinutes} 分钟");
        }

        /// <summary>
        /// 运行时设置是否启用自动保存
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetAutoSaveEnabled(bool enabled)
        {
            EnableAutoSave = enabled;
            Debug.Log($"[GameSetting] 自动保存已{(enabled ? "启用" : "禁用")}");
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化配置系统
        /// </summary>
        public void Initialize()
        {
            try
            {
                // 初始化对象池配置
                InitializePoolRuntime();

                // 应用性能设置
                ApplyPerSettingRuntime();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSetting] 配置初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 初始化对象池配置
        /// </summary>
        private void InitializePoolRuntime()
        {
            PoolCacheSet.Clear();
            foreach (string typeName in poolTypeNames)
            {
                try
                {
                    Type type = Type.GetType(typeName);
                    if (type != null)
                    {
                        PoolCacheSet.Add(type);
                    }
                    else
                    {
                        Debug.LogWarning($"[GameSetting] 无法找到类型: {typeName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameSetting] 初始化对象池类型 {typeName} 失败: {ex.Message}");
                }
            }

            Debug.Log($"[GameSetting] 对象池配置加载完成: {PoolCacheSet.Count} 个类型");
        }

        /// <summary>
        /// 应用性能设置 
        /// </summary>
        private void ApplyPerSettingRuntime()
        {
            //目标帧率
            Application.targetFrameRate = TargetFrameRate;
            //垂直同步
            QualitySettings.vSyncCount = EnableVSync ? 1 : 0;

        }

        #endregion



        #region 编辑器功能

#if UNITY_EDITOR
        /// <summary>
        /// 验证对象池之中的有效类型个数
        /// </summary>
        [TabGroup("编辑器工具"), Button("验证配置", ButtonSizes.Medium), GUIColor(0.7f, 0.7f, 1f), PropertyOrder(0)]
        public void EditorValidateConfiguration()
        {
            int validTypes = 0;// 有效类型
            int invalidTypes = 0;// 无效类型

            foreach (string typeName in poolTypeNames)
            {
                Type type = Type.GetType(typeName);
                if (type != null)
                {
                    validTypes++;
                }
                else
                {
                    invalidTypes++;
                }
            }

            Debug.Log($"[GameSetting] 配置验证完成 - 有效: {validTypes}, 无效: {invalidTypes}");
        }

        [TabGroup("编辑器工具"), Button("重置所有配置", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.7f), PropertyOrder(1)]
        public void EditorResetConfiguration()
        {
            if (EditorUtility.DisplayDialog("重置配置", "确定要重置所有配置吗？这将清除当前的对象池设置。", "确定", "取消"))
            {
                poolTypeNames.Clear();
                lastUpdateTime = "";

                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();

                Debug.Log("[GameSetting] 配置已重置");
            }
        }
        [TabGroup("对象池配置"), Button("刷新对象池配置", ButtonSizes.Large), GUIColor(0.7f, 1f, 0.7f)]
        public void EditorRefreshPoolConfiguration()
        {
            EditorRefreshPoolTypes();
            Debug.Log("[GameSetting] 对象池配置已刷新");
        }


        /// <summary>
        /// 刷新对象池类型列表
        /// </summary>
        private void EditorRefreshPoolTypes()
        {
            poolTypeNames.Clear();

            var poolTypes = TypeCache.GetTypesWithAttribute<PoolAttribute>();
            foreach (Type type in poolTypes)
            {
                poolTypeNames.Add(type.AssemblyQualifiedName);
            }

            lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeRefresh()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            ModuleHub root = null;
#if UNITY_2023_1_OR_NEWER
            root = UnityEngine.Object.FindFirstObjectByType<ModuleHub>();
#elif UNITY_2022_2_OR_NEWER
			root = UnityEngine.Object.FindAnyObjectByType<GameRoot>();
#else
			root = UnityEngine.Object.FindObjectOfType<GameRoot>();
#endif
            var gameSetting = root != null ? root.FrameSetting : null;
            if (gameSetting == null)
            {
                return;
            }

            if (gameSetting.autoRefreshOnPlay)
            {
                gameSetting.EditorRefreshPoolConfiguration();
            }
        }
#endif
        #endregion
    }
}

