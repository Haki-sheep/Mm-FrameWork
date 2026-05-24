using System;
using System.Collections.Generic;
using System.Security.Cryptography;

/// <summary>
/// 继承此接口的类可以被IOC容器管理
/// </summary>
public interface IIOCContainer
{

}

/// <summary>
/// 这是一个阉割版的DI 只有存储 查找的功能，缺少 自动注入,自动生命周期管理,自动依赖链解析
/// </summary>
public class IocContainer
{
    private IocContainer iocContainer;
    //交互处理器字典
    private Dictionary<Type, IIOCContainer> ccCompDict = new();

    public virtual void AddComp2Dict<T>(T t) where T : class, IIOCContainer
    { 
        ccCompDict.Add(typeof(T), t);
    }
    /// <summary>
    /// 初始化逻辑处理器 : 这里想要做的高级一点可以用反射获取继承接口的类
    /// </summary>
    public virtual void Init()
    {
        if (iocContainer is null)
            iocContainer = new();
        else
            iocContainer = this;
    }

    public bool TryGetComp<T>(out T t) where T : class, IIOCContainer
    {
        if (ccCompDict.TryGetValue(typeof(T), out var i_ItemOperateHandler)
            && i_ItemOperateHandler is T handler)
        {
            t = handler;
            return true;
        }
        //没找到后续功能就直接不能用了 所以null也别返回了
        throw new KeyNotFoundException($"未注册[{typeof(T).Name}]类型的Handler");
    }

    public T GetComp<T>() where T : class, IIOCContainer
    {
        if (ccCompDict.TryGetValue(typeof(T), out var i_ItemOperateHandler)
            && i_ItemOperateHandler is T handler)
        {
            return handler;
        }
        //没找到后续功能就直接不能用了 所以null也别返回了
        throw new InvalidOperationException($"未注册[{typeof(T).Name}]类型的Handler");
    }

    public void ClearItemOprateHandlerDict()
    {
        ccCompDict.Clear();
    }
}
