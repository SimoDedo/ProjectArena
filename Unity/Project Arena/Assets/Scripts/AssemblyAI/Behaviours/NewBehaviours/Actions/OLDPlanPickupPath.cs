using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using AI.Behaviours.NewBehaviours.Variables;
using AI.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Utils;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Debug = System.Diagnostics.Debug;
using Vector3 = UnityEngine.Vector3;

// TODO REMOVE
namespace AssemblyAI.Behaviours.NewBehaviours.Actions
{
    [Serializable]
    public class OLDPlanPickupPath : Action
    {
        [SerializeField] private int maxItemsToConsider = 2;
        [SerializeField] private SharedSelectedPathInfo chosenPath;
        private AIEntity entity;
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;
        private float agentSpeed;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            knowledgeBase = GetComponent<PickupKnowledgeBase>();
            navSystem = GetComponent<NavigationSystem>();
            agentSpeed = navSystem.GetSpeed();
        }

        public override TaskStatus OnUpdate()
        {
            var pickups = knowledgeBase.GetPickupKnowledgeForType(Pickable.PickupType.MEDKIT);
            var availablePickups = new List<bool>(pickups.Select(it => true));

            // TODO Ammo retrieval from entity
            var (_, pickup) = Recurse(pickups, availablePickups, 1, entity.health, null, transform.position, Time.time);

            chosenPath.Value = navSystem.CalculatePath(pickup.transform.position);
            return TaskStatus.Success;
        }

        private Tuple<float, Pickable> Recurse(Dictionary<Pickable, float> pickups, IList<bool> availablePickups, int stepNumber,
            int currentHealth, int[] currentAmmo,
            Vector3 currentPosition, float currentTime)
        {
            var bestScore = float.MinValue;
            Pickable bestPickup = null;
            var currentIndex = -1;
            foreach (var entry in pickups)
            {
                currentIndex++;
                if (!availablePickups[currentIndex]) continue;
                var pickup = entry.Key;
                var pickupActivationTime = entry.Value;
                var type = pickup.GetPickupType();
                var currentScore = 0f;
                var newAmmo = currentAmmo;
                var newHealth = currentHealth;
                switch (type)
                {
                    case Pickable.PickupType.MEDKIT:
                    {
                        var medkit = pickup as MedkitPickable;
                        var totalHealth = entity.GetMaxHealth();
                        Debug.Assert(medkit != null, nameof(medkit) + " != null");
                        var healthRecoverable = Mathf.Min(totalHealth - currentHealth, medkit.RestoredHealth);
                        newHealth += healthRecoverable;
                        currentScore = (float) healthRecoverable / totalHealth * entity.GetHealthScore();
                        break;
                    }
                    case Pickable.PickupType.AMMO:
                    {
                        // TODO calculate score and ammo recovered by this pickup                   
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var (path, pathScore, departureTime) = ScorePath(currentPosition, currentTime, pickup, pickupActivationTime);
                currentScore *= pathScore;
                if (stepNumber < maxItemsToConsider && pickups.Count > 1)
                {
                    availablePickups[currentIndex] = false;
                    var rtn = Recurse(
                        pickups,
                        availablePickups,
                        stepNumber + 1,
                        newHealth,
                        newAmmo,
                        pickup.transform.position,
                        departureTime
                    );
                    currentScore += rtn.Item1;
                    availablePickups[currentIndex] = true;
                }

                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestPickup = pickup;
                }
            }

            return new Tuple<float, Pickable>(bestScore, bestPickup);
        }

        private Tuple<NavMeshPath, float, float> ScorePath(Vector3 currentPosition, float currentTime, Pickable pickup,
            float pickupActivationTime)
        {
            var path = navSystem.CalculatePath(currentPosition, pickup.transform.position);
            var pathLength = path.Length();
            var estimatedArrivalTime = currentTime + pathLength / agentSpeed;

            var waitTime = Math.Max(0, pickupActivationTime - estimatedArrivalTime);
            var distanceDuringWaitTime = waitTime * agentSpeed / PATIENCE;
            var departureTime = estimatedArrivalTime + waitTime;
            var pathScore = ScoreDistance(pathLength + distanceDuringWaitTime);
            return new Tuple<NavMeshPath, float, float>(path, pathScore, departureTime);
        }

        private float ScoreDistance(float distance)
        {
            // Score is SCORE_AT_ZERO at 0 distance and reaches 0 at point MAX_DISTANCE quadratically 

            const float a = SCORE_AT_ZERO / (MAX_DISTANCE * MAX_DISTANCE);
            const float b = -2 * a * MAX_DISTANCE;
            const float c = SCORE_AT_ZERO;

            if (distance > MAX_DISTANCE) return 0;
            return a * distance * distance + b * distance + c;
        }

        private const float SCORE_AT_ZERO = 2f;
        private const float MAX_DISTANCE = 100f;
        private const float PATIENCE = 1f;
    }
}