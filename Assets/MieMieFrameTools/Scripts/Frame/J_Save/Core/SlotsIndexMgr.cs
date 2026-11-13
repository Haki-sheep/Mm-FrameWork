using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

namespace MiMieSaver
{
    /// <summary>
    /// 存档槽目录管理器
    /// </summary>
    public class SlotsIndexMgr : ISlotIndex
    {
        #region 字段

        /// <summary>
        /// 槽位索引文件路径
        /// </summary>
        private readonly string path;

        /// <summary>
        /// 槽位索引数据
        /// </summary>
        private SlotsIndexData data;

        #endregion

        #region 属性

        /// <summary>
        /// 当前槽位 ID
        /// </summary>
        public string CurrentSlotId => data.CurrentSlotId;

        /// <summary>
        /// 当前槽位
        /// </summary>
        public ISlot CurrentSlot => data.SlotList.FirstOrDefault(s => s.SlotId == data.CurrentSlotId);

        /// <summary>
        /// 所有槽位
        /// </summary>
        public IReadOnlyList<ISlot> Slots => data.SlotList;

        /// <summary>
        /// 槽位数量
        /// </summary>
        public int Count => data.SlotList.Count;

        #endregion

        #region 构造

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rootPath">存档根目录</param>
        public SlotsIndexMgr(string rootPath)
        {
            path = Path.Combine(rootPath, "slotsIndex.msgpack");
            Directory.CreateDirectory(rootPath);
            Load();
        }

        #endregion

        #region 槽位 CRUD

        /// <summary>
        /// 创建一个新的存档槽
        /// </summary>
        public ISlot CreatSlot(string displayerName)
        {
            if (data.SlotList == null)
                data.SlotList = new List<SlotData>();

            var slot = new SlotData(displayerName);
            data.SlotList.Add(slot);
            data.CurrentSlotId = slot.SlotId;
            Save();
            return slot;
        }

        /// <summary>
        /// 切换当前使用的存档槽
        /// </summary>
        public void SwitchSlot(string slotId)
        {
            if (data.SlotList == null) return;
            if (!data.SlotList.Exists(s => s.SlotId == slotId))
                throw new ArgumentException($"存档槽 {slotId} 不存在", nameof(slotId));
            data.CurrentSlotId = slotId;
            Save();
        }

        /// <summary>
        /// 删除指定的存档槽及其 dat 文件
        /// </summary>
        public void DeleteSlot(string slotId)
        {
            if (data.SlotList == null) return;
            var slot = data.SlotList.Find(s => s.SlotId == slotId);
            if (slot is null) return;

            data.SlotList.Remove(slot);

            if (data.CurrentSlotId == slotId)
            {
                data.CurrentSlotId = data.SlotList.Count > 0
                    ? data.SlotList[0].SlotId
                    : null;
            }

            Save();

            string filePath = GetSlotPath(slotId);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// 重命名指定的存档槽
        /// </summary>
        public void RenameSlot(string slotId, string newName)
        {
            if (data.SlotList == null) return;
            var slot = data.SlotList.Find(s => s.SlotId == slotId);
            if (slot != null)
            {
                slot.DisplayName = newName;
                Save();
            }
        }

        /// <summary>
        /// 更新指定的存档槽最后保存时间
        /// </summary>
        public void UpdateLastSaveTime(string slotId)
        {
            if (data.SlotList == null) return;
            var slot = data.SlotList.Find(s => s.SlotId == slotId);
            if (slot != null)
            {
                slot.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Save();
            }
        }

        #endregion

        #region 路径

        /// <summary>
        /// 获取指定的存档槽 dat 路径
        /// </summary>
        public string GetSlotPath(string slotId)
        {
            string dir = Path.GetDirectoryName(path);
            return Path.Combine(dir, $"{slotId}_SaveData.msgpack");
        }

        #endregion

        #region 孤立清理

        /// <summary>
        /// 清理孤立槽位 有索引无 dat
        /// </summary>
        public void CleanupOrphanedSlots()
        {
            if (data.SlotList == null) return;
            var orphaned = data.SlotList.Where(s => !File.Exists(GetSlotPath(s.SlotId))).ToList();
            if (orphaned.Count == 0) return;

            for (int i = orphaned.Count - 1; i >= 0; i--)
            {
                var slot = orphaned[i];
                data.SlotList.Remove(slot);

                if (data.CurrentSlotId == slot.SlotId)
                {
                    data.CurrentSlotId = data.SlotList.Count > 0
                        ? data.SlotList[0].SlotId
                        : null;
                }
            }

            Save();
        }

        /// <summary>
        /// 清理孤立文件 有 dat 无索引
        /// </summary>
        public void CleanupOrphanedFiles()
        {
            if (data.SlotList == null) return;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) return;

            string[] datFiles = Directory.GetFiles(dir, "*_SaveData.msgpack");
            var validNames = data.SlotList.Select(s => $"{s.SlotId}_SaveData.msgpack").ToHashSet();

            foreach (string file in datFiles)
            {
                if (!validNames.Contains(Path.GetFileName(file)))
                    File.Delete(file);
            }
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 从磁盘加载索引
        /// </summary>
        private void Load()
        {
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                SlotsIndexData loaded;
                try
                {
                    loaded = MessagePackSerializer.Deserialize<SlotsIndexData>(bytes);
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException($"存档槽索引 {path} 读取失败", exception);
                }
                data = loaded ?? new SlotsIndexData();
            }
            else
            {
                data = new SlotsIndexData();
            }

            if (data.SlotList == null)
                data.SlotList = new List<SlotData>();

            // 不在加载时自动清理孤立槽位 新建后尚未首次保存的槽位没有 dat 文件
        }

        /// <summary>
        /// 写入索引到磁盘
        /// </summary>
        private void Save()
        {
            AtomicFileWriter.WriteAllBytes(path, MessagePackSerializer.Serialize(data));
        }

        #endregion
    }
}
