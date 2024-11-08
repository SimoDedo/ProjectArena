namespace Logging
{
    public class ClassSingleton<T> where T : new()
    {
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new T();
                return s_Instance;
            }
        }
    }
}