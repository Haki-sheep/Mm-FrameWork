using MieMieFrameWork.Tools.Inspector;
using UnityEngine;

namespace MieMieUIFrameWork.UI.FloatingText
{
    /// <summary>
    /// 跳字管理器 场景只挂这一份 业务只调这里的 Play
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        /// <summary> 单例缓存 </summary>
        private static FloatingTextManager instance;

        /// <summary> 跳字世界预制体 </summary>
        [InspectorFoldout("资源")]
        [InspectorLabel("跳字世界预制体")]
        [SerializeField]
        private FloatingTextWorld worldPrefab;

        /// <summary> 运行时世界实例 </summary>
        [InspectorFoldout("资源")]
        [InspectorLabel("已有世界实例 可空")]
        [SerializeField]
        private FloatingTextWorld world;

        /// <summary> 切换场景不销毁 </summary>
        [InspectorFoldout("生命周期")]
        [InspectorLabel("切换场景不销毁")]
        [SerializeField]
        private bool dontDestroyOnLoad;

        public static FloatingTextManager Instance => instance;

        public FloatingTextWorld World => world;

        public int ActiveCount => world != null ? world.ActiveCount : 0;

        public int ActiveGlyphCount => world != null ? world.ActiveGlyphCount : 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureWorld();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>
        /// 播放数字跳字
        /// </summary>
        public void Play(Vector3 worldPosition, int value, bool isCrit = false)
        {
            EnsureWorld();
            if (world == null) return;
            world.Play(worldPosition, value, isCrit);
        }

        /// <summary>
        /// 播放数字跳字
        /// </summary>
        public void Play(Vector3 worldPosition, long value, bool isCrit = false)
        {
            EnsureWorld();
            if (world == null) return;
            world.Play(worldPosition, value, isCrit);
        }

        /// <summary>
        /// 播放短文本跳字
        /// </summary>
        public void Play(Vector3 worldPosition, string text, bool isCrit = false)
        {
            EnsureWorld();
            if (world == null) return;
            world.Play(worldPosition, text, isCrit);
        }

        /// <summary>
        /// 普通伤害数字
        /// </summary>
        public void PlayDamage(Vector3 worldPosition, long value)
        {
            Play(worldPosition, value, false);
        }

        /// <summary>
        /// 暴击伤害数字
        /// </summary>
        public void PlayCrit(Vector3 worldPosition, long value)
        {
            Play(worldPosition, value, true);
        }

        /// <summary>
        /// 静态入口 数字
        /// </summary>
        public static void Show(Vector3 worldPosition, long value, bool isCrit = false)
        {
            if (instance == null) return;
            instance.Play(worldPosition, value, isCrit);
        }

        /// <summary>
        /// 静态入口 短文本
        /// </summary>
        public static void Show(Vector3 worldPosition, string text, bool isCrit = false)
        {
            if (instance == null) return;
            instance.Play(worldPosition, text, isCrit);
        }

        private void EnsureWorld()
        {
            if (world != null) return;

            world = GetComponentInChildren<FloatingTextWorld>(true);
            if (world != null) return;

            if (worldPrefab != null)
            {
                world = Instantiate(worldPrefab, transform);
                world.name = "FloatingTextWorld";
                return;
            }

            Debug.LogError("[FloatingTextManager] 未绑定跳字世界预制体 无法播放", this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器预览普通伤害
        /// </summary>
        public void PreviewDamage()
        {
            Play(GetPreviewWorldPos(), 1234L, false);
        }

        /// <summary>
        /// 编辑器预览暴击伤害
        /// </summary>
        public void PreviewCrit()
        {
            Play(GetPreviewWorldPos(), 9999L, true);
        }

        /// <summary>
        /// 编辑器预览一批数字与短词
        /// </summary>
        public void PreviewBurst()
        {
            Vector3 origin = GetPreviewWorldPos();
            Play(origin, 12345L, false);
            Play(origin + Vector3.up * 0.7f + Vector3.right * 0.8f, 88888L, true);
            Play(origin + Vector3.up * 0.3f + Vector3.left * 0.8f, "MISS", false);
            Play(origin + Vector3.up * 1.1f + Vector3.right * 1.2f, "CRIT", true);
            Play(origin + Vector3.up * 1.5f, "HEAL", false);
        }

        /// <summary>
        /// 预览生成点
        /// </summary>
        private static Vector3 GetPreviewWorldPos()
        {
            Camera cam = Camera.main;
            if (cam != null)
                return cam.transform.position + cam.transform.forward * 3f;
            return Vector3.zero;
        }

        /// <summary>
        /// 编辑器烘焙绑定世界预制体
        /// </summary>
        public void EditorBindWorldPrefab(FloatingTextWorld prefab)
        {
            worldPrefab = prefab;
            world = null;
        }
#endif
    }
}
