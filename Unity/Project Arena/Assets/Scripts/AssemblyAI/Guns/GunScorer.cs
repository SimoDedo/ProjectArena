using System;
using UnityEngine;

namespace AI.Guns
{
    public abstract class GunScorer : MonoBehaviour
    {
        public abstract float GetGunScore(float distance);
        public abstract Tuple<float, float> GetOptimalRange();
    }
}