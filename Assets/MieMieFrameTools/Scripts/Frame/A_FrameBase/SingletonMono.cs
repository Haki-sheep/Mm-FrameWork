namespace MieMieFrameWork
{
    using UnityEngine;
    /// <summary>
    /// SingletonMono只给了GameRoot 其他单例都是用ManagerBase来管理的
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        private static readonly object locked = new();
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            lock (locked)
            {
                if (Instance != null)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this as T;
            }
        }

        private void OnDisable()
        {
            if (Instance != null)
                Instance = null;
        }
    }


}