namespace MieMieFrameWork
{
    using System;

    /// <summary>
    /// 无参事件 Key
    /// </summary>
    public readonly struct EventKey : IEquatable<EventKey>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建无参事件 Key
        /// </summary>
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
    /// 单参事件 Key
    /// </summary>
    public readonly struct EventKey<T> : IEquatable<EventKey<T>>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建单参事件 Key
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

    /// <summary>
    /// 双参事件 Key
    /// </summary>
    public readonly struct EventKey<T0, T1> : IEquatable<EventKey<T0, T1>>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建双参事件 Key
        /// </summary>
        public EventKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 比较 Key 是否相同
        /// </summary>
        public bool Equals(EventKey<T0, T1> other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// 比较对象是否相同
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EventKey<T0, T1> other && Equals(other);
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
        public static bool operator ==(EventKey<T0, T1> left, EventKey<T0, T1> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EventKey<T0, T1> left, EventKey<T0, T1> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 三参事件 Key
    /// </summary>
    public readonly struct EventKey<T0, T1, T2> : IEquatable<EventKey<T0, T1, T2>>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建三参事件 Key
        /// </summary>
        public EventKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 比较 Key 是否相同
        /// </summary>
        public bool Equals(EventKey<T0, T1, T2> other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// 比较对象是否相同
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EventKey<T0, T1, T2> other && Equals(other);
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
        public static bool operator ==(EventKey<T0, T1, T2> left, EventKey<T0, T1, T2> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EventKey<T0, T1, T2> left, EventKey<T0, T1, T2> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 四参事件 Key
    /// </summary>
    public readonly struct EventKey<T0, T1, T2, T3> : IEquatable<EventKey<T0, T1, T2, T3>>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建四参事件 Key
        /// </summary>
        public EventKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 比较 Key 是否相同
        /// </summary>
        public bool Equals(EventKey<T0, T1, T2, T3> other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// 比较对象是否相同
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EventKey<T0, T1, T2, T3> other && Equals(other);
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
        public static bool operator ==(EventKey<T0, T1, T2, T3> left, EventKey<T0, T1, T2, T3> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EventKey<T0, T1, T2, T3> left, EventKey<T0, T1, T2, T3> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 五参事件 Key
    /// </summary>
    public readonly struct EventKey<T0, T1, T2, T3, T4> : IEquatable<EventKey<T0, T1, T2, T3, T4>>, IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建五参事件 Key
        /// </summary>
        public EventKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 比较 Key 是否相同
        /// </summary>
        public bool Equals(EventKey<T0, T1, T2, T3, T4> other)
        {
            return Name == other.Name;
        }

        /// <summary>
        /// 比较对象是否相同
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is EventKey<T0, T1, T2, T3, T4> other && Equals(other);
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
        public static bool operator ==(EventKey<T0, T1, T2, T3, T4> left, EventKey<T0, T1, T2, T3, T4> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(EventKey<T0, T1, T2, T3, T4> left, EventKey<T0, T1, T2, T3, T4> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 事件 Key 名称接口
    /// </summary>
    public interface IEventKeyName
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        string Name { get; }
    }
}
