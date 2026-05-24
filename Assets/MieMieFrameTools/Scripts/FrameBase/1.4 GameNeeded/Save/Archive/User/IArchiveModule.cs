// Assets/Scripts/Archive/Interfaces/IArchiveModule.cs

using Game.Save;

namespace MieMieFrameTools
{
    /// <summary>
    /// 存档模块接口
    /// 所有需要存档的模块都要实现此接口
    /// </summary>
    public interface IArchiveModule
    {
        /// <summary>
        /// 模块名称（用于调试）
        /// </summary>
        string ModuleName { get; }

        /// <summary> 
        /// 创建新存档时的初始化
        /// </summary>
        void CreateArchive(SaveData saveData);

        /// <summary>
        /// 从存档读取数据
        /// </summary>
        void FromArchive(SaveData saveData);

        /// <summary>
        /// 写入存档数据
        /// </summary>
        void ToArchive(SaveData saveData);
    }
}