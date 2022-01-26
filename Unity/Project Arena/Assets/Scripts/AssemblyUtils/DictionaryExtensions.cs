using System;
using System.Collections.Generic;
using Accord.Statistics.Kernels;

namespace AssemblyUtils
{
    public static class DictionaryExtensions
    {
        public static void AddToKey<TKey>(this Dictionary<TKey, int> dictionary, TKey key, int toAdd)
        {
            dictionary.TryGetValue(key, out var current);
            dictionary[key] = current + toAdd;
        }

        public static void AddToKey<TKey>(this Dictionary<TKey, float> dictionary, TKey key, float toAdd)
        {
            dictionary.TryGetValue(key, out var current);
            dictionary[key] = current + toAdd;
        }

        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var rtn);
            return rtn;
        }
    }
}