using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MieMieFrameWork.Data.Luban
{
    /// <summary>
    /// 从本地目录加载全部 Luban 表并管理当前配置的重载与释放
    /// </summary>
    public sealed class LubanTablesDataModule : IDisposable
    {
        /// <summary> 配置文件所在目录 </summary>
        private readonly string directoryPath;

        /// <summary> 当前生效的完整配置表 </summary>
        private cfg.Tables tables;

        /// <summary> 模块是否已经释放 </summary>
        private bool disposed;

        public bool IsInitialized => tables != null;

        public int Revision { get; private set; }

        public cfg.Tables Tables
        {
            get
            {
                EnsureInitialized();
                return tables;
            }
        }

        /// <summary>
        /// 接收配置目录 不在构造阶段加载文件
        /// </summary>
        public LubanTablesDataModule(string directoryPath)
        {
            this.directoryPath = directoryPath;
        }

        /// <summary>
        /// 首次加载全部生成表并完成跨表引用解析
        /// </summary>
        public void Init()
        {
            ThrowIfDisposed();
            if (IsInitialized)
                throw new InvalidOperationException("Luban 配置只能初始化一次");

            tables = LoadTables();
            Revision = 1;
        }

        /// <summary>
        /// 完整加载成功后替换当前配置 失败时保留旧表与版本
        /// </summary>
        public void Reload()
        {
            EnsureInitialized();
            var NextTables = LoadTables();
            // 完整构建候选配置后才替换当前版本
            tables = NextTables;
            Revision++;
        }

        /// <summary>
        /// 解除当前配置引用并终止模块访问 重复释放不产生额外行为
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            // 先终止访问再清除当前引用 外部持有的旧表仍由其所有者管理
            disposed = true;
            tables = null;
        }

        /// <summary>
        /// 加载全部生成表并完成跨表引用解析
        /// </summary>
        private cfg.Tables LoadTables()
        {
            return new cfg.Tables(LoadJsonArray);
        }

        /// <summary>
        /// 按生成表名读取 JSON 失败时保留文件路径与原始异常
        /// </summary>
        private JArray LoadJsonArray(string fileName)
        {
            string FilePath = Path.Combine(directoryPath, fileName + ".json");
            try
            {
                string JsonText = File.ReadAllText(FilePath);
                return JArray.Parse(JsonText);
            }
            catch (Exception Exception)
            {
                throw new InvalidOperationException($"[Luban] 配置文件加载失败 {FilePath}", Exception);
            }
        }

        /// <summary>
        /// 拒绝初始化前或释放后访问配置
        /// </summary>
        private void EnsureInitialized()
        {
            ThrowIfDisposed();
            if (!IsInitialized)
                throw new InvalidOperationException("Luban 配置尚未初始化");
        }

        /// <summary>
        /// 拒绝释放后重新加载或访问配置
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LubanTablesDataModule));
        }
    }
}
