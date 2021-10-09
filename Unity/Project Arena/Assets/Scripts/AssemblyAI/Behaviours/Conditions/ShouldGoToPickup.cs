using System;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AssemblyAI.Behaviours.Conditions
{
    [Serializable]
    public class ShouldGoToPickup: Conditional
    {
        [SerializeField] private SharedSelectedPickupInfo pickupInfo;

        public override TaskStatus OnUpdate()
        {
            return Time.time >= pickupInfo.Value.estimatedActivationTime ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}