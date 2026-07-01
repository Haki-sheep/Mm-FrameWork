namespace MieMieFrameWork.Pool
{
    using UnityEngine;

    /// <summary>
    /// 标记实例属于哪个对象池
    /// </summary>
    public class PoolMember : MonoBehaviour
    {
        /// <summary>
        /// 所属池 Key
        /// </summary>
        public EntityId PoolKey { get; set; }
    }
}
