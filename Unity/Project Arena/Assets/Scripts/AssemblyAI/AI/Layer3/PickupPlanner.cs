using System;
using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using UnityEngine;
using UnityEngine.AI;
using Utils;

namespace AssemblyAI.AI.Layer3
{
    public class PickupPlanner
    {
        private readonly AIEntity me;
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;
        private GunManager gunManager;
        private Pickable chosenPickup;
        private float agentSpeed;

        public PickupPlanner(AIEntity entity)
        {
            me = entity;
        }

        public void Prepare()
        {
            navSystem = me.NavigationSystem;
            knowledgeBase = me.PickupKnowledgeBase;
            gunManager = me.GunManager;
            agentSpeed = navSystem.Speed;
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
                    path = pickupPath;
                }
            }

            chosenPickup = bestPickup;
            activationTime = chosenPickup == null ? float.MinValue : pickables[chosenPickup];
            return bestPickup;
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
            var guns = gunManager.NumberOfGuns;
            var maxGunIndex = Mathf.Min(pickable.AmmoAmounts.Length, guns);

            var totalCrateScore = 0f;
            var totalActiveGuns = 0;
            for (var i = 0; i < maxGunIndex; i++)
            {
                if (!gunManager.IsGunActive(i)) continue;
                totalActiveGuns++;
                var pickupAmmo = pickable.AmmoAmounts[i];
                var maxAmmo = gunManager.GetMaxAmmoForGun(i);
                var currentAmmo = gunManager.GetCurrentAmmoForGun(i);

                var recoveredAmmo = Mathf.Min(pickupAmmo, maxAmmo - currentAmmo);
                var ammoCrateValue = (1 - AMMO_NECESSITY_WEIGHT) * recoveredAmmo / maxAmmo;
                var ammoCrateWant = AMMO_NECESSITY_WEIGHT * (maxAmmo - currentAmmo) / maxAmmo;

                totalCrateScore += ammoCrateValue + ammoCrateWant;
            }

            return totalCrateScore / totalActiveGuns;
        }

        private float ScoreMedkit(MedkitPickable pickable)
        {
            const float medkitNecessityWeight = 0.8f;
            var pickupHealth = pickable.RestoredHealth;
            var maxHealth = me.MaxHealth;
            var currentHealth = me.Health;
            var recoveredHealth = Mathf.Min(pickupHealth, maxHealth - currentHealth);
            var medkitHealValue = (1 - medkitNecessityWeight) * recoveredHealth / maxHealth;
            var medkitWant = medkitNecessityWeight * (maxHealth - currentHealth) / maxHealth;
            return medkitHealValue + medkitWant;
        }

        private static float ScoreNeighborhood(Pickable pickup, Dictionary<Pickable, float> pickables)
        {
            const float maxNeighborhoodScore = 1f;
            const float neighborScore = 0.2f;
            const float maxNeighborhoodDistance = 20f;
            // For the sake of speed, we do not check the actual lenght of the path, but the distance
            // as the crow flies
            var pickupPos = pickup.transform.position;
            const float maxSquaredDistance = maxNeighborhoodDistance * maxNeighborhoodDistance;
            var neighborsCount = 0;
            foreach (var entry in pickables.Keys)
            {
                if (entry == pickup) continue;
                var distanceSquared = (entry.transform.position - pickupPos).sqrMagnitude;
                if (distanceSquared <= maxSquaredDistance)
                    neighborsCount++;
            }

            return Mathf.Min(maxNeighborhoodScore, neighborsCount * neighborScore);
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