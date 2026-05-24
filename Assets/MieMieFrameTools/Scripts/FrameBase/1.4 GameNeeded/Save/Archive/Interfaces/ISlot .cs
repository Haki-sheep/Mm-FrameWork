using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MieMieFrameTools
{
    /// <summary>
    /// 单个存档槽信息接口
    /// </summary>
    public interface ISlot 
    {
        /// <summary>
        /// 存档槽唯一ID
        /// </summary>
        string SlotId { get; }
        
        /// <summary>
        /// 存档槽显示名称
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// 创建时间戳
        /// </summary>
        long CreateTime { get; }
        
        /// <summary>
        /// 最后保存时间戳
        /// </summary>
        long LastSaveTime { get; }
    }
}