using System;
using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.Behaviours.Variables;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Utils;

namespace AI.AI.Layer3
{
    public class PickupPlanner : MonoBehaviour
    {
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;

        private AIEntity entity;
        private NavMeshPath chosenPath;
        private Pickable chosenPickup;

        private float entityLastHealth = 0f;
        private int[] entityLastAmmo = new int[0];

        private float agentSpeed;
        private float lastKBUpdateTime;
        private bool isFirstTime = true;

        // TODO No more entity, provide Guns instead
        public void SetParameters(AIEntity entity, NavigationSystem navSystem, PickupKnowledgeBase knowledgeBase)
        {
            this.entity = entity;
            this.navSystem = navSystem;
            this.knowledgeBase = knowledgeBase;
            agentSpeed = navSystem.GetSpeed();
        }

        private float CalculateCurrentPickupScore()
        {
            if (chosenPickup == null)
                return 0f;
            var kb = knowledgeBase.GetPickupKnowledge();
            var (value, time) = ScorePickup(kb, chosenPickup, out _);
            return Mathf.Min(1f, value / time / 10);
        }

        public Pickable ScorePickups(out NavMeshPath path, out float bestPickupScore, out float activationTime)
        {
            var pickables = knowledgeBase.GetPickupKnowledge();
            var bestPickupVelocity = float.MinValue;
            var bestPickupTime = float.MinValue;
            bestPickupScore = 0;
            path = null;
            Pickable bestPickup = null;

            // FIXME: maybe we can avoid expensive computations, but... when can we be sure that avoiding
            //   recalculation is possible?
            // if (CanUsePreviousResult())
            // {
            //     bestPickupScore = CalculateCurrentPickupScore();
            //     path = chosenPath;
            //     activationTime = pickables[chosenPickup];
            //     return chosenPickup;
            // }
            //
            // UpdateComputationSkipValues();

            foreach (var entry in pickables)
            {
                var (value, time) = ScorePickup(pickables, entry.Key, out var pickupPath);
                if (value == 0) continue;
                var velocity = value / time;
                if (velocity > bestPickupVelocity || Math.Abs(velocity - bestPickupVelocity) < 0.05 && time < bestPickupTime)
                {
                    bestPickupScore = Mathf.Min(1f, value / time / 10);
                    bestPickupVelocity = velocity;
                    bestPickupTime = time;
                    bestPickup = entry.Key;
                    chosenPath = path = pickupPath;
                }
            }

            chosenPickup = bestPickup;
            activationTime = chosenPickup == null ? float.MinValue : pickables[chosenPickup];
            return bestPickup;
        }

        private void UpdateComputationSkipValues()
        {
            isFirstTime = false;
            lastKBUpdateTime = knowledgeBase.GetLastUpdateTime();
            entityLastHealth = entity.Health;
            var totalGuns = entity.GetGuns().Count;
            if (entityLastAmmo.Length != totalGuns)
                entityLastAmmo = new int[totalGuns];
            for (var i=0; i<totalGuns; i++)
                entityLastAmmo[i] = entity.GetTotalAmmoForGun(i);
        }

        private bool CanUsePreviousResult()
        {
            var currentHealth = entity.Health;
            if (currentHealth != entityLastHealth) return false;
            var totalGuns = entity.GetGuns().Count;
            if (totalGuns != entityLastAmmo.Length) return false;
            for (var i=0; i<totalGuns; i++)
                if (entity.GetTotalAmmoForGun(i) != entityLastAmmo[i]) return false;
            return !isFirstTime && lastKBUpdateTime == knowledgeBase.GetLastUpdateTime();
        }

        private Tuple<float, float> ScorePickup(Dictionary<Pickable, float> pickables, Pickable pickup, out NavMeshPath path)
        {
            var activationTime = pickables[pickup];
            var pickupPosition = pickup.transform.position;

            // TODO don't recalculate path if this is current pickup (navSystem has latest path?)
            path = navSystem.CalculatePath(pickupPosition);
            var pathLength = path.Length();
            var pathTime = pathLength / agentSpeed;

            var estimatedArrival = pathTime + Time.time;
            // CAN BE NEGATIVE, not necessarily a good thing
            var waitTime = activationTime - estimatedArrival;

            var totalTime = pathTime + Mathf.Max(0f, waitTime);
            
            var distanceScore = ScoreDistance(pathLength) * DISTANCE_MODIFIER;
            var timeScore = ScoreTime(waitTime) * TIME_MODIFIER;
            var neighborhoodScore = ScoreNeighborhood(pickup, pickables) * NEIGHBORHOOD_MODIFIER;

            float valueScore;

            switch (pickup.GetPickupType())
            {
                case Pickable.PickupType.MEDKIT:
                {
                    var medkit = pickup as MedkitPickable;
                    Debug.Assert(medkit != null, nameof(medkit) + " != null");
                    valueScore = ScoreMedkit(medkit) * VALUE_MODIFIER;
                    break;
                }
                case Pickable.PickupType.AMMO:
                {
                    var ammoCrate = pickup as AmmoPickable;
                    Debug.Assert(ammoCrate != null, nameof(ammoCrate) + " != null");
                    valueScore = ScoreAmmoCrate(ammoCrate) * VALUE_MODIFIER;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return new Tuple<float, float>(valueScore, totalTime);
            //return valueScore + distanceScore + timeScore + neighborhoodScore;
        }

        private float ScoreAmmoCrate(AmmoPickable pickable)
        {
            const float AMMO_NECESSITY_WEIGHT = 0.8f;
            var guns = entity.GetGuns();
            var maxGunIndex = Mathf.Min(pickable.AmmoAmounts.Length, guns.Count);

            var totalCrateScore = 0f;
            var totalActiveGuns = 0;
            for (var i = 0; i < maxGunIndex; i++)
            {
                var currentGun = guns[i];
                if (!currentGun.isActiveAndEnabled) continue;
                totalActiveGuns++;
                var pickupAmmo = pickable.AmmoAmounts[i];
                var maxAmmo = currentGun.GetMaxAmmo();
                var currentAmmo = currentGun.GetCurrentAmmo();

                var recoveredAmmo = Mathf.Min(pickupAmmo, maxAmmo - currentAmmo);
                var ammoCrateValue = (1 - AMMO_NECESSITY_WEIGHT) * recoveredAmmo / maxAmmo;
                var ammoCrateWant = AMMO_NECESSITY_WEIGHT * (maxAmmo - currentAmmo) / maxAmmo;

                totalCrateScore += ammoCrateValue + ammoCrateWant;
            }

            return totalCrateScore / totalActiveGuns;
        }

        private float ScoreMedkit(MedkitPickable pickable)
        {
            const float MEDKIT_NECESSITY_WEIGHT = 0.8f;
            var pickupHealth = pickable.RestoredHealth;
            var maxHealth = entity.GetMaxHealth();
            var currentHealth = entity.Health;
            var recoveredHealth = Mathf.Min(pickupHealth, maxHealth - currentHealth);
            var medkitHealValue = (1 - MEDKIT_NECESSITY_WEIGHT) * recoveredHealth / maxHealth;
            var medkitWant = MEDKIT_NECESSITY_WEIGHT * (maxHealth - currentHealth) / maxHealth;
            return medkitHealValue + medkitWant;
        }

        private static float ScoreNeighborhood(Pickable pickup, Dictionary<Pickable, float> pickables)
        {
            const float MAX_NEIGHBORHOOD_SCORE = 1f;
            const float NEIGHBOR_SCORE = 0.2f;
            const float MAX_NEIGHBORHOOD_DISTANCE = 20f;
            // For the sake of speed, we do not check the actual lenght of the path, but the distance
            // as the crow flies
            var pickupPos = pickup.transform.position;
            const float maxSquaredDistance = MAX_NEIGHBORHOOD_DISTANCE * MAX_NEIGHBORHOOD_DISTANCE;
            var neighborsCount = 0;
            foreach (var entry in pickables.Keys)
            {
                if (entry == pickup) continue;
                var distanceSquared = (entry.transform.position - pickupPos).sqrMagnitude;
                if (distanceSquared <= maxSquaredDistance)
                    neighborsCount++;
            }

            return Mathf.Min(MAX_NEIGHBORHOOD_SCORE, neighborsCount * NEIGHBOR_SCORE);
        }


        private static float ScoreTime(float waitTime)
        {
            const float VARIANCE = 10;
            var rtn = NormalizedGaussian(waitTime, VARIANCE) - 1;
            // The more backward in time my knowledge is, the more likely it is to be wrong. Let's therefore
            // give a neutral score considering such uncertainty.
            if (waitTime < 0)
                rtn = (rtn + 1) / 2f;

            return rtn;
        }

        private static float ScoreDistance(float distance)
        {
            const float MAX_DISTANCE = 100f;
            // Score is 1 at distance 0 and -1 at distance MAX_DISTANCE

            const float a = 2f / (MAX_DISTANCE * MAX_DISTANCE);
            const float b = -2f * a * MAX_DISTANCE;
            const float c = 1f;

            if (distance > MAX_DISTANCE) return 0;
            return a * distance * distance + b * distance + c;
        }


        private static float NormalizedGaussian(float x, float sigmaSquared)
        {
            return Mathf.Exp(-x * x / (2f * sigmaSquared));
        }

        // TODO Tune parameters
        private const float VALUE_MODIFIER = 5f;
        private const float DISTANCE_MODIFIER = 1f;
        private const float TIME_MODIFIER = 3f;
        private const float NEIGHBORHOOD_MODIFIER = 1f;
    }
}