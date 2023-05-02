using System;
using System.Collections.Generic;
using AI.Layers.SensingLayer;
using Entity.Component;
using Others;
using Pickables;
using UnityEngine;
using Utils;

namespace AI.Layers.KnowledgeBase
{
    /// <summary>
    /// This components is able to select which pickup, if any, should the entity try to collect.
    ///
    /// Each pickup known is scored according to:
    /// - Pickup value: The value it provides to the entity (e.g. a medkit health restored)
    /// - Want value: The necessity of the pickup (e.g. we want a medkit more the lower our health)
    /// - Neighborhood value: How many nearby pickups there are (we can possibly gain much more ammo / health by
    ///    selecting pickups close to other pickups
    /// - Time to collect value: The time it takes to collect it (fast pickup times are preferred)
    /// - Uncertainty value: How uncertain we are of the pickup actual status (if we have no info of the pickup status
    ///     since a while, we might be unable to pick it up even if we think it is active) 
    /// </summary>
    public class PickupPlanner
    {
        // How often the plan should be recalculated, in seconds.
        private const float UPDATE_COOLDOWN = 0.5f;

        // Two pickups are considered neighbors if they are closer than this distance. 
        private const float MAX_DISTANCE_TO_BE_NEIGHBORS = 10f;

        // Bonus score given to a pickup for each neighbor it has.
        private const float BONUS_SCORE_PER_NEIGHBOR = 0.04f;

        // Curve used to calculate the multiplier due to the time to collect.
        private static readonly AnimationCurve TimeToCollectValueCurve = new AnimationCurve(
            new Keyframe(0, 5.0f),
            new Keyframe(2f, 2.0f),
            new Keyframe(10f, 0.2f),
            new Keyframe(20f, 0.1f),
            new Keyframe(100f, 0.005f)
        );

        // Curve used to calculate the multiplier due to the uncertainty time.
        private static readonly AnimationCurve TimeUncertaintyValueCurve = new AnimationCurve(
            new Keyframe(0, 1.0f),
            new Keyframe(3f, 0.8f),
            new Keyframe(10f, 0.4f),
            new Keyframe(20f, 0.2f),
            new Keyframe(100f, 0.1666f)
        );

        private readonly AIEntity me;

        private readonly Dictionary<Pickable, float> neighborsScore =
            new Dictionary<Pickable, float>(new MonoBehaviourEqualityComparer<Pickable>());

        private Pickable chosenPickup;
        private float chosenPickupEstimatedActivationTime = float.MaxValue;
        private float chosenPickupScore = float.MinValue;
        private GunManager gunManager;
        private PickupActivationEstimator pickupKnowledgeBase;
        private NavigationSystem navSystem;

        private float nextUpdateTime;
        private SightSensor sightSensor;
        private readonly int layerMask;

        public PickupPlanner(AIEntity entity)
        {
            me = entity;
            nextUpdateTime = float.MinValue;
            layerMask = LayerMask.GetMask("Default", "Entity", "Pickable");
        }

        // Finish preparing this component
        public void Prepare()
        {
            navSystem = me.NavigationSystem;
            pickupKnowledgeBase = me.PickupKnowledgeBase;
            gunManager = me.GunManager;
            sightSensor = me.SightSensor;

            CalculatePickupNeighborhoodScore();
        }

        /// <summary>
        /// Recalculates the best pickup every once in a while.
        /// </summary>
        public void Update()
        {
            if (Time.time < nextUpdateTime)
                // TODO maybe find a way to give a score to pickups which are active and very close to me.
                //   They should not require any heavy calculation and can be picked up very fast.
                return;

            nextUpdateTime = Time.time + UPDATE_COOLDOWN;
            ScorePickups();
        }

        /// <summary>
        /// Immediately forces update of the planner.
        /// </summary>
        public void ForceUpdate()
        {
            nextUpdateTime = Time.time + UPDATE_COOLDOWN;
            ScorePickups();
        }

        /// <summary>
        /// Returns the score of the pickup chosen.
        /// </summary>
        public float GetChosenPickupScore()
        {
            return Math.Min(1.0f, chosenPickupScore);
        }

        /// <summary>
        /// Returns the estimated activation time of the pickup chosen.
        /// </summary>
        public float GetChosenPickupEstimatedActivationTime()
        {
            return chosenPickupEstimatedActivationTime;
        }

        /// <summary>
        /// Returns the chosen pickup.
        /// </summary>
        public Pickable GetChosenPickup()
        {
            return chosenPickup;
        }

        // Score each pickup we know and update the best pickup info.
        private void ScorePickups()
        {
            var pickables = pickupKnowledgeBase.GetPickupsEstimatedActivationTimes();
            var bestPickupScore = float.MinValue;
            Pickable bestPickup = null;

            foreach (var entry in pickables)
            {
                var score = ScorePickup(entry.Key, entry.Value);
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

        // Score a specific pickup.
        private float ScorePickup(Pickable pickup, float activationTime)
        {
            var pickupPosition = pickup.transform.position;

            var valueScore = ScorePickupByType(pickup);
            if (valueScore == 0f) return valueScore;

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
                if (sightSensor.CanSeeObject(pickup.transform, layerMask))
                    // I can see the object for now, so there is no uncertainty, the object is there!
                    timeUncertainty = 0;
                else
                    // In this amount of time the pickup can be potentially be taken by someone before me
                    timeUncertainty = Mathf.Min(pickup.Cooldown, estimatedArrival - activationTime);
            }

            var timeMultiplier = GetTimeToCollectMultiplier(totalTime);
            var uncertaintyMultiplier = GetUncertaintyTimeMultiplier(timeUncertainty);

            var finalScore = valueScore * neighborhoodScore * timeMultiplier * uncertaintyMultiplier;

            return finalScore;
        }

        // Gets pickup value and want value of the given pickup.
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

        // Gets pickup and want value of an ammo crate.
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

        // Get pickup and want value of a medkit.
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

        // Get score of the pickup neighborhood.
        private void CalculatePickupNeighborhoodScore()
        {
            var pickups = pickupKnowledgeBase.GetPickups();

            foreach (var pickup1 in pickups)
            {
                var pickup1Position = pickup1.transform.position;
                var neighbors = 0;
                foreach (var pickup2 in pickups)
                {
                    if (pickup1 == pickup2) continue;

                    var pickup2Position = pickup2.transform.position;
                    var path = navSystem.CalculatePath(pickup1Position, pickup2Position);

                    if (path.Length() < MAX_DISTANCE_TO_BE_NEIGHBORS)
                    {
                        Debug.DrawLine(pickup1Position, pickup2Position, Color.yellow, 2f, false);
                        neighbors++;
                    }
                }

                neighborsScore[pickup1] = 1.0f + Math.Min(0.2f, neighbors * BONUS_SCORE_PER_NEIGHBOR);
            }
        }

        // Get multiplier for the time to collect.
        private static float GetTimeToCollectMultiplier(float timeToCollect)
        {
            return TimeToCollectValueCurve.Evaluate(timeToCollect);
        }

        // Get multiplier for the uncertainty time.
        private static float GetUncertaintyTimeMultiplier(float uncertaintyTime)
        {
            return TimeUncertaintyValueCurve.Evaluate(uncertaintyTime);
        }
    }
}