namespace MieMieFrameWork.Save
{
    using System;
    using System.IO;
    using MiMieSaver;
    using MieMieFrameWork.Diagnostics;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;

    /// <summary>
    /// 存档生命周期管理器 通过框架注册流程创建并持有存档核心
    /// </summary>
    [ManagerAttribute(Priority)]
    public sealed class ArchiveManager : IManagerBase, IDisposable
    {
        /// <summary> 存档管理器顺序 早于依赖存档的运行时管理器 </summary>
        public const int Priority = 0;

        /// <summary> 存档子目录 沿用根节点序列化配置 </summary>
        private readonly string archiveSubFolder;

        /// <summary> 存档核心实例 不持有业务模块的释放责任 </summary>
        private IArchiveMgr archiveMgr;

        public IArchiveMgr Archive => archiveMgr;

        public bool IsReady => archiveMgr != null;

        /// <summary>
        /// 接收存档目录配置 构造阶段不创建目录或读取存档
        /// </summary>
        public ArchiveManager(string archiveSubFolder)
        {
            this.archiveSubFolder = archiveSubFolder;
        }

        /// <summary>
        /// 由统一管理器阶段调用一次 创建原有存档核心
        /// </summary>
        public void Init()
        {
            string Folder = string.IsNullOrWhiteSpace(archiveSubFolder) ? "Archives" : archiveSubFolder.Trim();
            string RootPath = Path.Combine(Application.persistentDataPath, Folder);
            archiveMgr = new ArchiveMgr(RootPath);
            FrameLog.Info("存档管理器就绪", "Save");
        }

        /// <summary>
        /// 解除存档核心持有 不保存 不删除文件 不释放业务所有者的模块
        /// </summary>
        public void Dispose()
        {
            archiveMgr = null;
        }
    }
}
