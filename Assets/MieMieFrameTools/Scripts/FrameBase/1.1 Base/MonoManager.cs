namespace MieMieFrameWork
{
    using System;
    using UnityEngine;

    /// <summary>
    /// TMono的生命周期代理
    /// </summary>
    public class MonoManager : MonoBehaviour, IManagerBase
    {
        public void Init()
        {
        }

        private Action updateEvent;
        private Action LaterUpdateEvent;
        private Action FixedUpdateEvent;

        public void AddUpdateListener(Action action)
        {
            updateEvent += action;
        }

        public void RemoveUpdateListener(Action action)
        {
            updateEvent -= action;
        }

        public void AddLaterUpdateListener(Action action)
        {
            LaterUpdateEvent += action;
        }

        public void RemoveLaterUpdateListener(Action action)
        {
            LaterUpdateEvent -= action;
        }
        public void AddFixedUpdateListener(Action action)
        {
            FixedUpdateEvent += action;
        }

        public void RemoveFixedUpdateListener(Action action)
        {
            FixedUpdateEvent -= action;
        }
        public void Update()
        {
            updateEvent?.Invoke();
        }
        private void LateUpdate()
        {
            LaterUpdateEvent?.Invoke();
        }
        private void FixedUpdate()
        {
            FixedUpdateEvent?.Invoke();
        }
    }

}