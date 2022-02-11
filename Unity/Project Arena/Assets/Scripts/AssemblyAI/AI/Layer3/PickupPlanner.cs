using System;
using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using Others;
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
        private const float MAX_DISTANCE_TO_BE_NEIGHBORS = 20f;
        private const float BONUS_SCORE_PER_NEIGHBOR = 0.1f;

        private readonly Dictionary<Pickable, float> neighborsScore = new Dictionary<Pickable, float>();

        private static readonly AnimationCurve TimeToCollectValueCurve = new AnimationCurve(
            new Keyframe(0, 1.0f),
            new Keyframe(3f, 0.8f),
            new Keyframe(10f, 0.4f),
            new Keyframe(20f, 0.2f),
            new Keyframe(100f, 0.1666f)
        );


        private static readonly AnimationCurve TimeUncertaintyValueCurve = new AnimationCurve(
            new Keyframe(0, 1.0f),
            new Keyframe(3f, 0.8f),
            new Keyframe(10f, 0.4f),
            new Keyframe(20f, 0.2f),
            new Keyframe(100f, 0.1666f)
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

            // Calculate neighborhood score
            var pickups = knowledgeBase.GetPickups();

            foreach (var pickup1 in pickups)
            {
                var pickup1Position = pickup1.transform.position;
                var neighbors = 0;
                foreach (var pickup2 in pickups)
                {
                    if (pickup1 == pickup2)
                    {
                        continue;
                    }

                    var pickup2Position = pickup2.transform.position;
                    var path = navSystem.CalculatePath(pickup1Position, pickup2Position);

                    if (path.Length() < MAX_DISTANCE_TO_BE_NEIGHBORS)
                    {
                        Debug.DrawLine(pickup1Position, pickup2Position, Color.yellow, 2f, false);
                        neighbors++;
                    }
                }

                neighborsScore[pickup1] = Math.Min(0.5f, neighbors * BONUS_SCORE_PER_NEIGHBOR);
            }
        }

        public void Update()
        {
            if (Time.time < nextUpdateTime)
            {
                ScoreFastPickups();
                return;
            }

            nextUpdateTime = Time.time + UPDATE_COOLDOWN;
            ScorePickups();
        }

        private void ScoreFastPickups()
        {
            // var pickables = knowledgeBase.GetPickupsEstimatedActivationTimes();
            // var bestPickupScore = float.MinValue;
            // Pickable bestPickup = null;
            //
            // var myPosition = me.transform.position;
            // foreach (var entry in pickables)
            // {
            //     if ((myPosition - entry.Key.transform.position).sqrMagnitude >
            //         MAX_SQR_DISTANCE_TO_CALCULATE_PICKUP_SCORE_IMMEDIATELY)
            //     {
            //         continue;
            //     }
            //     
            //     
            // }
        }

        public void ForceUpdate()
        {
            nextUpdateTime = Time.time + UPDATE_COOLDOWN;
            ScorePickups();
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

        private void ScorePickups()
        {
            var pickables = knowledgeBase.GetPickupsEstimatedActivationTimes();
            var bestPickupScore = float.MinValue;
            Pickable bestPickup = null;

            foreach (var entry in pickables)
            {
                // TODO Find nice way to estimate score
                var score = ScorePickup(pickables, entry.Key);
                if (score == 0) continue;
                if (score > bestPickupScore)
                {
                    bestPickupScore = score;
                    bestPickup = entry.Key;
                }
            }

            if (bestPickup == null)
            {
                chosenPickup = null;
                chosenPickupScore = float.MinValue;
                chosenPickupEstimatedActivationTime = float.MaxValue;
                return;
            }

            chosenPickupEstimatedActivationTime = pickables[bestPickup];
            chosenPickup = bestPickup;
            chosenPickupScore = bestPickupScore;
        }

        private float ScorePickup(Dictionary<Pickable, float> pickables, Pickable pickup)
        {
            var activationTime = pickables[pickup];
            var pickupPosition = pickup.transform.position;

            var valueScore = ScorePickupByType(pickup);
            if (valueScore == 0f)
            {
                return valueScore;
            }

            var neighborhoodScore = neighborsScore[pickup];

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
                // Use all layers since the crates are in ignore layer
                if (sightSensor.CanSeeObject(pickup.transform, Physics.AllLayers))
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
                var ammoCrateWant = ammoNecessityWeight * Mathf.Pow(((float) maxAmmo - currentAmmo) / maxAmmo, 2f);

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


        private static float ScoreTimeToCollect(float timeToCollect)
        {
            return TimeToCollectValueCurve.Evaluate(timeToCollect);
        }

        private static float ScoreUncertaintyTime(float uncertaintyTime)
        {
            return TimeUncertaintyValueCurve.Evaluate(uncertaintyTime);
        }
    }
}