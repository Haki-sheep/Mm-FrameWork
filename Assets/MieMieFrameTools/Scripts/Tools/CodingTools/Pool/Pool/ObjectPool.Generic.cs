namespace MieMieFrameWork.Pool
{
    using System;
    using UnityEngine.Pool;

    public sealed class ObjectPool<T> : ObjectPool, IObjectPool<T> where T : class
    {
        #region 构造与获取

        /// <summary>
        /// 使用无参构造创建同类型对象
        /// </summary>
        public ObjectPool()
            : this(() => Activator.CreateInstance<T>())
        {
        }

        /// <summary>
        /// 配置对象创建与重置回调 总量为零表示不限制借出数量
        /// </summary>
        public ObjectPool(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null,
            Action<T> onDestroy = null, int maxInactive = DefaultMaxInactive, int maxTotal = 0)
            : base(typeof(T), () => createFunc(),
                onGet == null ? null : Value => onGet((T)Value),
                onRelease == null ? null : Value => onRelease((T)Value),
                onDestroy == null ? null : Value => onDestroy((T)Value), maxInactive, maxTotal)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));
        }

        /// <summary>
        /// 获取对象 空池创建 达到总量上限返回空
        /// </summary>
        public T Get() => (T)GetObj();

        /// <summary>
        /// 获取作用域租借 总量耗尽时立即报错
        /// </summary>
        public PooledObject<T> Get(out T value)
        {
            value = Get();
            if (value == null)
                throw new InvalidOperationException($"[Pool] 作用域租借超过总量上限 {typeof(T).FullName}");
            return new PooledObject<T>(value, this);
        }

        /// <summary>
        /// 归还当前池借出的对象
        /// </summary>
        public void Release(T value) => ReturnObject(value, true);

        #endregion
    }
}
