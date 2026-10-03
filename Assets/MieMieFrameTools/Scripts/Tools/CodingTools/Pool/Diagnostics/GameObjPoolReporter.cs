namespace MieMieFrameWork.Pool
{
    using UnityEngine;
    /// <summary>
    /// 对象池运行时快照
    /// </summary>
    public struct GameObjPoolReporter
    {
        /// <summary>
        /// 池 Key
        /// </summary>
        public EntityId PoolKey;

        /// <summary>
        /// 预制体名称
        /// </summary>
        public string PrefabName;

        /// <summary>
        /// 闲置数量
        /// </summary>
        public int PooledCount;

        /// <summary>
        /// 借出数量
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// 当前存活数量 包含闲置与借出
        /// </summary>
        public int TotalCreated;

        /// <summary>
        /// 闲置缓存上限 保留原字段名称
        /// </summary>
        public int MaxSize;

        /// <summary> 总量上限 零表示不限制 </summary>
        public int MaxTotal;

        public int MaxInactive => MaxSize;

        /// <summary>
        /// 闲置缓存占用率
        /// </summary>
        public float UsageRate => MaxInactive > 0 ? (float)PooledCount / MaxInactive : 0f;
    }
}
