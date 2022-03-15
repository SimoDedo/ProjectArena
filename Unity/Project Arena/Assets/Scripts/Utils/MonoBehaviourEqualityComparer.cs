using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// A comparer that checks that an object is equal to another, ignoring nullability concerns. Use with care!
    /// </summary>
    public class MonoBehaviourEqualityComparer<T>: IEqualityComparer<T> where T : MonoBehaviour
    {
        public bool Equals(T x, T y)
        {
            return x.GetInstanceID() == y.GetInstanceID();
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}