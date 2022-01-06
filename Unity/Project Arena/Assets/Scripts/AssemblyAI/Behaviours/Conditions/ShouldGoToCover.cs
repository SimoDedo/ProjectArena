using System;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AssemblyAI.Behaviours.Conditions
{
    // We might want to search for cover if the weapon we are using is running out of ammo
    // and, despite this, it remains the best weapon we can use in this situation.
    // Despite this, apply a random probability of actually going to take cover!
    
    // TODO Check how often conditionals with abort are reevaluated
    [Serializable]
    public class ShouldGoToCover : Conditional
    {
        private AIEntity entity;
        private Entity enemy;
        private GunManager gunManager;
        /// <summary>
        /// If true, we must now consider the random failure probability until we find out that we no longer
        /// need to cover
        /// </summary>
        [SerializeField] private SharedBool isGoingToCover;

        private float nextCoverAttempt;

        private const float TIMEOUT = 1.5f;
        
        private const float CHARGER_PERCENTAGE = 0.4f;
        private const float AVOID_COVER_PROBABILITY = 0.3f;
        
        public override void OnAwake()
        {
            entity = gameObject.GetComponent<AIEntity>();
            enemy = entity.GetEnemy();
            gunManager = entity.GunManager;
        }

        public override TaskStatus OnUpdate()
        {
            if (nextCoverAttempt >= Time.time)
            {
                // We were able to cover some time ago but decided not to. Do not change our mind too soon
                return TaskStatus.Failure;
            }

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
            if (!isGoingToCover.Value && Random.value < AVOID_COVER_PROBABILITY)
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