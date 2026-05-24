using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MieMieFrameTools
{
    /// <summary>
    /// 存档槽目录
    /// </summary>
    public interface ISlotIndex
    {
        /// <summary>
        /// 当前选中槽位
        /// </summary>
        ISlot CurrentSlot { get; }
        /// <summary>
        /// 所有槽位列表
        /// </summary>
        IReadOnlyList<ISlot> Slots { get; }
        /// <summary>
        /// 槽位数量
        /// </summary>
        int Count { get; }
    }
}