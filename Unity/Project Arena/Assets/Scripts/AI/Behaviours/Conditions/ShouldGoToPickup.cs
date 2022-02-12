using System;
using AI.AI.Layer3;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if we should go to the pickup, Running otherwise
    /// </summary>
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