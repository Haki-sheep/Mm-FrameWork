namespace MieMieFrameWork.Pool
{
    /// <summary>
    /// 对象池生命周期接口
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从池中取出时调用
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 放回池之前调用
        /// </summary>
        void OnDespawn();
    }
}
