using System;
using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using UnityEngine;

namespace AssemblyAI.AI.Layer3
{
    public class PickupPlanner
    {
        private readonly AIEntity me;
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;
        private GunManager gunManager;
        private AISightSensor sightSensor;
        
        private Pickable chosenPickup;
        private float chosenPickupScore = float.MinValue;
        private float chosenPickupEstimatedActivationTime = float.MaxValue;

        private float nextUpdateTime;
        private const float UPDATE_COOLDOWN = 0.5f;
        private const float MAX_SQR_DISTANCE_TO_CALCULATE_PICKUP_SCORE_IMMEDIATELY = 4f;

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
            nextUpdateTime = float.MinValue;
        }

        public void Prepare()
        {
            navSystem = me.NavigationSystem;
            knowledgeBase = me.PickupKnowledgeBase;
            gunManager = me.GunManager;
            sightSensor = me.SightSensor;
        }

        public void Update()
        {
            if (Time.time < nextUpdateTime)
            {
                ScorePickups(true);
                return;
            }

            nextUpdateTime = Time.time + UPDATE_COOLDOWN;
            ScorePickups(false);
        }

        public void ForceUpdate()
        {
            nextUpdateTime = Time.time + UPDATE_COOLDOWN;
            ScorePickups(false);
        }

        public float GetChosenPickupScore()
        {
            return Math.Min(1.0f, chosenPickupScore);
        }

        public float GetChosenPickupEstimatedActivationTime()
        {
            return chosenPickupEstimatedActivationTime;
        }

        public Pickable GetChosenPickup()
        {
            return chosenPickup;
        }
        
        private void ScorePickups(bool considerOnlyClosePickups)
        {
            var pickables = knowledgeBase.GetPickupKnowledge();
            var bestPickupTimeToPick = float.MaxValue;
            var bestPickupScore = float.MinValue;
            Pickable bestPickup = null;

            var position = me.transform.position;

            foreach (var entry in pickables)
            {
                if (considerOnlyClosePickups && (position - entry.Key.transform.position).sqrMagnitude >
                    MAX_SQR_DISTANCE_TO_CALCULATE_PICKUP_SCORE_IMMEDIATELY)
                {
                    continue;
                }

                // TODO Find nice way to estimate score
                var (score, time) = ScorePickup(pickables, entry.Key);
                if (score == 0) continue;
                if (score > bestPickupScore ||
                    Mathf.Abs(score - bestPickupScore) < 0.05f && time < bestPickupTimeToPick)
                {
                    bestPickupScore = score;
                    bestPickupTimeToPick = time;
                    bestPickup = entry.Key;
                }
            }

            if (bestPickup == null)
            {
                return;
            }

            chosenPickupEstimatedActivationTime = pickables[bestPickup];
            chosenPickup = bestPickup;
            chosenPickupScore = bestPickupScore;
        }

        /**
         * Returns tuple of score and estimated time required to pickup
         */
        private Tuple<float, float> ScorePickup(Dictionary<Pickable, float> pickables, Pickable pickup)
        {
            var activationTime = pickables[pickup];
            var pickupPosition = pickup.transform.position;

            var valueScore = ScorePickupByType(pickup);
            if (valueScore == 0f)
            {
                // This pickup is not useful for us... 
                return new Tuple<float, float>(valueScore, float.MaxValue);
            }

            var neighborhoodScore = ScoreNeighborhood(pickup, pickables);

            var path = navSystem.CalculatePath(pickupPosition);
            var pathTime = navSystem.EstimatePathDuration(path);
            float timeUncertainty;
            var estimatedArrival = pathTime + Time.time;

            float totalTime;
            if (estimatedArrival < activationTime)
            {
                totalTime = activationTime - Time.time;
                timeUncertainty = 0;
            }
            else
            {
                totalTime = pathTime;
                if (sightSensor.CanSeeObject(pickup.transform, Physics.AllLayers)
                ) // Use all layers since the crates are in ignore layer
                {
                    // I can see the object for now, so there is no uncertainty, the object is there!
                    timeUncertainty = 0;
                }
                else
                {
                    // In this amount of time the pickup can be potentially be taken by someone before me
                    timeUncertainty = estimatedArrival - activationTime;
                }
            }

            var timeMultiplier = ScoreTimeToCollect(totalTime);
            var uncertaintyMultiplier = ScoreUncertaintyTime(timeUncertainty);

            var finalScore = (valueScore + neighborhoodScore) * timeMultiplier * uncertaintyMultiplier;
            return new Tuple<float, float>(finalScore, totalTime);
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
                if (distanceSquared <= maxSquaredDistance) neighborsCount++;
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