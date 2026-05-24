using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MieMieFrameTools
{
    /// <summary>
    /// 存档槽目录管理器
    /// 管理 slotsIndex.json：槽位索引、CRUD、孤立数据清理
    /// </summary>
    public class SlotsIndexMgr : ISlotIndex
    {
        private readonly string path;
        private SlotsIndexData data;

        public string CurrentSlotId => data.currentSlotId;
        public ISlot CurrentSlot => data.slots.FirstOrDefault(s => s.SlotId == data.currentSlotId);
        public IReadOnlyList<ISlot> Slots => data.slots;
        public int Count => data.slots.Count;

        public SlotsIndexMgr(string rootPath)
        {
            path = Path.Combine(rootPath, "slotsIndex.json");
            Directory.CreateDirectory(rootPath);
            Load();
        }

        /// <summary>创建一个新的存档槽</summary>
        public ISlot CreatSlot(string displayerName)
        {
            if (data.slots == null)
                data.slots = new List<SlotData>();
            var slot = new SlotData(displayerName);
            data.slots.Add(slot);
            data.currentSlotId = slot.SlotId;
            Save();
            return slot;
        }

        /// <summary>切换当前使用的存档槽</summary>
        public void SwitchSlot(string slotId)
        {
            if (data.slots == null) return;
            data.currentSlotId = slotId;
            Save();
        }

        /// <summary>删除指定的存档槽，及其对应的 .dat 文件</summary>
        public void DeleteSlot(string slotId)
        {
            if (data.slots == null) return;
            var slot = data.slots.Find(s => s.SlotId == slotId);
            if (slot is null) return;

            data.slots.Remove(slot);

            if (data.currentSlotId == slotId)
            {
                data.currentSlotId = data.slots.Count > 0
                    ? data.slots[0].SlotId
                    : null;
            }

            Save();

            string filePath = GetSlotPath(slotId);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>重命名指定的存档槽</summary>
        public void RenameSlot(string slotId, string newName)
        {
            if (data.slots == null) return;
            var slot = data.slots.Find(s => s.SlotId == slotId);
            if (slot is not null)
            {
                slot.DisplayName = newName;
                Save();
            }
        }

        /// <summary>更新指定的存档槽最后保存时间</summary>
        public void UpdateLastSaveTime(string slotId)
        {
            if (data.slots == null) return;
            var slot = data.slots.Find(s => s.SlotId == slotId);
            if (slot is not null)
            {
                slot.LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Save();
            }
        }

        /// <summary>获取指定的存档槽路径</summary>
        public string GetSlotPath(string slotId)
        {
            string dir = Path.GetDirectoryName(path);
            return Path.Combine(dir, $"{slotId}_SaveData.dat");
        }

        /// <summary>
        /// 清理孤立槽位：有索引记录但无对应 .dat 文件的槽位
        /// </summary>
        public void CleanupOrphanedSlots()
        {
            if (data.slots == null) return;
            var orphaned = data.slots.Where(s => !File.Exists(GetSlotPath(s.SlotId))).ToList();
            if (orphaned.Count == 0) return;

            // 反序遍历，避免 List 容量变化导致跳元素
            for (int i = orphaned.Count - 1; i >= 0; i--)
            {
                var slot = orphaned[i];
                data.slots.Remove(slot);

                if (data.currentSlotId == slot.SlotId)
                {
                    data.currentSlotId = data.slots.Count > 0
                        ? data.slots[0].SlotId
                        : null;
                }
            }
            Save();
        }

        /// <summary>
        /// 清理孤立文件：存在于磁盘但不在 slotsIndex.json 索引中的 .dat 文件
        /// </summary>
        public void CleanupOrphanedFiles()
        {
            if (data.slots == null) return;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) return;

            string[] datFiles = Directory.GetFiles(dir, "*_SaveData.dat");
            var validNames = data.slots.Select(s => $"{s.SlotId}_SaveData.dat").ToHashSet();

            foreach (string file in datFiles)
            {
                if (!validNames.Contains(Path.GetFileName(file)))
                    File.Delete(file);
            }
        }

        private void Load()
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var loaded = JsonConvert.DeserializeObject<SlotsIndexData>(json);
                data = loaded ?? new SlotsIndexData();
            }
            else
            {
                data = new SlotsIndexData();
            }

            // 确保 slots 不为 null，防止 JSON 中 slots 为 null 导致后续操作崩溃
            if (data.slots == null)
                data.slots = new List<SlotData>();

            // 启动时自动修复数据：移除无效的槽位记录
            CleanupOrphanedSlots();
        }

        private void Save()
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
