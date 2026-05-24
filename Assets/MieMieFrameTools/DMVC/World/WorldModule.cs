using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class WorldModule
{
    private readonly Dictionary<string, ILogicBehaviour> logicBehaviourDict = new();
    private readonly Dictionary<string, IDataBehaviour> dataBehaviourDict = new();
    private readonly Dictionary<string, IMessageBehaviour> messageBehaviourDict = new();

    public virtual void OnCreate() { }
    public virtual void OnUpdate() { }
    public virtual void OnDestroy() { }

    public virtual void OnDestroyPostProcess(object args) { }

    /// <summary>
    /// 销毁世界
    /// </summary>
    /// <param name="nameSpace"></param>
    /// <param name="pars"></param>
    public virtual void DestroyWorld(string nameSpace,object pars = null)
    {
        List<string> needRemoveList = new();
        needRemoveList.Clear();
        // 释放逻辑层脚本
        foreach (var logic in logicBehaviourDict)
        {
            if(string.Equals(logic.Value.GetType().Namespace,nameSpace))
                needRemoveList.Add(logic.Key);
        }

        foreach (var key in needRemoveList){
            logicBehaviourDict[key].OnDestroy();
            logicBehaviourDict.Remove(key);
        }

        // 释放数据层脚本
        needRemoveList.Clear();
        foreach (var data in dataBehaviourDict)
        {
            if(string.Equals(data.Value.GetType().Namespace,nameSpace))
                needRemoveList.Add(data.Key);
        }

        foreach (var key in needRemoveList){
            dataBehaviourDict[key].OnDestroy();
            dataBehaviourDict.Remove(key);
        }

        // 释放消息层脚本
        needRemoveList.Clear();
        foreach (var message in messageBehaviourDict)
        {
            if(string.Equals(message.Value.GetType().Namespace,nameSpace))
                needRemoveList.Add(message.Key);
        }

        foreach (var key in needRemoveList){
            messageBehaviourDict[key].OnDestroy();
            messageBehaviourDict.Remove(key);
        }

        // 释放自身
        this.OnDestroy();
        this.OnDestroyPostProcess(pars);
    }
    
    public T GetExistLogicCtrl<T>() where T : class, ILogicBehaviour
    {
        ILogicBehaviour logic = null;
        if (logicBehaviourDict.TryGetValue(typeof(T).Name, out logic))
        {
            return logic as T;
        }
        Debug.LogError($"GetExistLogicCtrl<{typeof(T).Name}> failed");
        return default;
    }

    public T GetExistDataCtrl<T>() where T : class, IDataBehaviour
    {
        IDataBehaviour data = null;
        if (dataBehaviourDict.TryGetValue(typeof(T).Name, out data))
        {
            return data as T;
        }
        Debug.LogError($"GetExistDataCtrl<{typeof(T).Name}> failed");
        return default;
    }

    public T GetExistMessageCtrl<T>() where T : class, IMessageBehaviour
    {
        IMessageBehaviour message = null;
        if (messageBehaviourDict.TryGetValue(typeof(T).Name, out message))
        {
            return message as T;
        }
        Debug.LogError($"GetExistMessageCtrl<{typeof(T).Name}> failed");
        return default;
    }
}
