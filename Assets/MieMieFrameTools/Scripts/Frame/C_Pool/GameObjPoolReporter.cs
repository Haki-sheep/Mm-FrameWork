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
        /// 累计创建数量
        /// </summary>
        public int TotalCreated;

        /// <summary>
        /// 池上限
        /// </summary>
        public int MaxSize;

        /// <summary>
        /// 容量占用率
        /// </summary>
        public float UsageRate => MaxSize > 0 ? (float)TotalCreated / MaxSize : 0f;
    }
}
