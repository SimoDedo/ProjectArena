using System;
using AssemblyAI.AI.Layer3;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AssemblyAI.Behaviours.Conditions
{
    [Serializable]
    public class ShouldGoToPickup: Conditional
    {
        private PickupPlanner pickupPlanner;
        public override void OnAwake()
        {
            pickupPlanner = GetComponent<AIEntity>().PickupPlanner;
        }

        public override TaskStatus OnUpdate()
        {
            var (_, _, activationTime) = pickupPlanner.GetBestPickupInfo();
            return Time.time >= activationTime ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}