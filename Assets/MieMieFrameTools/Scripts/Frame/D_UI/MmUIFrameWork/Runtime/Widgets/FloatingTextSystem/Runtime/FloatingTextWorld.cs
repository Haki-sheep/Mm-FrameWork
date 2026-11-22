using MieMieFrameWork.Tools.Inspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace MieMieUIFrameWork.UI.FloatingText
{
    /// <summary>
    /// 跳字样式
    /// </summary>
    public enum FloatingTextStyle
    {
        Normal = 0,
        Crit = 1
    }

    /// <summary>
    /// 一条跳字的 GPU 实例数据
    /// </summary>
    public struct TextInstanceData
    {
        public Vector4 OriginTime;
        public Vector4 VelocityLife;
        public Vector4 Parms;
        public Vector4 Color;
        public Vector4 Indices0;
        public Vector4 Indices1;
        public Vector4 Indices2;
    }

    /// <summary>
    /// GPU Instancing 跳字 一条跳字一个 Instance
    /// </summary>
    public class FloatingTextWorld : MonoBehaviour
    {
        private const int BatchSize = 1023;
        private static readonly int OriginTimeId = Shader.PropertyToID("_OriginTime");
        private static readonly int VelocityLifeId = Shader.PropertyToID("_VelocityLife");
        private static readonly int ParmsId = Shader.PropertyToID("_Parms");
        private static readonly int GlyphColorId = Shader.PropertyToID("_GlyphColor");
        private static readonly int Indices0Id = Shader.PropertyToID("_Indices0");
        private static readonly int Indices1Id = Shader.PropertyToID("_Indices1");
        private static readonly int Indices2Id = Shader.PropertyToID("_Indices2");
        private static readonly int GlyphWidthId = Shader.PropertyToID("_GlyphWidth");
        private static readonly int GlyphHeightId = Shader.PropertyToID("_GlyphHeight");
        private static readonly int AtlasColumnsId = Shader.PropertyToID("_AtlasColumns");
        private static readonly int AtlasRowsId = Shader.PropertyToID("_AtlasRows");
        private static readonly int FadePowerId = Shader.PropertyToID("_FadePower");
        private static readonly int CritPopStartId = Shader.PropertyToID("_CritPopStart");
        private static readonly int CritPopEndId = Shader.PropertyToID("_CritPopEnd");
        private static readonly int CritPopInvDurationId = Shader.PropertyToID("_CritPopInvDuration");

        [InspectorFoldout("资源")]
        [InspectorLabel("图集")]
        [SerializeField]
        private Texture2D m_Atlas;

        [InspectorFoldout("资源")]
        [InspectorLabel("字符映射表")]
        [SerializeField]
        private FloatingTextCharMap m_CharMap;

        [InspectorFoldout("资源")]
        [InspectorLabel("材质")]
        [SerializeField]
        private Material m_Material;

        [InspectorFoldout("资源")]
        [InspectorLabel("相机 可空则用 Main")]
        [SerializeField]
        private Camera m_Camera;

        [InspectorFoldout("容量")]
        [InspectorLabel("图集列数")]
        [SerializeField]
        private int m_AtlasColumns = FloatingTextAtlasIds.Columns;

        [InspectorFoldout("容量")]
        [InspectorLabel("图集行数")]
        [SerializeField]
        private int m_AtlasRows = FloatingTextAtlasIds.Rows;

        [InspectorFoldout("容量")]
        [InspectorLabel("最大同屏条数")]
        [SerializeField]
        private int m_MaxEntries = 102400;

        [InspectorFoldout("容量")]
        [InspectorLabel("最大数字位数")]
        [SerializeField]
        private int m_MaxDigits = 8;

        [InspectorFoldout("字形尺寸")]
        [InspectorLabel("字形宽度")]
        [SerializeField]
        private float m_GlyphWidth = 0.35f;

        [InspectorFoldout("字形尺寸")]
        [InspectorLabel("字形高度")]
        [SerializeField]
        private float m_GlyphHeight = 0.45f;

        [InspectorFoldout("普通跳字动画")]
        [InspectorLabel("持续时间 秒")]
        [SerializeField]
        private float m_NormalLifetime = 0.85f;

        [InspectorFoldout("普通跳字动画")]
        [InspectorLabel("上升速度")]
        [SerializeField]
        private float m_RiseSpeed = 1.6f;

        [InspectorFoldout("普通跳字动画")]
        [InspectorLabel("缩放")]
        [SerializeField]
        private float m_NormalScale = 1f;

        [InspectorFoldout("普通跳字动画")]
        [InspectorLabel("颜色")]
        [SerializeField]
        private Color m_NormalColor = new Color(1f, 0.92f, 0.4f, 1f);

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("持续时间 秒")]
        [SerializeField]
        private float m_CritLifetime = 1.1f;

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("上升速度")]
        [SerializeField]
        private float m_CritRiseSpeed = 1.9f;

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("基础缩放")]
        [SerializeField]
        private float m_CritScale = 1.35f;

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("颜色")]
        [SerializeField]
        private Color m_CritColor = new Color(1f, 0.35f, 0.15f, 1f);

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("弹出起始倍率")]
        [SerializeField]
        private float m_CritPopStart = 1.2f;

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("弹出结束倍率")]
        [SerializeField]
        private float m_CritPopEnd = 1f;

        [InspectorFoldout("暴击跳字动画")]
        [InspectorLabel("弹出占寿命比例")]
        [SerializeField]
        [Range(0.05f, 1f)]
        private float m_CritPopLifetimeRatio = 0.25f;

        [InspectorFoldout("通用动画")]
        [InspectorLabel("水平随机偏移")]
        [SerializeField]
        private float m_HorizontalJitter = 0.35f;

        [InspectorFoldout("通用动画")]
        [InspectorLabel("淡出曲线指数 越大越晚淡")]
        [SerializeField]
        private float m_FadePower = 2f;

        private TextInstanceData[] m_Pool;
        private TextInstanceData[] m_Dense;
        private float[] m_SpawnTimes;
        private float[] m_Lifetimes;
        private int[] m_FreeStack;
        private int m_FreeCount;
        private int[] m_ActiveList;
        private int[] m_SlotToActive;
        private int m_ActiveCount;
        private int m_RingIndex;
        private int m_GlyphSum;

        private Mesh m_TextMesh;
        private MaterialPropertyBlock m_MPB;
        private Matrix4x4[] m_Matrices;
        private Vector4[] m_OriginTimeBatch;
        private Vector4[] m_VelocityLifeBatch;
        private Vector4[] m_ParmsBatch;
        private Vector4[] m_ColorBatch;
        private Vector4[] m_Indices0Batch;
        private Vector4[] m_Indices1Batch;
        private Vector4[] m_Indices2Batch;
        private Bounds m_DrawBounds;
        private bool m_BufferDirty;
        private int m_RngState;
        private int[] m_TempIndices;
        private static FloatingTextWorld s_Instance;

        public static FloatingTextWorld Instance => s_Instance;

        public int ActiveCount => m_ActiveCount;

        public int ActiveGlyphCount => m_GlyphSum;

        private void Awake()
        {
            s_Instance = this;
            m_RngState = 1234567;
            m_DrawBounds = new Bounds(Vector3.zero, Vector3.one * 500f);
            m_MPB = new MaterialPropertyBlock();
            m_TextMesh = CreateMultiGlyphMesh(FloatingTextAtlasIds.MaxGlyphsPerText);
            m_TempIndices = new int[FloatingTextAtlasIds.MaxGlyphsPerText];
            m_Matrices = new Matrix4x4[BatchSize];
            m_OriginTimeBatch = new Vector4[BatchSize];
            m_VelocityLifeBatch = new Vector4[BatchSize];
            m_ParmsBatch = new Vector4[BatchSize];
            m_ColorBatch = new Vector4[BatchSize];
            m_Indices0Batch = new Vector4[BatchSize];
            m_Indices1Batch = new Vector4[BatchSize];
            m_Indices2Batch = new Vector4[BatchSize];
            for (int i = 0; i < BatchSize; i++)
            {
                m_Matrices[i] = Matrix4x4.identity;
            }

            int cap = Mathf.Max(16, m_MaxEntries);
            m_Pool = new TextInstanceData[cap];
            m_Dense = new TextInstanceData[cap];
            m_SpawnTimes = new float[cap];
            m_Lifetimes = new float[cap];
            m_FreeStack = new int[cap];
            m_ActiveList = new int[cap];
            m_SlotToActive = new int[cap];
            m_FreeCount = cap;
            m_ActiveCount = 0;
            m_GlyphSum = 0;
            for (int i = 0; i < cap; i++)
            {
                m_FreeStack[i] = cap - 1 - i;
                m_SlotToActive[i] = -1;
            }

            EnsureMaterial();
            DisableLegacyMeshRenderer();
            if (m_Camera == null) m_Camera = Camera.main;
        }

        private void EnsureMaterial()
        {
            Shader shader = Shader.Find("MieMieUIFrameWork/FloatingTextInstanced");
            if (shader == null)
            {
                Debug.LogError("[FloatingText] 找不到 MieMieUIFrameWork/FloatingTextInstanced");
                return;
            }

            if (m_Material == null)
            {
                m_Material = new Material(shader);
            }
            else if (m_Material.shader != shader)
            {
                m_Material.shader = shader;
            }

            m_Material.enableInstancing = true;
            if (m_CharMap != null)
            {
                m_AtlasColumns = m_CharMap.Columns;
                m_AtlasRows = m_CharMap.Rows;
            }
            else
            {
                m_AtlasColumns = FloatingTextAtlasIds.Columns;
                m_AtlasRows = FloatingTextAtlasIds.Rows;
            }

            if (m_Atlas != null) m_Material.mainTexture = m_Atlas;
            ApplyMaterialParams();
        }

        private void DisableLegacyMeshRenderer()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
            if (m_TextMesh != null) Destroy(m_TextMesh);
        }

        public void Play(Vector3 worldPosition, int value, bool isCrit)
        {
            Play(worldPosition, (long)value, isCrit);
        }

        public void Play(Vector3 worldPosition, long value, bool isCrit)
        {
            if (value < 0) value = -value;

            int count = 0;
            if (isCrit)
            {
                m_TempIndices[count++] = ResolveCritIndex();
            }

            if (value == 0)
            {
                if (count < FloatingTextAtlasIds.MaxGlyphsPerText)
                {
                    m_TempIndices[count++] = ResolveDigitIndex(0);
                }
            }
            else
            {
                int digitStart = count;
                long temp = value;
                while (temp > 0 && count < FloatingTextAtlasIds.MaxGlyphsPerText && count - digitStart < m_MaxDigits)
                {
                    m_TempIndices[count++] = ResolveDigitIndex((int)(temp % 10));
                    temp /= 10;
                }

                int left = digitStart;
                int right = count - 1;
                while (left < right)
                {
                    int swap = m_TempIndices[left];
                    m_TempIndices[left] = m_TempIndices[right];
                    m_TempIndices[right] = swap;
                    left++;
                    right--;
                }
            }

            Color color = isCrit ? m_CritColor : m_NormalColor;
            float scale = isCrit ? m_CritScale : m_NormalScale;
            float lifetime = isCrit ? m_CritLifetime : m_NormalLifetime;
            float rise = isCrit ? m_CritRiseSpeed : m_RiseSpeed;
            SpawnInternal(worldPosition, count, color, scale, lifetime, rise, isCrit);
        }

        /// <summary>
        /// 播放短文本 支持 0-9 A-Z 与 * 暴击符
        /// </summary>
        public void Play(Vector3 worldPosition, string text, bool isCrit)
        {
            if (string.IsNullOrEmpty(text)) return;

            int count = 0;
            if (isCrit && count < FloatingTextAtlasIds.MaxGlyphsPerText)
            {
                m_TempIndices[count++] = ResolveCritIndex();
            }

            for (int i = 0; i < text.Length && count < FloatingTextAtlasIds.MaxGlyphsPerText; i++)
            {
                int index = ResolveCharIndex(text[i]);
                if (index < 0) continue;
                m_TempIndices[count++] = index;
            }

            if (count <= 0) return;

            Color color = isCrit ? m_CritColor : m_NormalColor;
            float scale = isCrit ? m_CritScale : m_NormalScale;
            float lifetime = isCrit ? m_CritLifetime : m_NormalLifetime;
            float rise = isCrit ? m_CritRiseSpeed : m_RiseSpeed;
            SpawnInternal(worldPosition, count, color, scale, lifetime, rise, isCrit);
        }

        private void SpawnInternal(
            Vector3 worldPosition,
            int glyphCount,
            Color color,
            float scale,
            float lifetime,
            float rise,
            bool isCrit)
        {
            int slot;
            bool wasActive;
            if (m_FreeCount > 0)
            {
                slot = m_FreeStack[--m_FreeCount];
                wasActive = false;
            }
            else
            {
                slot = m_RingIndex;
                m_RingIndex++;
                if (m_RingIndex >= m_Pool.Length) m_RingIndex = 0;
                wasActive = m_SlotToActive[slot] >= 0;
                if (wasActive)
                {
                    m_GlyphSum -= Mathf.RoundToInt(m_Pool[slot].Parms.x);
                }
            }

            float spawnTime = Time.time;
            float jx = NextSigned() * m_HorizontalJitter;
            float jz = NextSigned() * m_HorizontalJitter * 0.35f;
            Vector3 velocity = new Vector3(jx, rise, jz);

            for (int i = glyphCount; i < FloatingTextAtlasIds.MaxGlyphsPerText; i++)
            {
                m_TempIndices[i] = 0;
            }

            TextInstanceData data = new TextInstanceData
            {
                OriginTime = new Vector4(worldPosition.x, worldPosition.y, worldPosition.z, spawnTime),
                VelocityLife = new Vector4(velocity.x, velocity.y, velocity.z, lifetime),
                Parms = new Vector4(glyphCount, scale, 0f, isCrit ? 1f : 0f),
                Color = color,
                Indices0 = PackIndices(0),
                Indices1 = PackIndices(4),
                Indices2 = new Vector4(m_TempIndices[8], 0f, 0f, 0f)
            };

            m_Pool[slot] = data;
            m_SpawnTimes[slot] = spawnTime;
            m_Lifetimes[slot] = lifetime;
            m_GlyphSum += glyphCount;

            if (!wasActive) AddActive(slot);
            m_BufferDirty = true;
        }

        private Vector4 PackIndices(int start)
        {
            float a = start < FloatingTextAtlasIds.MaxGlyphsPerText ? m_TempIndices[start] : 0;
            float b = start + 1 < FloatingTextAtlasIds.MaxGlyphsPerText ? m_TempIndices[start + 1] : 0;
            float c = start + 2 < FloatingTextAtlasIds.MaxGlyphsPerText ? m_TempIndices[start + 2] : 0;
            float d = start + 3 < FloatingTextAtlasIds.MaxGlyphsPerText ? m_TempIndices[start + 3] : 0;
            return new Vector4(a, b, c, d);
        }

        private void LateUpdate()
        {
            float now = Time.time;
            for (int a = m_ActiveCount - 1; a >= 0; a--)
            {
                int slot = m_ActiveList[a];
                if (now - m_SpawnTimes[slot] >= m_Lifetimes[slot])
                {
                    m_GlyphSum -= Mathf.RoundToInt(m_Pool[slot].Parms.x);
                    RemoveActiveAt(a);
                    m_FreeStack[m_FreeCount++] = slot;
                    m_BufferDirty = true;
                }
            }

            if (m_ActiveCount <= 0 || m_Material == null) return;

            if (m_BufferDirty)
            {
                for (int i = 0; i < m_ActiveCount; i++)
                {
                    m_Dense[i] = m_Pool[m_ActiveList[i]];
                }

                m_BufferDirty = false;
            }

            ApplyMaterialParams();
            int offset = 0;
            while (offset < m_ActiveCount)
            {
                int count = m_ActiveCount - offset;
                if (count > BatchSize) count = BatchSize;
                for (int i = 0; i < count; i++)
                {
                    TextInstanceData data = m_Dense[offset + i];
                    m_OriginTimeBatch[i] = data.OriginTime;
                    m_VelocityLifeBatch[i] = data.VelocityLife;
                    m_ParmsBatch[i] = data.Parms;
                    m_ColorBatch[i] = data.Color;
                    m_Indices0Batch[i] = data.Indices0;
                    m_Indices1Batch[i] = data.Indices1;
                    m_Indices2Batch[i] = data.Indices2;
                }

                m_MPB.Clear();
                m_MPB.SetVectorArray(OriginTimeId, m_OriginTimeBatch);
                m_MPB.SetVectorArray(VelocityLifeId, m_VelocityLifeBatch);
                m_MPB.SetVectorArray(ParmsId, m_ParmsBatch);
                m_MPB.SetVectorArray(GlyphColorId, m_ColorBatch);
                m_MPB.SetVectorArray(Indices0Id, m_Indices0Batch);
                m_MPB.SetVectorArray(Indices1Id, m_Indices1Batch);
                m_MPB.SetVectorArray(Indices2Id, m_Indices2Batch);
                Graphics.DrawMeshInstanced(
                    m_TextMesh,
                    0,
                    m_Material,
                    m_Matrices,
                    count,
                    m_MPB,
                    ShadowCastingMode.Off,
                    false,
                    gameObject.layer);
                offset += count;
            }
        }

        private int ResolveCharIndex(char c)
        {
            if (m_CharMap != null && m_CharMap.TryGetIndex(c, out int index)) return index;
            return FloatingTextAtlasIds.CharToIndex(c);
        }

        private int ResolveDigitIndex(int digit)
        {
            char c = (char)('0' + digit);
            int index = ResolveCharIndex(c);
            if (index >= 0) return index;
            return FloatingTextAtlasIds.Digit0 + digit;
        }

        private int ResolveCritIndex()
        {
            if (m_CharMap != null) return m_CharMap.CritIndex;
            int index = ResolveCharIndex('*');
            if (index >= 0) return index;
            return FloatingTextAtlasIds.Crit;
        }

        private void AddActive(int slot)
        {
            m_ActiveList[m_ActiveCount] = slot;
            m_SlotToActive[slot] = m_ActiveCount;
            m_ActiveCount++;
        }

        private void RemoveActiveAt(int activeIndex)
        {
            int slot = m_ActiveList[activeIndex];
            m_SlotToActive[slot] = -1;
            int last = m_ActiveCount - 1;
            if (activeIndex != last)
            {
                int moved = m_ActiveList[last];
                m_ActiveList[activeIndex] = moved;
                m_SlotToActive[moved] = activeIndex;
            }

            m_ActiveCount = last;
        }

        private void ApplyMaterialParams()
        {
            m_Material.SetFloat(GlyphWidthId, m_GlyphWidth);
            m_Material.SetFloat(GlyphHeightId, m_GlyphHeight);
            m_Material.SetFloat(AtlasColumnsId, m_AtlasColumns);
            m_Material.SetFloat(AtlasRowsId, m_AtlasRows);
            m_Material.SetFloat(FadePowerId, Mathf.Max(0.01f, m_FadePower));
            m_Material.SetFloat(CritPopStartId, m_CritPopStart);
            m_Material.SetFloat(CritPopEndId, m_CritPopEnd);
            float ratio = Mathf.Max(0.05f, m_CritPopLifetimeRatio);
            m_Material.SetFloat(CritPopInvDurationId, 1f / ratio);
            if (m_Atlas != null) m_Material.mainTexture = m_Atlas;
        }

        private static Mesh CreateMultiGlyphMesh(int maxGlyphs)
        {
            int vertCount = maxGlyphs * 4;
            int triCount = maxGlyphs * 6;
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var uv2 = new Vector2[vertCount];
            var triangles = new int[triCount];

            for (int g = 0; g < maxGlyphs; g++)
            {
                int v = g * 4;
                vertices[v] = new Vector3(-0.5f, -0.5f, 0f);
                vertices[v + 1] = new Vector3(-0.5f, 0.5f, 0f);
                vertices[v + 2] = new Vector3(0.5f, 0.5f, 0f);
                vertices[v + 3] = new Vector3(0.5f, -0.5f, 0f);
                uvs[v] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(0f, 1f);
                uvs[v + 2] = new Vector2(1f, 1f);
                uvs[v + 3] = new Vector2(1f, 0f);
                float slot = g;
                uv2[v] = new Vector2(slot, 0f);
                uv2[v + 1] = new Vector2(slot, 0f);
                uv2[v + 2] = new Vector2(slot, 0f);
                uv2[v + 3] = new Vector2(slot, 0f);

                int t = g * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            var mesh = new Mesh();
            mesh.name = "FloatingTextMultiGlyph";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = uv2;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private float NextSigned()
        {
            m_RngState = m_RngState * 1103515245 + 12345;
            return ((m_RngState >> 16) & 0x7FFF) / 32768f * 2f - 1f;
        }

#if UNITY_EDITOR
        public void EditorBindResources(Texture2D atlas, Material material, FloatingTextCharMap charMap, Camera cam)
        {
            m_Atlas = atlas;
            m_Material = material;
            m_CharMap = charMap;
            m_Camera = cam;
            if (charMap != null)
            {
                m_AtlasColumns = charMap.Columns;
                m_AtlasRows = charMap.Rows;
            }
            else
            {
                m_AtlasColumns = FloatingTextAtlasIds.Columns;
                m_AtlasRows = FloatingTextAtlasIds.Rows;
            }
        }
#endif
    }
}
