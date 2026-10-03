namespace MieMieFrameWork.Pool
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class ObjectPool : IDisposable
    {
        /// <summary> 默认闲置缓存上限 </summary>
        public const int DefaultMaxInactive = 50;

        /// <summary> 池对象类型 </summary>
        private readonly Type objectType;

        /// <summary> 对象创建函数 </summary>
        private readonly Func<object> createFunc;

        /// <summary> 取出对象回调 </summary>
        private readonly Action<object> onGet;

        /// <summary> 归还对象回调 </summary>
        private readonly Action<object> onRelease;

        /// <summary> 销毁对象回调 </summary>
        private readonly Action<object> onDestroy;

        /// <summary> 闲置对象队列 </summary>
        private readonly Queue<object> poolQueue = new();

        /// <summary> 借出对象集合 </summary>
        private readonly HashSet<object> activeObjectHashList = new(ReferenceComparer.Instance);

        /// <summary> 闲置对象集合 </summary>
        private readonly HashSet<object> pooledObjectHashList = new(ReferenceComparer.Instance);

        /// <summary> 生命周期回调执行状态 禁止同池重入 </summary>
        private bool isInvokingCallback;

        public int CountInactive => poolQueue.Count;
        public int CountActive => activeObjectHashList.Count;
        public int CountAll => CountInactive + CountActive;
        public int MaxInactive { get; }
        public int MaxTotal { get; }
        public bool IsDisposed { get; private set; }

        #region 构造与获取

        /// <summary>
        /// 构造函数 将外部对象加入同类型池
        /// </summary>
        public ObjectPool(object obj)
            : this(obj.GetType(), () => Activator.CreateInstance(obj.GetType()), null, null, null, DefaultMaxInactive, 0)
        {
            ReturnObject(obj, false);
        }

        /// <summary>
        /// 配置创建函数与对象生命周期
        /// </summary>
        protected ObjectPool(Type objectType, Func<object> createFunc, Action<object> onGet,
            Action<object> onRelease, Action<object> onDestroy, int maxInactive, int maxTotal)
        {
            if (maxInactive < 0 || maxTotal < 0)
                throw new ArgumentOutOfRangeException(nameof(maxInactive), "对象池容量不能为负数");

            this.objectType = objectType;
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.onDestroy = onDestroy;
            MaxInactive = maxInactive;
            MaxTotal = maxTotal;
        }

        /// <summary>
        /// 从池中取出对象 空池按创建函数补充 达到总量上限返回空
        /// </summary>
        public object GetObj()
        {
            ThrowIfDisposed();
            object Value;
            if (poolQueue.Count > 0)
            {
                Value = poolQueue.Dequeue();
                pooledObjectHashList.Remove(Value);
            }
            else
            {
                if (MaxTotal > 0 && CountAll >= MaxTotal)
                    return null;

                Value = CreateObject();
                ValidateType(Value);
            }

            if (!activeObjectHashList.Add(Value))
                throw new InvalidOperationException($"[Pool] 创建函数重复返回借出对象 {objectType.FullName}");

            try
            {
                InvokeCallback(onGet, Value);
            }
            catch (Exception Exception)
            {
                activeObjectHashList.Remove(Value);
                var ErrorList = new List<Exception> { Exception };
                RunCleanup(onRelease, Value, ErrorList);
                RunCleanup(onDestroy, Value, ErrorList);
                throw new AggregateException($"[Pool] 获取对象失败 {objectType.FullName}", ErrorList);
            }
            return Value;
        }

        #endregion

        #region 归还与清理

        /// <summary>
        /// 将当前池借出的对象放入池中
        /// </summary>
        public void PushObj(object obj)
        {
            ReturnObject(obj, true);
        }

        /// <summary>
        /// 归还对象并重置状态 严格模式只接受当前池借出对象
        /// </summary>
        protected void ReturnObject(object obj, bool requireActive)
        {
            ThrowIfDisposed();
            ValidateType(obj);
            if (pooledObjectHashList.Contains(obj))
                throw new InvalidOperationException($"[Pool] 重复归还 {objectType.FullName}");

            bool IsActive = activeObjectHashList.Contains(obj);
            if (requireActive && !IsActive)
                throw new InvalidOperationException($"[Pool] 非法归还 {objectType.FullName} 非当前池借出对象");
            if (!IsActive && MaxTotal > 0 && CountAll >= MaxTotal)
                throw new InvalidOperationException($"[Pool] 接管对象超过总量上限 {objectType.FullName}");

            InvokeCallback(onRelease, obj);
            activeObjectHashList.Remove(obj);
            if (poolQueue.Count >= MaxInactive)
            {
                InvokeCallback(onDestroy, obj);
                return;
            }

            poolQueue.Enqueue(obj);
            pooledObjectHashList.Add(obj);
        }

        /// <summary>
        /// 清空闲置缓存 保留借出记录与生命周期配置
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            while (poolQueue.Count > 0)
            {
                var Value = poolQueue.Dequeue();
                pooledObjectHashList.Remove(Value);
                InvokeCallback(onDestroy, Value);
            }
        }

        /// <summary>
        /// 销毁整个池 重置借出对象并执行销毁回调
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            ThrowIfDisposed();
            IsDisposed = true;
            var ErrorList = new List<Exception>();
            foreach (var Value in activeObjectHashList)
            {
                RunCleanup(onRelease, Value, ErrorList);
                RunCleanup(onDestroy, Value, ErrorList);
            }

            activeObjectHashList.Clear();
            while (poolQueue.Count > 0)
                RunCleanup(onDestroy, poolQueue.Dequeue(), ErrorList);
            pooledObjectHashList.Clear();

            if (ErrorList.Count > 0)
                throw new AggregateException($"[Pool] 销毁对象池失败 {objectType.FullName}", ErrorList);
        }

        #endregion

        #region 内部约束

        /// <summary>
        /// 销毁前检查同池回调状态 不改变对象归属
        /// </summary>
        internal void ValidateDisposal()
        {
            if (!IsDisposed)
                ThrowIfDisposed();
        }

        /// <summary>
        /// 执行创建函数 禁止工厂改变当前池生命周期
        /// </summary>
        private object CreateObject()
        {
            isInvokingCallback = true;
            try
            {
                return createFunc();
            }
            finally
            {
                isInvokingCallback = false;
            }
        }

        /// <summary>
        /// 检查池生命周期 已销毁则立即报错
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException($"ObjectPool<{objectType.FullName}>");
            if (isInvokingCallback)
                throw new InvalidOperationException($"[Pool] 生命周期回调禁止同池重入 {objectType.FullName}");
        }

        /// <summary>
        /// 检查对象具体类型 保持管理器按类型归还契约
        /// </summary>
        private void ValidateType(object obj)
        {
            if (obj == null || obj.GetType() != objectType)
                throw new ArgumentException($"[Pool] 对象类型必须为 {objectType.FullName}", nameof(obj));
        }

        /// <summary>
        /// 执行清理回调 收集异常后继续释放剩余对象
        /// </summary>
        private void RunCleanup(Action<object> callback, object obj, List<Exception> errorList)
        {
            try
            {
                InvokeCallback(callback, obj);
            }
            catch (Exception Exception)
            {
                errorList.Add(Exception);
            }
        }

        /// <summary>
        /// 执行生命周期回调并保护同池状态转换
        /// </summary>
        private void InvokeCallback(Action<object> callback, object obj)
        {
            if (callback == null)
                return;
            isInvokingCallback = true;
            try
            {
                callback(obj);
            }
            finally
            {
                isInvokingCallback = false;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            /// <summary> 无状态引用比较器 </summary>
            public static readonly ReferenceComparer Instance = new();

            /// <summary>
            /// 按实例引用比较 不使用业务相等规则
            /// </summary>
            public new bool Equals(object left, object right) => ReferenceEquals(left, right);

            /// <summary>
            /// 获取实例引用哈希 不使用业务哈希规则
            /// </summary>
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

        #endregion
    }
}
