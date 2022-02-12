using System.Collections.Generic;

namespace Utils
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds the given value to the element with the key specified, if any.
        /// Otherwise, adds the given key to the map and associates the value to add to the key.
        /// </summary>
        public static void AddToKey<TKey>(this Dictionary<TKey, int> dictionary, TKey key, int toAdd)
        {
            dictionary.TryGetValue(key, out var current);
            dictionary[key] = current + toAdd;
        }

        /// <summary>
        /// Adds the given value to the element with the key specified, if any.
        /// Otherwise, adds the given key to the map and associates the value to add to the key.
        /// </summary>
        public static void AddToKey<TKey>(this Dictionary<TKey, float> dictionary, TKey key, float toAdd)
        {
            dictionary.TryGetValue(key, out var current);
            dictionary[key] = current + toAdd;
        }

        /// <summary>
        /// Returns the value present in the map with the given key or the default value if no such key is found.
        /// </summary>
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var rtn);
            return rtn;
        }
    }
}