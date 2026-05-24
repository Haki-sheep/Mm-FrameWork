using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public partial class WorldModule
{
    public void AddLogicCtrl(ILogicBehaviour logicBehaviour){
         logicBehaviourDict.Add(logicBehaviour.GetType().Name, logicBehaviour);
         logicBehaviour.OnCreate();
    }
    public void AddDataCtrl(IDataBehaviour dataBehaviour){
        dataBehaviourDict.Add(dataBehaviour.GetType().Name, dataBehaviour);
        dataBehaviour.OnCreate();
    }
    public void AddMessageCtrl(IMessageBehaviour messageBehaviour){
        messageBehaviourDict.Add(messageBehaviour.GetType().Name, messageBehaviour);
        messageBehaviour.OnCreate();
    }
}