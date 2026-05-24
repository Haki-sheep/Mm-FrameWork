using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Save;

namespace MieMieFrameTools
{
    /// <summary>
    /// 存档管理器核心接口
    /// 负责：存档槽管理（创建/删除/切换/重命名）、存档数据读写、存档路径管理
    /// 对应Protobuf的SaveData数据结构，基于字符串唯一ID识别存档槽
    /// </summary>
    public interface IArchiveMgr
    {
        /// <summary>
        /// 存档根目录路径
        /// </summary>
        string RootPath { get; }

        // ====================== 存档槽位操作 ======================
        /// <summary>
        /// 获取所有存档槽的索引信息
        /// </summary>
        /// <returns>所有存档槽的索引集合</returns>
        ISlotIndex GetAllSlotIndex();

        /// <summary>
        /// 创建一个新的存档槽
        /// </summary>
        /// <param name="displayerName">存档槽显示名称（玩家自定义的名字）</param>
        /// <returns>创建完成的存档槽对象</returns>
        ISlot CreatSlot(string displayerName);

        /// <summary>
        /// 切换当前使用的存档槽
        /// </summary>
        /// <param name="slotId">存档槽唯一ID（字符串类型，永不重复）</param>
        void SwitchSlot(string slotId);

        /// <summary>
        /// 删除指定的存档槽
        /// </summary>
        /// <param name="slotId">要删除的存档槽唯一ID</param>
        void DeleteSlot(string slotId);

        /// <summary>
        /// 重命名指定的存档槽
        /// 仅修改显示名称，不改变存档槽唯一ID
        /// </summary>
        /// <param name="slotId">存档槽唯一ID</param>
        /// <param name="newName">新的显示名称</param>
        void RenameSlot(string slotId, string newName);

        // ====================== 存档/读档 核心操作 ======================
        /// <summary>
        /// 获取当前激活槽位的存档数据
        /// </summary>
        SaveData GetArchive();

        /// <summary>
        /// 手动保存存档
        /// </summary>
        void Save();

        /// <summary>
        /// 加载存档
        /// </summary>
        void Load();

        // ====================== 模块管理 ======================

        /// <summary>
        /// 注册模块（以实例的运行时类型为键，兼容旧代码）
        /// </summary>
        void RegisterModule(IArchiveModule module);

        /// <summary>
        /// 注册模块（以 <typeparamref name="T"/> 为键，便于 <see cref="GetModule{T}"/> 查找）
        /// </summary>
        void RegisterModule<T>(T module) where T : class, IArchiveModule;

        /// <summary>
        /// 按类型获取模块；找不到则返回 null
        /// </summary>
        T GetModule<T>() where T : class, IArchiveModule;

        /// <summary>
        /// 按类型尝试获取模块
        /// </summary>
        bool TryGetModule<T>(out T module) where T : class, IArchiveModule;

        /// <summary>
        /// 是否已注册可解析为 <typeparamref name="T"/> 的模块
        /// </summary>
        bool HasModule<T>() where T : class, IArchiveModule;

        /// <summary>
        /// 注销当前解析为 <typeparamref name="T"/> 的模块（从顺序表与类型表中移除）
        /// </summary>
        bool UnregisterModule<T>() where T : class, IArchiveModule;

        /// <summary>
        /// 获取按注册顺序排列的模块列表（存档/读档遍历顺序）
        /// </summary>
        IReadOnlyList<IArchiveModule> GetModules();

        /// <summary>
        /// 判断当前激活的存档槽是否存在有效存档数据
        /// </summary>
        bool HasSaveData();
    }
}