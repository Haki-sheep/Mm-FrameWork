namespace MieMieFrameWork.Pool
{
    using System.Collections.Generic;

    public class ObjectPool
    {
        public Queue<object> poolQueue = new Queue<object>();

        #region 公共方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public ObjectPool(object obj)
        {
            PushObj(obj);
        }

        /// <summary>
        /// 将对象放入池中
        /// </summary>
        public void PushObj(object obj)
        {
            poolQueue.Enqueue(obj);
        }

        /// <summary>
        /// 从池中取出对象
        /// </summary>
        public object GetObj()
        {
            if (poolQueue.Count > 0)
                return poolQueue.Dequeue();

            return null;
        }

        #endregion
    }
}
