using System;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 场景对象持有一个独立图集组 Start 创建 OnDestroy 释放
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DynamicAtlasHost : MonoBehaviour
    {
        /// <summary>
        /// 图集组配置
        /// </summary>
        [SerializeField]
        private DynamicAtlasConfig config;

        /// <summary>
        /// 当前组件唯一持有的服务
        /// </summary>
        private DynamicAtlasService service;

        public bool IsInitialized => service != null && !service.IsDisposed;
        public DynamicAtlasService Service => IsInitialized ? service : throw new InvalidOperationException("动态图集尚未完成 Start 或已经释放");

        /// <summary>
        /// 唯一初始化入口
        /// </summary>
        private void Start()
        {
            InitComponents();
        }

        /// <summary>
        /// 从配置建立图集服务 不修改现有 ModuleHub 启动顺序
        /// </summary>
        private void InitComponents()
        {
            service = new DynamicAtlasService(config);
        }

        /// <summary>
        /// 终止加载并销毁本组件持有的图集
        /// </summary>
        private void OnDestroy()
        {
            service?.Dispose();
        }
    }
}
