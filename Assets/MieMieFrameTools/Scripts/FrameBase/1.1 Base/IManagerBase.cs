using System;

namespace MieMieFrameWork
{
    public interface IManagerBase
    {
        public void Init();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ManagerAttribute : Attribute
    {
        public int Priority { get; }

        public ManagerAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}