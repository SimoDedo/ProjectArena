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