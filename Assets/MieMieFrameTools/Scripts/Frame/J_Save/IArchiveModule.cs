using Game.Save;

namespace MiMieSaver
{
    /// <summary>
    /// 存档模块接口
    /// </summary>
    public interface IArchiveModule
    {
        #region 属性

        /// <summary>
        /// 模块名称
        /// </summary>
        string ModuleName { get; }

        #endregion

        #region 存档读写

        /// <summary>
        /// 向新档快照写入默认数据 不创建槽位或保存文件
        /// </summary>
        void CreateArchive(SaveData saveData);

        /// <summary>
        /// 从存档快照读取并建立模块运行时数据
        /// </summary>
        void FromArchive(SaveData saveData);

        /// <summary>
        /// 写入存档数据
        /// </summary>
        void ToArchive(SaveData saveData);

        #endregion
    }
}
