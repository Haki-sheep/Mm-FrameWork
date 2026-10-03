using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MieMieUIFrameWork.UI.FloatingText
{
    /// <summary>
    /// 跳字测试 A 日常 B 压测4k C 压测1万 D 压测10万
    /// </summary>
    public class FloatingTextTestSpawner : MonoBehaviour
    {
        /// <summary>
        /// 标准压测总时长 含预热 行业自动化压测常用 30s 窗口
        /// </summary>
        private const float ReportTotalSeconds = 30f;

        /// <summary>
        /// 预热秒数 灌满池后再计入统计
        /// </summary>
        private const float ReportWarmupSeconds = 3f;

        /// <summary>
        /// 十万级压测预热秒数 灌池更久
        /// </summary>
        private const float ReportWarmupSeconds100k = 6f;

        private enum ETestMode
        {
            Off = 0,
            Daily = 1,
            StressReport = 2
        }

        private enum EFpsCap
        {
            Cap60 = 60,
            Cap120 = 120
        }

        [SerializeField]
        private FloatingTextWorld m_World;

        [SerializeField]
        private float m_Radius = 8f;

        [SerializeField]
        private int m_DailyTargetActive = 80;

        [SerializeField]
        private int m_DailySpawnPerSecond = 60;

        [SerializeField]
        private int m_StressTargetActive = 4096;

        [SerializeField]
        private int m_Stress10kTargetActive = 10000;

        [SerializeField]
        private int m_Stress100kTargetActive = 100000;

        [SerializeField]
        private int m_StressSpawnPerSecond = 6000;

        [SerializeField]
        private int m_MaxSpawnPerFrame = 3000;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_CritChance = 0.25f;

        [SerializeField]
        private EFpsCap m_FpsCap = EFpsCap.Cap60;

        private ETestMode m_Mode = ETestMode.Off;
        private int m_ReportTargetActive = 4096;
        private float m_SpawnAcc;
        private float m_FpsAccum;
        private int m_FpsFrames;
        private float m_FpsTimer;
        private float m_AvgFps;
        private float m_MinFps = float.MaxValue;
        private float m_MaxFps;

        private bool m_ReportRunning;
        private float m_ReportElapsed;
        private readonly List<float> m_FrameDtList = new List<float>(4096);
        private long m_ActiveSum;
        private int m_ActiveSamples;
        private int m_ActiveMin = int.MaxValue;
        private int m_ActiveMax;
        private int m_GlyphSum;
        private string m_LastReportSummary = string.Empty;

        #region 测试运行与统计

        private void Reset()
        {
            var manager = FindFirstObjectByType<FloatingTextManager>();
            if (manager != null && manager.World != null)
            {
                m_World = manager.World;
                return;
            }

            m_World = FindFirstObjectByType<FloatingTextWorld>();
        }

        private void Awake()
        {
            ApplyFpsCap();
            ResolveWorld();
        }

        private void Start()
        {
            ResolveWorld();
        }

        private void ResolveWorld()
        {
            if (m_World != null) return;

            if (FloatingTextManager.Instance != null)
            {
                m_World = FloatingTextManager.Instance.World;
            }

            if (m_World == null)
            {
                m_World = FindFirstObjectByType<FloatingTextWorld>();
            }
        }

        private void ApplyFpsCap()
        {
            if (MieMieFrameWork.ModuleHub.Instance != null)
                return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = (int)m_FpsCap;
        }

        private void SetFpsCap(EFpsCap eCap)
        {
            if (MieMieFrameWork.ModuleHub.Instance != null)
                return;
            if (m_FpsCap == eCap) return;
            m_FpsCap = eCap;
            ApplyFpsCap();
            ResetFpsStats();
        }

        private void Update()
        {
            if (m_World == null) return;

            SampleFps();
            TickReport();

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                {
                    SetFpsCap(EFpsCap.Cap60);
                }

                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                {
                    SetFpsCap(EFpsCap.Cap120);
                }

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    SpawnPreviewBurst();
                }

                if (keyboard.aKey.wasPressedThisFrame)
                {
                    StopReport(false);
                    ToggleDaily();
                }

                if (keyboard.bKey.wasPressedThisFrame)
                {
                    ToggleStressReport(m_StressTargetActive);
                }

                if (keyboard.cKey.wasPressedThisFrame)
                {
                    ToggleStressReport(m_Stress10kTargetActive);
                }

                if (keyboard.dKey.wasPressedThisFrame)
                {
                    ToggleStressReport(m_Stress100kTargetActive);
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (m_ReportRunning)
                    {
                        StopReport(false);
                        Debug.LogWarning("[FloatingText] 标准压测已手动取消 未生成报告");
                    }

                    m_Mode = ETestMode.Off;
                    m_SpawnAcc = 0f;
                }
            }

            if (m_Mode == ETestMode.Off) return;
            AutoReplenish();
        }

        private void ToggleDaily()
        {
            if (m_Mode == ETestMode.Daily)
            {
                m_Mode = ETestMode.Off;
                m_SpawnAcc = 0f;
                return;
            }

            m_Mode = ETestMode.Daily;
            m_SpawnAcc = 0f;
            ResetFpsStats();
        }

        private void ToggleStressReport(int targetActive)
        {
            if (m_ReportRunning)
            {
                StopReport(false);
                Debug.LogWarning("[FloatingText] 标准压测已手动取消 未生成报告");
                return;
            }

            m_Mode = ETestMode.Off;
            StartStandardStressReport(targetActive);
        }

        private float GetReportWarmupSeconds()
        {
            if (m_ReportTargetActive >= 100000) return ReportWarmupSeconds100k;
            return ReportWarmupSeconds;
        }

        private int GetStressMaxSpawnPerFrame()
        {
            if (m_ReportTargetActive >= 100000) return Mathf.Max(m_MaxSpawnPerFrame, 3000);
            if (m_ReportTargetActive >= 10000) return Mathf.Max(m_MaxSpawnPerFrame, 800);
            return m_MaxSpawnPerFrame;
        }

        private void StartStandardStressReport(int targetActive)
        {
            m_ReportTargetActive = Mathf.Max(1, targetActive);
            m_Mode = ETestMode.StressReport;
            m_SpawnAcc = 0f;
            m_ReportRunning = true;
            m_ReportElapsed = 0f;
            m_FrameDtList.Clear();
            m_ActiveSum = 0;
            m_ActiveSamples = 0;
            m_ActiveMin = int.MaxValue;
            m_ActiveMax = 0;
            m_GlyphSum = 0;
            m_LastReportSummary = string.Empty;
            ResetFpsStats();
            float warmup = GetReportWarmupSeconds();
            Debug.Log(
                $"[FloatingText] 标准压测开始 总时长={ReportTotalSeconds:0}s 预热={warmup:0}s " +
                $"目标帧率={Application.targetFrameRate} VSync={QualitySettings.vSyncCount} 目标同屏={m_ReportTargetActive}");
        }

        private void TickReport()
        {
            if (!m_ReportRunning) return;

            float dt = Time.unscaledDeltaTime;
            m_ReportElapsed += dt;
            float warmup = GetReportWarmupSeconds();

            if (m_ReportElapsed >= warmup)
            {
                m_FrameDtList.Add(dt);
                int active = m_World.ActiveCount;
                m_ActiveSum += active;
                m_ActiveSamples++;
                if (active < m_ActiveMin) m_ActiveMin = active;
                if (active > m_ActiveMax) m_ActiveMax = active;
                m_GlyphSum += m_World.ActiveGlyphCount;
            }

            if (m_ReportElapsed >= ReportTotalSeconds)
            {
                FinishReportAndStop();
            }
        }

        private void FinishReportAndStop()
        {
            string report = BuildReport();
            m_LastReportSummary = report;
            Debug.Log(report);
            StopReport(true);
        }

        private void StopReport(bool finishedNormally)
        {
            m_ReportRunning = false;
            m_ReportElapsed = 0f;
            m_Mode = ETestMode.Off;
            m_SpawnAcc = 0f;
            if (!finishedNormally) m_FrameDtList.Clear();
        }

        private string BuildReport()
        {
            int sampleCount = m_FrameDtList.Count;
            float measureSeconds = ReportTotalSeconds - GetReportWarmupSeconds();
            float avgActive = m_ActiveSamples > 0 ? (float)m_ActiveSum / m_ActiveSamples : 0f;
            float avgGlyph = m_ActiveSamples > 0 ? (float)m_GlyphSum / m_ActiveSamples : 0f;
            int activeMin = m_ActiveMin == int.MaxValue ? 0 : m_ActiveMin;

            float avgDt = 0f;
            float minFps = 0f;
            float maxFps = 0f;
            float fps1Low = 0f;
            float fps5Low = 0f;
            float maxFrameMs = 0f;

            if (sampleCount > 0)
            {
                float sumDt = 0f;
                float bestDt = float.MaxValue;
                float worstDt = 0f;
                for (int i = 0; i < sampleCount; i++)
                {
                    float frameDt = m_FrameDtList[i];
                    sumDt += frameDt;
                    if (frameDt < bestDt) bestDt = frameDt;
                    if (frameDt > worstDt) worstDt = frameDt;
                }

                avgDt = sumDt / sampleCount;
                minFps = worstDt > 0.0001f ? 1f / worstDt : 0f;
                maxFps = bestDt > 0.0001f ? 1f / bestDt : 0f;
                maxFrameMs = worstDt * 1000f;
                fps1Low = CalcPercentLowFps(0.01f);
                fps5Low = CalcPercentLowFps(0.05f);
            }

            float avgFps = avgDt > 0.0001f ? 1f / avgDt : 0f;
            float avgFrameMs = avgDt * 1000f;
            int cap = MieMieFrameWork.ModuleHub.Instance != null
                ? (QualitySettings.vSyncCount == 0 ? Application.targetFrameRate : -1)
                : (int)m_FpsCap;
            float hitRate = cap > 0 ? Mathf.Clamp01(avgFps / cap) * 100f : 0f;

            var sb = new StringBuilder(512);
            sb.AppendLine("========== FloatingText Standard Stress Report ==========");
            sb.AppendLine($"Total: {ReportTotalSeconds:0.#}s | Warmup: {GetReportWarmupSeconds():0.#}s | Measure: {measureSeconds:0.#}s");
            sb.AppendLine($"FPS Cap: {cap} | Target Active: {m_ReportTargetActive}");
            sb.AppendLine($"Active Avg/Min/Max: {avgActive:0.0} / {activeMin} / {m_ActiveMax}");
            sb.AppendLine($"Glyph Avg: {avgGlyph:0.0} | Samples: {sampleCount}");
            sb.AppendLine($"FPS Avg/Min/Max: {avgFps:F2} / {minFps:F2} / {maxFps:F2}");
            sb.AppendLine($"FPS 1% Low / 5% Low: {fps1Low:F2} / {fps5Low:F2}");
            sb.AppendLine($"Frame ms Avg/Max: {avgFrameMs:F2} / {maxFrameMs:F2}");
            sb.AppendLine(cap > 0 ? $"Cap Hit Rate(Avg/Cap): {hitRate:F1}%" : "Cap Hit Rate(Avg/Cap): N/A VSync或平台默认策略");
            sb.AppendLine("========================================================");
            return sb.ToString();
        }

        /// <summary>
        /// 取最差 percent 帧的平均帧时再换算 FPS 即 1% Low / 5% Low
        /// </summary>
        private float CalcPercentLowFps(float worstPercent)
        {
            int sampleCount = m_FrameDtList.Count;
            if (sampleCount <= 0) return 0f;

            float[] sorted = m_FrameDtList.ToArray();
            System.Array.Sort(sorted);
            int worstCount = Mathf.Max(1, Mathf.CeilToInt(sampleCount * worstPercent));
            int start = sampleCount - worstCount;
            float sum = 0f;
            for (int i = start; i < sampleCount; i++)
            {
                sum += sorted[i];
            }

            float avgWorstDt = sum / worstCount;
            return avgWorstDt > 0.0001f ? 1f / avgWorstDt : 0f;
        }

        private void AutoReplenish()
        {
            int target = m_Mode == ETestMode.Daily ? m_DailyTargetActive : m_ReportTargetActive;
            float rate = m_Mode == ETestMode.Daily ? m_DailySpawnPerSecond : m_StressSpawnPerSecond;
            if (m_Mode == ETestMode.StressReport)
            {
                if (m_ReportTargetActive >= 100000) rate = Mathf.Max(rate, 150000f);
                else if (m_ReportTargetActive >= 10000) rate = Mathf.Max(rate, 12000f);
            }

            int maxSpawnPerFrame = m_Mode == ETestMode.StressReport
                ? GetStressMaxSpawnPerFrame()
                : m_MaxSpawnPerFrame;

            int need = target - m_World.ActiveCount;
            if (need < 0) need = 0;

            m_SpawnAcc += Time.deltaTime * rate;
            int byRate = (int)m_SpawnAcc;
            if (byRate > 0) m_SpawnAcc -= byRate;

            int spawn = byRate;
            if (spawn > need) spawn = need;
            if (spawn > maxSpawnPerFrame) spawn = maxSpawnPerFrame;

            if (need > spawn && m_Mode == ETestMode.StressReport)
            {
                int catchUp = need - spawn;
                if (catchUp > maxSpawnPerFrame - spawn)
                    catchUp = maxSpawnPerFrame - spawn;
                spawn += catchUp;
            }

            for (int i = 0; i < spawn; i++) SpawnOne();
        }

        private void SampleFps()
        {
            float fps = Time.unscaledDeltaTime > 0.0001f ? 1f / Time.unscaledDeltaTime : 0f;
            m_FpsAccum += fps;
            m_FpsFrames++;
            if (fps < m_MinFps) m_MinFps = fps;
            if (fps > m_MaxFps) m_MaxFps = fps;

            m_FpsTimer += Time.unscaledDeltaTime;
            if (m_FpsTimer >= 0.5f)
            {
                m_AvgFps = m_FpsFrames > 0 ? m_FpsAccum / m_FpsFrames : 0f;
                m_FpsAccum = 0f;
                m_FpsFrames = 0;
                m_FpsTimer = 0f;
            }
        }

        private void ResetFpsStats()
        {
            m_FpsAccum = 0f;
            m_FpsFrames = 0;
            m_FpsTimer = 0f;
            m_AvgFps = 0f;
            m_MinFps = float.MaxValue;
            m_MaxFps = 0f;
        }

        private void OnGUI()
        {
            bool HasFrameRoot = MieMieFrameWork.ModuleHub.Instance != null;
            GUILayout.BeginArea(new Rect(12f, 12f, 720f, 380f));
            GUILayout.Label("FloatingText 测试");
            GUILayout.Label(HasFrameRoot
                ? "A 日常 | B 压测4k | C 压测1万 | D 压测10万 | Space 预览 | Esc 取消 | 锁帧由画质服务管理"
                : "A 日常 | B 压测4k | C 压测1万 | D 压测10万 | Space 预览 | Esc 取消 | 1锁60 | 2锁120");
            string modeLabel = m_Mode == ETestMode.Off ? "Off" : m_Mode == ETestMode.Daily ? "Daily" : "StressReport";
            GUILayout.Label($"模式: {modeLabel}");
            if (m_World != null)
            {
                GUILayout.Label($"Active: {m_World.ActiveCount}   Glyph: {m_World.ActiveGlyphCount}");
            }

            float showMin = m_MinFps > 100000f ? 0f : m_MinFps;
            GUILayout.Label($"FPS Avg: {m_AvgFps:0.0}   Min: {showMin:0.0}   Max: {m_MaxFps:0.0}");
            GUILayout.Label(HasFrameRoot
                ? $"画质服务帧率策略: {Application.targetFrameRate}  VSync: {QualitySettings.vSyncCount}"
                : $"当前锁帧: {(int)m_FpsCap}");

            if (m_ReportRunning)
            {
                float left = ReportTotalSeconds - m_ReportElapsed;
                if (left < 0f) left = 0f;
                string phase = m_ReportElapsed < GetReportWarmupSeconds() ? "预热灌池" : "采样统计";
                GUILayout.Label($"标准压测进行中 目标={m_ReportTargetActive} [{phase}] 剩余 {left:0.0}s");
            }

            GUILayout.BeginHorizontal();
            bool PreviousGUIEnabled = GUI.enabled;
            GUI.enabled = PreviousGUIEnabled && !HasFrameRoot;
            if (GUILayout.Button(!HasFrameRoot && m_FpsCap == EFpsCap.Cap60 ? "[锁60]" : "锁60", GUILayout.Width(80f)))
            {
                SetFpsCap(EFpsCap.Cap60);
            }

            if (GUILayout.Button(!HasFrameRoot && m_FpsCap == EFpsCap.Cap120 ? "[锁120]" : "锁120", GUILayout.Width(80f)))
            {
                SetFpsCap(EFpsCap.Cap120);
            }
            GUI.enabled = PreviousGUIEnabled;

            if (GUILayout.Button(m_ReportRunning ? "取消压测" : "压测4k", GUILayout.Width(90f)))
            {
                ToggleStressReport(m_StressTargetActive);
            }

            if (GUILayout.Button("压测1万", GUILayout.Width(90f)))
            {
                ToggleStressReport(m_Stress10kTargetActive);
            }

            if (GUILayout.Button("压测10万", GUILayout.Width(90f)))
            {
                ToggleStressReport(m_Stress100kTargetActive);
            }

            if (GUILayout.Button("预览跳字", GUILayout.Width(80f)))
            {
                SpawnPreviewBurst();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 手动预览 数字与短词各来几条方便肉眼确认渲染
        /// </summary>
        private void SpawnPreviewBurst()
        {
            SpawnOne();
            SpawnOne();
            m_World.Play(transform.position + Vector3.up * 0.5f, 12345, false);
            m_World.Play(transform.position + Vector3.up * 1.2f + Vector3.right * 0.8f, 88888, true);
            m_World.Play(transform.position + Vector3.up * 0.8f + Vector3.left * 0.8f, "MISS", false);
            m_World.Play(transform.position + Vector3.up * 1.5f + Vector3.right * 1.2f, "CRIT", true);
            m_World.Play(transform.position + Vector3.up * 1.8f, "HEAL", false);
        }

        #endregion

        private static readonly string[] s_WordPool =
        {
            "MISS", "HIT", "CRIT", "DEAD", "MAX", "BAD", "GOOD", "PERFECT", "STUN", "HEAL"
        };

        private void SpawnOne()
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * m_Radius;
            pos.y = Mathf.Abs(pos.y) * 0.5f + transform.position.y;
            bool isCrit = Random.value < m_CritChance;

            // 约两成短词 其余数字 覆盖字母与数字混合压测
            if (Random.value < 0.2f)
            {
                string word = s_WordPool[Random.Range(0, s_WordPool.Length)];
                m_World.Play(pos, word, isCrit);
            }
            else
            {
                int value = Random.Range(1, 99999);
                m_World.Play(pos, value, isCrit);
            }
        }
    }
}
