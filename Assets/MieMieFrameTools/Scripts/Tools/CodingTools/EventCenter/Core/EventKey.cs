namespace MiMieEventBus
{
    using System;
    using System.Diagnostics;

    /// 事件 Key 类
    /// 用于标识事件的唯一性
    /// 以名称与消息类型共同标识事件槽位



    /// <summary>
    /// 事件 Key 名称接口
    /// </summary>
    public interface IEventKeyName
    {
        string Name { get; }
    }

    /// <summary>
    /// 无参事件 Key
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public readonly struct EventKey : IEquatable<EventKey>, IEventKeyName
    {

        public string Name { get; }

        /// <summary> 创建无参事件 Key </summary>
        public EventKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 比较 Key 是否相同
        /// </summary>
        public bool Equals(EventKey other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// 比较对象是否相同
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EventKey other && Equals(other);
        }

        /// <summary>
        /// 获取哈希值
        /// </summary>
        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        /// <summary>
        /// 输出事件名
        /// </summary>
        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(EventKey left, EventKey right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EventKey left, EventKey right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 消息事件 Key
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public readonly struct EventKey<T> : IEquatable<EventKey<T>>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建消息事件 Key
        /// </summary>
        public EventKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 比较 Key 是否相同
        /// </summary>
        public bool Equals(EventKey<T> other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// 比较对象是否相同
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EventKey<T> other && Equals(other);
        }

        /// <summary>
        /// 获取哈希值
        /// </summary>
        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        /// <summary>
        /// 输出事件名
        /// </summary>
        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(EventKey<T> left, EventKey<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EventKey<T> left, EventKey<T> right)
        {
            return !left.Equals(right);
        }
    }
}

