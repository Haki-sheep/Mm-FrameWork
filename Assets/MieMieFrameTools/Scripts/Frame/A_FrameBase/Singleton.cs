namespace MieMieFrameWork
{
    /// <summary>
    /// 无需继承Mono的单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> where T : Singleton<T>, new()
    {
        private readonly static object locked = new object();
        private static T instance;

        public static T Instance
        {
            get
            {
                lock (locked)
                {
                    if (instance == null)
                        instance = new T();
                    return instance;
                }

            }
        }
    }

}