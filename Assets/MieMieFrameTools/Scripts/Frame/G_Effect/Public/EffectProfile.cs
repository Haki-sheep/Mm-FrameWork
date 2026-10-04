namespace MieMieFrameWork.Effects
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// 视觉特效启动配置 由框架根节点显式引用
    /// </summary>
    [CreateAssetMenu(menuName = "MieMieFramework/视觉特效配置", fileName = "EffectProfile")]
    public sealed class EffectProfile : ScriptableObject
    {
        /// <summary> 特效定义 全局预算与档位参数 </summary>
        [SerializeField, InlineProperty, HideLabel]
        private EffectManager.EffectManagerConfig config = new EffectManager.EffectManagerConfig();

        public EffectManager.EffectManagerConfig Config => config;
    }
}
