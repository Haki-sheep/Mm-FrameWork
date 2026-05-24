// Assets/Scripts/Archive/Core/ArchiveMgr.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Save;
using Google.Protobuf;
using MieMieFrameWork;
using UnityEngine;

namespace MieMieFrameTools
{
    /// <summary>
    /// 存档管理器：槽位、读写、模块汇总
    /// </summary>
    public class ArchiveMgr : MonoBehaviour,IArchiveMgr,IManagerBase
    {
        // --------- 私有字段 ---------
        /// <summary>
        /// 存档根路径
        /// </summary>
        private readonly string rootPath;

        /// <summary>
        /// 存档槽管理器
        /// </summary>
        private readonly SlotsIndexMgr slotIndex;

        /// <summary>
        /// 模块有序列表
        /// </summary>
        private readonly List<IArchiveModule> moduleOrder = new();

        /// <summary>
        /// 模块类型字典
        /// </summary>
        private readonly Dictionary<Type, IArchiveModule> modulesByTypeDict = new();

        // --------- 公共属性 ---------
        /// <summary>
        /// 存档根路径
        /// </summary>
        public string RootPath => rootPath;

        // --------- 构造函数 ---------
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rootPath">存档根目录</param>
        public ArchiveMgr(string rootPath)
        {
            this.rootPath = rootPath;
            Directory.CreateDirectory(rootPath);
            slotIndex = new SlotsIndexMgr(rootPath);
        }

        // --------- 存档槽操作 ---------
        /// <summary>
        /// 获取所有槽位索引
        /// </summary>
        public ISlotIndex GetAllSlotIndex() => slotIndex;

        /// <summary>
        /// 创建存档槽
        /// </summary>
        public ISlot CreatSlot(string displayerName) => slotIndex.CreatSlot(displayerName);

        /// <summary>
        /// 切换存档槽
        /// </summary>
        public void SwitchSlot(string slotId) => slotIndex.SwitchSlot(slotId);

        /// <summary>
        /// 删除存档槽
        /// </summary>
        public void DeleteSlot(string slotId)
        {
            slotIndex.DeleteSlot(slotId);
        }

        /// <summary>
        /// 清理孤立文件：存在于磁盘但不在 slotsIndex.json 索引中的 .dat 文件
        /// </summary>
        public void CleanupOrphanedFiles() => slotIndex.CleanupOrphanedFiles();

        /// <summary>
        /// 清理孤立槽位：有索引记录但无对应 .dat 文件的槽位
        /// </summary>
        public void CleanupOrphanedSlots() => slotIndex.CleanupOrphanedSlots();

        /// <summary>
        /// 重命名存档槽
        /// </summary>
        public void RenameSlot(string slotId, string newName) => slotIndex.RenameSlot(slotId, newName);

        // --------- 存档数据读写 ---------
        /// <summary>
        /// 获取当前存档数据
        /// </summary>
        public SaveData GetArchive()
        {
            var slot = slotIndex.CurrentSlot;
            if (slot == null) return null;

            string path = slotIndex.GetSlotPath(slot.SlotId);
            if (!File.Exists(path)) return null;

            using var stream = File.OpenRead(path);
            return SaveData.Parser.ParseFrom(stream);
        }

        /// <summary>
        /// 保存存档
        /// </summary>
        public void Save()
        {
            var slot = slotIndex.CurrentSlot;
            if (slot is null) return;

            SaveData saveData;
            try
            {
                saveData = GetArchive() ?? new SaveData();
            }
            catch
            {
                saveData = new SaveData();
            }

            saveData.Meta ??= new MetaSave();
            saveData.Meta.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var module in moduleOrder)
                module.ToArchive(saveData);

            string path = slotIndex.GetSlotPath(slot.SlotId);
            using (var stream = File.Create(path))
                saveData.WriteTo(stream);

            slotIndex.UpdateLastSaveTime(slot.SlotId);
        }

        /// <summary>
        /// 加载存档
        /// </summary>
        public void Load()
        {
            var slot = slotIndex.CurrentSlot;
            if (slot is null) return;

            string path = slotIndex.GetSlotPath(slot.SlotId);
            if (!File.Exists(path)) return;

            using var stream = File.OpenRead(path);
            var saveData = SaveData.Parser.ParseFrom(stream);

            foreach (var module in moduleOrder)
                module.FromArchive(saveData);
        }

        /// <summary>
        /// 判断是否存在存档
        /// </summary>
        public bool HasSaveData()
        {
            var slot = slotIndex.CurrentSlot;
            if (slot == null) return false;
            return File.Exists(slotIndex.GetSlotPath(slot.SlotId));
        }

        // --------- 模块注册 ---------
        /// <summary>
        /// 注册模块
        /// </summary>
        public void RegisterModule(IArchiveModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            RegisterModuleInternal(module.GetType(), module);
        }

        /// <summary>
        /// 泛型注册模块
        /// </summary>
        public void RegisterModule<T>(T module) where T : class, IArchiveModule
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            RegisterModuleInternal(typeof(T), module);
        }

        /// <summary>
        /// 模块注册内部方法
        /// </summary>
        private void RegisterModuleInternal(Type key, IArchiveModule module)
        {
            if (modulesByTypeDict.TryGetValue(key, out var existing))
            {
                if (ReferenceEquals(existing, module))
                    return;
                RemoveModuleFromOrder(existing);
                RemoveAllKeysForInstance(existing);
            }

            modulesByTypeDict[key] = module;
            if (!moduleOrder.Contains(module))
                moduleOrder.Add(module);
        }

        // --------- 模块获取 ---------
        /// <summary>
        /// 获取指定模块
        /// </summary>
        public T GetModule<T>() where T : class, IArchiveModule
        {
            return TryGetModule(out T m) ? m : null;
        }

        /// <summary>
        /// 尝试获取指定模块
        /// </summary>
        public bool TryGetModule<T>(out T module) where T : class, IArchiveModule
        {
            Type t = typeof(T);
            if (modulesByTypeDict.TryGetValue(t, out var byKey) && byKey is T castByKey)
            {
                module = castByKey;
                return true;
            }

            foreach (var m in moduleOrder)
            {
                if (m is T match)
                {
                    module = match;
                    return true;
                }
            }

            module = null;
            return false;
        }

        /// <summary>
        /// 判断是否存在模块
        /// </summary>
        public bool HasModule<T>() where T : class, IArchiveModule
        {
            return TryGetModule<T>(out _);
        }

        /// <summary>
        /// 注销指定模块
        /// </summary>
        public bool UnregisterModule<T>() where T : class, IArchiveModule
        {
            if (!TryGetModule(out T m)) return false;
            var mod = (IArchiveModule)m;
            RemoveModuleFromOrder(mod);
            RemoveAllKeysForInstance(mod);
            return true;
        }

        // --------- 内部辅助方法 ---------
        /// <summary>
        /// 移除实例所有关联键
        /// </summary>
        private void RemoveAllKeysForInstance(IArchiveModule module)
        {
            var toRemove = new List<Type>();
            foreach (var kv in modulesByTypeDict)
            {
                if (ReferenceEquals(kv.Value, module))
                    toRemove.Add(kv.Key);
            }
            foreach (var kt in toRemove)
                modulesByTypeDict.Remove(kt);
        }

        /// <summary>
        /// 从有序列表移除模块
        /// </summary>
        private void RemoveModuleFromOrder(IArchiveModule module)
        {
            moduleOrder.Remove(module);
        }

        /// <summary>
        /// 获取所有模块
        /// </summary>
        public IReadOnlyList<IArchiveModule> GetModules() => moduleOrder;

        public void Init()
        {
            // TODO:默认初始化实现
        }

        /// <summary>
        /// 模块数量
        /// </summary>
        public int ModuleCount => moduleOrder.Count;
    }
}