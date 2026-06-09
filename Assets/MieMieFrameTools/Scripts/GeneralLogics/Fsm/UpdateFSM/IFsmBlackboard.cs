
namespace MieMieFrameWork.FSM
{
    /// <summary>
    /// FSM黑板接口 - 用于状态间数据共享
    /// </summary>
    public interface IFsmBlackboard
    {
        /// <summary>
        /// 设置数据
        /// </summary>
        public void SetValue<T>(EBlockBoardParme key, T value);

        /// <summary>
        /// 获取数据
        /// </summary>
        public T GetValue<T>(EBlockBoardParme key, T defaultValue = default);

        /// <summary>
        /// 检查是否存在指定键
        /// </summary>
        public bool HasKey(EBlockBoardParme key);

        /// <summary>
        /// 移除数据
        /// </summary>
        public void RemoveValue(EBlockBoardParme key);

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear();
    }

}
