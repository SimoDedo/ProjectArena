using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

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
