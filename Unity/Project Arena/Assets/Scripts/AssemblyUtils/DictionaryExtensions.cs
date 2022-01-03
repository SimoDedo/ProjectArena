using System;
using System.Collections.Generic;

namespace AssemblyUtils
{
    public static class DictionaryExtensions
    {
        public static void AddToKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, dynamic toAdd)
        {
            dictionary.TryGetValue(key, out var current);
            dictionary[key] = current + toAdd;
        }
    }
}