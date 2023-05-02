using System;
using AI.Behaviours.Variables;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Calculates and saves the path needed to reach the pickup chosen by the pickup planner.
    /// </summary>
    [Serializable]
    public class CalculatePathToPickup : Action
    {
        [SerializeField] private SharedSelectedPathInfo chosenPath;
        private NavigationSystem navSystem;
        private PickupPlanner pickupPlanner;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            pickupPlanner = entity.PickupPlanner;
        }

        public override TaskStatus OnUpdate()
        {
            var pickup = pickupPlanner.GetChosenPickup();
            var position = pickup.transform.position;
            chosenPath.Value = navSystem.CalculatePath(position);
            return TaskStatus.Success;
        }
    }
}