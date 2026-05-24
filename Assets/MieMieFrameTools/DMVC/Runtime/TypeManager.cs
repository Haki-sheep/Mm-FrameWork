using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;


public class TypeManager
{
    private static IBehaviourExecution mbehaviourExecution;
    public static void InitWolrdAssemblies(WorldModule targetWorld, IBehaviourExecution behaviourExecution)
    {
        mbehaviourExecution = behaviourExecution;
        AppDomain currentDomain = AppDomain.CurrentDomain;
        Assembly[] assemblies = currentDomain.GetAssemblies();
        Assembly worldAssembly = null;

        // 获取当前脚本运行的程序集 找的是Assembly-CSharp.csproj 也就是Unity默认的脚本程序集
        foreach (var assembly in assemblies)
        {

            // 这个部分可以修改
            if (assembly.GetName().Name == "Assembly-CSharp")
            {
                worldAssembly = assembly;
                break;
            }
        }

        if (worldAssembly == null)
        {
            Debug.LogError("WorldAssembly not found, please check the Assembly-CSharp.csproj file");
            return;
        }

        // 获取三接口类型
        Type logicType = typeof(ILogicBehaviour);
        Type dataType = typeof(IDataBehaviour);
        Type messageType = typeof(IMessageBehaviour);

        // 获取程序集中的所有类型 并创建三列表用于存储排序后的类型
        Type[] typeArr = worldAssembly.GetTypes();
        List<TypeOrder> logicTypeOrderList = new();
        List<TypeOrder> dataTypeOrderList = new();
        List<TypeOrder> messageTypeOrderList = new();

        // 遍历程序集中的所有类型 
        foreach (var type in typeArr)
        {
            string space = type.Namespace;

            // 通过对比每一个类型的命名空间 判断其是否与三模块接口赋值兼容(相同或继承)
            if (type.Namespace == targetWorld.GetType().Namespace)
            {
                if (type.IsAbstract) continue;

                if (logicType.IsAssignableFrom(type))
                {
                    // 先获取脚本执行顺序 再创建TypeOrder对象 最后添加到列表中
                    int orderIndex = GetLogicBehaviourExecutionOrderIndex(type);
                    TypeOrder typeOrder = new TypeOrder(orderIndex, type);
                    logicTypeOrderList.Add(typeOrder);
                }
                if (dataType.IsAssignableFrom(type))
                {
                    int orderIndex = GetDataBehaviourExecutionOrderIndex(type);
                    TypeOrder typeOrder = new TypeOrder(orderIndex, type);
                    dataTypeOrderList.Add(typeOrder);
                }
                if (messageType.IsAssignableFrom(type))
                {
                    int orderIndex = GetMessageBehaviourExecutionOrderIndex(type);
                    TypeOrder typeOrder = new TypeOrder(orderIndex, type);
                    messageTypeOrderList.Add(typeOrder);
                }
            }
        }
        // 对列表进行排序 : 升序
        logicTypeOrderList.Sort((a, b) => a.order.CompareTo(b.order));
        dataTypeOrderList.Sort((a, b) => a.order.CompareTo(b.order));
        messageTypeOrderList.Sort((a, b) => a.order.CompareTo(b.order));

        // 初始化脚本 通过反射的方式new对象 数据->消息->逻辑
        foreach (var typeOrder in dataTypeOrderList)
        {
            var dataBehaviour = Activator.CreateInstance(typeOrder.type) as IDataBehaviour;
            targetWorld.AddDataCtrl(dataBehaviour);
        }
        foreach (var typeOrder in messageTypeOrderList)
        {
            var messageBehaviour = Activator.CreateInstance(typeOrder.type) as IMessageBehaviour;
            targetWorld.AddMessageCtrl(messageBehaviour);
        }
        foreach (var typeOrder in logicTypeOrderList)
        {
            var logicBehaviour = Activator.CreateInstance(typeOrder.type) as ILogicBehaviour;
            targetWorld.AddLogicCtrl(logicBehaviour);
        }
        // 释放列表等资源
        logicTypeOrderList.Clear();
        dataTypeOrderList.Clear();
        messageTypeOrderList.Clear();
        typeArr = null;
        logicType = null;
        dataType = null;
        messageType = null;
        mbehaviourExecution = null;
    }

    /// <summary>
    /// 获取逻辑脚本执行顺序
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>执行顺序</returns>
    private static int GetLogicBehaviourExecutionOrderIndex(Type type)
    {
        // 先获取所有逻辑脚本 通过对比返回其数组所在索引
        var logicTypeArr = mbehaviourExecution.GetLogicBehaviourExecution();
        for (int i = 0; i < logicTypeArr.Length; i++)
        {
            if (logicTypeArr[i] == type)
            {
                return i;
            }
        }
        return 999;
    }
    private static int GetDataBehaviourExecutionOrderIndex(Type type)
    {
        var dataTypeArr = mbehaviourExecution.GetDataBehaviourExecution();
        for (int i = 0; i < dataTypeArr.Length; i++)
        {
            if (dataTypeArr[i] == type)
            {
                return i;
            }
        }
        return 999;
    }
    private static int GetMessageBehaviourExecutionOrderIndex(Type type)
    {
        var messageTypeArr = mbehaviourExecution.GetMessageBehaviourExecution();
        for (int i = 0; i < messageTypeArr.Length; i++)
        {
            if (messageTypeArr[i] == type)
            {
                return i;
            }
        }
        return 999;
    }
}