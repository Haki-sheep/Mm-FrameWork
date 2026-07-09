namespace MieMieFrameWork
{
    using MiMieEventBus;
    using UnityEngine;

    /// <summary>
    /// 局部事件总线挂载组件 OnDestroy 时自动清空
    /// </summary>
    public class MmLocalEventBusMono : MonoBehaviour
    {
        /// <summary>
        /// 局部总线实例
        /// </summary>
        public EventBusCore LocalBus { get; } = new EventBusCore();

        /// <summary>
        /// 销毁时清空总线
        /// </summary>
        private void OnDestroy()
        {
            LocalBus.Clear();
        }
    }
}
