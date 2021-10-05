using System;
using AI.Behaviours.NewBehaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI.Behaviours.NewBehaviours
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