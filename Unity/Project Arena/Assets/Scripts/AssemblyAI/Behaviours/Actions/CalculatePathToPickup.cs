using System;
using AssemblyAI.AI.Layer2;
using AssemblyAI.AI.Layer3;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
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
            var (pickup, _, _) = pickupPlanner.GetBestPickupInfo();
            var position = pickup.transform.position;
            chosenPath.Value = navSystem.CalculatePath(position);
            return TaskStatus.Success;
        }
    }
}