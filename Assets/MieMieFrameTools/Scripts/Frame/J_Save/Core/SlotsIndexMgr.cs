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
            var Slot = new SlotData(displayerName);
            var Candidate = CreateCandidate();
            Candidate.SlotList.Add(Slot);
            Candidate.CurrentSlotId = Slot.SlotId;
            Save(Candidate);

            data.SlotList.Add(Slot);
            data.CurrentSlotId = Slot.SlotId;
            return Slot;
        }

        /// <summary>
        /// 切换当前使用的存档槽
        /// </summary>
        public void SwitchSlot(string slotId)
        {
            if (!data.SlotList.Exists(s => s.SlotId == slotId))
                throw new ArgumentException($"存档槽 {slotId} 不存在", nameof(slotId));

            Save(new SlotsIndexData { CurrentSlotId = slotId, SlotList = data.SlotList });
            data.CurrentSlotId = slotId;
        }

        /// <summary>
        /// 删除指定的存档槽及其 dat 文件
        /// </summary>
        public void DeleteSlot(string slotId)
        {
            var Slot = data.SlotList.Find(s => s.SlotId == slotId);
            if (Slot is null) return;

            var Candidate = CreateCandidate();
            Candidate.SlotList.Remove(Slot);
            if (Candidate.CurrentSlotId == slotId)
                Candidate.CurrentSlotId = Candidate.SlotList.FirstOrDefault()?.SlotId;
            Save(Candidate);

            data.SlotList.Remove(Slot);
            data.CurrentSlotId = Candidate.CurrentSlotId;

            string filePath = GetSlotPath(slotId);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        /// <summary>
        /// 重命名指定的存档槽
        /// </summary>
        public void RenameSlot(string slotId, string newName)
        {
            var Slot = data.SlotList.Find(s => s.SlotId == slotId);
            if (Slot != null)
                SaveSlotChange(Slot, newName, Slot.LastSaveTime);
        }

        /// <summary>
        /// 更新指定的存档槽最后保存时间
        /// </summary>
        public void UpdateLastSaveTime(string slotId)
        {
            UpdateLastSaveTime(slotId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        internal void UpdateLastSaveTime(string slotId, long lastSaveTime)
        {
            var Slot = data.SlotList.Find(s => s.SlotId == slotId);
            if (Slot != null)
                SaveSlotChange(Slot, Slot.DisplayName, lastSaveTime);
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
            var (OrphanedSlotHashList, _) = GetCleanupCandidates();
            if (OrphanedSlotHashList.Count == 0) return;

            var PreviousSlot = data.SlotList.Find(s => s.SlotId == data.CurrentSlotId);
            var Candidate = CreateCandidate();
            Candidate.SlotList.RemoveAll(OrphanedSlotHashList.Contains);
            if (OrphanedSlotHashList.Contains(PreviousSlot))
                Candidate.CurrentSlotId = Candidate.SlotList.FirstOrDefault()?.SlotId;
            Save(Candidate);

            data.SlotList.Clear();
            data.SlotList.AddRange(Candidate.SlotList);
            data.CurrentSlotId = Candidate.CurrentSlotId;
        }

        /// <summary>
        /// 清理孤立文件 有 dat 无索引
        /// </summary>
        public void CleanupOrphanedFiles()
        {
            var (_, OrphanedFileList) = GetCleanupCandidates();
            foreach (string FilePath in OrphanedFileList)
                File.Delete(FilePath);
        }

        private (HashSet<SlotData> SlotHashList, List<string> FileList) GetCleanupCandidates()
        {
            string Dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(data.CurrentSlotId) && CurrentSlot == null)
                throw new InvalidDataException($"存档目录 {Dir} 的当前槽位 {data.CurrentSlotId} 在索引中不存在 已中止清理");

            var OrphanedSlotHashList = data.SlotList.Where(s => !File.Exists(GetSlotPath(s.SlotId))).ToHashSet();
            var OrphanedFileList = new List<string>();
            if (Directory.Exists(Dir))
            {
                var KnownFileNameHashList = data.SlotList.Select(s => Path.GetFileName(GetSlotPath(s.SlotId))).ToHashSet();
                foreach (string FilePath in Directory.EnumerateFiles(Dir, "*_SaveData.msgpack"))
                {
                    if (!KnownFileNameHashList.Contains(Path.GetFileName(FilePath)))
                        OrphanedFileList.Add(FilePath);
                }
            }

            if (OrphanedFileList.Count > 0 && !File.Exists(path))
                throw new InvalidDataException($"存档槽索引 {path} 已缺失 已中止清理");
            if (OrphanedSlotHashList.Count > 0 && OrphanedFileList.Count > 0)
                throw new InvalidDataException($"存档目录 {Dir} 同时存在 {OrphanedSlotHashList.Count} 个缺正文的槽位和 {OrphanedFileList.Count} 个无索引的正文 已中止清理");

            return (OrphanedSlotHashList, OrphanedFileList);
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
                if (loaded?.SlotList == null)
                    throw new InvalidDataException($"存档槽索引 {path} 缺少槽位列表");
                data = loaded;
            }
            else
            {
                if (Directory.EnumerateFiles(Path.GetDirectoryName(path), "*_SaveData.msgpack").Any())
                    throw new InvalidDataException($"存档目录中已有正文但缺少索引 {path}");
                data = new SlotsIndexData();
            }

            // 不在加载时自动清理孤立槽位 新建后尚未首次保存的槽位没有 dat 文件
        }

        private SlotsIndexData CreateCandidate()
        {
            return new SlotsIndexData
            {
                CurrentSlotId = data.CurrentSlotId,
                SlotList = new List<SlotData>(data.SlotList)
            };
        }

        /// <summary>
        /// 落盘后更新原槽位对象 使业务持有的 ISlot 继续反映最新值
        /// </summary>
        private void SaveSlotChange(SlotData slot, string displayName, long lastSaveTime)
        {
            var Candidate = CreateCandidate();
            Candidate.SlotList[Candidate.SlotList.IndexOf(slot)] = new SlotData
            {
                SlotId = slot.SlotId,
                DisplayName = displayName,
                CreateTime = slot.CreateTime,
                LastSaveTime = lastSaveTime
            };
            Save(Candidate);

            slot.DisplayName = displayName;
            slot.LastSaveTime = lastSaveTime;
        }

        /// <summary>
        /// 写入索引到磁盘
        /// </summary>
        private void Save(SlotsIndexData candidate)
        {
            AtomicFileWriter.WriteAllBytes(path, MessagePackSerializer.Serialize(candidate));
        }

        #endregion
    }
}
