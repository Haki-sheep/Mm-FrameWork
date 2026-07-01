namespace MieMieFrameWork
{
    using UnityEngine;

    /// <summary>
    /// 局部事件总线挂载组件 OnDestroy 时自动清空
    /// 这个类可以挂在需要一块「局部事件范围」的根节点上 和被挂载对象的生命周期一致
    /// </summary>
    public class EventBusHolder : MonoBehaviour
    {
        /// <summary>
        /// 局部总线实例
        /// </summary>
        public EventBus Bus { get; } = new EventBus();

        /// <summary>
        /// 销毁时清空总线
        /// </summary>
        private void OnDestroy()
        {
            Bus.Clear();
        }
    }
}
