using System;
using MessagePack;

namespace MiMieSaver
{
    /// <summary>
    /// 单个存档槽数据
    /// </summary>
    [MessagePackObject]
    public sealed partial class SlotData : ISlot
    {
        /// <summary>
        /// 存档槽唯一 ID
        /// </summary>
        [Key(0)]
        public string SlotId { get; set; }

        /// <summary>
        /// 存档槽显示名称
        /// </summary>
        [Key(1)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 创建时间戳
        /// </summary>
        [Key(2)]
        public long CreateTime { get; set; }

        /// <summary>
        /// 最后保存时间戳
        /// </summary>
        [Key(3)]
        public long LastSaveTime { get; set; }

        /// <summary>
        /// 默认构造
        /// </summary>
        [SerializationConstructor]
        public SlotData()
        {
            SlotId = Guid.NewGuid().ToString();
            DisplayName = "New Slot";
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 指定显示名构造
        /// </summary>
        public SlotData(string displayName) : this()
        {
            DisplayName = displayName;
        }
    }
}
