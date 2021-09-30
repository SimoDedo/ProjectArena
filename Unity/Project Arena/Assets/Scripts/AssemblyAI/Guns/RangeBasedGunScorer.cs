using UnityEngine;

namespace AI.Guns
{
    public class RangeBasedGunScorer : GunScorer
    {
        [SerializeField] private float minRange;
        [SerializeField] private float maxRange;

        public override float GetGunScore(float distance)
        {
            if (distance <= maxRange && distance >= minRange)
            {
                return 1.0f;
            }

            // TODO smooth range
            // TODO ammo
            return 0.0f;
        }
    }
}