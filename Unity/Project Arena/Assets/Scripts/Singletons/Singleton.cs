﻿using UnityEngine;

namespace Singletons
{
    /// <summary>
    ///     Singleton implementaion.
    ///     <summary>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T m_Instance;

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = FindObjectOfType<T>();

                    if (m_Instance == null) m_Instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }

                return m_Instance;
            }
        }
    }
}