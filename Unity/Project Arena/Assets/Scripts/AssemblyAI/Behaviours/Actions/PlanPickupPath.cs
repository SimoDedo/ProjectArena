using System;
using System.Collections.Generic;
using AI.KnowledgeBase;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Utils;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Debug = System.Diagnostics.Debug;

namespace AssemblyAI.Behaviours.NewBehaviours.Actions
{
    [Serializable]
    public class PlanPickupPath : Action
    {
        [SerializeField] private SharedSelectedPathInfo chosenPath;
        [SerializeField] private SharedSelectedPickupInfo chosenPickup;
        [SerializeField] private SharedVector3 chosenPickupPosition;
        private AIEntity entity;
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;
        private float agentSpeed;
        private float lastKBUpdateTime;
        private bool isFirstTime;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            knowledgeBase = GetComponent<PickupKnowledgeBase>();
            navSystem = GetComponent<NavigationSystem>();
            agentSpeed = navSystem.GetSpeed();
            isFirstTime = true;
        }

        public override TaskStatus OnUpdate()
        {
            var kbUpdateTime = knowledgeBase.GetLastUpdateTime();
            if (!isFirstTime && lastKBUpdateTime == kbUpdateTime)
                return TaskStatus.Running;

            lastKBUpdateTime = kbUpdateTime;

            // Plan: find pickable which has the best score, calculated from:
            // - Value that this item gives me;
            // - Distance required to reach it;
            // - Time required to reach it;
            // - Amount of pickups nearby the chosen one.
            var pickables = knowledgeBase.GetPickupKnowledge();
            var bestScore = float.MinValue;
            Pickable bestPickup = null;
            NavMeshPath pathToBestPickup = null;
            var bestPickupActivationTime = 0f;
            foreach (var entry in pickables)
            {
                var pickup = entry.Key;
                var activationTime = entry.Value;
                var pickupPosition = pickup.transform.position;

                var path = navSystem.CalculatePath(pickupPosition);
                var pathLength = path.Length();
                var pathTime = pathLength / agentSpeed;

                var estimatedArrival = pathTime + Time.time;
                // CAN BE NEGATIVE, not necessarily a good thing
                var waitTime = activationTime - estimatedArrival;

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

                var pickupTotalScore = valueScore + distanceScore + timeScore + neighborhoodScore;

                if (pickupTotalScore > bestScore)
                {
                    bestScore = pickupTotalScore;
                    bestPickup = pickup;
                    pathToBestPickup = path;
                    bestPickupActivationTime = activationTime;
                }
            }

            if (!isFirstTime && chosenPickup.Value.pickup != bestPickup)
            {
                return TaskStatus.Failure;
            }

            // TODO What if failure?
            isFirstTime = false;
            chosenPath.Value = pathToBestPickup;
            chosenPickup.Value = new SelectedPickupInfo
            {
                pickup = bestPickup,
                estimatedActivationTime = bestPickupActivationTime
            };
            chosenPickupPosition.Value = bestPickup.transform.position;
            return TaskStatus.Running;
        }

        private float ScoreAmmoCrate(AmmoPickable pickable)
        {
            const float AMMO_NECESSITY_WEIGHT = 0.8f;
            var guns = entity.GetGuns();
            var maxGunIndex = Mathf.Min(pickable.AmmoAmounts.Length, guns.Count);

            var totalCrateScore = 0f;

            for (var i = 0; i < maxGunIndex; i++)
            {
                var currentGun = guns[i];
                var pickupAmmo = pickable.AmmoAmounts[i];
                var maxAmmo = currentGun.GetMaxAmmo();
                var currentAmmo = currentGun.GetCurrentAmmo();

                var recoveredAmmo = Mathf.Min(pickupAmmo, maxAmmo - currentAmmo);
                var ammoCrateValue = (1 - AMMO_NECESSITY_WEIGHT) * recoveredAmmo / maxAmmo;
                var ammoCrateWant = AMMO_NECESSITY_WEIGHT * (maxAmmo - currentAmmo) / maxAmmo;

                totalCrateScore += ammoCrateValue + ammoCrateWant;
            }

            return totalCrateScore / maxGunIndex;
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

        private float ScoreNeighborhood(Pickable pickup, Dictionary<Pickable, float> healthPickables)
        {
            const float MAX_NEIGHBORHOOD_SCORE = 1f;
            const float NEIGHBOR_SCORE = 0.2f;
            const float MAX_NEIGHBORHOOD_DISTANCE = 20f;
            // For the sake of speed, we do not check the actual lenght of the path, but the distance
            // as the crow flies
            var pickupPos = pickup.transform.position;
            const float maxSquaredDistance = MAX_NEIGHBORHOOD_DISTANCE * MAX_NEIGHBORHOOD_DISTANCE;
            var neighborsCount = 0;
            foreach (var entry in healthPickables)
            {
                if (entry.Key == pickup) continue;
                var distanceSquared = (entry.Key.transform.position - pickupPos).sqrMagnitude;
                if (distanceSquared <= maxSquaredDistance)
                    neighborsCount++;
            }

            return Mathf.Min(MAX_NEIGHBORHOOD_SCORE, neighborsCount * NEIGHBOR_SCORE);
        }


        private float ScoreTime(float waitTime)
        {
            const float VARIANCE = 10;
            var rtn = NormalizedGaussian(waitTime, VARIANCE) - 1;
            // The more backward in time my knowledge is, the more likely it is to be wrong. Let's therefore
            // give a neutral score considering such uncertainty.
            if (waitTime < 0)
                rtn = (rtn + 1) / 2f;

            return rtn;
        }

        private float ScoreDistance(float distance)
        {
            const float MAX_DISTANCE = 100f;
            // Score is 1 at distance 0 and -1 at distance MAX_DISTANCE

            const float a = 2f / (MAX_DISTANCE * MAX_DISTANCE);
            const float b = -2f * a * MAX_DISTANCE;
            const float c = 1f;

            if (distance > MAX_DISTANCE) return 0;
            return a * distance * distance + b * distance + c;
        }


        private float NormalizedGaussian(float x, float sigmaSquared)
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