using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MieMieFrameWork.ChainedFsm
{
    public abstract class IChainedFsm
    {
        public Action OnEnterAction;
        public Action OnExitAction;

        public virtual void OnEnter()
        {
            OnEnterAction?.Invoke();
        }
        public virtual void OnExit()
        {
            OnExitAction?.Invoke();
        }
    }
}