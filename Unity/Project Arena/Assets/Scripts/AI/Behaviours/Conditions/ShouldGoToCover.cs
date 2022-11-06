using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entity.Component;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Return Success if we are better of searching for cover, Failure otherwise.
    /// </summary>
    [Serializable]
    public class ShouldGoToCover : Conditional
    {
        private const float TIMEOUT = 1.5f;

        private const float CHARGER_PERCENTAGE = 0.4f;

        // If true, we must now consider the random failure probability until we find out that we no longer need to
        // cover
        [SerializeField] private SharedBool isGoingToCover;

        private Entity.Entity enemy;
        private AIEntity entity;
        private float canSelectCoverProbability;
        private GunManager gunManager;
        private float avoidCoverProbability = 0.3f;

        private float nextCoverAttempt;

        public override void OnAwake()
        {
            entity = gameObject.GetComponent<AIEntity>();
            canSelectCoverProbability = entity.CanSelectCoverProbability;
            enemy = entity.GetEnemy();
            gunManager = entity.GunManager;
            var recklessness = entity.Recklessness;
            switch (recklessness)
            {
                case Recklessness.High:
                    avoidCoverProbability *= 3.0f;
                    break;
                case Recklessness.Low:
                    avoidCoverProbability /= 2f;
                    break;
                case Recklessness.Neutral:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (Random.value > canSelectCoverProbability)
            {
                // Too noob to consider cover.
                nextCoverAttempt = Time.time + TIMEOUT;
                return TaskStatus.Failure;
            }

            if (nextCoverAttempt >= Time.time)
                // We were able to cover some time ago but decided not to. Do not change our mind too soon
                return TaskStatus.Failure;

            var currentGun = gunManager.CurrentGunIndex;
            // TODO Do not evaluate by percentage but by speed of depletion of ammo
            var currentAmmo = gunManager.GetAmmoInChargerForGun(currentGun);
            var chargerSize = gunManager.GetChargerSizeForGun(currentGun);
            if (currentAmmo / (float) chargerSize > CHARGER_PERCENTAGE)
            {
                isGoingToCover.Value = false;
                // It is too soon to search for cover
                return TaskStatus.Failure;
            }

            var enemyDistance = (transform.position - enemy.transform.position).magnitude;
            var bestGunScore = float.MinValue;
            var bestGunIndex = -1;
            for (var i = 0; i < gunManager.NumberOfGuns; i++)
            {
                if (!gunManager.IsGunActive(i)) continue;
                var currentScore = gunManager.GetGunScore(i, enemyDistance, i == currentGun);
                if (currentScore > bestGunScore)
                {
                    bestGunScore = currentScore;
                    bestGunIndex = i;
                }
            }

            if (bestGunIndex != currentGun)
            {
                // We do not care about reloading the current weapon since we have a better one ready
                isGoingToCover.Value = false;
                return TaskStatus.Failure;
            }

            // TODO Make this a bot parameter? Tendency to cover?
            if (!isGoingToCover.Value && Random.value < avoidCoverProbability)
            {
                nextCoverAttempt = Time.time + TIMEOUT;
                // Avoid always rushing towards a cover point to not be predictable
                return TaskStatus.Failure;
            }

            isGoingToCover.Value = true;
            return TaskStatus.Success;
        }
    }
}