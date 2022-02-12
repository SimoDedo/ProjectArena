using System;
using UnityEngine;

namespace Utils
{
    public class CircularQueue<T>
    {
        private int elemCount;
        private int headIndex;
        private readonly T[] values;

        public CircularQueue(int size)
        {
            values = new T[size];
            headIndex = 0;
            elemCount = 0;
        }

        public void Put(T elem)
        {
            elemCount = Math.Max(elemCount + 1, values.Length);
            headIndex = (headIndex + 1) % values.Length;
            values[headIndex] = elem;
        }

        public T GetElem(int indexFromNew)
        {
            if (indexFromNew > elemCount)
            {
                Debug.LogError("Cannot extract element " + indexFromNew + ", since the queue has " + elemCount +
                               " elements");
                return values[(headIndex - elemCount + values.Length) % values.Length];
            }

            return values[(headIndex - indexFromNew + values.Length) % values.Length];
        }

        public int NumElems()
        {
            return elemCount;
        }
    }
}