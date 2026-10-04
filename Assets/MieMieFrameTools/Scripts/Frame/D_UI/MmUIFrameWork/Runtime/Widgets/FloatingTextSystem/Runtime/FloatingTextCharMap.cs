using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MieMieUIFrameWork.UI.FloatingText
{
    /// <summary>
    /// 跳字字符到图集格子的烘焙查表
    /// </summary>
    [CreateAssetMenu(menuName = "MieMieFramework/跳字字符映射", fileName = "FloatingTextCharMap")]
    public class FloatingTextCharMap : ScriptableObject
    {
        [LabelText("图集列数")]
        [SerializeField]
        private int m_Columns = 16;

        [LabelText("图集行数")]
        [SerializeField]
        private int m_Rows = 3;

        [LabelText("格子边长")]
        [SerializeField]
        private int m_CellSize = 64;

        [LabelText("暴击符索引")]
        [SerializeField]
        private int m_CritIndex = 10;

        [LabelText("字符表")]
        [SerializeField]
        [TextArea(2, 6)]
        private string m_Charset = string.Empty;

        [LabelText("索引数组")]
        [SerializeField]
        private ushort[] m_Indices;

        private Dictionary<char, int> m_LookupDict;

        public int Columns => m_Columns;

        public int Rows => m_Rows;

        public int CellSize => m_CellSize;

        public int CritIndex => m_CritIndex;

        public string Charset => m_Charset;

        public int GlyphCount => m_Charset != null ? m_Charset.Length : 0;

        /// <summary>
        /// 编辑器写入烘焙结果
        /// </summary>
        public void EditorSetBakedData(int columns, int rows, int cellSize, string charset, int critIndex)
        {
            m_Columns = columns;
            m_Rows = rows;
            m_CellSize = cellSize;
            m_Charset = charset ?? string.Empty;
            m_CritIndex = critIndex;
            int count = m_Charset.Length;
            m_Indices = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                m_Indices[i] = (ushort)i;
            }

            m_LookupDict = null;
        }

        public bool TryGetIndex(char c, out int index)
        {
            EnsureLookup();
            if (m_LookupDict.TryGetValue(c, out index)) return true;
            if (c >= 'a' && c <= 'z')
            {
                char upper = (char)(c - 32);
                if (m_LookupDict.TryGetValue(upper, out index)) return true;
            }

            index = -1;
            return false;
        }

        private void EnsureLookup()
        {
            if (m_LookupDict != null) return;
            m_LookupDict = new Dictionary<char, int>(64);
            if (string.IsNullOrEmpty(m_Charset) || m_Indices == null) return;
            int count = Mathf.Min(m_Charset.Length, m_Indices.Length);
            for (int i = 0; i < count; i++)
            {
                char c = m_Charset[i];
                if (!m_LookupDict.ContainsKey(c))
                {
                    m_LookupDict.Add(c, m_Indices[i]);
                }
            }
        }

        private void OnEnable()
        {
            m_LookupDict = null;
        }
    }
}
