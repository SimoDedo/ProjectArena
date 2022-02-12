using System;
using AI.AI.Layer2;
using AI.AI.Layer3;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class CalculatePathToPickup : Action
    {
        private PickupPlanner pickupPlanner;
        private NavigationSystem navSystem;
        [SerializeField] private SharedSelectedPathInfo chosenPath;

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