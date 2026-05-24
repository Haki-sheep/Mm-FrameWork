using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


    public interface IBehaviourExecution
    {
        Type[] GetLogicBehaviourExecution();
        Type[] GetDataBehaviourExecution();
        Type[] GetMessageBehaviourExecution();
    }