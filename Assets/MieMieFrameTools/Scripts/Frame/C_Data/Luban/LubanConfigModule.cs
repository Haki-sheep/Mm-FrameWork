using System;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;

namespace MieMieFrameWork.Data.Luban
{
    /// <summary>
    /// Luban 配置组件 不自行启动 由同物体上的 ModuleHub 负责生命周期
    /// </summary>
    [DisallowMultipleComponent]
    [Preserve]
    public sealed class LubanConfigModule : MonoBehaviour, IConfigModule
    {
        /// <summary> StreamingAssets 下的配置子目录 </summary>
        [SerializeField]
        private string relativeFolder = "DataTables";

        /// <summary> 当前 Luban 配置表模块 </summary>
        private LubanTablesDataModule dataModule;

        /// <summary> 配置组件是否已释放 </summary>
        private bool disposed;

        public cfg.Tables Tables => ActiveModule.Tables;

        public int Revision => ActiveModule.Revision;

        private LubanTablesDataModule ActiveModule
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(LubanConfigModule));
                if (dataModule == null)
                    throw new InvalidOperationException("Luban 配置尚未初始化");

                return dataModule;
            }
        }

        /// <summary>
        /// 由框架启动入口在管理器注册前创建并加载全部配置表
        /// </summary>
        public void InitComponents()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LubanConfigModule));
            if (dataModule != null)
                throw new InvalidOperationException("Luban 配置只能初始化一次");
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer)
                throw new NotSupportedException("当前 Luban 加载器只支持本地文件 请先为 Android 或 WebGL 接入异步配置加载阶段");

            string DirectoryPath = Path.Combine(Application.streamingAssetsPath, relativeFolder);
            dataModule = new LubanTablesDataModule(DirectoryPath);
            dataModule.Init();
        }

        /// <summary>
        /// 整份重载配置 失败时不替换当前表
        /// </summary>
        public void Reload()
        {
            ActiveModule.Reload();
        }

        /// <summary>
        /// 由配置所有者解除配置表持有 不隐式保存业务数据
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            dataModule?.Dispose();
        }
    }
}
