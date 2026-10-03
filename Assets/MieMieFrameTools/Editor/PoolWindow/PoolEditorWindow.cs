using System;
using System.Collections.Generic;
using MieMieFrameWork;
using MieMieFrameWork.Pool;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.PoolEditor
{
    public class PoolEditorWindow : EditorWindow, MieMieFrameWork.Editor.ToolsCenter.IMieMieToolsEmbeddedWindow
    {
        private const string PrewarmPrefsKey = "PoolEditor.PrewarmPresets";
        private const string AutoPrewarmPrefsKey = "PoolEditor.AutoPrewarmOnPlay";

        private enum E_Tab
        {
            Dashboard,
            Prewarm
        }

        private E_Tab currentTab = E_Tab.Dashboard;
        private Vector2 scrollPos;
        private readonly List<GameObjPoolReporter> poolInfoList = new();
        private readonly List<PrewarmPresetEntry> prewarmPresetList = new();

        private GameObject prewarmPrefab;
        private int prewarmCount = 10;
        private int prewarmMaxSize = ObjectPool.DefaultMaxInactive;
        private int burstCount = 20;
        private bool autoPrewarmOnPlay;
        private double lastRefreshTime;

        #region 编辑器功能

        public static void Open()
        {
            var window = GetWindow<PoolEditorWindow>("对象池");
            window.minSize = new Vector2(480f, 360f);
            window.LoadPrefs();
        }

        private void OnEnable()
        {
            LoadPrefs();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SavePrefs();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && autoPrewarmOnPlay)
                EditorApplication.delayCall += RunAllPrewarmPresets;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying
                || currentTab != E_Tab.Dashboard)
                return;

            if (EditorApplication.timeSinceStartup - lastRefreshTime > 0.25d)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        public void DrawEmbeddedGUI()
        {
            OnGUI();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4);
            currentTab = (E_Tab)GUILayout.Toolbar(
                (int)currentTab,
                new[] { "实时监控", "预热工坊" });
            EditorGUILayout.Space(6);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            switch (currentTab)
            {
                case E_Tab.Dashboard:
                    DrawDashboardTab();
                    break;
                case E_Tab.Prewarm:
                    DrawPrewarmTab();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MieMie 对象池中枢", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Get/Release · 预热 · 容量上限 · 重复归还检测", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("刷新", GUILayout.Width(60f)))
                    Repaint();
                if (GUILayout.Button("定位 PoolRoot", GUILayout.Width(100f)))
                    PingPoolRoot();
            }
        }

        private void DrawDashboardTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后查看实时池状态", MessageType.Info);
                return;
            }

            PoolManager poolMgr = TryGetPoolManager();
            if (poolMgr == null)
            {
                EditorGUILayout.HelpBox("运行时未找到 PoolManager 服务", MessageType.Warning);
                return;
            }

            poolMgr.CollectGameObjPoolInfoList(poolInfoList);
            if (poolInfoList.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无 GameObject 池 先预热或 Get 一次", MessageType.None);
                return;
            }

            int totalActive = 0;
            int totalPooled = 0;
            int totalCreated = 0;
            for (int i = 0; i < poolInfoList.Count; i++)
            {
                GameObjPoolReporter info = poolInfoList[i];
                totalActive += info.ActiveCount;
                totalPooled += info.PooledCount;
                totalCreated += info.TotalCreated;
                DrawPoolInfoCard(info);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"汇总  借出 {totalActive}  闲置 {totalPooled}  存活 {totalCreated}  池数 {poolInfoList.Count}", EditorStyles.helpBox);
        }


        private void DrawPoolInfoCard(GameObjPoolReporter info)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(info.PrefabName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    Color old = GUI.color;
                    GUI.color = GetUsageColor(info.UsageRate);
                    EditorGUILayout.LabelField($"闲置 {info.PooledCount}/{info.MaxInactive}", GUILayout.Width(100f));
                    GUI.color = old;
                }

                int TotalLimit = info.MaxTotal > 0 ? info.MaxTotal : info.TotalCreated;
                DrawBar("借出", info.ActiveCount, TotalLimit, new Color(1f, 0.55f, 0.2f));
                DrawBar("闲置", info.PooledCount, info.MaxInactive, new Color(0.3f, 0.75f, 1f));
                DrawBar("存活", info.TotalCreated, TotalLimit, new Color(0.45f, 0.9f, 0.5f));
                string LimitText = info.MaxTotal > 0 ? info.MaxTotal.ToString() : "不限制";
                EditorGUILayout.LabelField($"Key {info.PoolKey}  借出 {info.ActiveCount}  总量上限 {LimitText}", EditorStyles.miniLabel);
            }
        }

        private static void DrawBar(string label, int value, int max, Color color)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(36f));
                Rect rect = GUILayoutUtility.GetRect(1f, 14f, GUILayout.ExpandWidth(true));
                float rate = max > 0 ? Mathf.Clamp01((float)value / max) : 0f;
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                Rect fill = new Rect(rect.x, rect.y, rect.width * rate, rect.height);
                EditorGUI.DrawRect(fill, color);
                GUI.Label(rect, $" {value}", EditorStyles.miniLabel);
            }
        }

        private static Color GetUsageColor(float rate)
        {
            if (rate >= 0.9f) return new Color(1f, 0.35f, 0.35f);
            if (rate >= 0.7f) return new Color(1f, 0.8f, 0.2f);
            return new Color(0.5f, 1f, 0.55f);
        }

        private void DrawPrewarmTab()
        {
            EditorGUILayout.LabelField("快速预热", EditorStyles.boldLabel);
            prewarmPrefab = (GameObject)EditorGUILayout.ObjectField("预制体", prewarmPrefab, typeof(GameObject), false);
            prewarmCount = EditorGUILayout.IntField("预热数量", Mathf.Max(0, prewarmCount));
            prewarmMaxSize = EditorGUILayout.IntField("缓存与总量上限", Mathf.Max(1, prewarmMaxSize));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("加入预设"))
                    AddPrewarmPreset();
                if (GUILayout.Button("立即预热") && Application.isPlaying)
                    RunPrewarm(prewarmPrefab, prewarmCount, prewarmMaxSize);
                if (GUILayout.Button("压力测试") && Application.isPlaying)
                    RunBurstTest(prewarmPrefab, burstCount, prewarmMaxSize);
            }

            burstCount = EditorGUILayout.IntField("压力连取数量", Mathf.Max(1, burstCount));
            EditorGUILayout.Space(6);
            autoPrewarmOnPlay = EditorGUILayout.ToggleLeft("进入 Play 时自动执行全部预设", autoPrewarmOnPlay);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("预设列表", EditorStyles.boldLabel);

            for (int i = prewarmPresetList.Count - 1; i >= 0; i--)
            {
                PrewarmPresetEntry entry = prewarmPresetList[i];
                GameObject prefab = LoadPrefab(entry.prefabGuid);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false, GUILayout.Width(140f));
                    EditorGUILayout.LabelField($"x{entry.count}  max{entry.maxSize}", GUILayout.Width(90f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("▶", GUILayout.Width(24f)) && Application.isPlaying)
                        RunPrewarm(prefab, entry.count, entry.maxSize);
                    if (GUILayout.Button("×", GUILayout.Width(24f)))
                        prewarmPresetList.RemoveAt(i);
                }
            }

            if (GUILayout.Button("保存预设"))
                SavePrefs();
        }

        private void AddPrewarmPreset()
        {
            if (prewarmPrefab == null)
                return;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prewarmPrefab));
            prewarmPresetList.Add(new PrewarmPresetEntry
            {
                prefabGuid = guid,
                count = prewarmCount,
                maxSize = prewarmMaxSize
            });
            SavePrefs();
        }

        private void RunPrewarm(GameObject prefab, int count, int maxSize)
        {
            if (prefab == null)
                return;

            PoolManager poolMgr = TryGetPoolManager();
            if (poolMgr == null)
                return;

            PoolHandle poolHandle = poolMgr.GetPool(prefab, maxSize);
            poolHandle.Prewarm(count);
            Debug.Log($"[PoolEditor] 预热完成 {prefab.name} x{count}");
        }

        /// <summary>
        /// 连续租借测试容量 回收所有本次实例并汇总获取与清理错误
        /// </summary>
        private void RunBurstTest(GameObject prefab, int count, int maxSize)
        {
            if (prefab == null)
                return;

            PoolManager poolMgr = TryGetPoolManager();
            if (poolMgr == null)
                return;

            var Handle = poolMgr.GetPool(prefab, maxSize);
            int Success = 0;
            using (UnityEngine.Pool.ListPool<PressureLease>.Get(out var BorrowedList))
            using (UnityEngine.Pool.ListPool<Exception>.Get(out var ErrorList))
            {
                try
                {
                    for (int Index = 0; Index < count; Index++)
                    {
                        var Value = Handle.Get(activate: false);
                        if (Value == null)
                            break;
                        BorrowedList.Add(new PressureLease(Value, Handle.GetLeaseVersion(Value)));
                        Success++;
                    }
                }
                catch (Exception Exception)
                {
                    ErrorList.Add(Exception);
                }
                finally
                {
                    foreach (var Lease in BorrowedList)
                    {
                        try
                        {
                            if (!Handle.TryRelease(Lease.Instance, Lease.Version))
                                throw new InvalidOperationException($"[PoolEditor] 压力实例租借已失效 {prefab.name}");
                        }
                        catch (Exception Exception)
                        {
                            ErrorList.Add(Exception);
                            try
                            {
                                Handle.DiscardAfterFailedRelease(Lease.Instance, Lease.Version);
                            }
                            catch (Exception CleanupException)
                            {
                                ErrorList.Add(CleanupException);
                            }
                        }
                    }
                }
                if (ErrorList.Count > 0)
                    throw new AggregateException($"[PoolEditor] 压力操作失败 {prefab.name}", ErrorList);
            }

            Debug.Log($"[PoolEditor] 压力连取 {prefab.name} 请求 {count} 成功 {Success} 已归还全部实例");
        }

        private void RunAllPrewarmPresets()
        {
            if (!Application.isPlaying)
                return;

            for (int i = 0; i < prewarmPresetList.Count; i++)
            {
                PrewarmPresetEntry entry = prewarmPresetList[i];
                GameObject prefab = LoadPrefab(entry.prefabGuid);
                RunPrewarm(prefab, entry.count, entry.maxSize);
            }
        }

        private static PoolManager TryGetPoolManager()
        {
            if (!Application.isPlaying || ModuleHub.Instance == null)
                return null;

            try
            {
                return PoolManager.Instance;
            }
            catch
            {
                return null;
            }
        }

        private static void PingPoolRoot()
        {
            PoolManager poolMgr = TryGetPoolManager();
            if (poolMgr == null)
            {
                EditorUtility.DisplayDialog("对象池", "运行时未找到 PoolManager 服务", "确定");
                return;
            }

            if (poolMgr.PoolRoot == null)
            {
                EditorUtility.DisplayDialog("对象池", "PoolRoot 未配置", "确定");
                return;
            }

            Selection.activeGameObject = poolMgr.PoolRoot.gameObject;
            EditorGUIUtility.PingObject(poolMgr.PoolRoot.gameObject);
        }

        private static GameObject LoadPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private void LoadPrefs()
        {
            autoPrewarmOnPlay = EditorPrefs.GetBool(AutoPrewarmPrefsKey, false);
            string json = EditorPrefs.GetString(PrewarmPrefsKey, string.Empty);
            prewarmPresetList.Clear();
            if (string.IsNullOrEmpty(json))
                return;

            PrewarmPresetWrapper wrapper = JsonUtility.FromJson<PrewarmPresetWrapper>(json);
            if (wrapper?.items != null)
                prewarmPresetList.AddRange(wrapper.items);
        }

        private void SavePrefs()
        {
            EditorPrefs.SetBool(AutoPrewarmPrefsKey, autoPrewarmOnPlay);
            var wrapper = new PrewarmPresetWrapper { items = prewarmPresetList.ToArray() };
            EditorPrefs.SetString(PrewarmPrefsKey, JsonUtility.ToJson(wrapper));
        }

        #endregion

        private readonly struct PressureLease
        {
            public GameObject Instance { get; }
            public ulong Version { get; }

            /// <summary>
            /// 保存本次压力实例与租借轮次
            /// </summary>
            public PressureLease(GameObject instance, ulong version)
            {
                Instance = instance;
                Version = version;
            }
        }

        [Serializable]
        private class PrewarmPresetEntry
        {
            public string prefabGuid;
            public int count = 10;
            public int maxSize = ObjectPool.DefaultMaxInactive;
        }

        [Serializable]
        private class PrewarmPresetWrapper
        {
            public PrewarmPresetEntry[] items;
        }

    }
}
