using UnityEngine;

namespace ScriptableObjectArchitecture
{
    public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = CreateInstance<T>();
                return s_Instance;
            }
        }
    }
}
