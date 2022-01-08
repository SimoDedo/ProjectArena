using System;
using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using UnityEngine;
using UnityEngine.AI;

namespace AssemblyAI.AI.Layer3
{
    public class PickupPlanner
    {
        private readonly AIEntity me;
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;
        private GunManager gunManager;
        private Pickable chosenPickup;

        private static readonly AnimationCurve TimeValueCurve = new AnimationCurve(
            new Keyframe(0, 2.5f),
            new Keyframe(3f, 2f),
            new Keyframe(10f, 1f),
            new Keyframe(20f, 0.5f),
            new Keyframe(100f, 0.3f)
        );
 

        public PickupPlanner(AIEntity entity)
        {
            me = entity;
        }

        public void Prepare()
        {
            navSystem = me.NavigationSystem;
            knowledgeBase = me.PickupKnowledgeBase;
            gunManager = me.GunManager;
        }

        public Pickable ScorePickups(out NavMeshPath path, out float bestPickupScore, out float activationTime)
        {
            var pickables = knowledgeBase.GetPickupKnowledge();
            var bestPickupValue = float.MinValue;
            var bestPickupTime = float.MaxValue;
            bestPickupScore = 0;
            path = null;
            Pickable bestPickup = null;
            
            foreach (var entry in pickables)
            {
                // TODO Find nice way to estimate score
                var value = ScorePickup(pickables, entry.Key, out var time, out var pickupPath);
                if (value == 0) continue;
                if (value > bestPickupValue || Mathf.Abs(value - bestPickupScore) < 0.05f && time < bestPickupTime)
                {
                    bestPickupScore = Mathf.Min(1f, value / time / 5);
                    bestPickupValue = value;
                    bestPickupTime = time;
                    bestPickup = entry.Key;
                    path = pickupPath;
                }
            }

            chosenPickup = bestPickup;
            activationTime = chosenPickup == null ? float.MinValue : pickables[chosenPickup];
            return bestPickup;
        }

        private float ScorePickup(Dictionary<Pickable, float> pickables, Pickable pickup, out float totalTime, out NavMeshPath path)
        {
            var activationTime = pickables[pickup];
            var pickupPosition = pickup.transform.position;

            var valueScore = ScorePickupByType(pickup);
            if (valueScore == 0f)
            {
                // This pickup is not useful for us... 
                path = new NavMeshPath();
                totalTime = float.MaxValue;
                return valueScore;
            }
            var neighborhoodScore = ScoreNeighborhood(pickup, pickables);

            path = navSystem.CalculatePath(pickupPosition);
            var pathTime = navSystem.EstimatePathDuration(path);
            float timeUncertainty;
            var estimatedArrival = pathTime + Time.time;

            if (estimatedArrival < activationTime)
            {
                totalTime = activationTime - Time.time;
                timeUncertainty = 0;
            }
            else
            {
                totalTime = pathTime;
                timeUncertainty = estimatedArrival - activationTime;
            }
            
            var timeMultiplier = ScoreTimeToCollect(totalTime);
            var uncertaintyMultiplier = ScoreUncertaintyTime(timeUncertainty);

            var finalScore = (valueScore + neighborhoodScore) * timeMultiplier * uncertaintyMultiplier;
            return finalScore;
        }

        private float ScorePickupByType(Pickable pickup)
        {
            float valueScore;
            switch (pickup.GetPickupType())
            {
                case Pickable.PickupType.MEDKIT:
                {
                    var medkit = pickup as MedkitPickable;
                    Debug.Assert(medkit != null, nameof(medkit) + " != null");
                    valueScore = ScoreMedkit(medkit);
                    break;
                }
                case Pickable.PickupType.AMMO:
                {
                    var ammoCrate = pickup as AmmoPickable;
                    Debug.Assert(ammoCrate != null, nameof(ammoCrate) + " != null");
                    valueScore = ScoreAmmoCrate(ammoCrate);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return valueScore;
        }

        private float ScoreAmmoCrate(AmmoPickable pickable)
        {
            const float ammoNecessityWeight = 0.8f;
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
                var ammoCrateValue = (1 - ammoNecessityWeight) * recoveredAmmo / maxAmmo;
                var ammoCrateWant = ammoNecessityWeight * (maxAmmo - currentAmmo) / maxAmmo;

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
            // TODO Maybe cache neighborhood info
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


        private static float ScoreTimeToCollect(float timeToCollect)
        {
            return TimeValueCurve.Evaluate(timeToCollect);
        }
        
        private static float ScoreUncertaintyTime(float uncertaintyTime)
        {
            return TimeValueCurve.Evaluate(uncertaintyTime);
        }
    }
}