namespace MieMieFrameWork
{
    using System;
    using MieMieFrameWork.Business.Quality;
    using MieMieFrameWork.Data;
    using MieMieFrameWork.Effects;
    using MieMieFrameWork.Pool;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// 检查器上展示的框架配置 以及可选配置模块的持有与生命周期
    /// </summary>
    public partial class ModuleHub
    {
        /// <summary> YooAsset 默认资源包 与采集器配置一致 </summary>
        [SerializeField, LabelText("默认资源包")]
        private string resourcePackageName = "DefaultPackage";

        /// <summary> 资源版本和清单请求超时秒数 </summary>
        [SerializeField, LabelText("资源清单超时秒数")]
        private int resourceManifestTimeout = 60;

        /// <summary>
        /// 存档子目录名
        /// </summary>
        [SerializeField, LabelText("存档子目录")]
        private string archiveSubFolder = "Archives";

        /// <summary>
        /// 对象池管理器配置
        /// </summary>
        [SerializeField, LabelText("对象池管理器配置")]
        private PoolManager.PoolManagerConfig poolManagerConfig = new PoolManager.PoolManagerConfig();

        /// <summary>
        /// 音频管理器配置
        /// </summary>
        [SerializeField, LabelText("音频管理器配置")]
        private AudioManager.AudioManagerConfig audioManagerConfig = new AudioManager.AudioManagerConfig();

        /// <summary> 视觉特效配置资产 启动时读取 不自动加载特效资源 </summary>
        [SerializeField, LabelText("视觉特效配置")]
        private EffectProfile effectProfile;

        /// <summary> 未绑定配置资产的旧根节点继续使用原内嵌配置 </summary>
        [SerializeField, LabelText("旧版视觉特效配置"), HideIf("@effectProfile != null")]
        private EffectManager.EffectManagerConfig effectManagerConfig = new EffectManager.EffectManagerConfig();

        /// <summary> 画质档位映射与本机帧率偏好策略 </summary>
        [SerializeField, LabelText("画质管理器配置")]
        private QualityConfig qualityConfig = new QualityConfig();

        /// <summary> 从当前根节点获取并持有的可选配置生命周期接口 </summary>
        private IConfigModule configModule;

        public bool HasConfig => configModule != null;

        /// <summary>
        /// 在管理器注册前初始化配置组件 未挂载时补挂已安装的 Luban 组件
        /// </summary>
        internal void InitConfigModule()
        {
            configModule = GetComponent<IConfigModule>();
            if (configModule == null)
            {
                var ConfigType = ResolveLubanConfigType();
                if (ConfigType == null)
                    return;

                configModule = this.GetOrAddComponent<IConfigModule>(ConfigType);
            }

            try
            {
                configModule.InitComponents();
            }
            catch (Exception Exception)
            {
                throw new InvalidOperationException($"[ModuleHub] 配置组件 {configModule.GetType().FullName} 初始化失败", Exception);
            }
        }

        /// <summary>
        /// 发现可选 Luban 配置组件类型 不建立 Runtime 到 Luban 的程序集引用
        /// </summary>
        private static Type ResolveLubanConfigType()
        {
            const string ConfigTypeName = "MieMieFrameWork.Data.Luban.LubanConfigModule";
            const string AssemblyName = "MieMieFrameWork.Data.Luban";
            var ConfigType = Type.GetType($"{ConfigTypeName}, {AssemblyName}");
            if (ConfigType != null)
                return ConfigType;

            foreach (var Assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (Assembly.GetName().Name == AssemblyName)
                    return Assembly.GetType(ConfigTypeName);
            }

            return null;
        }

        /// <summary>
        /// 获取已加载的配置组件 调用方需引用对应配置程序集
        /// </summary>
        public T GetConfig<T>() where T : class => configModule as T;

        /// <summary>
        /// 解除配置持有并释放模块 失败后不再次释放同一实例
        /// </summary>
        internal void ReleaseConfigModule()
        {
            var ConfigModule = configModule;
            configModule = null;
            ConfigModule?.Dispose();
        }
    }
}
