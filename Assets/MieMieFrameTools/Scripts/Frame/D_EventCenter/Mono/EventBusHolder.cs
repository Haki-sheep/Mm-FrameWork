namespace MieMieFrameWork
{
    using UnityEngine;

    /// <summary>
    /// 局部事件总线挂载组件 OnDestroy 时自动清空
    /// </summary>
    public class EventBusHolder : MonoBehaviour
    {
        /// <summary>
        /// 局部总线实例
        /// </summary>
        public TypedEventBus Bus { get; } = new TypedEventBus();

        /// <summary>
        /// 销毁时清空总线
        /// </summary>
        private void OnDestroy()
        {
            Bus.Clear();
        }
    }
}
