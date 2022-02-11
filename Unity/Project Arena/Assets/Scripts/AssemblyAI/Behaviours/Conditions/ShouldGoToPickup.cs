using System;
using AssemblyAI.AI.Layer3;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AssemblyAI.Behaviours.Conditions
{
    [Serializable]
    public class ShouldGoToPickup : Conditional
    {
        private PickupPlanner pickupPlanner;

        public override void OnAwake()
        {
            pickupPlanner = GetComponent<AIEntity>().PickupPlanner;
        }

        public override TaskStatus OnUpdate()
        {
            return Time.time >= pickupPlanner.GetChosenPickupEstimatedActivationTime()
                ? TaskStatus.Success
                : TaskStatus.Running;
        }
    }
}