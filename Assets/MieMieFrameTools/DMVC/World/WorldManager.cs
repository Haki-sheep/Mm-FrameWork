using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager
{
    private static List<WorldModule> worldModuleList = new();
    
    // 存储世界模块与其对应的脚本执行顺序
    private static readonly Dictionary<Type, Func<IBehaviourExecution>> worldExecutionDict = new()
    {
       
    };
    public static WorldModule defaultWorldModule {get;private set;}

    /// <summary>
    /// 创建世界模块
    /// </summary>
    /// <typeparam name="T">世界模块类型</typeparam>
    public static void CreateWorld<T>() where T : WorldModule, new()
    {
        T world = new T();
        defaultWorldModule = world;
        
        TypeManager.InitWolrdAssemblies(world,GetBehaviourExecution(world));
        world.OnCreate();
        worldModuleList.Add(world);
    }

    /// <summary>
    /// 获取世界模块
    /// </summary>
    /// <typeparam name="T">世界模块类型</typeparam>
    /// <returns>世界模块</returns>
    public static WorldModule GetWorldModule<T>() where T : WorldModule,new()
    {
        foreach (var module in worldModuleList)
        {
            if(module.GetType() == typeof(T))
            {
                return module;
            }
        }

        Debug.LogError($"WorldModule {typeof(T).Name} not found");
        return null;
    }

    /// <summary>
    /// 获取世界模块的脚本执行顺序
    /// </summary>
    /// <param name="world">世界模块</param>
    /// <returns>脚本执行顺序</returns>
    public static IBehaviourExecution GetBehaviourExecution(WorldModule world){
        if (worldExecutionDict.TryGetValue(world.GetType(), out var factory))
        {
            return factory();
        }
        return null;
    }

    public static void DestroyWorld<T>(object pars = null) where T : WorldModule
    {
        foreach (var module in worldModuleList)
        {
            if(defaultWorldModule.GetType().Name == module.GetType().Name)
            {
                module.DestroyWorld(typeof(T).Namespace,pars);
                worldModuleList.Remove(module);
                break;
            }
        }
    }
}
