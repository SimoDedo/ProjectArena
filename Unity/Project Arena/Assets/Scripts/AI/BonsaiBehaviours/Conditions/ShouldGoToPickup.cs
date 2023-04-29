using System;
using AI.Layers.Planners;
using BehaviorDesigner.Runtime.Tasks;
using Bonsai;
using Bonsai.CustomNodes;
using UnityEngine;

namespace AI.BonsaiBehaviours.Conditions
{
    /// <summary>
    /// Returns Success if we should go to the pickup, Running otherwise
    /// </summary>
    [BonsaiNode("Conditional/")]
    public class ShouldGoToPickup : AutoConditionalAbort
    {
        private PickupPlanner pickupPlanner;

        public override void OnStart()
        {
            pickupPlanner = Actor.GetComponent<AIEntity>().PickupPlanner;
        }

        public override bool Condition()
        {
            return Time.time >= pickupPlanner.GetChosenPickupEstimatedActivationTime();
        }

        public override Status Run()
        {
            return Condition() ? Status.Success : Status.Running;
        }
    }
}