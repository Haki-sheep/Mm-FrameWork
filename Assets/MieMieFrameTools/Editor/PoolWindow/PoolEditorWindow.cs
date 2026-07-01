using System;
using System.Collections.Generic;
using System.Linq;
using MieMieFrameWork;
using MieMieFrameWork.Pool;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.PoolEditor
{
    public class PoolEditorWindow : EditorWindow
    {
        private const string PrewarmPrefsKey = "PoolEditor.PrewarmPresets";
        private const string AutoPrewarmPrefsKey = "PoolEditor.AutoPrewarmOnPlay";

        private enum E_Tab
        {
            Dashboard,
            Prewarm,
            Radar,
            Scan
        }

        private E_Tab currentTab = E_Tab.Dashboard;
        private Vector2 scrollPos;
        private readonly List<GameObjPoolReporter> poolInfoList = new();
        private readonly List<PrewarmPresetEntry> prewarmPresetList = new();
        private readonly List<PoolRadarEntry> radarEntryList = new();
        private readonly List<string> poolableTypeNameList = new();

        private GameObject prewarmPrefab;
        private int prewarmCount = 10;
        private int prewarmMaxSize = 50;
        private string prewarmPath = string.Empty;
        private int burstCount = 20;
        private bool autoPrewarmOnPlay;
        private double lastRefreshTime;

        [MenuItem("Tools/MieMieFrameWork/对象池")]
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
            if (!Application.isPlaying || currentTab != E_Tab.Dashboard)
                return;

            if (EditorApplication.timeSinceStartup - lastRefreshTime > 0.25d)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4);
            currentTab = (E_Tab)GUILayout.Toolbar((int)currentTab, new[] { "实时监控", "预热工坊", "场景雷达", "IPoolable扫描" });
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
                case E_Tab.Radar:
                    DrawRadarTab();
                    break;
                case E_Tab.Scan:
                    DrawScanTab();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MieMie 对象池中枢", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Get/Push · IPoolable · 预热 · 容量上限 · 重复归还检测", EditorStyles.miniLabel);

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
                EditorGUILayout.HelpBox("场景中未找到 PoolManager", MessageType.Warning);
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
            EditorGUILayout.LabelField($"汇总  借出 {totalActive}  闲置 {totalPooled}  累计 {totalCreated}  池数 {poolInfoList.Count}", EditorStyles.helpBox);
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
                    EditorGUILayout.LabelField($"{info.TotalCreated}/{info.MaxSize}", GUILayout.Width(70f));
                    GUI.color = old;
                }

                DrawBar("借出", info.ActiveCount, info.MaxSize, new Color(1f, 0.55f, 0.2f));
                DrawBar("闲置", info.PooledCount, info.MaxSize, new Color(0.3f, 0.75f, 1f));
                DrawBar("容量", info.TotalCreated, info.MaxSize, new Color(0.45f, 0.9f, 0.5f));
                EditorGUILayout.LabelField($"Key {info.PoolKey}  借出 {info.ActiveCount}  闲置 {info.PooledCount}", EditorStyles.miniLabel);
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
            prewarmMaxSize = EditorGUILayout.IntField("池上限", Mathf.Max(1, prewarmMaxSize));
            prewarmPath = EditorGUILayout.TextField("Addressable路径", prewarmPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("加入预设"))
                    AddPrewarmPreset();
                if (GUILayout.Button("立即预热") && Application.isPlaying)
                    RunPrewarm(prewarmPrefab, prewarmCount, prewarmMaxSize, prewarmPath);
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
                    if (!string.IsNullOrEmpty(entry.path))
                        EditorGUILayout.LabelField(entry.path, EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("▶", GUILayout.Width(24f)) && Application.isPlaying)
                        RunPrewarm(prefab, entry.count, entry.maxSize, entry.path);
                    if (GUILayout.Button("×", GUILayout.Width(24f)))
                        prewarmPresetList.RemoveAt(i);
                }
            }

            if (GUILayout.Button("保存预设"))
                SavePrefs();
        }

        private void DrawRadarTab()
        {
            EditorGUILayout.LabelField("场景雷达  借出中的池对象", EditorStyles.boldLabel);
            if (GUILayout.Button("扫描当前场景"))
                ScanScenePoolMembers();

            if (radarEntryList.Count == 0)
            {
                EditorGUILayout.HelpBox("点击扫描查找带 PoolMember 的活跃对象", MessageType.None);
                return;
            }

            for (int i = 0; i < radarEntryList.Count; i++)
            {
                PoolRadarEntry entry = radarEntryList[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(entry.Name, GUILayout.Width(160f));
                    EditorGUILayout.LabelField($"PoolKey {entry.PoolKey}", GUILayout.Width(100f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("定位", GUILayout.Width(48f)) && entry.Target != null)
                    {
                        Selection.activeGameObject = entry.Target;
                        EditorGUIUtility.PingObject(entry.Target);
                    }
                }
            }
        }

        private void DrawScanTab()
        {
            EditorGUILayout.LabelField("项目扫描  IPoolable 实现类", EditorStyles.boldLabel);
            if (GUILayout.Button("扫描 Assets 下所有脚本"))
                ScanPoolableTypes();

            if (poolableTypeNameList.Count == 0)
            {
                EditorGUILayout.HelpBox("扫描后列出所有实现 IPoolable 的类型", MessageType.None);
                return;
            }

            for (int i = 0; i < poolableTypeNameList.Count; i++)
                EditorGUILayout.LabelField("• " + poolableTypeNameList[i]);
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
                maxSize = prewarmMaxSize,
                path = prewarmPath
            });
            SavePrefs();
        }

        private void RunPrewarm(GameObject prefab, int count, int maxSize, string path)
        {
            if (prefab == null)
                return;

            PoolManager poolMgr = TryGetPoolManager();
            if (poolMgr == null)
                return;

            if (!string.IsNullOrWhiteSpace(path))
                poolMgr.RegisterPoolPath(path, prefab);

            poolMgr.Prewarm(prefab, count, maxSize);
            Debug.Log($"[PoolEditor] 预热完成 {prefab.name} x{count}");
        }

        private void RunBurstTest(GameObject prefab, int count, int maxSize)
        {
            if (prefab == null)
                return;

            PoolManager poolMgr = TryGetPoolManager();
            if (poolMgr == null)
                return;

            int success = 0;
            for (int i = 0; i < count; i++)
            {
                if (poolMgr.GetGameObj(prefab, null, maxSize) != null)
                    success++;
            }

            Debug.Log($"[PoolEditor] 压力连取 {prefab.name} 请求 {count} 成功 {success}");
        }

        private void RunAllPrewarmPresets()
        {
            if (!Application.isPlaying)
                return;

            for (int i = 0; i < prewarmPresetList.Count; i++)
            {
                PrewarmPresetEntry entry = prewarmPresetList[i];
                GameObject prefab = LoadPrefab(entry.prefabGuid);
                RunPrewarm(prefab, entry.count, entry.maxSize, entry.path);
            }
        }

        private void ScanScenePoolMembers()
        {
            radarEntryList.Clear();
            PoolMember[] memberList = FindObjectsByType<PoolMember>(FindObjectsInactive.Include);
            for (int i = 0; i < memberList.Length; i++)
            {
                PoolMember member = memberList[i];
                if (!member.gameObject.activeInHierarchy)
                    continue;

                radarEntryList.Add(new PoolRadarEntry
                {
                    Name = member.gameObject.name,
                    PoolKey = member.PoolKey,
                    Target = member.gameObject
                });
            }
        }

        private void ScanPoolableTypes()
        {
            poolableTypeNameList.Clear();
            string[] guids = AssetDatabase.FindAssets("t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".cs") || path.Contains("/Editor/"))
                    continue;

                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null)
                    continue;

                Type type = script.GetClass();
                if (type != null && typeof(IPoolable).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    poolableTypeNameList.Add(type.FullName);
            }

            poolableTypeNameList.Sort();
        }

        private static PoolManager TryGetPoolManager()
        {
            if (!Application.isPlaying || ModuleHub.Instance == null)
                return null;

            try
            {
                return ModuleHub.Instance.GetManager<PoolManager>();
            }
            catch
            {
                return null;
            }
        }

        private static void PingPoolRoot()
        {
            PoolManager poolMgr = UnityEngine.Object.FindAnyObjectByType<PoolManager>();
            if (poolMgr == null)
            {
                EditorUtility.DisplayDialog("对象池", "场景中未找到 PoolManager", "确定");
                return;
            }

            Selection.activeGameObject = poolMgr.gameObject;
            EditorGUIUtility.PingObject(poolMgr.gameObject);
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

        [Serializable]
        private class PrewarmPresetEntry
        {
            public string prefabGuid;
            public int count = 10;
            public int maxSize = 50;
            public string path;
        }

        [Serializable]
        private class PrewarmPresetWrapper
        {
            public PrewarmPresetEntry[] items;
        }

        private class PoolRadarEntry
        {
            public string Name;
            public EntityId PoolKey;
            public GameObject Target;
        }
    }
}
