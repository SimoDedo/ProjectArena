using System;
using UnityEngine;

namespace AI.Guns
{
    /// <summary>
    /// Component used by the AI to select the best weapon to use depending on the enemy position.
    /// </summary>
    public abstract class GunScorer : MonoBehaviour
    {
        /// <summary>
        /// Gets the score of the gun associated with this component, depending on the distance to the enemy.
        /// Can additionally get the score if pretending the charger is empty.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="fakeEmptyCharger"></param>
        /// <returns></returns>
        public abstract float GetGunScore(float distance, bool fakeEmptyCharger = false);

        /// <summary>
        /// Get the optimal range of the weapon.
        /// </summary>
        public abstract Tuple<float, float> GetOptimalRange();
    }
}