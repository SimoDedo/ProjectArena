using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.Core;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Calculates and saves the path needed to reach the pickup chosen by the pickup planner.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class CalculatePathToPickup : Task
    {
        public string chosenPathKey;
        private NavMeshPath ChosenPath
        {
            set => Blackboard.Set(chosenPathKey, value);
        }
        private NavigationSystem navSystem;
        private PickupPlanner pickupPlanner;

        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            pickupPlanner = entity.PickupPlanner;
        }

        public override Status Run()
        {
            var pickup = pickupPlanner.GetChosenPickup();
            var position = pickup.transform.position;
            ChosenPath = navSystem.CalculatePath(position);
            return Status.Success;
        }
    }
}