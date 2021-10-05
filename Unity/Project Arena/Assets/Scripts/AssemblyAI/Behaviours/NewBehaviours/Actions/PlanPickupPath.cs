using System;
using System.Collections.Generic;
using Accord.Math.Differentiation;
using AI.Behaviours.NewBehaviours.Variables;
using AI.KnowledgeBase;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using Utils;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

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
        
        public override void OnEnd()
        {
            base.OnEnd();
        }

        public override void OnBehaviorComplete()
        {
            base.OnBehaviorComplete();
        }

        public override void OnBehaviorRestart()
        {
            base.OnBehaviorRestart();
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
            var healthPickables = knowledgeBase.GetPickupKnowledgeForType(Pickable.PickupType.MEDKIT);
            var bestScore = float.MinValue;
            Pickable bestPickup = null;
            NavMeshPath pathToBestPickup = null;
            var bestPickupActivationTime = 0f;
            foreach (var entry in healthPickables)
            {
                var pickup = entry.Key as MedkitPickable;
                if (pickup == null)
                {
                    Debug.LogWarning("Pickup reported as medkit but it is not of the correct class!");
                    continue;
                }

                var activationTime = entry.Value;
                var pickupPosition = pickup.transform.position;

                var path = navSystem.CalculatePath(pickupPosition);
                var pathLength = path.Length();
                var pathTime = pathLength / agentSpeed;

                var estimatedArrival = pathTime + Time.time;
                // CAN BE NEGATIVE, not necessarily a good thing
                var waitTime = activationTime - estimatedArrival;

                var valueScore = ScoreMedkit(entity.health, entity.GetMaxHealth(), pickup.RestoredHealth);
                var distanceScore = ScoreDistance(pathLength);
                var timeScore = ScoreTime(waitTime);
                var neighborhoodScore = ScoreNeighborhood(pickup, healthPickables);

                var pickupTotalScore =
                    valueScore * VALUE_MODIFIER +
                    distanceScore * DISTANCE_MODIFIER +
                    timeScore * TIME_MODIFIER +
                    neighborhoodScore * NEIGHBORHOOD_MODIFIER;

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

        private float ScoreMedkit(float currentHealth, float totalHealth, float pickupHealth)
        {
            const float MEDKIT_NECESSITY_WEIGHT = 0.8f;
            var recoveredHealth = Mathf.Min(pickupHealth, totalHealth - currentHealth);
            var medkitHealValue = (1 - MEDKIT_NECESSITY_WEIGHT) * recoveredHealth / totalHealth;
            var medkitWant = MEDKIT_NECESSITY_WEIGHT * (totalHealth - currentHealth) / totalHealth;
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